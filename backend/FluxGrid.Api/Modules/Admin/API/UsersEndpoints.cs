using System.Security.Claims;
using FluxGrid.Api.Shared.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.Admin.API;

public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/users")
            .RequireAuthorization()
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", async (AppDbContext db, string? search, string? role, int page = 1, int pageSize = 20) =>
        {
            var query = db.Users.Include(u => u.Roles).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(s) ||
                    u.Email.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Roles.Any(r => r.Name == role));
            }

            var total = await query.CountAsync();
            var users = await query
                .OrderBy(u => u.Username)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Name = u.Username,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    Roles = u.Roles.Select(r => r.Name).ToList(),
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(new { users, total, page, pageSize });
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var user = await db.Users
                .Include(u => u.Roles)
                .Where(u => u.Id == id)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Name = u.Username,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    Roles = u.Roles.Select(r => r.Name).ToList(),
                    CreatedAt = u.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (user is null)
                return Results.NotFound(new { message = "User not found." });

            return Results.Ok(user);
        });

        group.MapPost("/", async (CreateUserRequest request, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
                return Results.Json(new { message = "Name is required (max 100 chars)." }, statusCode: 400);

            if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 255)
                return Results.Json(new { message = "Email is required (max 255 chars)." }, statusCode: 400);

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
                return Results.Json(new { message = "Password must be at least 8 characters." }, statusCode: 400);

            if (await db.Users.AnyAsync(u => u.Email == request.Email))
                return Results.Json(new { message = "Email already exists." }, statusCode: 409);

            if (await db.Users.AnyAsync(u => u.Username == request.Name))
                return Results.Json(new { message = "Username already exists." }, statusCode: 409);

            var user = new User
            {
                Username = request.Name,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsActive = true
            };

            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == request.Role);
                if (role is not null)
                    user.Roles.Add(role);
            }

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                id = user.Id.ToString(),
                name = user.Username,
                email = user.Email,
                message = "User created successfully."
            });
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateUserRequest request, AppDbContext db) =>
        {
            var user = await db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == id);
            if (user is null)
                return Results.NotFound(new { message = "User not found." });

            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
                return Results.Json(new { message = "Name is required (max 100 chars)." }, statusCode: 400);

            if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 255)
                return Results.Json(new { message = "Email is required (max 255 chars)." }, statusCode: 400);

            if (await db.Users.AnyAsync(u => u.Email == request.Email && u.Id != id))
                return Results.Json(new { message = "Email already exists." }, statusCode: 409);

            user.Username = request.Name;
            user.Email = request.Email;

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                if (request.Password.Length < 8)
                    return Results.Json(new { message = "Password must be at least 8 characters." }, statusCode: 400);
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }

            user.Roles.Clear();
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == request.Role);
                if (role is not null)
                    user.Roles.Add(role);
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                id = user.Id.ToString(),
                name = user.Username,
                email = user.Email,
                message = "User updated successfully."
            });
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user is null)
                return Results.NotFound(new { message = "User not found." });

            user.IsActive = false;
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "User deactivated successfully." });
        });
    }
}

public record UserDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public List<string> Roles { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}

public record CreateUserRequest(string Name, string Email, string Password, string? Role);
public record UpdateUserRequest(string Name, string Email, string? Password, string? Role);
