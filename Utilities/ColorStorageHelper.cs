using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using System.Drawing;

namespace TOLTECH_APPLICATION.Utilities
{
    // <summary>
    /// Utilitaires pour convertir et stocker des couleurs entre SKColor et System.Drawing.Color (ARGB int).
    /// </summary>
    public static class ColorStorageHelper
    {
        /// <summary>
        /// Convertit un SKColor en int ARGB (compatibilité Settings).
        /// </summary>
        public static int ArgbFromSKColor(SKColor sk)
        {
            // SKColor channels sont bytes (Red, Green, Blue, Alpha)
            Color d = Color.FromArgb(sk.Alpha, sk.Red, sk.Green, sk.Blue);
            return d.ToArgb(); // int ARGB
        }

        /// <summary>
        /// Reconstruit un SKColor à partir d'un int ARGB (lu depuis les Settings).
        /// </summary>
        public static SKColor SKColorFromArgb(int argb)
        {
            Color d = Color.FromArgb(argb);
            return new SKColor(d.R, d.G, d.B, d.A);
        }

        /// <summary>
        /// Convertit un System.Drawing.Color en SKColor.
        /// </summary>
        public static SKColor SKColorFromDrawingColor(Color d)
            => new SKColor(d.R, d.G, d.B, d.A);

        /// <summary>
        /// Convertit un SKColor en System.Drawing.Color.
        /// </summary>
        public static Color DrawingColorFromSKColor(SKColor sk)
            => Color.FromArgb(sk.Alpha, sk.Red, sk.Green, sk.Blue);

        /// <summary>
        /// Lit un int ARGB depuis les Settings et retourne un SKColor, avec fallback sécurisé.
        /// </summary>
        public static SKColor LoadSKColorFromSettings(Func<int> readArgbFunc, SKColor fallback)
        {
            try
            {
                int argb = readArgbFunc();
                return SKColorFromArgb(argb);
            }
            catch
            {
                return fallback;
            }
        }
    }
}
