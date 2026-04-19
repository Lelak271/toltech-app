using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Toltech.App.Converters;
using Toltech.App.Models;
using Toltech.App.Services;
using Toltech.App.ToltechCalculation;
using Toltech.ComputeEngine.Contracts;  

namespace Toltech.App.ToltechCalculation.Resux
{
    // =========================================================================
    // DTOs JSON — classes de sérialisation/désérialisation
    // =========================================================================

    public class ResuxFileJson
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
        [JsonPropertyName("data")] public List<ResultEachDataJson> Data { get; set; } = new();
    }

    public class ResultEachDataJson
    {
        [JsonPropertyName("idData")] public int IdData { get; set; }
        [JsonPropertyName("nameData")] public string NameData { get; set; }
        [JsonPropertyName("nameOri")] public string NameOri { get; set; }
        [JsonPropertyName("nameExtre")] public string NameExtre { get; set; }
        [JsonPropertyName("tolOri")] public ToleranceInfoJson TolOri { get; set; }
        [JsonPropertyName("tolInt")] public ToleranceInfoJson TolInt { get; set; }
        [JsonPropertyName("tolExtr")] public ToleranceInfoJson TolExtr { get; set; }
        [JsonPropertyName("inflX")] public double InflX { get; set; }
        [JsonPropertyName("inflY")] public double InflY { get; set; }
        [JsonPropertyName("inflZ")] public double InflZ { get; set; }
    }

    public class ToleranceInfoJson
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("value")] public double Value { get; set; }
    }

    // =========================================================================
    // ResuxSerializer — sérialisation et lecture des fichiers .resux (JSON)
    // =========================================================================

    public class ResuxSerializer
    {
        #region Modèles internes (consommés par l'UI)

        public class ToleranceInfo
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public double Value { get; set; }
            public string? ValueRaw { get; set; }
        }

        public class ResultEachData
        {
            public int IdData { get; set; }
            public string NameOri { get; set; }
            public string NameExtre { get; set; }
            public string NameData { get; set; }

            public ToleranceInfo TolOriInfo { get; set; } = new() { Name = "TolOri" };
            public ToleranceInfo TolIntInfo { get; set; } = new() { Name = "TolInt" };
            public ToleranceInfo TolExtrInfo { get; set; } = new() { Name = "TolExtr" };

            public double InfluenceX { get; set; }
            public double InfluenceY { get; set; }
            public double InfluenceZ { get; set; }

            public double InfluencWC { get; set; }
            public double ContribWCOri { get; set; }
            public double ContribWCInt { get; set; }
            public double ContribWCExtr { get; set; }
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

            public List<ResultEachData> Data { get; set; } = new();
        }

        public class ResuxFileMetadata
        {
            public string Projet { get; set; }
            public string USerName { get; set; }
            public string VersionToltech { get; set; }
            public string TypeCalcul { get; set; }
            public string Format { get; set; }
            public string Separator { get; set; }
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

        public async Task WriteResultsToFileV2Async(ConcurrentDictionary<int, List<PrimaryResults>> AllResults, List<Requirements> ReqCompute)
        {
            string nameModel = Path.GetFileNameWithoutExtension(ModelManager.ModelActif);
            string folderPath = ModelManager.GetTolTechTempPath();
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filePath = Path.Combine(folderPath, $"ResultsUI_{nameModel}_{timestamp}.resux");

            var converter = new IdToNameConverter();
            var rng = new Random(); // une seule instance en dehors de la boucle

            var root = new ResuxFileJson
            {
                Projet = nameModel,
                UserName = Environment.UserName,
                Version = "1.0.0",
                TypeCalcul = "OneMatrix",
            };

            foreach (var (idReq, dataList) in AllResults)
            {
                var requirement = ReqCompute.FirstOrDefault(r => r.Id_req == idReq); // TO DO mettre fallback ? 

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

                foreach (var item in dataList)
                {
                    var modelData = await DatabaseService.ActiveInstance.GetModelDataByIdAsync(item.IdData);
                    if (modelData == null) continue;

                    reqDto.Data.Add(new ResultEachDataJson
                    {
                        IdData = item.IdData,
                        NameData = modelData.Model,
                        NameOri = converter.Convert(modelData.OriginePartId, typeof(string), null, CultureInfo.CurrentCulture) as string,
                        NameExtre = converter.Convert(modelData.ExtremitePartId, typeof(string), null, CultureInfo.CurrentCulture) as string,
                        TolOri = new ToleranceInfoJson { Id = modelData.IdTolOri.ToString(), Name = modelData.NameTolOri, Value = modelData.TolOri },
                        TolInt = new ToleranceInfoJson { Id = modelData.IdTolInt.ToString(), Name = modelData.NameTolInt, Value = modelData.TolInt },
                        TolExtr = new ToleranceInfoJson { Id = modelData.IdTolExtre.ToString(), Name = modelData.NameTolExtre, Value = modelData.TolExtr },
                        InflX = item.InflX,
                        InflY = item.InflY,
                        InflZ = item.InflZ,
                    });
                }

                root.Resultats.Add(reqDto);
            }

            string json = JsonSerializer.Serialize(root, WriteOptions);
            await File.WriteAllTextAsync(filePath, json);

            ModelManager.FilePathResx = filePath;
        }

        #endregion

        #region Lecture

        /// <summary>
        /// 
        /// </summary>
        public ResultsForReq LoadInfluencedWCFromFile(
            int targetIdReq,
            string filePath,
            double Ucoord = 0, double Vcoord = 0, double Wcoord = 0)
        {
            var result = new ResultsForReq { IdReq = targetIdReq };

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return result; // TODO creer fonction pour voir la corruption du fichier 

            var root = JsonSerializer.Deserialize<ResuxFileJson>(File.ReadAllText(filePath), ReadOptions);
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

            result.Data = reqJson.Data.Select(d =>
            {
                double influenceWC = ComputeInfluenceWC(d.InflX, d.InflY, d.InflZ, dirU, dirV, dirW);

                return new ResultEachData
                {
                    IdData = d.IdData,
                    NameData = d.NameData,
                    NameOri = d.NameOri,
                    NameExtre = d.NameExtre,
                    TolOriInfo = MapTolerance(d.TolOri),
                    TolIntInfo = MapTolerance(d.TolInt),
                    TolExtrInfo = MapTolerance(d.TolExtr),
                    InfluenceX = d.InflX,
                    InfluenceY = d.InflY,
                    InfluenceZ = d.InflZ,
                    InfluencWC = influenceWC,
                    ContribWCOri = Math.Abs(d.TolOri.Value * influenceWC),
                    ContribWCInt = Math.Abs(d.TolInt.Value * influenceWC),
                    ContribWCExtr = Math.Abs(d.TolExtr.Value * influenceWC),
                };
            }).ToList();

            return result;
        }

        public HashSet<int> GetAllReqIdsFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return new HashSet<int>();

            var root = JsonSerializer.Deserialize<ResuxFileJson>(File.ReadAllText(filePath), ReadOptions);
            return root?.Resultats.Select(r => r.IdReq).ToHashSet() ?? new HashSet<int>();
        }

        public ResuxFileMetadata ExtractMetadataFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            var root = JsonSerializer.Deserialize<ResuxFileJson>(File.ReadAllText(filePath), ReadOptions);
            if (root == null) return null;

            return new ResuxFileMetadata
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

        /// <summary>
        /// Projection signée du vecteur d'influence sur la direction unitaire (u, v, w).
        /// </summary>
        private static double ComputeInfluenceWC(
            double inflX, double inflY, double inflZ,
            double dirU, double dirV, double dirW)
        {
            if (double.IsNaN(inflX) || double.IsNaN(inflY) || double.IsNaN(inflZ) ||
                double.IsNaN(dirU) || double.IsNaN(dirV) || double.IsNaN(dirW))
                return 0.0;

            double dirNorm = Math.Sqrt(dirU * dirU + dirV * dirV + dirW * dirW);
            if (Math.Abs(dirNorm) < Toltech.App.Services.Constants.EPSILON) return 0.0;

            return (inflX * dirU + inflY * dirV + inflZ * dirW) / dirNorm;
        }

        /// <summary>
        /// Projette un vecteur (x, y, z) sur la direction (u, v, w).
        /// </summary>
        public static (double Px, double Py, double Pz) ProjectVectorOntoDirection(
            double x, double y, double z,
            double u, double v, double w)
        {
            double normSquared = u * u + v * v + w * w;
            if (Math.Abs(normSquared) < Toltech.App.Services.Constants.EPSILON)
                throw new ArgumentException("Le vecteur directeur (u, v, w) ne peut pas être nul.");

            double k = (x * u + y * v + z * w) / normSquared;
            return (k * u, k * v, k * w);
        }

        /// <summary>
        /// Convertit un DTO ToleranceInfoJson en modèle interne ToleranceInfo.
        /// </summary>
        private static ToleranceInfo MapTolerance(ToleranceInfoJson dto) => new()
        {
            Id = dto.Id,
            Name = dto.Name,
            Value = dto.Value,
            ValueRaw = dto.Value.ToString("F4", CultureInfo.InvariantCulture),
        };

        public List<(int IdReq, string Name)> ExtractReqHeaders(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return new List<(int, string)>();

            var root = JsonSerializer.Deserialize<ResuxFileJson>(File.ReadAllText(filePath), ReadOptions);

            return root?.Resultats
                .Select(r => (r.IdReq, r.NameReq))
                .ToList()
                ?? new List<(int, string)>();
        }

        public double? ReadValueFromFile(int targetIdReq, string filePath, string key)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            var root = JsonSerializer.Deserialize<ResuxFileJson>(File.ReadAllText(filePath), ReadOptions);
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