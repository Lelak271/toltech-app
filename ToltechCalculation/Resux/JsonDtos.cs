using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Toltech.App.Converters;
using Toltech.App.Models;
using Toltech.App.Services;
using Toltech.Solver.Contracts;

namespace Toltech.App.ToltechCalculation.Resux
{
    #region  DTOs JSON — classes de sérialisation/désérialisation

    public class ResultFileJson
    {
        [JsonPropertyName("projet")] public string Projet { get; set; }
        [JsonPropertyName("utilisateur")] public string UserName { get; set; }
        [JsonPropertyName("version")] public string Version { get; set; }
        [JsonPropertyName("typeCalcul")] public string TypeCalcul { get; set; }
        [JsonPropertyName("resultats")] public List<ResultsForReqJson> Resultats { get; set; } = new();
    }

    public class ResultsForReqJson
    {
        [JsonPropertyName("idReq")] public int IdReq { get; set; }
        [JsonPropertyName("nameReq")] public string NameReq { get; set; }
        [JsonPropertyName("coordU")] public double CoordU { get; set; }
        [JsonPropertyName("coordV")] public double CoordV { get; set; }
        [JsonPropertyName("coordW")] public double CoordW { get; set; }
        [JsonPropertyName("namePart1")] public string NamePart1 { get; set; }
        [JsonPropertyName("namePart2")] public string NamePart2 { get; set; }
        [JsonPropertyName("targetWC")] public double TargetWC { get; set; }
        [JsonPropertyName("targetSTAT")] public double TargetSTAT { get; set; }
        [JsonPropertyName("linkages")] public List<ResultEachDataJson> Linkages { get; set; } = new();
    }

    public class ResultEachDataJson
    {
        [JsonPropertyName("idData")] public int IdData { get; set; }
        [JsonPropertyName("nameData")] public string NameData { get; set; }
        [JsonPropertyName("nameOri")] public string NameOri { get; set; }
        [JsonPropertyName("nameExtre")] public string NameExtre { get; set; }

        [JsonPropertyName("N")] public ToleranceTripletJson N { get; set; }
        [JsonPropertyName("T1")] public ToleranceTripletJson T1 { get; set; }
        [JsonPropertyName("T2")] public ToleranceTripletJson T2 { get; set; }
        [JsonPropertyName("Rn")] public ToleranceTripletJson Rn { get; set; }
        [JsonPropertyName("RT1")] public ToleranceTripletJson RT1 { get; set; }
        [JsonPropertyName("RT2")] public ToleranceTripletJson RT2 { get; set; }


    }

    public class ToleranceTripletJson
    {
        [JsonPropertyName("origin")] public ToleranceDefinitionJson Origin { get; set; }
        [JsonPropertyName("intermediate")] public ToleranceDefinitionJson Intermediate { get; set; }
        [JsonPropertyName("extremity")] public ToleranceDefinitionJson Extremity { get; set; }
    }

    public class ToleranceDefinitionJson
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("comment")] public string Comment { get; set; }
        [JsonPropertyName("value")] public double Value { get; set; }

        [JsonPropertyName("inflX")] public double InflX { get; set; }
        [JsonPropertyName("inflY")] public double InflY { get; set; }
        [JsonPropertyName("inflZ")] public double InflZ { get; set; }
    }

    #endregion

    // =========================================================================
    // ResuxSerializer — sérialisation et lecture des fichiers .resux (JSON)
    // =========================================================================

    public class ResuxSerializer
    {
        #region Modèles internes (consommés par l'UI)

        public class ResultFileMetadata
        {
            public string Projet { get; set; }
            public string USerName { get; set; }
            public string VersionToltech { get; set; }
            public string TypeCalcul { get; set; }
            public string Format { get; set; }
            public string Separator { get; set; }
        }
        public class ResultsForReq
        {
            public int IdReq { get; set; }
            public string NameReq { get; set; }
            public double CoordU { get; set; }
            public double CoordV { get; set; }
            public double CoordW { get; set; }
            public string NamePart1 { get; set; }
            public string NamePart2 { get; set; }
            public double TargetWC { get; set; }
            public double TargetSTAT { get; set; }

            public List<ResultEachData> Linkages { get; set; } = new();
        }
        public class ResultEachData
        {
            public int IdData { get; set; }
            public string NameOri { get; set; }
            public string NameExtre { get; set; }
            public string NameData { get; set; }

            public ToleranceTriplet N { get; set; } = new();
            public ToleranceTriplet T1 { get; set; } = new();
            public ToleranceTriplet T2 { get; set; } = new();
            public ToleranceTriplet Rn { get; set; } = new();
            public ToleranceTriplet RT1 { get; set; } = new();
            public ToleranceTriplet RT2 { get; set; } = new();

            // Construit lors de la lecture
            public double InfluencWCN { get; set; }
            public double InfluencWCT1 { get; set; }
            public double InfluencWCT2 { get; set; }
            public double RotationInfluencWCN { get; set; }
            public double RotationInfluencWCRT1 { get; set; }
            public double RotationInfluencWCRT2 { get; set; }
            public double GlobalContribWCOri { get; set; }
            public double GlobalContribWCInt { get; set; }
            public double GlobalContribWCExtr { get; set; }
        }
        public class ToleranceTriplet
        {
            public ToleranceDefinition Origin { get; set; }
            public ToleranceDefinition Intermediate { get; set; }
            public ToleranceDefinition Extremity { get; set; }
        }
        public class ToleranceDefinition
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public string? Comment { get; set; }
            public double Value { get; set; }
            public string? ValueRaw { get; set; }

            public double InflX { get; set; }
            public double InflY { get; set; }
            public double InflZ { get; set; }

        }


        #endregion

        #region Options JSON partagées

        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true
        };

        private static readonly JsonSerializerOptions ReadOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        #endregion

        #region Écriture

        public async Task WriteResultsToFileV3Async(
            ComputeResult AllResults,
            List<Requirements> ReqCompute)
        {
            string nameModel = Path.GetFileNameWithoutExtension(ModelManager.ModelActif);
            string folderPath = ModelManager.GetTolTechTempPath();
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filePath = Path.Combine(folderPath, $"ResultsUI_{nameModel}_{timestamp}.resux");

            var converter = new IdToNameConverter();
            var rng = new Random();

            var root = new ResultFileJson
            {
                Projet = nameModel,
                UserName = Environment.UserName,
                Version = "1.0.0",
                TypeCalcul = "OneMatrix",
            };

            foreach (var (idReq, reqResult) in AllResults.ResultsNew)
            {
                var requirement = ReqCompute.FirstOrDefault(r => r.Id_req == idReq);

                double v1 = rng.NextDouble() * 20;
                double v2 = rng.NextDouble() * 20;

                var reqDto = new ResultsForReqJson
                {
                    IdReq = idReq,
                    NameReq = requirement.NameReq,
                    CoordU = requirement.CoordU,
                    CoordV = requirement.CoordV,
                    CoordW = requirement.CoordW,
                    NamePart1 = converter.Convert(requirement.PartReq1Id, typeof(string), null, CultureInfo.CurrentCulture) as string,
                    NamePart2 = converter.Convert(requirement.PartReq2Id, typeof(string), null, CultureInfo.CurrentCulture) as string,
                    TargetWC = Math.Max(v1, v2),
                    TargetSTAT = Math.Min(v1, v2),
                };

                // Regrouper les DecompositionResults par IdData,
                // puis indexer par Type pour récupérer les influences rapidement
                var byIdData = reqResult.Details
                    .GroupBy(d => d.IdData)
                    .ToDictionary(
                        g => g.Key,
                        g => g.ToDictionary(d => d.Type));

                foreach (var (idData, byType) in byIdData)
                {
                    var md = await DatabaseService.ActiveInstance.GetModelDataByIdAsync(idData);
                    if (md == null) continue;

                    // Helper local : récupère (InflX, InflY, InflZ) pour un type donné,
                    // retourne (0,0,0) si le type est absent pour cet idData
                    (double X, double Y, double Z) Infl(UnknownType type) =>
                        byType.TryGetValue(type, out var d) ? (d.InflX, d.InflY, d.InflZ) : (0, 0, 0);

                    var inflN = Infl(UnknownType.Tu);
                    var inflT1 = Infl(UnknownType.Tv);
                    var inflT2 = Infl(UnknownType.Tw);
                    var inflRn = Infl(UnknownType.RotA);
                    var inflRT1 = Infl(UnknownType.RotB);
                    var inflRT2 = Infl(UnknownType.RotC);
                    // FAUX N pas forcement egal a Tu mais depend de la liaison putain

                    reqDto.Linkages.Add(new ResultEachDataJson
                    {
                        IdData = idData,
                        NameData = md.Model,
                        NameOri = converter.Convert(md.OriginePartId, typeof(string), null, CultureInfo.CurrentCulture) as string,
                        NameExtre = converter.Convert(md.ExtremitePartId, typeof(string), null, CultureInfo.CurrentCulture) as string,

                        N = MakeTriplet(
                            md.NOriginToleranceId, md.NOriginName, md.NOriginDescription, md.NOriginValue,
                            md.NIntermediateToleranceId, md.NIntermediateName, md.NIntermediateDescription, md.NIntermediateValue,
                            md.NExtremityToleranceId, md.NExtremityName, md.NExtremityDescription, md.NExtremityValue,
                            inflN.X, inflN.Y, inflN.Z),

                        T1 = MakeTriplet(
                            md.T1OriginToleranceId, md.T1OriginName, md.T1OriginDescription, md.T1OriginValue,
                            md.T1IntermediateToleranceId, md.T1IntermediateName, md.T1IntermediateDescription, md.T1IntermediateValue,
                            md.T1ExtremityToleranceId, md.T1ExtremityName, md.T1ExtremityDescription, md.T1ExtremityValue,
                            inflT1.X, inflT1.Y, inflT1.Z),

                        T2 = MakeTriplet(
                            md.T2OriginToleranceId, md.T2OriginName, md.T2OriginDescription, md.T2OriginValue,
                            md.T2IntermediateToleranceId, md.T2IntermediateName, md.T2IntermediateDescription, md.T2IntermediateValue,
                            md.T2ExtremityToleranceId, md.T2ExtremityName, md.T2ExtremityDescription, md.T2ExtremityValue,
                            inflT2.X, inflT2.Y, inflT2.Z),

                        Rn = MakeTriplet(
                            md.RnOriginToleranceId, md.RnOriginName, md.RnOriginDescription, md.RnOriginValue,
                            md.RnIntermediateToleranceId, md.RnIntermediateName, md.RnIntermediateDescription, md.RnIntermediateValue,
                            md.RnExtremityToleranceId, md.RnExtremityName, md.RnExtremityDescription, md.RnExtremityValue,
                            inflRn.X, inflRn.Y, inflRn.Z),

                        RT1 = MakeTriplet(
                            md.RT1OriginToleranceId, md.RT1OriginName, md.RT1OriginDescription, md.RT1OriginValue,
                            md.RT1IntermediateToleranceId, md.RT1IntermediateName, md.RT1IntermediateDescription, md.RT1IntermediateValue,
                            md.RT1ExtremityToleranceId, md.RT1ExtremityName, md.RT1ExtremityDescription, md.RT1ExtremityValue,
                            inflRT1.X, inflRT1.Y, inflRT1.Z),

                        RT2 = MakeTriplet(
                            md.RT2OriginToleranceId, md.RT2OriginName, md.RT2OriginDescription, md.RT2OriginValue,
                            md.RT2IntermediateToleranceId, md.RT2IntermediateName, md.RT2IntermediateDescription, md.RT2IntermediateValue,
                            md.RT2ExtremityToleranceId, md.RT2ExtremityName, md.RT2ExtremityDescription, md.RT2ExtremityValue,
                            inflRT2.X, inflRT2.Y, inflRT2.Z),
                    });
                }

                root.Resultats.Add(reqDto);
            }

            string json = JsonSerializer.Serialize(root, WriteOptions);
            await File.WriteAllTextAsync(filePath, json);
            ModelManager.FilePathResx = filePath;
        }
        // -----------------------------------------------------------------------
        // Helpers privés
        // -----------------------------------------------------------------------

        private static ToleranceTripletJson MakeTriplet(
            int idOri, string nameOri, string commentOri, double valOri,
            int idInt, string nameInt, string commentInt, double valInt,
            int idExtr, string nameExtr, string commentExtr, double valExtr,
            double inflX, double inflY, double inflZ) => new()
            {
                Origin = MakeDef(idOri, nameOri, commentOri, valOri, inflX, inflY, inflZ),
                Intermediate = MakeDef(idInt, nameInt, commentInt, valInt, inflX, inflY, inflZ),
                Extremity = MakeDef(idExtr, nameExtr, commentExtr, valExtr, inflX, inflY, inflZ),
            };

        private static ToleranceDefinitionJson MakeDef(
            int id, string name, string comment, double value,
            double inflX, double inflY, double inflZ) => new()
            {
                Id = id.ToString(),
                Name = name,
                Comment = comment,
                Value = value,
                InflX = inflX,
                InflY = inflY,
                InflZ = inflZ,
            };
        #endregion

        #region Lecture

        public ResultsForReq LoadInfluencedWCFromFile(
            int targetIdReq,
            string filePath,
            double Ucoord = 0, double Vcoord = 0, double Wcoord = 0)
        {
            var result = new ResultsForReq { IdReq = targetIdReq };

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return result;

            var root = JsonSerializer.Deserialize<ResultFileJson>(File.ReadAllText(filePath), ReadOptions);
            var reqJson = root?.Resultats.FirstOrDefault(r => r.IdReq == targetIdReq);
            if (reqJson == null) return result;

            // Direction : libre si fournie, sinon direction de l'exigence
            double freeNorm = Math.Sqrt(Ucoord * Ucoord + Vcoord * Vcoord + Wcoord * Wcoord);
            double dirU = freeNorm > 0 ? Ucoord : reqJson.CoordU;
            double dirV = freeNorm > 0 ? Vcoord : reqJson.CoordV;
            double dirW = freeNorm > 0 ? Wcoord : reqJson.CoordW;

            result.NameReq = reqJson.NameReq;
            result.CoordU = reqJson.CoordU;
            result.CoordV = reqJson.CoordV;
            result.CoordW = reqJson.CoordW;
            result.NamePart1 = reqJson.NamePart1;
            result.NamePart2 = reqJson.NamePart2;
            result.TargetWC = reqJson.TargetWC;
            result.TargetSTAT = reqJson.TargetSTAT;

            result.Linkages = reqJson.Linkages.Select(d =>
            {
                // Mappe un ToleranceTripletJson → ToleranceTriplet (modèle interne)
                ToleranceTriplet MapTriplet(ToleranceTripletJson t) => new()
                {
                    Origin = MapDef(t?.Origin),
                    Intermediate = MapDef(t?.Intermediate),
                    Extremity = MapDef(t?.Extremity),
                };

                // Calcule l'influence WC pour chaque position d'un triplet
                double InflWC(ToleranceDefinitionJson def) =>
                    ComputeInfluenceWC(def?.InflX ?? 0, def?.InflY ?? 0, def?.InflZ ?? 0, dirU, dirV, dirW);

                // Somme absolue des influences WC sur tous les axes et toutes les positions
                double SumTripletInfl(ToleranceTripletJson t) =>
                    Math.Abs(InflWC(t?.Origin));

                var allTriplets = new[] { d.N, d.T1, d.T2, d.Rn, d.RT1, d.RT2 };

                var translationTriplets = new[] { d.N, d.T1, d.T2 };
                var rotationTriplets = new[] { d.Rn, d.RT1, d.RT2 };

                double globalInfluencWCN = SumTripletInfl(d.N);
                double globalInfluencWCT1 = SumTripletInfl(d.T1);
                double globalInfluencWCT2 = SumTripletInfl(d.T2);
                double globalRotationInfluencWCRn = SumTripletInfl(d.Rn);
                double globalRotationInfluencWCRT1 = SumTripletInfl(d.RT1);
                double globalRotationInfluencWCRT2 = SumTripletInfl(d.RT2);

                return new ResultEachData
                {
                    IdData = d.IdData,
                    NameData = d.NameData,
                    NameOri = d.NameOri,
                    NameExtre = d.NameExtre,

                    N = MapTriplet(d.N),
                    T1 = MapTriplet(d.T1),
                    T2 = MapTriplet(d.T2),
                    Rn = MapTriplet(d.Rn),
                    RT1 = MapTriplet(d.RT1),
                    RT2 = MapTriplet(d.RT2),

                    InfluencWCN = globalInfluencWCN,
                    InfluencWCT1 = globalInfluencWCT1,
                    InfluencWCT2 = globalInfluencWCT2,
                    RotationInfluencWCN = globalRotationInfluencWCRn,
                    RotationInfluencWCRT1 = globalRotationInfluencWCRT1,
                    RotationInfluencWCRT2 = globalRotationInfluencWCRT2,


                    GlobalContribWCOri =
      Math.Abs((translationTriplets[0]?.Origin?.Value ?? 0) * globalInfluencWCN)
    + Math.Abs((translationTriplets[1]?.Origin?.Value ?? 0) * globalInfluencWCT1)
    + Math.Abs((translationTriplets[2]?.Origin?.Value ?? 0) * globalInfluencWCT2)
    + Math.Abs((rotationTriplets[0]?.Origin?.Value ?? 0) * globalRotationInfluencWCRn)
    + Math.Abs((rotationTriplets[1]?.Origin?.Value ?? 0) * globalRotationInfluencWCRT1)
    + Math.Abs((rotationTriplets[2]?.Origin?.Value ?? 0) * globalRotationInfluencWCRT2),

                    GlobalContribWCInt =
      Math.Abs((translationTriplets[0]?.Intermediate?.Value ?? 0) * globalInfluencWCN)
    + Math.Abs((translationTriplets[1]?.Intermediate?.Value ?? 0) * globalInfluencWCT1)
    + Math.Abs((translationTriplets[2]?.Intermediate?.Value ?? 0) * globalInfluencWCT2)
    + Math.Abs((rotationTriplets[0]?.Intermediate?.Value ?? 0) * globalRotationInfluencWCRn)
    + Math.Abs((rotationTriplets[1]?.Intermediate?.Value ?? 0) * globalRotationInfluencWCRT1)
    + Math.Abs((rotationTriplets[2]?.Intermediate?.Value ?? 0) * globalRotationInfluencWCRT2),

                    GlobalContribWCExtr =
      Math.Abs((translationTriplets[0]?.Extremity?.Value ?? 0) * globalInfluencWCN)
    + Math.Abs((translationTriplets[1]?.Extremity?.Value ?? 0) * globalInfluencWCT1)
    + Math.Abs((translationTriplets[2]?.Extremity?.Value ?? 0) * globalInfluencWCT2)
    + Math.Abs((rotationTriplets[0]?.Extremity?.Value ?? 0) * globalRotationInfluencWCRn)
    + Math.Abs((rotationTriplets[1]?.Extremity?.Value ?? 0) * globalRotationInfluencWCRT1)
    + Math.Abs((rotationTriplets[2]?.Extremity?.Value ?? 0) * globalRotationInfluencWCRT2),
                };
            }).ToList();

            return result;
        }

        public HashSet<int> GetAllReqIdsFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return new HashSet<int>();

            var root = JsonSerializer.Deserialize<ResultFileJson>(File.ReadAllText(filePath), ReadOptions);
            return root?.Resultats.Select(r => r.IdReq).ToHashSet() ?? new HashSet<int>();
        }

        public ResultFileMetadata ExtractMetadataFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            var root = JsonSerializer.Deserialize<ResultFileJson>(File.ReadAllText(filePath), ReadOptions);
            if (root == null) return null;

            return new ResultFileMetadata
            {
                Projet = root.Projet,
                USerName = root.UserName,
                VersionToltech = root.Version,
                TypeCalcul = root.TypeCalcul,
                Format = "JSON (.resux)",
                Separator = "N/A",
            };
        }

        #endregion

        #region Helpers privés

        private static ToleranceDefinition MapDef(ToleranceDefinitionJson? dto) => new()
        {
            Id = dto?.Id,
            Name = dto?.Name,
            Comment = dto?.Comment,
            Value = dto?.Value ?? 0,
            ValueRaw = dto?.Value.ToString("F4", CultureInfo.InvariantCulture),
            InflX = dto?.InflX ?? 0,
            InflY = dto?.InflY ?? 0,
            InflZ = dto?.InflZ ?? 0,
        };

        #endregion

        #region Helpers privés

        /// <summary>
        /// Projection signée du vecteur d'influence sur la direction unitaire (u, v, w).
        /// </summary>
        public static double ComputeInfluenceWC(
            double inflX, double inflY, double inflZ,
            double dirU, double dirV, double dirW)
        {
            if (double.IsNaN(inflX) || double.IsNaN(inflY) || double.IsNaN(inflZ) ||
                double.IsNaN(dirU) || double.IsNaN(dirV) || double.IsNaN(dirW))
                return 0.0;

            double dirNorm = Math.Sqrt(dirU * dirU + dirV * dirV + dirW * dirW);
            if (Math.Abs(dirNorm) < Constants.EPSILON) return 0.0;

            return (inflX * dirU + inflY * dirV + inflZ * dirW) / dirNorm;
        }


        public List<(int IdReq, string Name)> ExtractReqHeaders(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return new List<(int, string)>();

            var root = JsonSerializer.Deserialize<ResultFileJson>(File.ReadAllText(filePath), ReadOptions);

            return root?.Resultats
                .Select(r => (r.IdReq, r.NameReq))
                .ToList()
                ?? new List<(int, string)>();
        }

        public double? ReadValueFromFile(int targetIdReq, string filePath, string key)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            var root = JsonSerializer.Deserialize<ResultFileJson>(File.ReadAllText(filePath), ReadOptions);
            var reqJson = root?.Resultats.FirstOrDefault(r => r.IdReq == targetIdReq);

            if (reqJson == null) return null;

            return key switch
            {
                "CoordU" => reqJson.CoordU,
                "CoordV" => reqJson.CoordV,
                "CoordW" => reqJson.CoordW,
                "TargetWC" => reqJson.TargetWC,
                "TargetSTAT" => reqJson.TargetSTAT,
                _ => null
            };
        }

        #endregion
    }
}