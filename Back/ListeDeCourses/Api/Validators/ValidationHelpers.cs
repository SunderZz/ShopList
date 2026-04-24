using ListeDeCourses.Api.Common;
using ListeDeCourses.Api.DTOs;

namespace ListeDeCourses.Api.Validators;

internal static class ValidationHelpers
{
    public static bool BeAllowedUnit(string? unit)
        => UnitCatalog.IsAllowed(unit);

    public static bool BeHttpOrHttpsUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)) return false;
        if (!string.IsNullOrEmpty(uri.UserInfo)) return false;
        return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }

    public static string AllowedUnitsList => string.Join(", ", UnitCatalog.AllowedUnits);

    public static int NameMin => DtoConstraints.NameMin;
    public static int NameMax => DtoConstraints.NameMax;
    public static int IngredientNameMax => DtoConstraints.IngredientNameMax;
    public static int AisleMax => DtoConstraints.AisleMax;
    public static int UnitMax => DtoConstraints.UnitMax;
    public static int SourceUrlMax => DtoConstraints.SourceUrlMax;
    public static int EmailMax => DtoConstraints.EmailMax;
    public static int PseudoMin => DtoConstraints.PseudoMin;
    public static int PseudoMax => DtoConstraints.PseudoMax;
    public static int PasswordMin => DtoConstraints.PasswordMin;
    public static int PasswordMax => DtoConstraints.PasswordMax;
}
