using System.Globalization;

namespace TOLTECH_APPLICATION.Utilities
{
    public static class ConversionHelper
    {
        /// <summary>
        /// Convertit une chaîne en double, acceptant les virgules ou points, puis convertit en invariant (avec point).
        /// </summary>
        public static double TryParseInvariant(string input, double defaultValue = 0.0)
        {
            if (string.IsNullOrWhiteSpace(input))
                return defaultValue;

            // Remplacer la virgule par un point pour uniformiser
            string normalized = input.Replace(',', '.');

            // Utiliser CultureInfo.InvariantCulture pour garantir le point comme séparateur
            return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double result)
                ? result
                : defaultValue;
        }
    }
}
