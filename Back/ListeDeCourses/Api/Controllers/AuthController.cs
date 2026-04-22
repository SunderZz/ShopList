using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ListeDeCourses.Api.Auth;
using ListeDeCourses.Api.DTOs;
using ListeDeCourses.Api.Models;
using ListeDeCourses.Api.Repositories;
using ListeDeCourses.Api.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ListeDeCourses.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly JwtSettings _jwt;
        private readonly UtilisateurRepository _users;

        public AuthController(IOptions<JwtSettings> jwtOptions, UtilisateurRepository users)
        {
            _jwt = jwtOptions.Value;
            _users = users;
        }

        public sealed class LoginRequest
        {
            [Required, EmailAddress] public required string Email { get; init; }
            [Required, MinLength(3)] public required string Password { get; init; }
        }

        public sealed record AuthUserResponse(string Id, string Email, string Pseudo, bool IsSuperUser);

        public sealed record LoginResponse(string Token, AuthUserResponse User);

        public sealed class RegisterRequest
        {
            [Required, EmailAddress] public required string Email { get; init; }
            [Required, MinLength(DtoConstraints.PseudoMin)] public required string Pseudo { get; init; }
            [Required, MinLength(DtoConstraints.PasswordMin)] public required string Password { get; init; }
        }

        [AllowAnonymous]
        [EnableRateLimiting("Auth")]
        [HttpPost("register")]
        public async Task<ActionResult<AuthUserResponse>> Register([FromBody] RegisterRequest body, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var email = body.Email.Trim();
            var pseudo = body.Pseudo.Trim();

            var existing = await _users.GetByEmailAsync(email, ct);
            if (existing is not null)
                return Conflict("Email déjà utilisé.");

            var user = new Utilisateur
            {
                Email = email,
                Pseudo = pseudo,
                IsSuperUser = false,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.Password)
            };

            var created = await _users.CreateAsync(user, ct);

            return Ok(ToAuthUserResponse(created));
        }

        [AllowAnonymous]
        [EnableRateLimiting("Auth")]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest body, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var email = body.Email.Trim();

            var user = await _users.GetByEmailAsync(email, ct);
            var valid = user is not null
                        && !string.IsNullOrEmpty(user.PasswordHash)
                        && BCrypt.Net.BCrypt.Verify(body.Password, user.PasswordHash);

            if (!valid)
                return Unauthorized();

            var extraClaims = new[]
            {
                new Claim("role", user!.IsSuperUser ? "superuser" : "user")
            };

            var token = JwtTokenGenerator.Generate(user, _jwt, extraClaims);
            return Ok(new LoginResponse(token, ToAuthUserResponse(user)));
        }

        private static AuthUserResponse ToAuthUserResponse(Utilisateur user) =>
            new(
                user.Id ?? string.Empty,
                user.Email ?? string.Empty,
                user.Pseudo ?? string.Empty,
                user.IsSuperUser
            );

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<object>> Me(CancellationToken ct)
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst("email")?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;

            Utilisateur? dbUser = null;

            if (!string.IsNullOrEmpty(userId))
                dbUser = await _users.GetByIdAsync(userId, ct);

            if (dbUser is null && !string.IsNullOrEmpty(email))
                dbUser = await _users.GetByEmailAsync(email, ct);

            if (dbUser is null)
                return Unauthorized();

            return Ok(new
            {
                id = dbUser.Id ?? string.Empty,
                email = dbUser.Email ?? string.Empty,
                pseudo = dbUser.Pseudo ?? string.Empty,
                isSuperUser = dbUser.IsSuperUser,
                role = dbUser.IsSuperUser ? "superuser" : "user"
            });
        }
    }
}
