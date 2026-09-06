using System.Collections.ObjectModel;

namespace Toltech.App.Utilities.Object3D
{
    /// <summary>
    /// Noeud de l'arborescence d'un modèle OBJ.
    /// </summary>
    public sealed class CadNode
    {
        public string Name { get; }

        public CadNodeType Type { get; }

        public CadObject Owner { get; }

        public ObservableCollection<CadNode> Children { get; } = new ObservableCollection<CadNode>();

        public CadNode(string name, CadNodeType type, CadObject owner)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Sans nom" : name;
            Type = type;
            Owner = owner;
        }

        public override string ToString() => Name;
    }

    public enum CadNodeType
    {
        Object,
        Group,
        Mesh
    }
}
