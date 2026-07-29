using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toltech.ComputeEngine.Contracts;

namespace Toltech.App.Models.Mapping
{
    public static class ModelDataToleranceMapper
    {
        #region Linkages
        public static ComputeModelData ToCompute(this ModelData model)
        {
            return new ComputeModelData
            {
                Id = model.Id,

                Linkage = (LinkageType)model.Linkage,

                OriginePartId = model.OriginePartId,
                ExtremitePartId = model.ExtremitePartId,

                Active = model.Active,

                CoordX = model.CoordX,
                CoordY = model.CoordY,
                CoordZ = model.CoordZ,

                CoordU = model.CoordU,
                CoordV = model.CoordV,
                CoordW = model.CoordW,
                CoordU2 = model.CoordU2,
                CoordV2 = model.CoordV2,
                CoordW2 = model.CoordW2,

                Model = model.Model,

                N = model.GetN(),
                T1 = model.GetT1(),
                T2 = model.GetT2(),
                Rn = model.GetRn(),
                RT1 = model.GetRT1(),
                RT2 = model.GetRT2()
            };
        }

        public static ToleranceTriplet GetN(this ModelData m)
        {
            return CreateTriplet(
                m.NOriginValue,
                m.NOriginDescription,
                m.NOriginName,
                m.NOriginUseDatabase,
                m.NOriginToleranceId,

                m.NIntermediateValue,
                m.NIntermediateDescription,
                m.NIntermediateName,
                m.NIntermediateUseDatabase,
                m.NIntermediateToleranceId,

                m.NExtremityValue,
                m.NExtremityDescription,
                m.NExtremityName,
                m.NExtremityUseDatabase,
                m.NExtremityToleranceId);
        }

        public static ToleranceTriplet GetT1(this ModelData m)
        {
            return CreateTriplet(
                m.T1OriginValue,
                m.T1OriginDescription,
                m.T1OriginName,
                m.T1OriginUseDatabase,
                m.T1OriginToleranceId,

                m.T1IntermediateValue,
                m.T1IntermediateDescription,
                m.T1IntermediateName,
                m.T1IntermediateUseDatabase,
                m.T1IntermediateToleranceId,

                m.T1ExtremityValue,
                m.T1ExtremityDescription,
                m.T1ExtremityName,
                m.T1ExtremityUseDatabase,
                m.T1ExtremityToleranceId);
        }

        public static ToleranceTriplet GetT2(this ModelData m)
        {
            return CreateTriplet(
                m.T2OriginValue,
                m.T2OriginDescription,
                m.T2OriginName,
                m.T2OriginUseDatabase,
                m.T2OriginToleranceId,

                m.T2IntermediateValue,
                m.T2IntermediateDescription,
                m.T2IntermediateName,
                m.T2IntermediateUseDatabase,
                m.T2IntermediateToleranceId,

                m.T2ExtremityValue,
                m.T2ExtremityDescription,
                m.T2ExtremityName,
                m.T2ExtremityUseDatabase,
                m.T2ExtremityToleranceId);
        }

        public static ToleranceTriplet GetRn(this ModelData m)
        {
            return CreateTriplet(
                m.RnOriginValue,
                m.RnOriginDescription,
                m.RnOriginName,
                m.RnOriginUseDatabase,
                m.RnOriginToleranceId,

                m.RnIntermediateValue,
                m.RnIntermediateDescription,
                m.RnIntermediateName,
                m.RnIntermediateUseDatabase,
                m.RnIntermediateToleranceId,

                m.RnExtremityValue,
                m.RnExtremityDescription,
                m.RnExtremityName,
                m.RnExtremityUseDatabase,
                m.RnExtremityToleranceId);
        }

        public static ToleranceTriplet GetRT1(this ModelData m)
        {
            return CreateTriplet(
                m.RT1OriginValue,
                m.RT1OriginDescription,
                m.RT1OriginName,
                m.RT1OriginUseDatabase,
                m.RT1OriginToleranceId,

                m.RT1IntermediateValue,
                m.RT1IntermediateDescription,
                m.RT1IntermediateName,
                m.RT1IntermediateUseDatabase,
                m.RT1IntermediateToleranceId,

                m.RT1ExtremityValue,
                m.RT1ExtremityDescription,
                m.RT1ExtremityName,
                m.RT1ExtremityUseDatabase,
                m.RT1ExtremityToleranceId);
        }

        public static ToleranceTriplet GetRT2(this ModelData m)
        {
            return CreateTriplet(
                m.RT2OriginValue,
                m.RT2OriginDescription,
                m.RT2OriginName,
                m.RT2OriginUseDatabase,
                m.RT2OriginToleranceId,

                m.RT2IntermediateValue,
                m.RT2IntermediateDescription,
                m.RT2IntermediateName,
                m.RT2IntermediateUseDatabase,
                m.RT2IntermediateToleranceId,

                m.RT2ExtremityValue,
                m.RT2ExtremityDescription,
                m.RT2ExtremityName,
                m.RT2ExtremityUseDatabase,
                m.RT2ExtremityToleranceId);
        }

        private static ToleranceTriplet CreateTriplet(
            double originValue,
            string originDescription,
            string originName,
            bool originUseDatabase,
            int originToleranceId,

            double intermediateValue,
            string intermediateDescription,
            string intermediateName,
            bool intermediateUseDatabase,
            int intermediateToleranceId,

            double extremityValue,
            string extremityDescription,
            string extremityName,
            bool extremityUseDatabase,
            int extremityToleranceId)
        {
            return new ToleranceTriplet
            {
                Origin = new ToleranceDefinition
                {
                    Value = originValue,
                    Description = originDescription,
                    Name = originName,
                    UseDatabase = originUseDatabase,
                    ToleranceId = originToleranceId
                },

                Intermediate = new ToleranceDefinition
                {
                    Value = intermediateValue,
                    Description = intermediateDescription,
                    Name = intermediateName,
                    UseDatabase = intermediateUseDatabase,
                    ToleranceId = intermediateToleranceId
                },

                Extremity = new ToleranceDefinition
                {
                    Value = extremityValue,
                    Description = extremityDescription,
                    Name = extremityName,
                    UseDatabase = extremityUseDatabase,
                    ToleranceId = extremityToleranceId
                }
            };
        }

        #endregion

        #region Requirement

        public static ComputeRequirement ToCompute(this Requirements req)
        {
            return new ComputeRequirement
            {
                Id_req = req.Id_req,
                NameReq = req.NameReq,

                CoordX = req.CoordX,
                CoordY = req.CoordY,
                CoordZ = req.CoordZ,

                CoordU = req.CoordU,
                CoordV = req.CoordV,
                CoordW = req.CoordW,

                //CoordU2 = req.CoordU2,
                //CoordV2 = req.CoordV2,
                //CoordW2 = req.CoordW2,

                Tol1 = req.tol1,
                Tol2 = req.tol2,

                IdTol1 = req.Id_tol1,
                IdTol2 = req.Id_tol2,

                CheckBox1 = req.CheckBox1,
                CheckBox2 = req.CheckBox2,

                PartReq1Id = req.PartReq1Id,
                PartReq2Id = req.PartReq2Id,

                Commentaire = req.Commentaire
            };
        }

        #endregion

        #region Part

        public static ComputePart ToCompute(this Part part)
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

        #endregion
    }
}
