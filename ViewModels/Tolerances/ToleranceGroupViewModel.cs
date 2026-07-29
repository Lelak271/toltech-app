using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toltech.App.ViewModels
{
    /// <summary>
    /// Regroupe les 3 positions d'une même tolérance (Origin, Intermediate, Extremity).
    /// Pur regroupement, aucune logique propre.
    /// </summary>
    public sealed class ToleranceGroupViewModel
    {
        public ToleranceSlotViewModel Extremity { get; }

        public ToleranceSlotViewModel Intermediate { get; }

        public ToleranceSlotViewModel Origin { get; }

        public ToleranceGroupViewModel(
            ToleranceSlotViewModel extremity,
            ToleranceSlotViewModel intermediate,
            ToleranceSlotViewModel origin)
        {
            Extremity = extremity;
            Intermediate = intermediate;
            Origin = origin;
        }
    }
}
