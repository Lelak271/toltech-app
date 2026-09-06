using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using Toltech.App.Models;
using Toltech.Solver.Contracts;

namespace Toltech.App.Converters
{
    public class LinkageToImageConverter : IValueConverter
    {
        private static readonly Dictionary<LinkageType, string> ImageMap = new()
        {
            [LinkageType.PointContact] = "/Asset/Linkages/ponctuelle.png",
            [LinkageType.LinearContact] = "/Asset/Linkages/Lineaire_Rectiligne.png",
            [LinkageType.AnnularContact] = "/Asset/Linkages/Lineaire_Annulaire.png",
            [LinkageType.PlanarContact] = "/Asset/Linkages/Appui_Plan.png",
            [LinkageType.RevoluteContact] = "/Asset/Linkages/Pivot.png",
            [LinkageType.PrismaticContact] = "/Asset/Linkages/Glissiere.png",
            [LinkageType.CylindricalContact] = "/Asset/Linkages/Pivot_Glissant.png",
            [LinkageType.SphericalContact] = "/Asset/Linkages/Rotule.png",
            [LinkageType.FixedContact] = "/Asset/Linkages/Encastrement.png",
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is LinkageType linkage && ImageMap.TryGetValue(linkage, out var path)
                ? path
                : "/Asset/Liaisons/Default.png";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class LinkageToDescriptionConverter : IValueConverter
    {
        private static readonly Dictionary<LinkageType, string> DescriptionMap = new()
        {
            [LinkageType.PointContact] = "Liaison ponctuelle : contact en un point, 5 degrés de liberté.",
            [LinkageType.LinearContact] = "Liaison linéaire rectiligne : contact suivant une ligne droite.",
            [LinkageType.AnnularContact] = "Liaison linéaire annulaire : contact suivant un cercle.",
            [LinkageType.PlanarContact] = "Appui plan : contact suivant une surface plane, 3 degrés de liberté.",
            [LinkageType.RevoluteContact] = "Liaison pivot : rotation autour d'un axe unique.",
            [LinkageType.PrismaticContact] = "Liaison glissière : translation suivant un axe unique.",
            [LinkageType.CylindricalContact] = "Liaison pivot glissant : rotation + translation sur le même axe.",
            [LinkageType.SphericalContact] = "Liaison rotule : rotation libre autour d'un point.",
            [LinkageType.FixedContact] = "Encastrement : aucun degré de liberté, liaison rigide totale.",
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is LinkageType linkage && DescriptionMap.TryGetValue(linkage, out var text)
                ? text
                : string.Empty;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}