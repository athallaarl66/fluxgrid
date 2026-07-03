using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluxGrid.Api.Shared.RBAC;
using Microsoft.IdentityModel.Tokens;

namespace FluxGrid.Api.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/login", (LoginRequest request, IConfiguration config) =>
        {
            if (request.Username != "admin" || request.Password != "admin123")
                return Results.Unauthorized();

            var secretKey = config["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT secret not configured");
            var issuer = config["Jwt:Issuer"] ?? "FluxGrid";
            var audience = config["Jwt:Audience"] ?? "FluxGrid";
            var expiry = int.Parse(config["Jwt:ExpiryMinutes"] ?? "60");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-001"),
                new Claim(ClaimTypes.Email, "admin@fluxgrid.com"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("permissions", string.Join(",", Permissions.All))
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiry),
                signingCredentials: credentials
            );

            return Results.Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiresAt = token.ValidTo
            });
        })
        .AllowAnonymous();
    }
}

public record LoginRequest(string Username, string Password);
