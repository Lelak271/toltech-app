using Toltech.App.Models;
using Toltech.App.ToltechCalculation;
using Toltech.ComputeEngine.Contracts;

namespace Toltech.App.Services
{
    public static class ComputeMapper
    {
        public static List<ComputeModelData> ToComputeModelData(List<ModelData> input)
        {
            return input.Select(m => new ComputeModelData
            {
                Id = m.Id,
                OriginePartId = m.OriginePartId,
                ExtremitePartId = m.ExtremitePartId,
                Active = m.Active,

                CoordX = m.CoordX,
                CoordY = m.CoordY,
                CoordZ = m.CoordZ,

                CoordU = m.CoordU,
                CoordV = m.CoordV,
                CoordW = m.CoordW,

                TolOri = m.TolOri,
                TolInt = m.TolInt,
                TolExtr = m.TolExtr,

                IdTolOri = m.IdTolOri,
                IdTolInt = m.IdTolInt,
                IdTolExtre = m.IdTolExtre
            }).ToList();
        }

        public static List<ComputeRequirement> ToComputeRequirements(List<Requirements> input)
        {
            return input.Select(r => new ComputeRequirement
            {
                Id_req = r.Id_req,
                NameReq = r.NameReq,

                CoordX = r.CoordX,
                CoordY = r.CoordY,
                CoordZ = r.CoordZ,

                CoordU = r.CoordU,
                CoordV = r.CoordV,
                CoordW = r.CoordW,

                Tol1 = r.tol1,
                Tol2 = r.tol2,

                IdTol1 = r.Id_tol1,
                IdTol2 = r.Id_tol2,

                CheckBox1 = r.CheckBox1,
                CheckBox2 = r.CheckBox2,

                PartReq1Id = r.PartReq1Id,
                PartReq2Id = r.PartReq2Id,

                Commentaire = r.Commentaire
            }).ToList();
        }

        public static ComputePart ToComputePart(Part part)
        {
            if (part == null) return null;

            return new ComputePart
            {
                Id = part.Id,
                NamePart = part.NamePart,
                MasseVol = part.MasseVol,
                ImagePart = part.ImagePart,
                Comment = part.Comment,
                IsFixed = part.IsFixed,
                IsActive = part.IsActive
            };
        }

    }
}
