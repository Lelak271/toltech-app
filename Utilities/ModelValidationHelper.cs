using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using TOLTECH_APPLICATION.Services;

namespace TOLTECH_APPLICATION.Utilities
{
    /// <summary>
    /// Contient des helpers pour la validation des données métier dans l’application Toltech.
    /// Fournit des méthodes statiques pour vérifier la présence d’un modèle actif,
    /// d’une pièce active, et pour valider la syntaxe des noms de pièces.
    /// </summary>

    internal class ModelValidationHelper
    {
        public static bool CheckModelActif(bool msg = true)
        {
            if (string.IsNullOrEmpty(ModelManager.ModelActif))
            {
                if (msg == true)
                {
                    MessageBox.Show("Aucun modèle sélectionné.", "Notification Toltech", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return false;
            }
            return true;
        }
    }

    public static class NameValidationHelper
    {
        /// <summary>
        /// Valide un nom de pièce en vérifiant les caractères interdits et le format général.
        /// Affiche un message d'erreur en cas de problème.
        /// </summary>
        /// <param name="nomPiece">Le nom de pièce à valider.</param>
        /// <returns>True si le nom est valide, False sinon.</returns>
        public static bool ValiderNomDePiece(string nomPiece)
        {
            if (string.IsNullOrWhiteSpace(nomPiece))
            {
                MessageBox.Show(
                    "Le nom de la pièce est vide ou ne contient que des espaces.\nVeuillez saisir un nom valide.",
                    "Nom de pièce invalide",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            // Supprimer les espaces de début et fin
            nomPiece = nomPiece.Trim();

            // Caractères interdits (selon Windows)
            char[] caracteresInterdits = Path.GetInvalidFileNameChars();

            if (nomPiece.Any(c => caracteresInterdits.Contains(c)))
            {
                string interdits = string.Join(" ", caracteresInterdits.Select(c => $"'{c}'"));
                MessageBox.Show(
                    $"Le nom de la pièce contient des caractères non autorisés :\n{interdits}\n\nVeuillez utiliser uniquement des lettres, chiffres, tirets (-) ou underscores (_).",
                    "Nom de pièce invalide",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            // Longueur maximale recommandée
            if (nomPiece.Length > 100)
            {
                MessageBox.Show(
                    "Le nom de la pièce est trop long (plus de 100 caractères).\nVeuillez le raccourcir.",
                    "Nom de pièce invalide",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            return true;
        }


        public static bool TryValidateName(string name, out string errorMessage, int maxLength = 100)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                errorMessage = "Le nom est vide ou ne contient que des espaces.";
                return false;
            }

            name = name.Trim();

            // Caractères interdits par Windows
            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (name.Any(c => invalidChars.Contains(c)))
            {
                string chars = string.Join(" ", invalidChars.Select(c => $"'{c}'"));
                errorMessage = $"Le nom contient des caractères interdits : {chars}";
                return false;
            }

            // Longueur maximale
            if (name.Length > maxLength)
            {
                errorMessage = $"Le nom est trop long (max {maxLength} caractères).";
                return false;
            }

            // Vérification supplémentaire optionnelle : nom réservé
            string[] reservedNames = { "CON", "PRN", "AUX", "NUL",
                                       "COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
                                       "LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9" };
            if (reservedNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                errorMessage = $"Le nom '{name}' est réservé par le système et ne peut pas être utilisé.";
                return false;
            }

            return true;
        }


    }

}
