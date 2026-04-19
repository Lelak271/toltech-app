using System.Diagnostics;
using System.Windows;
using TOLTECH_APPLICATION.Models;
using TOLTECH_APPLICATION.Services;
using TOLTECH_APPLICATION.Services.Logging;
using TOLTECH_APPLICATION.ToltechCalculation;
using TOLTECH_APPLICATION.ToltechCalculation.Validation;
using TOLTECH_APPLICATION.Utilities;
using Toltech.ComputeEngine.Contracts;
using Toltech.ComputeEngine;

namespace TOLTECH_APPLICATION.ToltechCalculation.Helpers
{
    public class ComputeValidationService
    {
        private IComputeEngine _computeEngine;
        private static ILoggerService _logger;
        private static DatabaseService _databaseService;

        public ComputeValidationService(IComputeEngine computeEngine, DatabaseService databaseService)
        {
            _logger = App.Logger;
            _computeEngine = computeEngine;
            _databaseService = databaseService;
        }

        private static bool IsVectorZero(double u, double v, double w)
            => Math.Abs(u) < 1e-6 && Math.Abs(v) < 1e-6 && Math.Abs(w) < 1e-6;

        // VALIDATION D'UNE PIÈCE
        public async Task<bool> ValidationPart(Part part)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));

            var errors = new List<string>();

            var allModelData = await _databaseService.GetAllModelDataAsync();
            var partModelData = allModelData
                .Where(md => md.ExtremitePartId == part.Id && md.Active == true)
                .ToList();

            // Contacts à direction nulle
            var invalidContacts = partModelData
                .Where(d => IsVectorZero(d.CoordU, d.CoordV, d.CoordW))
                .Select(d => $"{d.Origine} → {d.Extremite} (Contact : {d.Model})")
                .Distinct().ToList();

            if (invalidContacts.Any())
            {
                var msg = ValidationRuleRegistry.ContactZeroDirection
                    .GetMessage(string.Join("\n  - ", invalidContacts));
                errors.Add(msg);
                _logger.LogWarning(msg, nameof(ComputeValidationService));
            }

            // Origine manquante
            if (partModelData.Any(m => m.OriginePartId == 0 || m.OriginePartId == null))
            {
                var msg = ValidationRuleRegistry.MissingPartOrigin.GetMessage();
                errors.Add(msg);
                _logger.LogWarning(msg, nameof(ComputeValidationService));
            }

            // Isostatisme
            var computeModelData = ComputeMapper.ToComputeModelData(allModelData);
            var computePart = ComputeMapper.ToComputePart(part);
            if (computePart.IsFixed != true && !await _computeEngine.IsPartIsostaticAsync(computeModelData, computePart))
            {
                var msg = ValidationRuleRegistry.PartNotIsostatic.GetMessage();
                errors.Add(msg);
                _logger.LogWarning(msg, nameof(ComputeValidationService));
            }

            if (errors.Any())
            {
                MessageBox.Show(
                    "Erreurs détectées lors de la validation :\n\n" + string.Join("\n\n", errors),
                    "Validation de la MiP - Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            MessageBox.Show(
                ValidationRuleRegistry.ValidationSuccess.GetMessage(
                    part.IsFixed == true ? "[Pièce fixe] - Modèle valide" : "Modèle valide"),
                "Validation de la MiP - Ok",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return true;
        }

        // VALIDATION DU MODÈLE COMPLET
        public async Task<bool> ValidateGraphDataAsync(
            bool showMessages = true,
            List<ComputeRequirement>? requirements = null)
        {
            if (!ModelValidationHelper.CheckModelActif(true)) return false;

            var errors = new List<string>();
            var modelData = await _databaseService.GetAllModelDataAsync();
            var requirementsUI = await _databaseService.GetAllRequirementsAsync();
            requirements = ComputeMapper.ToComputeRequirements(requirementsUI);

            // Pièces absentes du graphe
            var graphPartIds = new HashSet<int>(
                modelData
                    .SelectMany(md => new[] { md.OriginePartId, md.ExtremitePartId })
                    .Where(id => id.HasValue).Select(id => id!.Value));

            var missingFromGraph = requirements
                .SelectMany(r => new int?[] { r.PartReq1Id, r.PartReq2Id })
                .Where(id => id.HasValue && !graphPartIds.Contains(id!.Value))
                .Select(id => id!.Value).Distinct().ToList();

            if (missingFromGraph.Any())
            {
                var msg = ValidationRuleRegistry.PartMissingFromGraph
                    .GetMessage(string.Join(", ", missingFromGraph));
                errors.Add(msg);
                _logger.LogWarning(msg, nameof(ComputeValidationService));
            }

            // Exigences sans pièces
            if (requirements.Any(r => r.PartReq1Id == 0 || r.PartReq1Id == null
                                   || r.PartReq2Id == 0 || r.PartReq2Id == null))
            {
                var msg = ValidationRuleRegistry.RequirementMissingPart.GetMessage();
                errors.Add(msg);
                _logger.LogWarning(msg, nameof(ComputeValidationService));
            }

            // Directions nulles sur les exigences
            var invalidReqs = requirements
                .Where(r => IsVectorZero(r.CoordU, r.CoordV, r.CoordW))
                .Select(r => $"{r.PartReq1Id} → {r.PartReq2Id} (Exigence : {r.NameReq})")
                .Distinct().ToList();

            if (invalidReqs.Any())
            {
                var msg = ValidationRuleRegistry.RequirementZeroDirection
                    .GetMessage(string.Join("\n  - ", invalidReqs));
                errors.Add(msg);
                _logger.LogWarning(msg, nameof(ComputeValidationService));
            }

            // Directions nulles sur les contacts
            var invalidContacts = modelData
                .Where(d => IsVectorZero(d.CoordU, d.CoordV, d.CoordW))
                .Select(d => $"{d.Origine} → {d.Extremite} (Contact : {d.Model})")
                .Distinct().ToList();

            if (invalidContacts.Any())
            {
                var msg = ValidationRuleRegistry.ContactZeroDirection
                    .GetMessage(string.Join("\n  - ", invalidContacts));
                errors.Add(msg);
                if (showMessages)
                    _logger.LogWarning(msg, nameof(ComputeValidationService));
            }

            // Isostatisme global
            if (!await IsIsoModelAsync())
            {
                var msg = ValidationRuleRegistry.ModelNotIsostatic.GetMessage();
                errors.Add(msg);
                if (showMessages)
                    _logger.LogWarning(msg, nameof(ComputeValidationService));
            }

            if (errors.Any())
            {
                if (showMessages)
                    MessageBox.Show(
                        "Erreurs détectées lors de la validation des données :\n\n" + string.Join("\n\n", errors),
                        "Validation de la MiP - Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (showMessages)
                MessageBox.Show(
                    ValidationRuleRegistry.ValidationSuccess.GetMessage("Modélisation valide pour les calculs."),
                    "Validation de la MiP - Valide",
                    MessageBoxButton.OK, MessageBoxImage.Information);

            return true;
        }

        /// <summary>
        /// VALIDATION PRÉ-CALCUL
        /// Isostatisme de toutes les pièces
        /// Isostatisme global
        /// </summary>
        /// <param name="modelData"></param>
        /// <param name="requirements"></param>
        /// <returns></returns>

        public async Task<bool> ValidationCalculsAsync(List<ComputeModelData> modelData, List<ComputeRequirement> requirements)
        {
            var errors = new List<string>();

            // Isostatisme par pièce (sans affichage)
            var isoResults = await ComputeIsoForAllPartAsync(modelData);
            var errorsIsoResults = new List<string>();
            if (!isoResults.Values.All(v => v))
            {
                var failedIds = isoResults.Where(kv => !kv.Value).Select(kv => kv.Key);

                foreach (int id in failedIds)
                {
                    string name = await _databaseService.GetPartNameByID(id);
                    errorsIsoResults.Add($"  • '{name}' (id {id})");
                }

                var msg = "IsoPart : " + ValidationRuleRegistry.PartNotIsostatic
                    .GetMessage("d");
                errors.Add(msg);
                errors.AddRange(errorsIsoResults);
                _logger.LogWarning(msg, nameof(ComputeValidationService));
            }

            // Cohérence graphe (sans affichage)
            if (!await ValidateGraphDataAsync(showMessages: false, requirements: requirements))
            {
                var msg = "Graphe : " + ValidationRuleRegistry.GraphIncoherent.GetMessage();
                errors.Add(msg);
                _logger.LogWarning(msg, nameof(ComputeValidationService));
            }

            if (errors.Any())
            {
                MessageBox.Show(
                    "Des erreurs ont été détectées lors de la validation :\n\n" + string.Join("\n\n", errors),
                    "Echec du calcul",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }



        // ISOSTATISME INTERNE (sans affichage)
        public async Task<Dictionary<int, bool>> ComputeIsoForAllPartAsync(List<ComputeModelData> modelData)
        {
            var results = new Dictionary<int, bool>();

            try
            {
                var allParts = await _databaseService.GetAllPartsAsync();
                if (allParts is not { Count: > 0 }) return results;

                foreach (var part in allParts)
                {
                    var computePart = ComputeMapper.ToComputePart(part);
                    if (computePart.IsFixed == true) continue;
                    try { results[computePart.Id] = await _computeEngine.IsPartIsostaticAsync(modelData, computePart); }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Erreur isostatisme pièce {computePart.Id} : {ex.Message}");
                        results[computePart.Id] = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur générale ComputeIsoForAllPartAsync : {ex.Message}");
            }

            return results;
        }

        // ISOSTATISME AVEC AFFICHAGE (appelé depuis les handlers)
        public async Task CheckIsoForAllPartAsync()
        {
            var modelData = await _databaseService.GetAllModelDataAsync();
            var computeModelData = ComputeMapper.ToComputeModelData(modelData);

            var results = await ComputeIsoForAllPartAsync(computeModelData);

            if (results.Count == 0)
            {
                var msg = ValidationRuleRegistry.NoDataAvailable.GetMessage();
                _logger.LogWarning(msg, nameof(ComputeValidationService));
                MessageBox.Show(msg, "Validation impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (results.Values.All(v => v))
            {
                MessageBox.Show(
                    ValidationRuleRegistry.ValidationSuccess.GetMessage(),
                    "Validation réussie",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var failedLines = new List<string>();
            foreach (var (id, _) in results.Where(r => !r.Value))
            {
                string name = await _databaseService.GetPartNameByID(id);
                failedLines.Add($"  • '{name}' (id {id})");
            }

            var failMsg = ValidationRuleRegistry.PartNotIsostatic
                .GetMessage(string.Join("\n", failedLines));
            _logger.LogWarning(failMsg, nameof(ComputeValidationService));
            MessageBox.Show(failMsg, "Échec de la validation", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private async Task<bool> IsIsoModelAsync()
        {
            if (!ModelValidationHelper.CheckModelActif(true)) return false;

            var (partId1, partId2) = await GetTwoRandomPartIdsAsync();
            if (partId1 == 0 || partId2 == 0) return false;

            var modelData = await _databaseService.GetAllModelDataAsync();
            var computeModelData = ComputeMapper.ToComputeModelData(modelData);
            var fixPart = await _databaseService.GetFixedPartAsync();

            return await _computeEngine.IsModelIsostaticAsync(computeModelData, partId1, partId2, fixPart.Id);
        }

        private async Task<(int partId1, int partId2)> GetTwoRandomPartIdsAsync()
        {
            var parts = await _databaseService.GetAllPartsAsync();
            var activeParts = parts.Where(p => p.IsActive == true).ToList();

            if (activeParts.Count < 2)
            {
                MessageBox.Show("Pas assez de pièces pour réaliser la vérification.");
                return (0, 0);
            }

            var rng = new Random();
            int partID1 = activeParts[rng.Next(activeParts.Count)].Id;
            int partID2;
            do { partID2 = parts[rng.Next(parts.Count)].Id; }
            while (partID2 == partID1);

            return (partID1, partID2);
        }

    }
}