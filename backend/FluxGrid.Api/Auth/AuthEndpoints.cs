using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FluxGrid.Api.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/login", async (LoginRequest request, IConfiguration config, AppDbContext db) =>
        {
            var user = await db.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

            if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Results.Unauthorized();

            var secretKey = config["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT secret not configured");
            var issuer = config["Jwt:Issuer"] ?? "FluxGrid";
            var audience = config["Jwt:Audience"] ?? "FluxGrid";
            var expiry = int.Parse(config["Jwt:ExpiryMinutes"] ?? "60");

            var allPermissions = user.Roles
                .SelectMany(r => r.Permissions)
                .Distinct()
                .ToList();

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, string.Join(",", user.Roles.Select(r => r.Name)))
            };

            foreach (var permission in allPermissions)
            {
                claims.Add(new Claim("permissions", permission));
            }

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
