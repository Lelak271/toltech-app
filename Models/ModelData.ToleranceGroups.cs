using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toltech.App.ViewModels;
using static Toltech.App.Models.ModelData;

namespace Toltech.App.Models
{


    /// <summary>
    /// Mémorise le dernier groupe de tolérance sélectionné pour chaque modèle
    /// durant la session de l'application.
    /// </summary>
    public static class ToleranceGroupSelectionCache
    {
        private static readonly Dictionary<int, int> _cache = new();

        /// <summary>Retourne true si ce panel a déjà un choix mémorisé.</summary>
        public static bool TryGet(int modelId, out int group)
        {
            if (modelId <= 0)
            {
                group = default;
                return false;
            }
            return _cache.TryGetValue(modelId, out group);
        }

        /// <summary>Mémorise le choix de l'utilisateur pour ce panel.</summary>
        public static void Save(int modelId, int group)
        {
            if (modelId <= 0)
                return;
            _cache[modelId] = group;
        }
    }

}
