using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TOLTECH_APPLICATION.FrontEnd.Controls;

namespace TOLTECH_APPLICATION.Security
{
    class AccessControl
    {
        // Date limite de validité (exemple : 31 décembre 2025)
        private static readonly DateTime ExpirationDate = new DateTime(2025, 12, 31);

        // Activation du mot de passe (à false pour désactiver en release si souhaité)
        private static readonly bool RequirePassword = true;

        // Mot de passe défini en dur (à améliorer pour un vrai usage)
        private const string Password = "lp";

        /// <summary>
        /// Vérifie si l’accès est autorisé (date et mot de passe si activé).
        /// </summary>
        public static bool VerifyAccess()
        {
            // Vérification de la date de validité
            if (DateTime.Now > ExpirationDate)
            {
                System.Windows.MessageBox.Show(
                    "La période d’utilisation est expirée.",
                    "Accès refusé",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return false;
            }

            // Vérification du mot de passe uniquement si activé
            if (RequirePassword)
            {
                int maxAttempts = 3;
                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    var dialog = new PasswordWindow(); // fenêtre WPF personnalisée
                    bool? result = dialog.ShowDialog();

                    if (result == true && dialog.EnteredPassword == Password)
                    {
                        // Mot de passe correct
                        return true;
                    }

                    int remaining = maxAttempts - attempt;
                    string message = remaining > 0
                        ? $"Mot de passe incorrect. Il vous reste {remaining} tentative(s)."
                        : "Nombre maximum de tentatives atteint.";

                    System.Windows.MessageBox.Show(
                        message,
                        "Accès refusé",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }

                // Toutes les tentatives ont échoué
                return false;
            }

            // Si mot de passe non requis
            return true;
        }

    }
}
