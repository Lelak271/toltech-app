namespace TOLTECH_APPLICATION.ToltechCalculation.Validation
{
    public enum ValidationCategory
    {
        DirectionVector,
        MissingOrigin,
        EmptyPartReference,
        MissingPartInGraph,
        Isostatic,
        GraphCoherence
    }

    /// <summary>
    /// Règle de validation : catégorie + message paramétrable par les détails.
    /// Pour ajouter une règle, déclarer une propriété dans <see cref="ValidationRuleRegistry"/>.
    /// </summary>
    public sealed class ValidationRule
    {
        public ValidationCategory Category { get; init; }
        public Func<string, string> MessageTemplate { get; init; }

        public string GetMessage(string details = "")
            => MessageTemplate(details);
    }

    /// <summary>
    /// Registre de toutes les règles de validation métier.
    /// Chaque règle porte sa catégorie et son message paramétrable.
    /// </summary>
    public static class ValidationRuleRegistry
    {
        public static readonly ValidationRule RequirementZeroDirection = new()
        {
            Category        = ValidationCategory.DirectionVector,
            MessageTemplate = details =>
                $"• Certaines exigences ont une direction nulle (U, V, W = 0) :\n  - {details}"
        };

        public static readonly ValidationRule ContactZeroDirection = new()
        {
            Category        = ValidationCategory.DirectionVector,
            MessageTemplate = details =>
                $"• Certains contacts ont une direction nulle (U, V, W = 0) :\n  - {details}"
        };

        public static readonly ValidationRule MissingPartOrigin = new()
        {
            Category        = ValidationCategory.MissingOrigin,
            MessageTemplate = details =>
                string.IsNullOrEmpty(details)
                    ? "• La pièce n'a pas d'origine renseignée dans le modèle."
                    : $"• Certaines pièces n'ont pas d'origine renseignée :\n"
        };

        public static readonly ValidationRule RequirementMissingPart = new()
        {
            Category        = ValidationCategory.EmptyPartReference,
            MessageTemplate = _ =>
                "• Certaines exigences n'ont pas de pièces définies (PartReq1Id ou PartReq2Id = 0 / null)."
        };

        public static readonly ValidationRule PartMissingFromGraph = new()
        {
            Category        = ValidationCategory.MissingPartInGraph,
            MessageTemplate = details =>
                $"• Des pièces référencées dans les exigences sont absentes du graphe : {details}"
        };

        public static readonly ValidationRule PartNotIsostatic = new()
        {
            Category        = ValidationCategory.Isostatic,
            MessageTemplate = details =>
                string.IsNullOrEmpty(details)
                    ? "• La pièce n'est pas isostatique. Vérifiez les liaisons et les degrés de liberté contraints."
                    : $"• Plusieurs pièces ne sont pas isostatiques : \n{details}"
        };

        public static readonly ValidationRule ModelNotIsostatic = new()
        {
            Category        = ValidationCategory.Isostatic,
            MessageTemplate = _ =>
                "• Le système global n'est pas isostatique. Vérifiez la cohérence de l'ensemble des liaisons."
        };

        public static readonly ValidationRule GraphIncoherent = new()
        {
            Category        = ValidationCategory.GraphCoherence,
            MessageTemplate = _ =>
                "• La structure du graphe ou les données associées sont invalides. Vérifiez la cohérence du graphe de liaisons."
        };

        public static readonly ValidationRule NoDataAvailable = new()
        {
            Category        = ValidationCategory.GraphCoherence,
            MessageTemplate = _ =>
                "La validation n'a pas pu être effectuée.\nAucune donnée exploitable n'a été retournée."
        };

        public static readonly ValidationRule ValidationSuccess = new()
        {
            Category        = ValidationCategory.GraphCoherence,
            MessageTemplate = details =>
                string.IsNullOrEmpty(details)
                    ? "Toutes les pièces sont correctement contraintes."
                    : details
        };
    }
}
