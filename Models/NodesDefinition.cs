using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SQLite;

namespace Toltech.App.Models
{
    public class NodesDefinition : INotifyPropertyChanged
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        private string _nodeName;
        public string NodeName
        {
            get => _nodeName;
            set => SetField(ref _nodeName, value);
        }

        private bool _isFixed = false;
        public bool IsFixed
        {
            get => _isFixed;
            set => SetField(ref _isFixed, value);
        }

        private NodeType _type = NodeType.Folder;
        public NodeType Type
        {
            get => _type;
            set => SetField(ref _type, value);
        }

        private int? _parentId;
        public int? ParentId
        {
            get => _parentId;
            set => SetField(ref _parentId, value);
        }

        private bool _isFolder = false;
        public bool IsFolder
        {
            get => _isFolder;
            set => SetField(ref _isFolder, value);
        }

        private int _displayOrder = 0;
        public int DisplayOrder
        {
            get => _displayOrder;
            set => SetField(ref _displayOrder, value);
        }

        private int? _linkedRequirementId;
        public int? LinkedRequirementId
        {
            get => _linkedRequirementId;
            set => SetField(ref _linkedRequirementId, value);
        }
        private int _linkedOriginalId;
        public int LinkedOriginalId
        {
            get => _linkedOriginalId;
            set => SetField(ref _linkedOriginalId, value);
        }

        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetField(ref _isExpanded, value);
        }

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set => SetField(ref _isActive, value);
        }

        private bool _isSelected = false;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetField(ref _isSelected, value);
        }

        private bool _isEditing;
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    OnPropertyChanged(nameof(IsEditing));
                }
            }
        }

        private ObservableCollection<NodesDefinition> _children;

        [Ignore]
        public ObservableCollection<NodesDefinition> Children
        {
            get => _children;
            set
            {
                if (_children != value)
                {
                    _children = value;
                    OnPropertyChanged(); // Notifie le TreeView que la collection a changé
                }
            }
        }



        public enum NodeType
        {
            Folder = 0,
            ModelFolder = 1,
            PositionnementFolder = 2,
            ExigencesFolder = 3,
            PartNode = 4,
            RequirementNode = 5,
            DataNode = 6
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);

            if (propertyName == nameof(IsExpanded))
            {
                OnNodeChanged?.Invoke(NodeChangeType.ExpansionChanged, this);
            }
            else if (propertyName == nameof(IsSelected))
            {
                OnNodeChanged?.Invoke(NodeChangeType.SelectionChanged, this);
            }
            else if (propertyName == nameof(NodeName))
            {
                OnNodeChanged?.Invoke(NodeChangeType.NameChanged, this);
            }
            else
            {
                OnNodeChanged?.Invoke(NodeChangeType.Updated, this);
            }


            return true;
        }

        // Événement statique pour notifier les changements
        public static event Action<NodeChangeType, NodesDefinition> OnNodeChanged;

        public static void RaiseNodeChanged(NodeChangeType changeType, NodesDefinition node)
        {
            OnNodeChanged?.Invoke(changeType, node);
        }


        public enum NodeChangeType
        {
            Updated,
            Added,
            Deleted,
            NameChanged,
            ParentChanged,
            SelectionChanged,
            StructureChanged,
            ExpansionChanged
        }

        #endregion
    }
}
