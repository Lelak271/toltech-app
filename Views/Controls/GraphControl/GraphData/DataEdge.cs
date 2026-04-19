using Westermo.GraphX.Common.Models;

namespace TOLTECH_APPLICATION.Views.Controls.GrapheControl.GraphData
{


    public class DataEdge : EdgeBase<DataVertex>
    {
       
        public DataEdge(DataVertex source, DataVertex target, double weight = 1)
            : base(source, target, weight)
        {
        }
        public DataEdge()
            : base(null, null)
        {
        }

        /// Custom string property for example
        public string Text { get; set; }

        #region GET members
        public override string ToString()
        {
            return Text;
        }

        public bool IsRequirement { get; set; }
        #endregion

        public override int GetHashCode()
        {
            return $"{Source}-{Target}-{Text}".GetHashCode(); // Inclure le texte pour rendre l’arête unique
        }

        public override bool Equals(object obj)
        {
            if (obj is not DataEdge other) return false;
            return Source == other.Source && Target == other.Target && Text == other.Text;
        }

    }
}