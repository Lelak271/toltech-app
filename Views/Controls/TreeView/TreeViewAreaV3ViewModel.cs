using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TOLTECH_APPLICATION.Models;
using TOLTECH_APPLICATION.Services;
using TOLTECH_APPLICATION.ViewModels;
using static TOLTECH_APPLICATION.FrontEnd.Controls.Dashboard.BarChartControl;
using static TOLTECH_APPLICATION.Models.NodesDefinition;


namespace TOLTECH_APPLICATION.Views.Controls.TreeView
{
    public class TreeViewAreaV3ViewModel : INotifyPropertyChanged // TODO passé en BaseVm
    {
        private readonly MainViewModel _mainVM;
        private Func<Task> _treeUpdateAction;

        private readonly DomainService _domainService;

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion

        #region Propriétés exposées au XAML

        private ObservableCollection<NodesDefinition> _rootNodes = new ObservableCollection<NodesDefinition>();
        public ObservableCollection<NodesDefinition> RootNodes
        {
            get => _rootNodes;
            set => SetField(ref _rootNodes, value, nameof(RootNodes));
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetField(ref _isLoading, value, nameof(IsLoading));
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetField(ref _statusMessage, value, nameof(StatusMessage));
        }

        private NodesDefinition _selectedNode;
        public NodesDefinition SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (_selectedNode != value)
                {
                    _selectedNode = value;
                    OnPropertyChanged(nameof(SelectedNode));
                    //Debug.WriteLine($"[TreeViewVM] SelectedNode changed to: {_selectedNode?.NodeName} - ID : {_selectedNode?.Id}");

                    // Ici vous pouvez aussi déclencher un filtrage automatique
                    //UpdateVisibleRequirements();
                }
            }
        }


        public bool IsEditing { get; set; }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region ICommand
        public ICommand RefreshCommand { get; }
        public ICommand AddSubFolderCommand { get; }
        public ICommand DeleteFolderCommand { get; }
        public ICommand RenameFolderCommand { get; }
        public ICommand GroupSelectionIntoSubFolderCommand { get; }
        public ICommand DeleteNodePartCommand { get; }
        public ICommand RenameNodeCommand { get; }
        public ICommand ShowNodePropertiesCommand { get; }
        public ICommand QuickAnalyzeCommand { get; }
        public ICommand ExportDataCommand { get; }
        public ICommand DesactiveNodePartCommand { get; }

        public RelayCommand CreateRequirementFromTreeCommand { get; }
        public ICommand DeleteRequirementFromTreeCommand { get; }

        public RelayCommand CreatePartFromTreeCommand { get; }

        // Indicateur interne pour éviter reentrancy
        private bool _isLoadingTreeView = false;

        #endregion

        #region Constructeur
        public TreeViewAreaV3ViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;
            _domainService = mainVM.DomainService;

            #region ICommand Initializations

            RefreshCommand = new RelayCommand(async _ => await LoadTreeViewDataAsync(), _ => !_isLoadingTreeView);
            AddSubFolderCommand = new RelayCommand<NodesDefinition>(AddSubFolderAsync);
            DeleteFolderCommand = new RelayCommand<NodesDefinition>(DeleteFolderAsync);
            GroupSelectionIntoSubFolderCommand = new RelayCommand<List<NodesDefinition>>(GroupSelectionIntoSubFolderAsync);
            RenameNodeCommand = new RelayCommand<NodesDefinition>(RenameNode);
            ShowNodePropertiesCommand = new RelayCommand<NodesDefinition>(ShowNodeProperties);
            QuickAnalyzeCommand = new RelayCommand<NodesDefinition>(QuickAnalyze);
            ExportDataCommand = new RelayCommand<NodesDefinition>(ExportData);

            #region Datas Commands via TreeView

            CreatePartFromTreeCommand = new RelayCommand(_ =>
            {
                if (_mainVM.DataVM?.CreatePartCommand?.CanExecute(null) == true)
                    _mainVM.DataVM.CreatePartCommand.Execute(null);
            });


            DeleteNodePartCommand = new RelayCommand<NodesDefinition>(DeleteNodePartAsync);

            #endregion

            #region Requirements Commands via TreeView

            CreateRequirementFromTreeCommand = new RelayCommand(async _ =>
            {
                if (_mainVM?.RequirementVM != null)
                    await _mainVM.RequirementVM.CreateRequirementAsync();
            });


            DeleteRequirementFromTreeCommand = new RelayCommand<NodesDefinition>(DeleteNodeReqAsync);

            #endregion

            #region Part Commands via TreeView

            DesactiveNodePartCommand = new RelayCommand<NodesDefinition>(ReverseActiveNodeqAsync);

            #endregion

            _treeUpdateAction = () => _ = LoadTreeViewDataAsync();
            EventsManager.TreeViewUpdated += _treeUpdateAction;
            EventsManager.ModelOpen += _treeUpdateAction;
            //LoadTreeViewDataAsync();
            #endregion

        }


        #endregion

        #region Loader

        /// <summary>
        /// Méthode principale pour charger les données du TreeView.
        /// Réutilise vos sous-fonctions existantes :
        /// - DatabaseService.ActiveInstance.CreateTableIfNotExistsAsync<NodesDefinition>()
        /// - EnsureDefaultFoldersAsync()
        /// - BuildPartHierarchy(allNodes)
        /// </summary>
        public async Task LoadTreeViewDataAsync()
        {
            Debug.WriteLine("[TreeViewVM] - LoadTreeViewDataAsync()");
            //await DatabaseService.ActiveInstance.PublicSyncTablesAsync(); 

            if (_isLoadingTreeView) return;
            if (_isLoadingTreeView) return;
            _isLoadingTreeView = true;
            IsLoading = true;
            StatusMessage = "Chargement...";

            var globalWatch = Stopwatch.StartNew();

            try
            {
                // --- Étape 1 : Sauvegarder les IDs sélectionnés existants (pour réappliquer la sélection)
                var selectedIds = RootNodes.SelectMany(r => Flatten(r)).Where(n => n.IsSelected).Select(n => n.Id).ToHashSet();

                // --- Étape 3 : S'assurer que la table existe
                //await DatabaseService.ActiveInstance.CreateTableIfNotExistsAsync<NodesDefinition>();

                // --- Étape 4 : Charger tous les noeuds depuis la DB  
                var allNodes = await EnsureDefaultFoldersAsync();

                // --- Étape 5 : Réappliquer les sélections
                foreach (var node in allNodes.Where(n => selectedIds.Contains(n.Id)))
                {
                    node.IsSelected = true;
                }

                // --- Étape 6 : Construire la hiérarchie (réutiliser BuildPartHierarchy)
                var rootNodesEnumerable = BuildPartHierarchy(allNodes);

                // --- Étape 7 : Mettre à jour la collection liée (RootNodes) pour que le TreeView se mette à jour automatiquement
                RootNodes = new ObservableCollection<NodesDefinition>(rootNodesEnumerable);

                // --- Étape 8 : Statut final
                StatusMessage = $"Chargé : {RootNodes.Count} racines. ({globalWatch.ElapsedMilliseconds} ms)";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Chargement annulé.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Erreur lors du chargement.";
                Debug.WriteLine($"❌ Erreur LoadTreeViewDataAsync : {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _isLoadingTreeView = false;
                IsLoading = false;
            }
        }


        #region Helpers
        // -------------------------

        /// <summary>
        /// Aplatit un arbre pour itérer sur tous les nœuds (pré-order).
        /// Utilisé pour extraire les IDs sélectionnés existants.
        /// </summary>
        private static IEnumerable<NodesDefinition> Flatten(NodesDefinition root)
        {
            if (root == null) yield break;
            yield return root;
            if (root.Children == null) yield break;
            foreach (var child in root.Children)
            {
                foreach (var descendant in Flatten(child))
                    yield return descendant;
            }
        }

        /// <summary>
        /// Retourne tous les NodesDefinition (créés ou chargés depuis la DB).
        /// Utiliser votre méthode EnsureDefaultFoldersAsync() existante.
        /// </summary>
        /// <returns>IEnumerable<NodesDefinition></returns>
        private async Task<IEnumerable<NodesDefinition>> EnsureDefaultFoldersAsync()
        {
            //Debug.WriteLine("TreeViewAreaV2 - EnsureDefaultFoldersAsync()");

            var allNodes = await GetAllNodesSafeAsync();
            string fullPath = ModelManager.ModelActif;
            string modelName = System.IO.Path.GetFileNameWithoutExtension(fullPath) ?? "Toletch Model...";


            // Assurer les dossiers racines par défaut 
            var modelFolder = await EnsureFolderExistsAsync(allNodes, NodeType.ModelFolder, modelName);
            int? idOfModelFolder = modelFolder.Id;
            allNodes = await GetAllNodesSafeAsync();
            var positionnementFolder = await EnsureFolderExistsAsync(allNodes, NodeType.PositionnementFolder, "Positionnement", idOfModelFolder);
            var exigencesFolder = await EnsureFolderExistsAsync(allNodes, NodeType.ExigencesFolder, "Exigences", idOfModelFolder);

            // Rafraîchir après création éventuelle
            allNodes = await GetAllNodesSafeAsync();

            // Déplacer les nœuds selon votre logique
            await MovePartNodesNotInFolderAsync(allNodes, positionnementFolder);
            await MoveRequirementNodesAsync(allNodes, exigencesFolder);

            // Recalcul ordres d’affichage 
            await NormalizeDisplayOrderAsync(positionnementFolder.Id);
            await NormalizeDisplayOrderAsync(exigencesFolder.Id);

            // Rechargement final
            return await GetAllNodesSafeAsync();
        }

        /// <summary>
        /// Reconstruit la hiérarchie d'après la liste plate de noeuds.
        /// Utiliser votre BuildPartHierarchy(allNodes) existante.
        /// </summary>
        private static ObservableCollection<NodesDefinition> BuildPartHierarchy(IEnumerable<NodesDefinition> allParts, NodesDefinition? parentNode = null)
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

        private async Task<List<NodesDefinition>> GetAllNodesSafeAsync()
        {
            if (DatabaseService.ActiveInstance == null)
                return new List<NodesDefinition>(); // TODO Mis pour eviter execption au demarrage appli
            return await DatabaseService.ActiveInstance.GetAllNodesAsync();
        }

        /// <summary>
        /// Vérifie l’existence d’un dossier par nom, le crée si absent.
        /// </summary>
        private async Task<NodesDefinition> EnsureFolderExistsAsync(List<NodesDefinition> allNodes, NodeType type, string defaultName, int? parentID = 0)
        {
            // Cherche le dossier par Id
            var folder = allNodes.FirstOrDefault(p => p.IsFolder && p.Type == type);

            if (folder != null)
            {
                if (folder.Type == NodeType.ModelFolder)
                {
                    folder.ParentId = 0; // Racine absolue
                    folder.NodeName = defaultName;
                }
                else if (folder.Type == NodeType.PositionnementFolder || folder.Type == NodeType.ExigencesFolder)
                {
                    folder.ParentId = parentID; // Racine dépendante
                }

                await DatabaseService.ActiveInstance.UpdateNodeAsync(folder);
                return folder;
            }

            // Si absent, créer
            folder = new NodesDefinition
            {
                NodeName = defaultName,  // Nom par défaut si jamais il faut créer
                IsFolder = true,
                IsFixed = false,
                Type = type,
                IsExpanded = true,
                ParentId = type == NodeType.PositionnementFolder || type == NodeType.ExigencesFolder
                           ? 1
                           : 0,
                DisplayOrder = allNodes.Count(p => p.ParentId == 0)
            };
            await DatabaseService.ActiveInstance.AddNodeAsync(folder);
            return folder;
        }

        /// <summary>
        /// Réattribue les DisplayOrder séquentiels pour un parent donné (via DB).
        /// </summary>
        private async Task NormalizeDisplayOrderAsync(int parentId)
        {
            var children = await DatabaseService.ActiveInstance.GetNodesByParentIdAsync(parentId);
            var ordered = children.OrderBy(c => c.DisplayOrder).ToList();
            var nodesToUpdate = new List<NodesDefinition>();

            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered[i].DisplayOrder != i)
                {
                    ordered[i].DisplayOrder = i;
                    nodesToUpdate.Add(ordered[i]); // collection des nœuds à mettre à jour
                }
            }

            if (nodesToUpdate.Any())
            {
                await DatabaseService.ActiveInstance.UpdateAllNodesAsync(nodesToUpdate);
            }

        }

        /// <summary>
        /// Déplace tous les nœuds "part/model" (LinkedRequirementId == null) qui NE SONT PAS déjà dans un dossier
        /// vers le dossier "Positionnement".
        /// </summary>
        private async Task MovePartNodesNotInFolderAsync(List<NodesDefinition> allNodes, NodesDefinition targetFolder)
        {
            // construire lookup pour la recherche d'ancêtres
            var nodesById = allNodes.Where(n => n != null).ToDictionary(n => n.Id, n => n);

            // sélection des nœuds "part/model" non-folder et non déjà dans un dossier
            var toMove = allNodes
                .Where(n => n.Type == NodeType.PartNode && !IsNodeInsideFolder(n, nodesById))
                .ToList();

            if (!toMove.Any()) return;

            int maxOrder = allNodes.Where(p => p.ParentId == targetFolder.Id)
                                   .Select(p => (int?)p.DisplayOrder)
                                   .Max() ?? -1;

            foreach (var node in toMove)
            {
                node.ParentId = targetFolder.Id;
                node.DisplayOrder = ++maxOrder;
            }
            await DatabaseService.ActiveInstance.UpdateAllNodesAsync(toMove);
        }

        /// <summary>
        /// Déplace tous les nœuds liés à une exigence (LinkedRequirementId != null) qui NE SONT PAS déjà dans un dossier
        /// vers le dossier "Exigences".
        /// </summary>
        private async Task MoveRequirementNodesAsync(List<NodesDefinition> allNodes, NodesDefinition exigencesFolder)
        {
            // construire lookup pour la recherche d'ancêtres
            var nodesById = allNodes.Where(n => n != null).ToDictionary(n => n.Id, n => n);

            // sélection des nœuds 'exigence' non-folder et non déjà dans un dossier
            var toMove = allNodes
                .Where(n => n.Type == NodeType.RequirementNode && !IsNodeInsideFolder(n, nodesById))
                .ToList();

            if (!toMove.Any()) return;

            int maxOrder = allNodes.Where(p => p.ParentId == exigencesFolder.Id)
                                   .Select(p => (int?)p.DisplayOrder)
                                   .Max() ?? -1;

            foreach (var node in toMove)
            {
                node.ParentId = exigencesFolder.Id;
                node.DisplayOrder = ++maxOrder;
            }
            await DatabaseService.ActiveInstance.UpdateAllNodesAsync(toMove);
        }

        /// <summary>
        /// Indique si le nœud est contenu (directement ou indirectement) dans un dossier (ancêtre IsFolder == true).
        /// </summary>
        private bool IsNodeInsideFolder(NodesDefinition node, Dictionary<int, NodesDefinition> nodesById)
        {
            var current = node;
            while (current != null && current.ParentId.HasValue)
            {
                if (!nodesById.TryGetValue(current.ParentId.Value, out var parent))
                    break; // parent introuvable, on considère qu'il n'est pas dans un dossier
                if (parent.IsFolder)
                    return true;
                current = parent;
            }
            return false;
        }

        #endregion

        #endregion

        #region Context Menu - Elementary Functions

        #region Add / Delete Sub-Folder
        private async void AddSubFolderAsync(NodesDefinition clickedNode)
        {
            if (clickedNode == null)
                return;

            await CreateFolderParentId(clickedNode.Id);
        }

        private async void GroupSelectionIntoSubFolderAsync(List<NodesDefinition> selectedNodes)
        {
            if (selectedNodes == null || selectedNodes.Count == 0)
                return;

            await CreateSubFolderWithNodes(selectedNodes);
        }

        // Crée un nouveau dossier et y déplace les nodes fournis.
        private async Task CreateSubFolderWithNodes(List<NodesDefinition> nodesToGroup)
        {
            if (nodesToGroup == null || nodesToGroup.Count == 0)
                return;

            // Déterminer le parent commun du nouveau dossier.
            // Ici, on prend le parent du premier node sélectionné.
            var firstNode = nodesToGroup[0];

            if (firstNode == null)
                return;

            // Crée un nouveau dossier sous le parent
            var newFolder = await CreateFolderParentId(firstNode.ParentId);

            if (newFolder == null)
                return;

            // Déplace tous les nodes sélectionnés dans le nouveau dossier
            foreach (var node in nodesToGroup)
            {
                // Mettre à jour le ParentId et ParentNode
                node.ParentId = newFolder.Id;
            }
            await DatabaseService.ActiveInstance.UpdateAllNodesAsync(nodesToGroup);

            // Facultatif : expand le nouveau dossier dans l'arbre
            newFolder.IsExpanded = true;
        }

        private async Task<NodesDefinition> CreateFolderParentId(int? parentId)
        {
            var folder = new NodesDefinition
            {
                NodeName = "New Folder",
                IsFolder = true,
                ParentId = parentId,
                Type = NodeType.Normal
            };

            await DatabaseService.ActiveInstance.AddNodeAsync(folder);

            return folder;
        }

        #region Delete Folder 

        /// <summary>
        /// Supprime un folder donné et remonte ses enfants vers le parent.
        /// </summary>
        private async void DeleteFolderAsync(NodesDefinition folder)
        {
            if (folder == null || !folder.IsFolder)
                return;

            await DeleteFolderAndPromoteChildrenAsync(folder);

            // Notifie la mise à jour de l'arbre
            //EventsManager.RaiseNodesUpdated();
        }

        /// <summary>
        /// Supprime le folder et remonte ses enfants vers le parent.
        /// </summary>
        private async Task DeleteFolderAndPromoteChildrenAsync(NodesDefinition folder)
        {
            int? parentId = folder.ParentId;

            if (folder.Children != null && folder.Children.Count > 0)
            {
                foreach (var child in folder.Children.ToList())
                {
                    child.ParentId = parentId;
                }
                await DatabaseService.ActiveInstance.UpdateAllNodesAsync(folder.Children);
            }

            // Supprimer le folder de la base
            await DatabaseService.ActiveInstance.DeleteNodeAsync(folder);
        }

        #endregion


        #endregion

        #region Rename Inline
        public async Task RenameFolderAsync(NodesDefinition folder, string newName)
        {
            if (folder == null) return;

            try
            {
                folder.NodeName = newName; // met à jour l'objet en mémoire
                await DatabaseService.ActiveInstance.UpdateNodeAsync(folder);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors du renommage : {ex.Message}");
            }
        }
        public async Task RenamePartAsync(NodesDefinition part)
        {
            if (part == null) return;

            try
            {
                int IdPart = part.LinkedOriginalId; // On récupere l'id de base lors de la création de la table NodesDefinition
                string oldName = await DatabaseService.ActiveInstance.GetExtremiteByIdAsync(IdPart); // Pas tres jolie
                await DatabaseService.ActiveInstance.UpdatePartNameAsync(IdPart, part.NodeName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors du renommage : {ex.Message}");
            }
        }
        public async Task RenameReqAsync(NodesDefinition req, string newName)
        {
            if (req == null) return;

            try
            {
                await DatabaseService.ActiveInstance.UpdateRequirementNameAsync(req.LinkedRequirementId, newName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors du renommage : {ex.Message}");
            }
        }

        #endregion


        private async Task DeleteNodePartAsync(NodesDefinition node)
        {

            if (node.Type != NodeType.PartNode || node == null)
                return;

            if (_mainVM.PartVM == null)
                throw new InvalidOperationException("PartVM n'est pas initialisé."); //TBD

            await _domainService.DeletePartByIdAsync(node.LinkedOriginalId);
        }
        private async Task DeleteNodeReqAsync(NodesDefinition node)
        {

            if (node.Type != NodeType.RequirementNode || node == null)
                return;

            int? idreq = node.LinkedRequirementId;

            if (_mainVM.RequirementVM == null)
                throw new InvalidOperationException("RequirementVM n'est pas initialisé."); //TBD
            await _mainVM.RequirementVM.DeleteRequirementByIdAsync(idreq);
        }
        private async Task ReverseActiveNodeqAsync(NodesDefinition node)
        {
            var type = node.Type;
            if (_mainVM.PartVM == null)
                throw new InvalidOperationException("PartVM n'est pas initialisé."); //TBD
            if (_mainVM.RequirementVM == null)
                throw new InvalidOperationException("RequirementVM n'est pas initialisé."); //TBD

            switch
                (type)
            {
                case NodeType.PartNode:
                    await _mainVM.PartVM.ReverseActivePartByIdAsync(node.LinkedOriginalId);
                    break;
                case NodeType.RequirementNode:
                    await _mainVM.RequirementVM.ReverseActiveReqByIdAsync(node.LinkedRequirementId);
                    break;
                default:
                    break;
            }

        }

        public void PropagateSelectionToDataVM(NodesDefinition? node)
        {
            if (node == null)
                return;

            // Ne propager que pour les DataNode (ou autre type voulu)
            if (node.Type == NodeType.DataNode)
            {
                // Appel à la fonction de la DatasVM
                _mainVM.DataVM.SelectDataByNodeId(node.LinkedOriginalId);
            }
        }


        private void RenameNode(NodesDefinition node)
        {
            Debug.WriteLine($"RenameNode called for {node?.NodeName}");
        }

        private void ShowNodeProperties(NodesDefinition node)
        {
            Debug.WriteLine($"ShowNodeProperties called for {node?.NodeName}");
        }

        private void QuickAnalyze(NodesDefinition node)
        {
            Debug.WriteLine($"QuickAnalyze called for {node?.NodeName}");
        }

        private void ExportData(NodesDefinition node)
        {
            Debug.WriteLine($"ExportData called for {node?.NodeName}");
        }

        #endregion

        #region Gestion du Drop 


        public async Task MoveNodes(List<NodesDefinition>? nodes, NodesDefinition? dropTarget, bool insertAbove)
        {
            if (nodes == null || nodes.Count == 0 || dropTarget == null)
                return;

            // Validation des règles
            foreach (var node in nodes)
            {
                if (!CanMoveNode(node, dropTarget))
                    return; // ou ignorer selon le comportement souhaité
            }

            NodesDefinition parentFolder;
            int insertIndex;

            if (dropTarget.IsFolder)
            {
                // Si drop sur un dossier, l'élément devient enfant
                parentFolder = dropTarget;
                insertIndex = dropTarget.Children.Count > 0 ? dropTarget.Children.Max(c => c.DisplayOrder) + 1 : 0;
            }
            else
            {
                // Sinon, on reste dans le même parent que dropTarget
                parentFolder = await FindParentFolderAsync(dropTarget);
                insertIndex = dropTarget.DisplayOrder;
                if (!insertAbove)
                    insertIndex -= 1;
            }

            int? parentFolderId = parentFolder?.Id;

            if (dropTarget.Type == NodeType.DataNode)
            {
                // Cas particulier de drag drop des Datas
                Debug.WriteLine("[TreeViewViewModel] - RaiseNodesDataDrag()");
                parentFolderId = dropTarget?.ParentId;
            }

            await DatabaseService.ActiveInstance.UpdateDisplayOrderForMoveAsync(
                nodes,
                parentFolderId,
                insertIndex);

            if (dropTarget.Type == NodeType.RequirementNode)
            {
                Debug.WriteLine("[TreeViewViewModel] - RaiseNodesReqDrag()");
                var visibleRequirementIds = await ListIDReqOfSelectFolderAsync();
                var nameParentFolder = await NameParentFolderAsync();

                await EventsManager.RaiseNodReqSelectChangedAsync(
                    visibleRequirementIds,
                    nameParentFolder);
            }
            else if (dropTarget.Type == NodeType.DataNode)
            {
                // Cas particulier de drag drop des Datas
                Debug.WriteLine("[TreeViewViewModel] - RaiseNodesDataDrag()");
                EventsManager.RaiseNodesDataDragAsync(); // Permet la MAJ UI
            }


        }

        #region Helper

        /// <summary>
        /// Trouve le parent folder d'un nœud donné en remontant l'arborescence
        /// </summary>
        private async Task<NodesDefinition> FindParentFolderAsync(NodesDefinition node)
        {
            if (node.ParentId == null)
                return null;

            var allNodes = await GetAllNodesSafeAsync();
            var parent = allNodes.FirstOrDefault(n => n.Id == node.ParentId);

            while (parent != null && !parent.IsFolder)
            {
                parent = allNodes.FirstOrDefault(n => n.Id == parent.ParentId);
            }

            return parent;
        }


        private bool CanMoveNode(NodesDefinition node, NodesDefinition dropTarget)
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
            NodesDefinition targetFolder = dropTarget.IsFolder ? dropTarget : FindParentRecursive(RootNodes, node);
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

        #endregion

        #region Expanded Collapsed
        private bool _isUpdatingExpansion = false;

        public async Task OnNodeExpandedAsync(NodesDefinition node)
        {
            if (_isUpdatingExpansion)
                return;

            _isUpdatingExpansion = true;
            try
            {
                node.IsExpanded = true;
                await DatabaseService.ActiveInstance.UpdateNodeExpansionAsync(node, true);
            }
            finally
            {
                _isUpdatingExpansion = false;
            }
        }

        public async Task OnNodeCollapsedAsync(NodesDefinition node)
        {
            if (_isUpdatingExpansion)
                return;

            _isUpdatingExpansion = true;
            try
            {
                node.IsExpanded = false;
                await DatabaseService.ActiveInstance.UpdateNodeExpansionAsync(node, false);
            }
            finally
            {
                _isUpdatingExpansion = false;
            }
        }


        #endregion

        #region FilteredView
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

            var allNodes = await DatabaseService.ActiveInstance.GetChildrenAsync(parentFolder.Id);

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

            string nameParentFolder =parentFolder.NodeName;

            return nameParentFolder;
        }

        #endregion

    }
}
