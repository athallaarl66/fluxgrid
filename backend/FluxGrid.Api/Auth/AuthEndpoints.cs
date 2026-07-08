using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using FluxGrid.Api.Shared.Domain.Entities;
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

            if (user.MustChangePassword)
            {
                return Results.Json(new { password_change_required = true }, statusCode: 200);
            }

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
                new("tenant_id", user.TenantId.ToString())
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

        app.MapGet("/api/auth/me", async (HttpContext http, AppDbContext db) =>
        {
            var userIdStr = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdStr is null || !Guid.TryParse(userIdStr, out var userId))
                return Results.Unauthorized();

            var user = await db.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user is null)
                return Results.Unauthorized();

            return Results.Ok(new
            {
                user = new
                {
                    id = user.Id.ToString(),
                    email = user.Email,
                    name = user.Username,
                    roles = user.Roles.Select(r => r.Name).ToList()
                }
            });
        })
        .RequireAuthorization();

        app.MapPost("/api/auth/change-password", async (ChangePasswordRequest request, IConfiguration config, AppDbContext db) =>
        {
            var user = await db.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

            if (user is null)
                return Results.Json(new { message = "User not found." }, statusCode: 404);

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
                return Results.Json(new { message = "Old password is incorrect." }, statusCode: 400);

            if (request.NewPassword != request.ConfirmNewPassword)
                return Results.Json(new { message = "New password and confirmation do not match." }, statusCode: 400);

            var strengthError = ValidatePasswordStrength(request.NewPassword);
            if (strengthError is not null)
                return Results.Json(new { message = strengthError }, statusCode: 400);

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.MustChangePassword = false;
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
                new("tenant_id", user.TenantId.ToString())
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

    private static string? ValidatePasswordStrength(string password)
    {
        if (password.Length < 8)
            return "Password must be at least 8 characters long.";

        if (!Regex.IsMatch(password, "[A-Z]"))
            return "Password must contain at least one uppercase letter.";

        if (!Regex.IsMatch(password, "[a-z]"))
            return "Password must contain at least one lowercase letter.";

        if (!Regex.IsMatch(password, "[0-9]"))
            return "Password must contain at least one digit.";

        if (!Regex.IsMatch(password, "[!@#$%^&*()\\-_=+\\[{\\]};:',.<>?/`~]"))
            return "Password must contain at least one special character.";

        return null;
    }
}

public record LoginRequest(string Username, string Password);
public record ChangePasswordRequest(string Username, string OldPassword, string NewPassword, string ConfirmNewPassword);
