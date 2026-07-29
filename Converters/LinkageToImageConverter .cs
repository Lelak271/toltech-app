using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using Toltech.App.Models;

namespace Toltech.App.Converters
{
    public class LinkageToImageConverter : IValueConverter
    {
        private static readonly Dictionary<ModelData.LiaisonType, string> ImageMap = new()
        {
            [ModelData.LiaisonType.PointContact] = "/Asset/Linkages/ponctuelle.png",
            [ModelData.LiaisonType.LinearContact] = "/Asset/Linkages/Lineaire_Rectiligne.png",
            [ModelData.LiaisonType.AnnularContact] = "/Asset/Linkages/Lineaire_Annulaire.png",
            [ModelData.LiaisonType.PlanarContact] = "/Asset/Linkages/Appui_Plan.png",
            [ModelData.LiaisonType.RevoluteContact] = "/Asset/Linkages/Pivot.png",
            [ModelData.LiaisonType.PrismaticContact] = "/Asset/Linkages/Glissiere.png",
            [ModelData.LiaisonType.CylindricalContact] = "/Asset/Linkages/Pivot_Glissant.png",
            [ModelData.LiaisonType.SphericalContact] = "/Asset/Linkages/Rotule.png",
            [ModelData.LiaisonType.FixedContact] = "/Asset/Linkages/Encastrement.png",
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is ModelData.LiaisonType linkage && ImageMap.TryGetValue(linkage, out var path)
                ? path
                : "/Asset/Liaisons/Default.png";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class LinkageToDescriptionConverter : IValueConverter
    {
        private static readonly Dictionary<ModelData.LiaisonType, string> DescriptionMap = new()
        {
            [ModelData.LiaisonType.PointContact] = "Liaison ponctuelle : contact en un point, 5 degrés de liberté.",
            [ModelData.LiaisonType.LinearContact] = "Liaison linéaire rectiligne : contact suivant une ligne droite.",
            [ModelData.LiaisonType.AnnularContact] = "Liaison linéaire annulaire : contact suivant un cercle.",
            [ModelData.LiaisonType.PlanarContact] = "Appui plan : contact suivant une surface plane, 3 degrés de liberté.",
            [ModelData.LiaisonType.RevoluteContact] = "Liaison pivot : rotation autour d'un axe unique.",
            [ModelData.LiaisonType.PrismaticContact] = "Liaison glissière : translation suivant un axe unique.",
            [ModelData.LiaisonType.CylindricalContact] = "Liaison pivot glissant : rotation + translation sur le même axe.",
            [ModelData.LiaisonType.SphericalContact] = "Liaison rotule : rotation libre autour d'un point.",
            [ModelData.LiaisonType.FixedContact] = "Encastrement : aucun degré de liberté, liaison rigide totale.",
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is ModelData.LiaisonType linkage && DescriptionMap.TryGetValue(linkage, out var text)
                ? text
                : string.Empty;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}