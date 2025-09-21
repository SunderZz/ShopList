using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ListeDeCourses.Api.Models;
using ListeDeCourses.Api.Settings;
using Microsoft.IdentityModel.Tokens;

namespace ListeDeCourses.Api.Auth
{
    public static class JwtTokenGenerator
    {
        private static readonly JwtSecurityTokenHandler Handler = new();

        private static class CustomClaimNames
        {
            public const string Pseudo = "pseudo";
            public const string IsSuperUser = "isSuperUser";
        }

        public static string Generate(Utilisateur user, JwtSettings settings, IEnumerable<Claim>? extraClaims = null)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(settings);

            if (string.IsNullOrWhiteSpace(settings.Key))
                throw new ArgumentException("JWT Key is missing in settings.", nameof(settings));

            if (settings.ExpirationMinutes <= 0)
                throw new ArgumentException("JWT expiration must be greater than zero.", nameof(settings));

            var now = DateTime.UtcNow;

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var claims = BuildClaims(user, extraClaims);

            var token = new JwtSecurityToken(
                issuer: settings.Issuer,
                audience: settings.Audience,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(settings.ExpirationMinutes),
                signingCredentials: signingCredentials
            );

            return Handler.WriteToken(token);
        }

        private static IEnumerable<Claim> BuildClaims(Utilisateur user, IEnumerable<Claim>? extraClaims)
        {
            yield return new Claim(JwtRegisteredClaimNames.Sub, user.Id ?? string.Empty);
            yield return new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(user.Pseudo))
                yield return new Claim(CustomClaimNames.Pseudo, user.Pseudo);

            yield return new Claim(CustomClaimNames.IsSuperUser, user.IsSuperUser.ToString().ToLowerInvariant());

            if (extraClaims is not null)
            {
                foreach (var c in extraClaims)
                    yield return c;
            }
        }
    }
}
