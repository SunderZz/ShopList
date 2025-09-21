using ListeDeCourses.Api.DTOs;

namespace ListeDeCourses.Api.Validators;

internal static class ValidationHelpers
{
    private static readonly HashSet<string> AllowedUnits =
        new(StringComparer.OrdinalIgnoreCase) { "g", "kg", "paquet" };

    public static bool BeAllowedUnit(string? unit)
        => unit is null || unit.Trim().Length == 0 || AllowedUnits.Contains(unit.Trim());

    public static int NameMin => DtoConstraints.NameMin;
    public static int NameMax => DtoConstraints.NameMax;
    public static int IngredientNameMax => DtoConstraints.IngredientNameMax;
    public static int AisleMax => DtoConstraints.AisleMax;
    public static int UnitMax => DtoConstraints.UnitMax;
    public static int EmailMax => DtoConstraints.EmailMax;
    public static int PseudoMin => DtoConstraints.PseudoMin;
    public static int PseudoMax => DtoConstraints.PseudoMax;
    public static int PasswordMin => DtoConstraints.PasswordMin;
    public static int PasswordMax => DtoConstraints.PasswordMax;
}
