using Toltech.App.Models;
using Toltech.App.Services;
using static Toltech.App.Models.NodesDefinition;

namespace Toltech.App.Utilities
{
    public class NodeSyncService
    {
        private readonly DatabaseService _databaseService;
        public NodeSyncService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        private bool _isSyncRunning;

        public async Task SafeSyncAsync()
        {
            if (_isSyncRunning)
                return;

            try
            {
                _isSyncRunning = true;
                await SyncNodesTableAsync();
            }
            finally
            {
                _isSyncRunning = false;
            }
        }

        #region Syncronisation NodesDefinition
       
        /// <summary>
        /// Synchronise la table des nœuds <see cref="NodesDefinition"/> avec les données existantes 
        /// dans les tables <see cref="Requirements"/> et <see cref="ModelData"/>.
        /// </summary>
        /// <remarks>
        /// Cette méthode effectue plusieurs opérations pour maintenir la cohérence entre la base de données
        /// et l'arborescence des nœuds :
        /// <list type="bullet">
        ///     <item>Récupère tous les nœuds existants, requirements et modèles.</item>
        ///     <item>Filtre les nœuds pertinents : <see cref="NodeType.PartNode"/> et <see cref="NodeType.RequirementNode"/>.</item>
        ///     <item>Compare les nœuds existants avec les données sources en utilisant <c>LinkedOriginalId</c> comme référence unique.</item>
        ///     <item>Mets à jour les nœuds dont les propriétés ont changé (par exemple <c>NodeName</c>).</item>
        ///     <item>Ajoute les nœuds manquants pour les requirements et les parts.</item>
        ///     <item>Supprime les nœuds orphelins ne correspondant plus à aucune entrée des tables sources.</item>
        ///     <item>Assure l’unicité des <see cref="NodeType.PartNode"/> par <c>Extremite</c> pour éviter les doublons.</item>
        /// </list>
        /// </remarks>
        /// 
        private async Task SyncNodesTableAsync()
        {
            var allRequirements = await _databaseService.GetAllRequirementsAsync();
            var allModelData = await _databaseService.GetAllModelDataAsync();
            var allPart = await _databaseService.GetAllPartsAsync();
            var allNodes = await _databaseService.GetAllNodesAsync();

            var toInsert = new List<NodesDefinition>();
            var toUpdate = new List<NodesDefinition>();
            var toDelete = new List<NodesDefinition>();

            await SyncRequirementNodes(allNodes, allRequirements, toInsert, toUpdate, toDelete);
            await SyncPartNodes(allNodes, allPart, toInsert, toUpdate, toDelete);
            await SyncDataNodes(allNodes, allModelData, toInsert, toUpdate, toDelete);

            // COMMIT UNIQUE
            await _databaseService.RunInTransactionAsync(async () =>
            {
                if (toDelete.Any())
                    await _databaseService.DeleteRangeAsync(toDelete);

                if (toInsert.Any())
                    await _databaseService.InsertRangeAsync(toInsert);

                if (toUpdate.Any())
                    await _databaseService.UpdateRangeAsync(toUpdate);
            });

            // Event UNIQUE
            if (toInsert.Any() || toUpdate.Any() || toDelete.Any())
            {
                NodesDefinition.RaiseNodeChanged(NodeChangeType.Updated, null);
            }
        }

        private Task SyncRequirementNodes(
                     List<NodesDefinition> allNodes,
                     List<Requirements> allRequirements,
                     List<NodesDefinition> toInsert,
                     List<NodesDefinition> toUpdate,
                     List<NodesDefinition> toDelete)
        {
            var defaultFolder = GetDefaultFolderIdForReqAsync(allNodes);

            var existingNodes = allNodes
                .Where(n => n.Type == NodeType.RequirementNode && n.LinkedRequirementId.HasValue)
                .ToDictionary(n => n.LinkedRequirementId!.Value);

            var requirementsById = allRequirements.ToDictionary(r => r.Id_req);

            foreach (var (id, node) in existingNodes)
            {
                if (requirementsById.TryGetValue(id, out var req))
                {
                    var newName = req.NameReq.Trim();

                    if (node.NodeName != newName || node.IsActive != req.IsActive)
                    {
                        node.NodeName = newName;
                        node.IsActive = req.IsActive;
                        toUpdate.Add(node);
                    }

                    requirementsById.Remove(id);
                }
                else
                {
                    toDelete.Add(node);
                }
            }

            foreach (var req in requirementsById.Values)
            {
                toInsert.Add(new NodesDefinition
                {
                    NodeName = req.NameReq.Trim(),
                    Type = NodeType.RequirementNode,
                    LinkedRequirementId = req.Id_req,
                    ParentId = defaultFolder.Result
                });
            }

            return Task.CompletedTask;
        }

        private Task SyncDataNodes(
                    List<NodesDefinition> allNodes,
                    List<ModelData> allModelData,
                    List<NodesDefinition> toInsert,
                    List<NodesDefinition> toUpdate,
                    List<NodesDefinition> toDelete)
        {
            // --- 1. Mapping PartNode -> NodeId ---
            var partNodesByPartId = allNodes
                .Where(n => n.Type == NodeType.PartNode && n.LinkedOriginalId > 0)
                .ToDictionary(n => n.LinkedOriginalId, n => n.Id);

            // --- 2. Mapping existant DataNode ---
            var existingDataByModelId = allNodes
                .Where(n => n.Type == NodeType.DataNode && n.LinkedOriginalId > 0)
                .ToDictionary(n => n.LinkedOriginalId);

            // --- 3. Index des DisplayOrder par parent ---
            var maxOrderByParent = allNodes
                .GroupBy(n => n.ParentId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(n => n.DisplayOrder).DefaultIfEmpty(0).Max()
                );

            // --- 4. Synchronisation ---
            foreach (var data in allModelData)
            {
                if (!data.ExtremitePartId.HasValue)
                    continue;

                if (!partNodesByPartId.TryGetValue(data.ExtremitePartId.Value, out var parentNodeId))
                    continue;

                if (existingDataByModelId.TryGetValue(data.Id, out var node))
                {
                    // UPDATE si nécessaire uniquement
                    if (node.NodeName != data.Model ||
                        node.ParentId != parentNodeId ||
                        node.IsActive != data.Active)
                    {
                        node.NodeName = data.Model;
                        node.ParentId = parentNodeId;
                        node.IsActive = data.Active;

                        toUpdate.Add(node);
                    }

                    existingDataByModelId.Remove(data.Id);
                }
                else
                {
                    // INSERT
                    int nextOrder = maxOrderByParent.TryGetValue(parentNodeId, out var currentMax)
                        ? currentMax + 1
                        : 0;

                    var newNode = new NodesDefinition
                    {
                        NodeName = data.Model,
                        Type = NodeType.DataNode,
                        LinkedOriginalId = data.Id,
                        ParentId = parentNodeId,
                        IsActive = data.Active,
                        DisplayOrder = nextOrder
                    };

                    toInsert.Add(newNode);

                    // mise à jour du max local (important)
                    maxOrderByParent[parentNodeId] = nextOrder;
                }
            }

            // --- 5. Orphelins ---
            foreach (var orphan in existingDataByModelId.Values)
            {
                toDelete.Add(orphan);
            }

            return Task.CompletedTask;
        }

        private Task SyncPartNodes(
                    List<NodesDefinition> allNodes,
                    List<Part> allParts,
                    List<NodesDefinition> toInsert,
                    List<NodesDefinition> toUpdate,
                    List<NodesDefinition> toDelete)
        {
            var defaultFolder = GetDefaultFolderIdForPartAsync(allNodes);

            // --- 1. Index des nodes existants ---
            var existingPartNodesById = allNodes
                .Where(n => n.Type == NodeType.PartNode && n.LinkedOriginalId > 0)
                .ToDictionary(n => n.LinkedOriginalId, n => n);

            // --- 2. Index des parts ---
            var partsById = allParts
                .Where(p => p.Id > 0)
                .ToDictionary(p => p.Id);

            // --- 3. Index DisplayOrder racine ---
            int maxRootOrder = allNodes
                .Where(n => n.ParentId == 0)
                .Select(n => n.DisplayOrder)
                .DefaultIfEmpty(0)
                .Max();

            // --- 4. Synchronisation ---
            foreach (var (partId, part) in partsById)
            {
                if (existingPartNodesById.TryGetValue(partId, out var node))
                {
                    // UPDATE si nécessaire
                    if (!string.Equals(node.NodeName, part.NamePart?.Trim(), StringComparison.Ordinal) ||
                        node.IsActive != part.IsActive)
                    {
                        node.NodeName = part.NamePart?.Trim();
                        node.IsActive = part.IsActive;

                        toUpdate.Add(node);
                    }

                    existingPartNodesById.Remove(partId);
                }
                else
                {
                    // INSERT
                    var newNode = new NodesDefinition
                    {
                        NodeName = part.NamePart?.Trim(),
                        Type = NodeType.PartNode,
                        LinkedOriginalId = part.Id,
                        ParentId = defaultFolder.Result,
                        IsActive = part.IsActive,
                        DisplayOrder = ++maxRootOrder
                    };

                    toInsert.Add(newNode);
                }
            }

            // --- 5. Suppression des orphelins ---
            foreach (var orphan in existingPartNodesById.Values)
            {
                toDelete.Add(orphan);
            }

            return Task.CompletedTask;
        }


        #endregion

        private async Task<int?> GetDefaultFolderIdForPartAsync(List<NodesDefinition> nodes)
        {
            if (nodes == null || nodes.Count == 0)
                return null;

            // PositionnementFolder prioritaire
            var positionnement = nodes
                .FirstOrDefault(n => n.IsFolder && n.Type == NodeType.PositionnementFolder);

            if (positionnement != null)
                return positionnement.Id;

            // ModelFolder fallback
            var modelFolder = nodes
                .FirstOrDefault(n => n.IsFolder && n.Type == NodeType.ModelFolder);

            if (modelFolder != null)
                return modelFolder.Id;

            // fallback racine (ParentId = 0 ou null selon votre modèle)
            return 0;
        }
        private async Task<int?> GetDefaultFolderIdForReqAsync(List<NodesDefinition> nodes)
        {
            if (nodes == null || nodes.Count == 0)
                return null;

            // ExigencesFolder prioritaire
            var exigences = nodes
                .FirstOrDefault(n => n.IsFolder && n.Type == NodeType.ExigencesFolder);

            if (exigences != null)
                return exigences.Id;
            // ModelFolder fallback
            var modelFolder = nodes
                .FirstOrDefault(n => n.IsFolder && n.Type == NodeType.ModelFolder);

            if (modelFolder != null)
                return modelFolder.Id;

            // fallback racine (ParentId = 0 ou null selon votre modèle)
            return 0;
        }

    }
}
