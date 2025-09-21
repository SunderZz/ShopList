using System.ComponentModel.DataAnnotations;

namespace ListeDeCourses.Api.Settings;

public class JwtSettings
{
    [Required, MinLength(1)]
    public string Issuer { get; set; } = "ListeDeCourses.Api";

    [Required, MinLength(1)]
    public string Audience { get; set; } = "ListeDeCourses.Client";

    [Required, MinLength(32, ErrorMessage = "JWT key must be at least 32 characters long.")]
    public string Key { get; set; } = "CHANGE_ME_SUPER_SECRET_MIN_32_CHARS";

    [Range(1, 24 * 60, ErrorMessage = "Expiration must be between 1 minute and 24h.")]
    public int ExpirationMinutes { get; set; } = 120;
}
