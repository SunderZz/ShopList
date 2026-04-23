namespace ListeDeCourses.Api.Common;

public sealed record UnitDefinition(
    string Unit,
    string Family,
    string CanonicalUnit,
    double FactorToCanonical,
    int SortOrder);

public static class UnitCatalog
{
    public const string PieceUnit = "unit\u00E9";

    private static readonly IReadOnlyList<UnitDefinition> Definitions =
    [
        new("g", "mass", "g", 1d, 0),
        new("kg", "mass", "g", 1000d, 1),
        new("ml", "volume", "ml", 1d, 2),
        new("cl", "volume", "ml", 10d, 3),
        new("l", "volume", "ml", 1000d, 4),
        new("paquet", "package", "paquet", 1d, 5),
        new(PieceUnit, "piece", PieceUnit, 1d, 6),
    ];

    private static readonly Dictionary<string, UnitDefinition> DefinitionsByUnit =
        Definitions.ToDictionary(definition => definition.Unit, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> AllowedUnits { get; } =
        Definitions.Select(definition => definition.Unit).ToList();

    public static string? Normalize(string? unit)
    {
        if (string.IsNullOrWhiteSpace(unit)) return null;

        var trimmed = unit.Trim();
        return DefinitionsByUnit.TryGetValue(trimmed, out var definition)
            ? definition.Unit
            : trimmed;
    }

    public static bool IsAllowed(string? unit)
        => string.IsNullOrWhiteSpace(unit) || DefinitionsByUnit.ContainsKey(unit.Trim());

    public static bool TryConvertToCanonical(double? quantity, string? unit, out double canonicalQuantity, out string canonicalUnit)
    {
        canonicalQuantity = default;
        canonicalUnit = string.Empty;

        if (!quantity.HasValue) return false;

        var normalizedUnit = Normalize(unit);
        if (normalizedUnit is null || !DefinitionsByUnit.TryGetValue(normalizedUnit, out var definition))
            return false;

        canonicalQuantity = quantity.Value * definition.FactorToCanonical;
        canonicalUnit = definition.CanonicalUnit;
        return true;
    }

    public static int GetSortOrder(string? unit)
    {
        var normalizedUnit = Normalize(unit);
        return normalizedUnit is not null && DefinitionsByUnit.TryGetValue(normalizedUnit, out var definition)
            ? definition.SortOrder
            : int.MaxValue;
    }
}
