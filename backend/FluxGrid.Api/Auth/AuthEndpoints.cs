using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluxGrid.Api.Shared.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Seed;
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

            if (user is null)
                return Results.Unauthorized();

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                var remaining = (int)(user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes;
                return Results.Json(new { message = $"Account locked. Try again in {remaining} minutes." }, statusCode: 401);
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;
                var maxAttempts = int.Parse(config["Security:Lockout:MaxFailedAttempts"] ?? "5");
                var lockoutMinutes = int.Parse(config["Security:Lockout:LockoutMinutes"] ?? "15");

                if (user.FailedLoginAttempts >= maxAttempts)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(lockoutMinutes);
                }

                await db.SaveChangesAsync();
                return Results.Unauthorized();
            }

            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await db.SaveChangesAsync();

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
                new(ClaimTypes.Role, string.Join(",", user.Roles.Select(r => r.Name))),
                new("tenant_id", DataSeeder.DefaultTenantId.ToString())
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
