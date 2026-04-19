using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toltech.App.Models;
using System.Collections;
using System.Collections.Generic;

namespace Toltech.App.Utilities
{
    /// <summary>
    /// Comparateur personnalisé permettant de trier une collection de <see cref="Requirements"/>
    /// selon un ordre d'identifiants prédéfini.
    /// <para>
    /// Utile pour restaurer un ordre personnalisé (ex: ordre défini par l'utilisateur)
    /// indépendamment de l'ordre naturel des IDs en base de données.
    /// </para>
    /// <para>
    /// Les éléments dont l'identifiant est absent de la liste de référence
    /// sont automatiquement placés en fin de collection.
    /// </para>
    /// </summary>
    public class RequirementIdOrderComparer : IComparer
    {
        private readonly List<int> _orderedIds;

        public RequirementIdOrderComparer(IEnumerable<int> orderedIds)
        {
            _orderedIds = orderedIds?.ToList() ?? new List<int>();
        }

        public int Compare(object x, object y)
        {
            if (x is not Requirements r1 || y is not Requirements r2)
                return 0;

            int index1 = _orderedIds.IndexOf(r1.Id_req);
            int index2 = _orderedIds.IndexOf(r2.Id_req);

            // Si un Id n’est pas présent, le placer à la fin
            if (index1 == -1) index1 = int.MaxValue;
            if (index2 == -1) index2 = int.MaxValue;

            return index1.CompareTo(index2);
        }
    }
}
