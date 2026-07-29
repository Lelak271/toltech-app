using Toltech.App.Models;
using Toltech.App.ToltechCalculation;
using Toltech.ComputeEngine.Contracts;

namespace Toltech.App.Services
{
    public static class ComputeMapper
    {
        //public static List<ComputeModelData> ToComputeModelData(List<ModelData> input)
        //{
        //    return input.Select(m => new ComputeModelData
        //    {
        //        Id = m.Id,

        //        Linkage = (LinkageType)m.Linkage,

        //        OriginePartId = m.OriginePartId,
        //        ExtremitePartId = m.ExtremitePartId,
        //        Active = m.Active,

        //        CoordX = m.CoordX,
        //        CoordY = m.CoordY,
        //        CoordZ = m.CoordZ,

        //        CoordU = m.CoordU,
        //        CoordV = m.CoordV,
        //        CoordW = m.CoordW,

        //        CoordU2 = m.CoordU2,
        //        CoordV2 = m.CoordV2,
        //        CoordW2 = m.CoordW2,

        //        // TO DO
        //        // Pas de prise ne compte des tolérnaces pour le moment, à voir si on les mets dans le calcul ou pas
        //        // A mettre si le moteur de calcul les prends en compte, sinon on les mets dans le calcul UI
        //        // Creer une hiararchie de tolérances et ne pas garder la structure plate de tolérances dans le modeldata
        //        #region Proprietes de tolérances
        //        //#region N

        //        //NOriginValue = m.NOriginValue,
        //        //NIntermediateValue = m.NIntermediateValue,
        //        //NExtremityValue = m.NExtremityValue,

        //        //NOriginDescription = m.NOriginDescription,
        //        //NIntermediateDescription = m.NIntermediateDescription,
        //        //NExtremityDescription = m.NExtremityDescription,

        //        //NOriginName = m.NOriginName,
        //        //NIntermediateName = m.NIntermediateName,
        //        //NExtremityName = m.NExtremityName,
        //        //#endregion

        //        //#region T1

        //        //T1OriginValue = m.T1OriginValue,
        //        //T1IntermediateValue = m.T1IntermediateValue,
        //        //T1ExtremityValue = m.T1ExtremityValue,

        //        //T1OriginDescription = m.T1OriginDescription,
        //        //T1IntermediateDescription = m.T1IntermediateDescription,
        //        //T1ExtremityDescription = m.T1ExtremityDescription,
        //        //T1OriginName = m.T1OriginName,
        //        //T1IntermediateName = m.T1IntermediateName,
        //        //T1ExtremityName = m.T1ExtremityName,

        //        //#endregion

        //        //#region T2

        //        //T2OriginValue = m.T2OriginValue,
        //        //T2IntermediateValue = m.T2IntermediateValue,
        //        //T2ExtremityValue = m.T2ExtremityValue,

        //        //T2OriginDescription = m.T2OriginDescription,
        //        //T2IntermediateDescription = m.T2IntermediateDescription,
        //        //T2ExtremityDescription = m.T2ExtremityDescription,
        //        //T2OriginName = m.T2OriginName,
        //        //T2IntermediateName = m.T2IntermediateName,
        //        //T2ExtremityName = m.T2ExtremityName,

        //        //#endregion

        //        //#region Rn

        //        //RnOriginValue = m.RnOriginValue,
        //        //RnIntermediateValue = m.RnIntermediateValue,
        //        //RnExtremityValue = m.RnExtremityValue,

        //        //RnOriginDescription = m.RnOriginDescription,
        //        //RnIntermediateDescription = m.RnIntermediateDescription,
        //        //RnExtremityDescription = m.RnExtremityDescription,
        //        //RnOriginName = m.RnOriginName,
        //        //RnIntermediateName = m.RnIntermediateName,
        //        //RnExtremityName = m.RnExtremityName,

        //        //#endregion

        //        //#region RT1

        //        //RT1OriginValue = m.RT1OriginValue,
        //        //RT1IntermediateValue = m.RT1IntermediateValue,
        //        //RT1ExtremityValue = m.RT1ExtremityValue,

        //        //RT1OriginDescription = m.RT1OriginDescription,
        //        //RT1IntermediateDescription = m.RT1IntermediateDescription,
        //        //RT1ExtremityDescription = m.RT1ExtremityDescription,
        //        //RT1OriginName = m.RT1OriginName,
        //        //RT1IntermediateName = m.RT1IntermediateName,
        //        //RT1ExtremityName = m.RT1ExtremityName,

        //        //#endregion

        //        //#region RT2

        //        //RT2OriginValue = m.RT2OriginValue,
        //        //RT2IntermediateValue = m.RT2IntermediateValue,
        //        //RT2ExtremityValue = m.RT2ExtremityValue,

        //        //RT2OriginDescription = m.RT2OriginDescription,
        //        //RT2IntermediateDescription = m.RT2IntermediateDescription,
        //        //RT2ExtremityDescription = m.RT2ExtremityDescription,
        //        //RT2OriginName = m.RT2OriginName,
        //        //RT2IntermediateName = m.RT2IntermediateName,
        //        //RT2ExtremityName = m.RT2ExtremityName,

        //        //#endregion
        //        #endregion

        //    }).ToList();
        //}

        //public static List<ComputeRequirement> ToComputeRequirements(List<Requirements> input)
        //{
        //    return input.Select(r => new ComputeRequirement
        //    {
        //        Id_req = r.Id_req,
        //        NameReq = r.NameReq,

        //        CoordX = r.CoordX,
        //        CoordY = r.CoordY,
        //        CoordZ = r.CoordZ,

        //        CoordU = r.CoordU,
        //        CoordV = r.CoordV,
        //        CoordW = r.CoordW,

        //        //CoordU2 = r.CoordU2,
        //        //CoordV2 = r.CoordV2,
        //        //CoordW2 = r.CoordW2,

        //        Tol1 = r.tol1,
        //        Tol2 = r.tol2,

        //        IdTol1 = r.Id_tol1,
        //        IdTol2 = r.Id_tol2,

        //        CheckBox1 = r.CheckBox1,
        //        CheckBox2 = r.CheckBox2,

        //        PartReq1Id = r.PartReq1Id,
        //        PartReq2Id = r.PartReq2Id,

        //        Commentaire = r.Commentaire
        //    }).ToList();
        //}

        //public static ComputePart ToComputePart(Part part)
        //{
        //    if (part == null) return null;

        //    return new ComputePart
        //    {
        //        Id = part.Id,
        //        NamePart = part.NamePart,
        //        MasseVol = part.MasseVol,
        //        ImagePart = part.ImagePart,
        //        Comment = part.Comment,
        //        IsFixed = part.IsFixed,
        //        IsActive = part.IsActive
        //    };
        //}

    }
}
