//using System.Collections.Concurrent;
//using System.Diagnostics;
//using System.Globalization;
//using System.IO;
//using MathNet.Numerics.LinearAlgebra;
//using Toltech.App.Services;
//using Toltech.App.ToltechCalculation;
//using static Toltech.App.ToltechCalculation.MainCompute;
//using Toltech.App.Models;
//using Toltech.App.Converters;

//namespace Toltech.App.ToltechCalculation.Resux
//{
//    public class ResuxSerializer
//    {
//        #region Constructeur
//        public ResuxSerializer()
//        {
//        }

//        #endregion

//        #region Variables 

//        // Extract des variables pour generer l'UI des résultats
//        public class ResultEachData
//        {
//            public int IdData { get; set; }
//            public string NameOri { get; set; }
//            public string NameExtre { get; set; }
//            public string NameData { get; set; }

//            // Tolérances groupées (origine / interne / extrémité)
//            public ToleranceInfo TolOriInfo { get; set; } = new ToleranceInfo { Name = "TolOri" };
//            public ToleranceInfo TolIntInfo { get; set; } = new ToleranceInfo { Name = "TolInt" };
//            public ToleranceInfo TolExtrInfo { get; set; } = new ToleranceInfo { Name = "TolExtr" };

//            public double InfluenceX { get; set; } // From Toltech
//            public double InfluenceY { get; set; } // From Toltech
//            public double InfluenceZ { get; set; } // From Toltech


//            // Valeurs à calculer avec InfluenceMatrix
//            public double InfluencWC { get; set; } // Dépendra de la direction de la ponctuelle 
//            public double ContribWCOri { get; set; }
//            public double ContribWCInt { get; set; }
//            public double ContribWCExtr { get; set; }
//        }

//        public class ResultsForReq
//        {
//            public int IdReq { get; set; }
//            public string NameReq { get; set; }
//            public double CoordU { get; set; }
//            public double CoordV { get; set; }
//            public double CoordW { get; set; }
//            public string NamePart1 { get; set; }
//            public string NamePart2 { get; set; }
//            public double TargetWC { get; set; }
//            public double TargetSTAT { get; set; }

//            public List<ResultEachData> Data { get; set; } = new List<ResultEachData>();
//        }

//        /// <summary>
//        /// Contient les informations d'une tolérance : id (texte), nom (texte) et valeur numérique.
//        /// ValeurRaw conserve le format brut si nécessaire pour affichage/stockage.
//        /// </summary>
//        public class ToleranceInfo
//        {
//            /// <summary>Identifiant de la tolérance (tel qu'il apparaît dans le fichier).</summary>
//            public string? Id { get; set; }

//            /// <summary>Nom de la tolérance (ex : "TolOri").</summary>
//            public string? Name { get; set; }

//            /// <summary>Valeur numérique de la tolérance (0 si absent ou non-parsable).</summary>
//            public double Value { get; set; }

//            /// <summary>Chaîne source, si vous devez garder l'original pour affichage.</summary>
//            public string? ValueRaw { get; set; }
//        }

//        public class ResuxFileMetadata
//        {
//            public string Projet { get; set; }
//            public string USerName { get; set; }
//            public string VersionToltech { get; set; }
//            public string TypeCalcul { get; set; }
//            public string Format { get; set; }
//            public string Separator { get; set; }

//        }

//        #endregion

//        #region Genérateur de .txt RESUX
//        private static async Task WriteHeaderAsync(StreamWriter writer, string nameModel, string calculationType)
//        {
//            await writer.WriteLineAsync($"# Fichier de résultats TolTech");
//            await writer.WriteLineAsync($"# Utilisateur: {Environment.UserName}");
//            await writer.WriteLineAsync($"# Projet     : {nameModel}");
//            await writer.WriteLineAsync($"# Version Toltech    : 1.0.0");
//            await writer.WriteLineAsync($"# Type de calcul    : {calculationType}");
//            await writer.WriteLineAsync($"# Format     : texte tabulé (.resux)");
//            await writer.WriteLineAsync($"# Séparateur : tabulation (\\t)");
//            await writer.WriteLineAsync($"#");
//            await writer.WriteLineAsync($"# Colonnes exigence :");
//            await writer.WriteLineAsync($"#   IdReq\tNameReq\tTol1\tTol2");
//            await writer.WriteLineAsync($"#");
//            await writer.WriteLineAsync($"# Colonnes données associées (par bloc) :");
//            await writer.WriteLineAsync($"#   IdData\tNameData\tTolOri\tTolInt\tTolExtr\tInflX\tInflY\tInflZ");
//            await writer.WriteLineAsync();

//            await writer.WriteLineAsync("\r\n  _______    ____    _        _______   ______    _____   _    _                                                 \r\n |__   __|  / __ \\  | |      |__   __| |  ____|  / ____| | |  | |                                                \r\n    | |    | |  | | | |         | |    | |__    | |      | |__| |    ______     _ __    ___   ___   _   _  __  __\r\n    | |    | |  | | | |         | |    |  __|   | |      |  __  |   |______|   | '__|  / _ \\ / __| | | | | \\ \\/ /\r\n    | |    | |__| | | |____     | |    | |____  | |____  | |  | |              | |    |  __/ \\__ \\ | |_| |  >  < \r\n    |_|     \\____/  |______|    |_|    |______|  \\_____| |_|  |_|              |_|     \\___| |___/  \\__,_| /_/\\_\\\r\n                                                                                                                 \r\n                                                                                                                 \r\n");

//        }

//        /// <summary>
//        /// Écrit les informations d'une exigence et ses données associées dans un fichier.
//        /// </summary>
//        /// <param name="writer">Le StreamWriter utilisé pour écrire dans le fichier de sortie.</param>
//        /// <param name="idReq">L'identifiant de l'exigence à écrire.</param>
//        /// <param name="dataList">
//        /// Liste des données associées à l'exigence. 
//        /// Chaque élément est un tuple contenant :
//        /// - ComposanteId : identifiant de la composante (ex : "Comp_123")  
//        /// - X : valeur de l'influence en X  
//        /// - Y : valeur de l'influence en Y  
//        /// - Z : valeur de l'influence en Z
//        /// </param>
//        /// <returns>Une tâche asynchrone représentant l'opération d'écriture.</returns>
//        private async Task WriteRequirementBlockAsync(StreamWriter writer, int idReq, List<PrimaryResults> dataList)
//        {
//            var requirement = await DatabaseService.ActiveInstance.GetReqsByIdAsync(idReq);

//            string formattedTol1 = requirement.tol1.ToString("F4", CultureInfo.InvariantCulture);
//            string formattedTol2 = requirement.tol2.ToString("F4", CultureInfo.InvariantCulture);
//            string formattedCoordU = requirement.CoordU.ToString("F4", CultureInfo.InvariantCulture);
//            string formattedCoordV = requirement.CoordV.ToString("F4", CultureInfo.InvariantCulture);
//            string formattedCoordW = requirement.CoordW.ToString("F4", CultureInfo.InvariantCulture);

//            var random = new Random();
//            double value1 = random.NextDouble() * 20;
//            double value2 = random.NextDouble() * 20;

//            // On assigne en garantissant que TargetWC > TargetSTAT
//            double randomTargetWC = Math.Max(value1, value2);
//            double randomTargetSTAT = Math.Min(value1, value2);

//            var converter = new IdToNameConverter();

//            var columns = new[]
//            {
//                "IdReq", idReq.ToString(),
//                "NameReq", requirement.NameReq,
//                "NamePart1", converter.Convert(requirement.PartReq1Id, typeof(string), null, CultureInfo.CurrentCulture) as string,
//                "Tol1", formattedTol1,
//                "NamePart2", converter.Convert(requirement.PartReq2Id, typeof(string), null, CultureInfo.CurrentCulture) as string,
//                "Tol2", formattedTol2,
//                "CoordU", formattedCoordU,
//                "CoordV", formattedCoordV,
//                "CoordW", formattedCoordW,
//                "TargetWC", randomTargetWC.ToString("F2", CultureInfo.InvariantCulture),
//                "TargetSTAT", randomTargetSTAT.ToString("F2", CultureInfo.InvariantCulture)
//            };

//            await writer.WriteLineAsync(string.Join("\t", columns));
//            await writer.WriteLineAsync();


//            int count = 0;
//            foreach (var iddata in dataList)
//            {
//                count++;
//                int id_data = iddata.IdData;
//                var modelData = await DatabaseService.ActiveInstance.GetModelDataByIdAsync(id_data);

//                if (modelData==null)
//                    return;

//                var matrix = Matrix<double>.Build.DenseOfArray(new double[,]
//                             {
//                                { iddata.InflX },
//                                { iddata.InflY },
//                                { iddata.InflZ }
//                             });


//                var res = new[]
//                 {
//                    "IdData",   id_data.ToString(),
//                    "NameData", modelData.Model,

//                    "NameTolOri", modelData.NameTolOri ?? string.Empty,
//                    "IdTolOri",   modelData.IdTolOri.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
//                    "TolOri",     modelData.TolOri.ToString("F4", CultureInfo.InvariantCulture),

//                    "NameTolInt", modelData.NameTolInt ?? string.Empty,
//                    "IdTolInt",   modelData.IdTolInt.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
//                    "TolInt",     modelData.TolInt.ToString("F4", CultureInfo.InvariantCulture),

//                    "NameTolExtr", modelData.NameTolExtre ?? string.Empty,
//                    "IdTolExtr",   modelData.IdTolExtre.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
//                    "TolExtr",     modelData.TolExtr.ToString("F4", CultureInfo.InvariantCulture),

//                    "NomOri",   converter.Convert(modelData.OriginePartId, typeof(string), null, CultureInfo.CurrentCulture) as string,
//                    "NomExtre", converter.Convert(modelData.ExtremitePartId, typeof(string), null, CultureInfo.CurrentCulture) as string,

//                    "InflX", iddata.InflX.ToString("F4", CultureInfo.InvariantCulture),
//                    "InflY", iddata.InflY.ToString("F4", CultureInfo.InvariantCulture),
//                    "InflZ", iddata.InflZ.ToString("F4", CultureInfo.InvariantCulture)
//                };

//                await writer.WriteLineAsync(string.Join("\t", res));
//                await writer.WriteLineAsync();
//            }

//            await writer.WriteLineAsync(new string('-', 170));
//            await writer.WriteLineAsync();
//        }

//        public async Task WriteResultsToFileV2Async(ConcurrentDictionary<int, List<PrimaryResults>> AllResults)
//        {
//            string nameModel = Path.GetFileNameWithoutExtension(ModelManager.ModelActif);
//            string folderPath = ModelManager.GetTolTechTempPath();
//            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
//            string filePath = Path.Combine(folderPath, $"ResultsUI_{nameModel}_{timestamp}.resux");

//            using (StreamWriter writer = new StreamWriter(filePath))
//            {
//                await WriteHeaderAsync(writer, nameModel, "OneMatrix");

//                foreach (var kvp in AllResults)
//                {
//                    await WriteRequirementBlockAsync(writer, kvp.Key, kvp.Value);
//                }
//            }
//            ModelManager.FilePathResx = filePath;

//        }

//        #endregion

//        #region Lecture de .txt resux

//        // Renvoie l'influence, la Contribution, les tols etc... par ID au vu de la direction UVW choisie
//        // Extrait les valeurs du RESUX
//        public ResultsForReq LoadInfluencedWCFromFile(int targetIdReq, string filePath, double Ucoord = 0, double Vcoord = 0, double Wcoord = 0)
//        {
//            var resultForReq = new ResultsForReq { IdReq = targetIdReq };
//            var entriesById = new Dictionary<int, ResultEachData>(capacity: 64);

//            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
//                return resultForReq;

//            // Direction libre fournie
//            double freeU = Ucoord, freeV = Vcoord, freeW = Wcoord;
//            double freeNorm = Math.Sqrt(freeU * freeU + freeV * freeV + freeW * freeW);

//            bool inTargetBlock = false;
//            double reqCoordU = 0, reqCoordV = 0, reqCoordW = 0;
//            string reqName = string.Empty;
//            string currentOriginName = string.Empty;
//            string currentExtremityName = string.Empty;
//            double targetWc = 0;
//            double targetStat = 0;

//            // Lecture en streaming (mémoire moindre pour gros fichiers)
//            foreach (var rawLine in File.ReadLines(filePath))
//            {
//                if (string.IsNullOrWhiteSpace(rawLine))
//                    continue;

//                // Si ligne de début de bloc Exigence
//                if (rawLine.StartsWith("IdReq\t", StringComparison.Ordinal))
//                {
//                    // Parse key/value dans cette ligne d'entête
//                    var headerMap = ParseKeyValueTokens(rawLine);

//                    // Déterminer l'ID courant (IdReq peut être après le mot-clé)
//                    if (TryGetIntFromMap(headerMap, "IdReq", out int currentIdReq) && currentIdReq == targetIdReq)
//                    {
//                        inTargetBlock = true;

//                        reqCoordU = TryGetDoubleFromMap(headerMap, "CoordU", reqCoordU);
//                        reqCoordV = TryGetDoubleFromMap(headerMap, "CoordV", reqCoordV);
//                        reqCoordW = TryGetDoubleFromMap(headerMap, "CoordW", reqCoordW);

//                        reqName = headerMap.TryGetValue("NameReq", out var nameValue) ? nameValue : string.Empty;

//                        currentOriginName = headerMap.TryGetValue("NamePart1", out var n1) ? n1 : currentOriginName;
//                        currentExtremityName = headerMap.TryGetValue("NamePart2", out var n2) ? n2 : currentExtremityName;

//                        targetWc = TryGetDoubleFromMap(headerMap, "TargetWC", targetWc);
//                        targetStat = TryGetDoubleFromMap(headerMap, "TargetSTAT", targetStat);

//                        // Remplir dans l'objet de résultat
//                        resultForReq.CoordU = reqCoordU;
//                        resultForReq.CoordV = reqCoordV;
//                        resultForReq.CoordW = reqCoordW;
//                        resultForReq.NameReq = reqName;
//                        resultForReq.NamePart1 = currentOriginName;
//                        resultForReq.NamePart2 = currentExtremityName;
//                        resultForReq.TargetWC = targetWc;
//                        resultForReq.TargetSTAT = targetStat;
//                    }
//                    else
//                    {
//                        // On n'est pas dans le bloc cible
//                        inTargetBlock = false;
//                    }

//                    continue;
//                }

//                // Si on n'est pas dans le bloc désiré, on ignore
//                if (!inTargetBlock)
//                    continue;

//                // On ne s'intéresse qu'aux lignes contenant des valeurs IdData
//                if (!rawLine.Contains("IdData", StringComparison.Ordinal))
//                    continue;

//                // Tokenize et convertir en map clé->valeur
//                var kv = ParseKeyValueTokens(rawLine);

//                // Tentative d'extraction IdData (prioritaire)
//                if (!TryGetIntFromMap(kv, "IdData", out int idData))
//                {
//                    // Si pas trouvé, essayer positionnel (ancienne logique)
//                    // token example: "1 IdData \t201\tNameData\tSurface1..."
//                    var parts = rawLine.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
//                    if (parts.Length >= 2 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int idPos))
//                        idData = idPos;
//                    else
//                        continue; // pas d'id exploitable -> ignorer
//                }

//                // Nom de la donnée
//                string nameData = kv.TryGetValue("NameData", out var ndata) ? ndata
//                                  : ExtractNameFromParts(rawLine) ?? string.Empty;

//                // Origine / extremité (peuvent être présentes dans la ligne data)
//                if (kv.TryGetValue("NomOri", out var nomOriVal)) currentOriginName = nomOriVal;
//                if (kv.TryGetValue("NomExtre", out var nomExtVal)) currentExtremityName = nomExtVal;

//                // Tolérances et contributions (Tol * influence)
//                string idTolOri = kv.TryGetValue("IdTolOri", out var tmpIdTolOri) ? tmpIdTolOri : null;
//                string nameTolOri = kv.TryGetValue("NameTolOri", out var tmpNameTolOri) ? tmpNameTolOri : "TolOri";
//                double tolOri = TryGetDoubleFromMap(kv, "TolOri", 0.0);

//                string idTolInt = kv.TryGetValue("IdTolInt", out var tmpIdTolInt) ? tmpIdTolInt : null;
//                string nameTolInt = kv.TryGetValue("NameTolInt", out var tmpNameTolInt) ? tmpNameTolInt : "TolInt";
//                double tolInt = TryGetDoubleFromMap(kv, "TolInt", 0.0);

//                string idTolExtr = kv.TryGetValue("IdTolExtr", out var tmpIdTolExtr) ? tmpIdTolExtr : null;
//                string nameTolExtr = kv.TryGetValue("NameTolExtr", out var tmpNameTolExtr) ? tmpNameTolExtr : "TolExtr";
//                double tolExtr = TryGetDoubleFromMap(kv, "TolExtr", 0.0);

//                // Construire les objets ToleranceInfo (Value est le double utilisable, ValueRaw la chaîne formatée)
//                var tolOriInfo = new ToleranceInfo
//                {
//                    Id = idTolOri,
//                    Name = nameTolOri,
//                    Value = tolOri,
//                    ValueRaw = tolOri.ToString("F4", CultureInfo.InvariantCulture)
//                };

//                var tolIntInfo = new ToleranceInfo
//                {
//                    Id = idTolInt,
//                    Name = nameTolInt,
//                    Value = tolInt,
//                    ValueRaw = tolInt.ToString("F4", CultureInfo.InvariantCulture)
//                };

//                var tolExtrInfo = new ToleranceInfo
//                {
//                    Id = idTolExtr,
//                    Name = nameTolExtr,
//                    Value = tolExtr,
//                    ValueRaw = tolExtr.ToString("F4", CultureInfo.InvariantCulture)
//                };

//                // Influences
//                bool hasInflX = TryGetDoubleFromMapSafe(kv, "InflX", out double inflX);
//                bool hasInflY = TryGetDoubleFromMapSafe(kv, "InflY", out double inflY);
//                bool hasInflZ = TryGetDoubleFromMapSafe(kv, "InflZ", out double inflZ);

//                // Déterminer la direction à utiliser :
//                double dirU = (freeNorm > 0) ? freeU : reqCoordU;
//                double dirV = (freeNorm > 0) ? freeV : reqCoordV;
//                double dirW = (freeNorm > 0) ? freeW : reqCoordW;

//                var entry = new ResultEachData
//                {
//                    IdData = idData,
//                    NameData = nameData,
//                    NameOri = currentOriginName ?? string.Empty,
//                    NameExtre = currentExtremityName ?? string.Empty,

//                    // Remplacez les propriétés ci-dessous par les noms exacts de votre classe si nécessaire
//                    TolOriInfo = tolOriInfo,
//                    TolIntInfo = tolIntInfo,
//                    TolExtrInfo = tolExtrInfo,

//                    // Conserver les influences brutes lues
//                    InfluenceX = hasInflX ? inflX : 0.0,
//                    InfluenceY = hasInflY ? inflY : 0.0,
//                    InfluenceZ = hasInflZ ? inflZ : 0.0
//                };

//                double influenceWC = 0.0;
//                if (hasInflX && hasInflY && hasInflZ)
//                {
//                    influenceWC = ComputeInfluenceWC_Minimal(inflX, inflY, inflZ, dirU, dirV, dirW);
//                }

//                // Calculer les contributions pondérées
//                entry.InfluencWC = influenceWC;
//                entry.ContribWCOri = Math.Abs(tolOri * influenceWC);
//                entry.ContribWCInt = Math.Abs(tolInt * influenceWC);
//                entry.ContribWCExtr = Math.Abs(tolExtr * influenceWC);


//                // Stocker (remplace si même id rencontré)
//                entriesById[idData] = entry;
//            }

//            resultForReq.Data = entriesById.Values.ToList();
//            return resultForReq;
//        }

//        // --- Fonction annexe minimale (projection signée sur la direction unitaire)
//        private static double ComputeInfluenceWC_Minimal(
//            double inflX, double inflY, double inflZ,
//            double dirU, double dirV, double dirW)
//        {
//            if (double.IsNaN(inflX) || double.IsNaN(inflY) || double.IsNaN(inflZ) ||
//                double.IsNaN(dirU) || double.IsNaN(dirV) || double.IsNaN(dirW))
//            {
//                return 0.0;
//            }

//            double dirNorm = Math.Sqrt(dirU * dirU + dirV * dirV + dirW * dirW);
//            if (dirNorm == 0.0) return 0.0;

//            double dot = inflX * dirU + inflY * dirV + inflZ * dirW;
//            return dot / dirNorm; // projection signée v·n
//        }

//        public ResuxFileMetadata ExtractMetadataFromFile(string filePath)
//        {
//            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
//                return null;

//            var metadata = new ResuxFileMetadata();

//            using (var reader = new StreamReader(filePath))
//            {
//                string line;
//                while ((line = reader.ReadLine()) != null)
//                {
//                    if (string.IsNullOrWhiteSpace(line))
//                        continue;

//                    // On ne traite que les lignes de commentaire de l'en-tête
//                    if (!line.StartsWith("#"))
//                        break;

//                    // Nettoyage de la ligne
//                    line = line.TrimStart('#').Trim();

//                    // Split sur ':', première partie = clé, seconde = valeur
//                    int colonIndex = line.IndexOf(':');
//                    if (colonIndex < 0)
//                        continue;

//                    string key = line.Substring(0, colonIndex).Trim();
//                    string value = line.Substring(colonIndex + 1).Trim();

//                    // Mapping des clés sur les propriétés
//                    switch (key.ToLowerInvariant())
//                    {
//                        case "projet":
//                            metadata.Projet = value;
//                            break;
//                        case "utilisateur":
//                            metadata.USerName = value;
//                            break;
//                        case "version toltech":
//                            metadata.VersionToltech = value;
//                            break;
//                        case "type de calcul":
//                            metadata.TypeCalcul = value;
//                            break;
//                        case "format":
//                            metadata.Format = value;
//                            break;
//                        case "séparateur":
//                            metadata.Separator = value;
//                            break;
//                        default:
//                            // Clé inconnue : ignorer ou logger
//                            break;
//                    }
//                }
//            }

//            return metadata;
//        }


//        #region Auxiliaire de "Fonction principales"

//        /// <summary>
//        /// Projette un vecteur (x, y, z) sur la direction définie par (u, v, w).
//        /// </summary>
//        public static (double Px, double Py, double Pz) ProjectVectorOntoDirection(double x, double y, double z, double u, double v, double w)
//        {
//            // Norme au carré du vecteur directeur
//            double normSquared = u * u + v * v + w * w;

//            if (normSquared == 0)
//                throw new ArgumentException("Le vecteur directeur (u, v, w) ne peut pas être nul.");

//            // Produit scalaire entre les deux vecteurs
//            double dotProduct = x * u + y * v + z * w;

//            // Coefficient de projection
//            double k = dotProduct / normSquared;

//            // Vecteur projeté
//            double px = k * u;
//            double py = k * v;
//            double pz = k * w;

//            return (px, py, pz);
//        }

//        /// <summary>
//        /// Analyse une ligne tabulée et construit une map clé/valeur.
//        /// Corrigé : gère correctement les valeurs vides ("clé" → "").
//        /// Exemple : "NameTolOri\t\tIdTolOri\t123" → { "NameTolOri":"", "IdTolOri":"123" }.
//        /// </summary>
//        private static Dictionary<string, string> ParseKeyValueTokens(string line)
//        {
//            // On ne supprime PAS les entrées vides, sinon on décale tout.
//            var parts = line.Split('\t');
//            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

//            for (int i = 0; i < parts.Length; i++)
//            {
//                string token = parts[i].Trim();

//                // On considère un token comme clé seulement si c'est une clé connue
//                if (IsLikelyKey(token))
//                {
//                    // Si la valeur suivante existe, même vide → on la garde.
//                    string value = (i + 1 < parts.Length) ? parts[i + 1].Trim() : string.Empty;

//                    dict[token] = value;
//                    i++; // sauter la valeur (même vide)
//                }
//            }

//            return dict;
//        }

//        /// <summary>
//        /// Vérifie si une chaîne est probablement une clé (ex : IdData, NameData...).
//        /// </summary>
//        private static bool IsLikelyKey(string token)
//        {
//            if (string.IsNullOrEmpty(token)) return false;
//            // Exemples de clés attendues : IdReq, IdData, NameData, TolOri, TolInt, TolExtr, InflX, InflY, InflZ, NomOri, NomExtre, CoordU...
//            // Condition simple : commence par une lettre et contient au moins une lettre
//            return char.IsLetter(token[0]) && token.Any(char.IsLetter);
//        }

//        /// <summary>
//        /// Tente de lire un entier à partir d’une map clé/valeur.
//        /// </summary>
//        private static bool TryGetIntFromMap(Dictionary<string, string> map, string key, out int value)
//        {
//            value = 0;
//            if (map.TryGetValue(key, out var s) && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
//            {
//                value = v;
//                return true;
//            }
//            return false;
//        }

//        /// <summary>
//        /// Récupère un double dans une map clé/valeur, ou retourne une valeur par défaut si absent ou invalide.
//        /// </summary> 
//        private static double TryGetDoubleFromMap(Dictionary<string, string> map, string key, double defaultValue)
//        {
//            if (TryGetDoubleFromMapSafe(map, key, out var v))
//                return v;
//            return defaultValue;
//        }

//        /// <summary>
//        /// Tente d’extraire un double d’une map clé/valeur.
//        /// </summary>
//        private static bool TryGetDoubleFromMapSafe(Dictionary<string, string> map, string key, out double value)
//        {
//            value = 0.0;
//            if (map.TryGetValue(key, out var s) && double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double v))
//            {
//                value = v;
//                return true;
//            }
//            return false;
//        }

//        /// <summary>
//        /// Extrait le nom d’une donnée à partir d’une ligne brute si la map ne contient pas "NameData".
//        /// </summary>
//        private static string? ExtractNameFromParts(string line)
//        {
//            var parts = line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
//            // Cherche "NameData" suivi d'une valeur
//            for (int i = 0; i < parts.Length - 1; i++)
//            {
//                if (parts[i].Equals("NameData", StringComparison.OrdinalIgnoreCase))
//                    return parts[i + 1].Trim();
//            }

//            // fallback : si la structure positionnelle existe (ex: "1 IdData \t201\tNameData\tSurface1"), essayer parts[3]
//            if (parts.Length >= 4)
//                return parts[3].Trim();

//            return null;
//        }

//        /// <summary>
//        /// Lit un fichier et retourne tous les identifiants d’exigences (IdReq) trouvés.
//        /// </summary>
//        public HashSet<int> GetAllReqIdsFromFile(string filePath)
//        {
//            var ids = new HashSet<int>();

//            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
//                return ids;

//            foreach (var rawLine in File.ReadLines(filePath))
//            {
//                if (string.IsNullOrEmpty(rawLine)) continue;

//                if (!rawLine.StartsWith("IdReq\t", StringComparison.Ordinal)) continue;

//                // ParseKeyValueTokens et TryGetIntFromMap doivent être accessibles (helpers existants)
//                var headerMap = ParseKeyValueTokens(rawLine);
//                if (TryGetIntFromMap(headerMap, "IdReq", out int idReq))
//                {
//                    ids.Add(idReq);
//                }
//            }

//            return ids;
//        }
//        #endregion

//        #endregion

//    }
//}
