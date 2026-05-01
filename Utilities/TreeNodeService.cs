using System.Diagnostics;
using DocumentFormat.OpenXml.Wordprocessing;
using Toltech.App.Models;
using Toltech.App.Services;
using static Toltech.App.Models.NodesDefinition;
using System.Collections.ObjectModel;
using System.IO;

namespace Toltech.App.Utilities
{
    /// <summary>
    /// Service central pour la gestion des NodesDefinition du TreeView.
    /// Orchestration de la sync, CRUD, DisplayOrder et notification des changements.
    /// Instancié une seule fois dans MainVM et injecté dans les VMs enfants.
    /// </summary>
    public class TreeNodeService
    {
        private readonly DatabaseService _databaseService;
        private readonly DomainService _domainService;
        private readonly NodeSyncService _nodeSyncService;

        // ── État partagé ──────────────────────────────────────────────
        // Le node sélectionné est ici, plus dans TreeViewViewModel,
        // pour que RequirementsVM puisse y accéder sans référencer le TreeVM.
        private NodesDefinition? _selectedNode;
        public NodesDefinition? SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (_selectedNode == value) return;
                _selectedNode = value;
                SelectedNodeChanged?.Invoke(_selectedNode);
            }

        }

        // ── Événements ───────────────────────────────────────────────
        /// Déclenché après tout Add / Update / Delete sur un node
        public event Action<NodeChangeType, NodesDefinition>? NodeChanged;

        /// Déclenché quand la sélection change dans le TreeView
        public event Action<NodesDefinition?>? SelectedNodeChanged;

        /// Déclenché après une sync complète (SafeSyncAsync)
        public event Action? SyncCompleted;

        public TreeNodeService(DatabaseService databaseService, DomainService domainService)
        {
            _databaseService = databaseService;
            _domainService = domainService;

            _nodeSyncService = new NodeSyncService(_databaseService);

            EventsManager.TreeViewUpdated += async () =>
            {
                Debug.WriteLine("[TreeNodeService] - TreeViewUpdated event received, refreshing nodes...");
                await _nodeSyncService.SafeSyncAsync();
            };
        }

        public async Task InsertAsync(NodesDefinition node)
        {
            await _databaseService.InsertAsync(node);
        }
        public async Task UpdateAsync(NodesDefinition node)
        {
            await _databaseService.UpdateAsync(node);
        }
        public async Task DeleteAsync(NodesDefinition node)
        {
            await _databaseService.DeleteAsync(node);
        }

        public async Task UpdateRangeAsync(IEnumerable<NodesDefinition> nodesToUpdate)
        {
            await _databaseService.UpdateRangeAsync(nodesToUpdate);
        }

        public async Task UpdatePartNameAsync(int partId, string newName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(newName);
            if (partId <= 0)
                throw new ArgumentException("Identifiant de part invalide.", nameof(partId));

            // Récupération de la part
            var part = await _databaseService.GetPartByIdAsync(partId);

            // Mise à jour du nom
            part.NamePart = newName;

            await _databaseService.UpdateAsync(part);

            Debug.WriteLine("[DatabaseService] - NotifyModelDataChanged()");
            await EventsManager.RaiseModelDataAddOrDeletedAsync();
            Debug.WriteLine("[DatabaseService] - NotifyPartAddDeleted()");
            await EventsManager.RaisePartAddOrDeletedAsync();

            //RequestSyncAsync(); TODO: à voir si on doit faire une sync complète ou juste une notification de changement
        }

        public async Task UpdateRequirementNameAsync(int? idReq, string newName)
        {
            var requirement = await _databaseService.GetReqsByIdAsync(idReq);

            if (requirement == null)
                throw new InvalidOperationException($"Aucune exigence trouvée avec Id = {idReq}");

            // Met à jour le nom
            requirement.NameReq = newName;

            // Sauvegarde dans la DB
            // TODO a revoir refacto db tree => 26/04/2026
            await _databaseService.UpdateAsync(requirement);

            //RequestSyncAsync(); TODO: à voir si on doit faire une sync complète ou juste une notification de changement

            Debug.WriteLine("[DatabaseService] - NotifyRequirementChanged()");
            await EventsManager.RaiseRequirementAddOrDeletedAsync();
        }

        public Task<List<NodesDefinition>> GetChildrenAsync(int? parentId)
        {
            return _databaseService.GetChildrenAsync(parentId);
        }

        public async Task<List<NodesDefinition>> GetAllNodesAsync()
        {
            return await _databaseService.GetAllNodesAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="movedNodes"></param>
        /// <param name="parentFolderId"></param>
        /// <param name="insertIndex"></param>
        /// <returns></returns>
        public async Task UpdateDisplayOrderForMoveAsync(
           IReadOnlyList<NodesDefinition> movedNodes,
           int? parentFolderId,
           int insertIndex)
        {

            if (movedNodes == null || movedNodes.Count == 0)
                return;

            // Chargement initial des siblings
            var siblings = (await _databaseService.GetChildrenAsync(parentFolderId))
                .OrderBy(s => s.DisplayOrder)
                .ToList();

            foreach (var node in movedNodes)
            {
                node.ParentId = parentFolderId;
                node.DisplayOrder = insertIndex;

                // Décalage incrémental existant
                foreach (var s in siblings
                    .Where(s => s.DisplayOrder <= insertIndex && !movedNodes.Contains(s)))
                {
                    s.DisplayOrder -= 1;
                    await _databaseService.UpdateAsync(s);
                }

                await _databaseService.UpdateAsync(node);
                insertIndex++;
            }

            // --- Normalisation finale ---
            await _databaseService.NormalizeDisplayOrderAsync(parentFolderId);

            Debug.WriteLine("[DatabaseService] - NotifyNodeUpdated()");
            await EventsManager.RaiseNodesUpdatedAsync();
        }

        public async Task DeleteNodeAsync(NodesDefinition node)
        {
            if (node == null)
                return;

            switch (node.Type)
            {
                case NodeType.PartNode:
                    await _domainService.DeletePartWithDatasByIdAsync(node.LinkedOriginalId);
                    break;

                case NodeType.RequirementNode:
                    await _domainService.DeleteRequirementByIdAsync(node.LinkedRequirementId.Value);
                    break;

                case NodeType.DataNode:
                    await _domainService.DeleteDatasByIdsAsync(node.LinkedOriginalId);
                    break;

                case NodeType.Folder:
                case NodeType.PositionnementFolder:
                case NodeType.ModelFolder:
                    await _databaseService.DeleteAsync(node);
                    break;
            }

            await _nodeSyncService.SafeSyncAsync();
        }

        public async Task DeleteFolderAndPromoteChildrenAsync(NodesDefinition folder)
        {
            if (folder == null)
                return;

            int? parentId = folder.ParentId;

            // 1. Charger les enfants depuis la DB (source fiable)
            var children = await _databaseService.GetChildrenAsync(folder.Id);

            // 2. Récupérer les siblings de destination
            var targetSiblings = await _databaseService.GetChildrenAsync(parentId);

            int insertIndex = targetSiblings.Count;

            // 3. Repositionner les enfants
            foreach (var child in children)
            {
                child.ParentId = parentId;
                child.DisplayOrder = insertIndex++;
            }

            // 4. Transaction (IMPORTANT)
            await _databaseService.RunInTransactionAsync(async () =>
            {
                await _databaseService.UpdateRangeAsync(children);
                await _databaseService.DeleteAsync(folder);
            });

            // 5. Normalisation (optionnel mais recommandé)
            await _databaseService.NormalizeDisplayOrderAsync(parentId);
        }


        public async Task RenameNodeAsync(NodesDefinition node, string newName)
        {
            if (node == null || string.IsNullOrWhiteSpace(newName))
                return;

            try
            {
                switch (node.Type)
                {
                    // --- Folder (Tree only) ---
                    case NodeType.PositionnementFolder:
                    case NodeType.ModelFolder:
                        node.NodeName = newName;
                        await _databaseService.UpdateAsync(node);
                        break;

                    // --- Part (métier) ---
                    case NodeType.PartNode:
                        await _domainService.UpdatePartNameAsync(node.LinkedOriginalId, newName);
                        node.NodeName = newName; // sync UI
                        break;

                    // --- Requirement (métier) ---
                    case NodeType.RequirementNode:
                        await _domainService.UpdateRequirementNameAsync(node.LinkedRequirementId, newName);
                        node.NodeName = newName;
                        break;

                    // --- Data (métier) ---
                    case NodeType.DataNode:
                        await _domainService.UpdateModelDataNameAsync(node.LinkedOriginalId, newName);
                        node.NodeName = newName;
                        break;

                    default:
                        throw new NotSupportedException($"Type non supporté : {node.Type}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TreeNodeService] RenameNodeAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task ToggleNodeActiveAsync(NodesDefinition node)
        {
            if (node == null)
                return;

            try
            {
                switch (node.Type)
                {
                    case NodeType.PartNode:
                        await _domainService.ToggleActivePartByIdAsync(node.LinkedOriginalId);
                        break;

                    case NodeType.RequirementNode:
                        await _domainService.ToggleActiveRequirementByIdAsync(node.LinkedRequirementId);
                        break;

                    case NodeType.DataNode:
                        await _domainService.ToggleActiveModelDataByIdAsync(node.LinkedOriginalId);
                        break;

                    case NodeType.PositionnementFolder:
                    case NodeType.ModelFolder:
                        // aucun comportement métier
                        return;

                    default:
                        return;
                }

                // optionnel : refresh tree si nécessaire
                await _nodeSyncService.SafeSyncAsync();
            }
            catch (Exception ex)
            {
                //_logger.LogError("ToggleNodeActiveAsync failed", "", ex);
                throw; // ou swallow selon votre politique globale
            }
        }

        /// <summary>
        /// Wrapper DB pour normaliser le DisplayOrder d'un ensemble de nodes partageant le même parent.
        /// </summary>
        /// <param name="parentId">ID du parent dont les enfants doivent être normalisés</param>
        /// <returns></returns>
        public async Task NormalizeDisplayOrderAsync(int parentId)
        {
            await _databaseService.NormalizeDisplayOrderAsync(parentId);
        }

        public async Task MoveNodesAsync(
            List<NodesDefinition>? nodes,
            NodesDefinition? dropTarget,
            bool insertAbove)
        {
            if (nodes == null || nodes.Count == 0 || dropTarget == null)
                return;

            foreach (var node in nodes)
            {
                if (!await CanMoveNode(node, dropTarget))
                    return;
            }

            var (parentFolderId, insertIndex) = await ResolveDropContextAsync(dropTarget, insertAbove, nodes);

            await UpdateDisplayOrderForMoveAsync(
                nodes,
                parentFolderId,
                insertIndex);

            await HandlePostMoveEventsAsync(dropTarget);
        }

        private async Task<(int? parentFolderId, int insertIndex)> ResolveDropContextAsync(
                        NodesDefinition dropTarget,
                        bool insertAbove,
                        List<NodesDefinition> nodes)
        {
            NodesDefinition parentFolder;
            int insertIndex;

            if (dropTarget.IsFolder)
            {
                parentFolder = dropTarget;

                insertIndex = dropTarget.Children?.Count > 0
                    ? dropTarget.Children.Max(c => c.DisplayOrder) + 1
                    : 0;
            }
            else
            {
                parentFolder = await FindParentFolderAsync(dropTarget);

                insertIndex = dropTarget.DisplayOrder;

                if (!insertAbove)
                    insertIndex -= 1;
            }

            int? parentFolderId = parentFolder?.Id;

            // cas spécial DataNode
            if (dropTarget.Type == NodeType.DataNode)
                parentFolderId = dropTarget.ParentId;

            return (parentFolderId, insertIndex);
        }
        private async Task HandlePostMoveEventsAsync(NodesDefinition dropTarget)
        {
            if (dropTarget.Type == NodeType.RequirementNode)
            {
                var visibleRequirementIds = await ListIDReqOfSelectFolderAsync();
                var nameParentFolder = await NameParentFolderAsync();

                await EventsManager.RaiseNodReqSelectChangedAsync(
                    visibleRequirementIds,
                    nameParentFolder);
            }
            else if (dropTarget.Type == NodeType.DataNode)
            {
                EventsManager.RaiseNodesDataDragAsync();
            }
        }

        #region Helper

        /// <summary>
        /// Retourne la liste des IDs des Requirements contenus dans le dossier parent du nœud sélectionné.
        /// </summary>
        public async Task<IReadOnlyCollection<int>> ListIDReqOfSelectFolderAsync()
        {
            if (SelectedNode == null)
                return Array.Empty<int>();

            var parentFolder = await FindParentFolderAsync(SelectedNode);
            if (parentFolder == null)
                return Array.Empty<int>();

            var allNodes = await _databaseService.GetChildrenAsync(parentFolder.Id);

            // Filtrer uniquement les nœuds "RequirementNode" (non-folder)
            var requirementIds = allNodes
                .Where(n => !n.IsFolder
                            && n.Type == NodeType.RequirementNode
                            && n.LinkedRequirementId != null)
                .OrderByDescending(n => n.DisplayOrder)
                .Select(n => n.LinkedRequirementId.Value)
                .ToList();

            return requirementIds;
        }

        /// <summary>
        /// Trouve le parent folder d'un nœud donné en remontant l'arborescence
        /// </summary>
        public async Task<string> NameParentFolderAsync()
        {
            if (SelectedNode == null)
                return "";

            var parentFolder = await FindParentFolderAsync(SelectedNode);
            if (parentFolder == null)
                return "";

            string nameParentFolder = parentFolder.NodeName;

            return nameParentFolder;
        }


        /// <summary>
        /// Trouve le parent folder d'un nœud donné en remontant l'arborescence
        /// </summary>
        private async Task<NodesDefinition> FindParentFolderAsync(NodesDefinition node)
        {
            if (node.ParentId == null)
                return null;

            var allNodes = await GetAllNodesAsync();
            var parent = allNodes.FirstOrDefault(n => n.Id == node.ParentId);

            while (parent != null && !parent.IsFolder)
            {
                parent = allNodes.FirstOrDefault(n => n.Id == parent.ParentId);
            }

            return parent;
        }


        private async Task<bool> CanMoveNode(NodesDefinition node, NodesDefinition dropTarget)
        {
            if (node == null || dropTarget == null)
                return false;

            // 1️⃣ Interdiction de se déplacer soi-même
            if (node.Id == dropTarget.Id)
                return false;

            // 2️⃣ Interdiction de se déplacer dans sa propre hiérarchie
            if (IsDescendantOf(dropTarget, node))
                return false;

            // 3️⃣ Vérifier le type du folder parent
            NodesDefinition targetFolder = dropTarget.IsFolder
                ? dropTarget
                : await FindParentFolderFromDbAsync(dropTarget);

            if (targetFolder != null)
            {
                if (node.Type == NodeType.ExigencesFolder && targetFolder.Type == NodeType.PositionnementFolder ||
                    node.Type == NodeType.PositionnementFolder && targetFolder.Type == NodeType.ExigencesFolder)
                {
                    return false;
                }
            }

            // 4️⃣ Interdiction de drag les DataNode hors de leur part 
            if (node.Type == NodeType.DataNode)
            {
                if (dropTarget.ParentId != node.ParentId)
                {
                    return false;
                }

            }

            return true;
        }

        private bool IsDescendantOf(NodesDefinition possibleChild, NodesDefinition possibleParent)
        {
            if (possibleChild == null || possibleParent == null)
                return false;

            foreach (var child in possibleParent.Children)
            {
                if (child.Id == possibleChild.Id)
                    return true;

                if (IsDescendantOf(possibleChild, child))
                    return true;
            }
            return false;
        }

        private async Task<NodesDefinition?> FindParentFolderFromDbAsync(NodesDefinition node)
        {
            var current = node;

            while (current.ParentId.HasValue)
            {
                var parent = await _databaseService.GetNodeByIdAsync(current.ParentId.Value);

                if (parent == null)
                    break;

                if (parent.IsFolder)
                    return parent;

                current = parent;
            }

            return null;
        }

        private NodesDefinition FindParentRecursive(IEnumerable<NodesDefinition> nodes, NodesDefinition target)
        {
            foreach (var n in nodes)
            {
                if (n.Children.Contains(target))
                    return n;

                var result = FindParentRecursive(n.Children, target);
                if (result != null)
                    return result;
            }
            return null;
        }

        #endregion


        #region Cohérence TreeView

        public async Task RepairTreeIfNeededAsync()
        {
            var nodes = await _databaseService.GetAllNodesAsync();

            if (nodes == null || nodes.Count == 0)
                return;

            bool hasIssues = HasStructuralIssues(nodes);

            if (!hasIssues)
                return;

            // 🔧 réparation lourde uniquement si nécessaire
            await _nodeSyncService.SafeSyncAsync();

            // 🔧 nettoyage ordre
            var folders = nodes.Where(n => n.IsFolder).ToList();

            foreach (var folder in folders)
            {
                await _databaseService.NormalizeDisplayOrderAsync(folder.Id);
            }
        }

        private static bool HasStructuralIssues(List<NodesDefinition> nodes)
        {
            var nodesById = nodes.ToDictionary(n => n.Id);

            foreach (var node in nodes)
            {
                // 1. Parent inexistant
                if (node.ParentId.HasValue && !nodesById.ContainsKey(node.ParentId.Value))
                    return true;

                // 2. Cycle (node parent de lui-même indirectement)
                if (IsCyclic(node, nodesById))
                    return true;
            }

            // 3. Dossiers obligatoires manquants
            if (!nodes.Any(n => n.Type == NodeType.ModelFolder))
                return true;

            if (!nodes.Any(n => n.Type == NodeType.PositionnementFolder))
                return true;

            if (!nodes.Any(n => n.Type == NodeType.ExigencesFolder))
                return true;

            return false;
        }

        private static bool IsCyclic(NodesDefinition node, Dictionary<int, NodesDefinition> nodesById)
        {
            var visited = new HashSet<int>();
            var current = node;

            while (current.ParentId.HasValue)
            {
                if (!nodesById.TryGetValue(current.ParentId.Value, out current))
                    break;

                if (!visited.Add(current.Id))
                    return true; // cycle détecté
            }

            return false;
        }

        #endregion


        #region Default Folder

        public async Task<IEnumerable<NodesDefinition>> EnsureDefaultFoldersAsync()
        {
            var allNodes = await GetAllNodesAsync();
            string modelName = Path.GetFileNameWithoutExtension(
                                   ModelManager.ModelActif) ?? "Toletch Model";

            // ── 1. Garantir les 3 nœuds structurels ──────────────────────────
            bool dirty = false;

            var modelFolder = allNodes.FirstOrDefault(n => n.Type == NodeType.ModelFolder);
            if (modelFolder == null)
            {
                modelFolder = await CreateFolderAsync(NodeType.ModelFolder, modelName, parentId: 0);
                dirty = true;
            }
            else if (modelFolder.NodeName != modelName || modelFolder.ParentId != 0)
            {
                modelFolder.NodeName = modelName;
                modelFolder.ParentId = 0;
                await UpdateAsync(modelFolder);
                dirty = true;
            }

            var positionnementFolder = await EnsureChildFolderAsync(
                allNodes, NodeType.PositionnementFolder, "Positionnement", modelFolder.Id, dirty);

            var exigencesFolder = await EnsureChildFolderAsync(
                allNodes, NodeType.ExigencesFolder, "Exigences", modelFolder.Id, dirty);

            // ── 2. Recharger une seule fois si on a écrit ─────────────────────
            if (dirty)
                allNodes = await GetAllNodesAsync();

            // ── 3. Replacer les orphelins ─────────────────────────────────────
            var nodesById = allNodes.Where(n => n != null).ToDictionary(n => n.Id);
            var toMoveInPos = GetOrphanNodes(allNodes, nodesById, NodeType.PartNode);
            var toMoveInExg = GetOrphanNodes(allNodes, nodesById, NodeType.RequirementNode);

            await MoveNodesToFolderAsync(toMoveInPos, positionnementFolder, allNodes);
            await MoveNodesToFolderAsync(toMoveInExg, exigencesFolder, allNodes);

            // ── 4. Normaliser les ordres ──────────────────────────────────────
            await NormalizeDisplayOrderAsync(positionnementFolder.Id);
            await NormalizeDisplayOrderAsync(exigencesFolder.Id);

            // ── 5. Rechargement final unique ──────────────────────────────────
            return await GetAllNodesAsync();
        }

        // ─────────────────────────────────────────────────────────────────────────────

        private async Task<NodesDefinition> EnsureChildFolderAsync(
            List<NodesDefinition> allNodes,
            NodeType type,
            string name,
            int parentId,           // ← ID réel du parent, pas hardcodé
            bool dirty)
        {
            var folder = allNodes.FirstOrDefault(n => n.Type == type);

            if (folder == null)
            {
                dirty = true;
                return await CreateFolderAsync(type, name, parentId);
            }

            // Corriger seulement si nécessaire
            bool needsUpdate = folder.ParentId != parentId;
            if (needsUpdate)
            {
                folder.ParentId = parentId;
                await UpdateAsync(folder);
                dirty = true;
            }

            return folder;
        }

        private async Task<NodesDefinition> CreateFolderAsync(
            NodeType type, string name, int parentId)
        {
            var folder = new NodesDefinition
            {
                NodeName = name,
                IsFolder = true,
                IsFixed = false,
                Type = type,
                IsExpanded = true,
                ParentId = parentId,
                DisplayOrder = 0
            };
            await InsertAsync(folder);
            return folder;
        }

        private List<NodesDefinition> GetOrphanNodes(
            List<NodesDefinition> allNodes,
            Dictionary<int, NodesDefinition> nodesById,
            NodeType type)
        {
            return allNodes
                .Where(n => n.Type == type && !IsNodeInsideFolder(n, nodesById))
                .ToList();
        }

        private async Task MoveNodesToFolderAsync(
            List<NodesDefinition> nodes,
            NodesDefinition targetFolder,
            List<NodesDefinition> allNodes)
        {
            if (!nodes.Any()) return;

            int maxOrder = allNodes
                .Where(p => p.ParentId == targetFolder.Id)
                .Select(p => (int?)p.DisplayOrder)
                .Max() ?? -1;

            foreach (var node in nodes)
            {
                node.ParentId = targetFolder.Id;
                node.DisplayOrder = ++maxOrder;
            }

            await UpdateRangeAsync(nodes);
        }

        private bool IsNodeInsideFolder(NodesDefinition node, Dictionary<int, NodesDefinition> nodesById)
        {
            var current = node;
            while (current?.ParentId.HasValue == true)
            {
                if (!nodesById.TryGetValue(current.ParentId.Value, out var parent))
                    break; // parent introuvable → considéré orphelin
                if (parent.IsFolder)
                    return true;
                current = parent;
            }
            return false;
        }

        #endregion

        /// <summary>
        /// Reconstruit la hiérarchie d'après la liste plate de noeuds.
        /// Utiliser votre BuildPartHierarchy(allNodes) existante.
        /// </summary>
        public ObservableCollection<NodesDefinition> BuildPartHierarchy(IEnumerable<NodesDefinition> allParts, NodesDefinition? parentNode = null)
        {
            int parentId = parentNode?.Id ?? 0;

            var childrenList = allParts
              .Where(p => p.ParentId == parentId)
              .OrderByDescending(p => p.DisplayOrder)
              .ThenByDescending(p => p.Id) // stabilité inverse
              .ToList();

            var childrenCollection = new ObservableCollection<NodesDefinition>();

            foreach (var child in childrenList)
            {
                // Assignation récursive en ObservableCollection
                child.Children = BuildPartHierarchy(allParts, child);
                childrenCollection.Add(child);
            }

            return childrenCollection;
        }

    }
}
