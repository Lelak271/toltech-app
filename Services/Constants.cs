using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toltech.App.Services
{
    public static class Constants
    {

        // ----------------------------
        // Tolérances générales
        // ----------------------------

        /// <summary>
        /// Tolérance pour comparer des valeurs flottantes proches de zéro
        /// </summary>
        public const double EPSILON = 1e-10;

        /// <summary>
        /// Tolérance pour comparer des angles (radians)
        /// </summary>
        public const double ANGLE_EPSILON = 1e-8;

        /// <summary>
        /// Tolérance pour les distances (unités de l’application, mm)
        /// </summary>
        public const double DISTANCE_EPSILON = 1e-6;

        /// <summary>
        /// Tolérance pour vérifier l’égalité de vecteurs ou points
        /// </summary>
        public const double VECTOR_EPSILON = 1e-10;

        // ----------------------------
        // Paramètres de calcul spécifiques
        // ----------------------------

        /// <summary>
        /// Petit facteur pour éviter la division par zéro
        /// </summary>
        public const double SMALL_VALUE = 1e-12;

        /// <summary>
        /// Tolérance pour tests d’orthogonalité ou alignement
        /// </summary>
        public const double ORTHO_EPSILON = 1e-8;

        /// <summary>
        /// Facteur maximum de conditionnement pour matrices
        /// </summary>
        public const double CONDITION_NUMBER_MAX = 1e12;

        // ----------------------------
        // Paramètres physiques / géométriques (si nécessaire)
        // ----------------------------

    }
}
