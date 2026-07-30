using FluxGrid.Api.Shared.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.Admin.API;

public static class RolesEndpoints
{
    public static void MapRolesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/roles")
            .RequireAuthorization()
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var roles = await db.Roles
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    Permissions = r.Permissions,
                    UserCount = r.Users.Count
                })
                .ToListAsync();

            return Results.Ok(roles);
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var role = await db.Roles
                .Where(r => r.Id == id)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    Permissions = r.Permissions,
                    UserCount = r.Users.Count
                })
                .FirstOrDefaultAsync();

            if (role is null)
                return Results.NotFound(new { message = "Role not found." });

            return Results.Ok(role);
        });

        group.MapPost("/", async (CreateRoleRequest request, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
                return Results.Json(new { message = "Name is required (max 100 chars)." }, statusCode: 400);

            if (await db.Roles.AnyAsync(r => r.Name == request.Name))
                return Results.Json(new { message = "Role name already exists." }, statusCode: 409);

            var role = new Role
            {
                Name = request.Name,
                Description = request.Description,
                Permissions = request.Permissions ?? []
            };

            db.Roles.Add(role);
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                id = role.Id.ToString(),
                name = role.Name,
                message = "Role created successfully."
            });
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateRoleRequest request, AppDbContext db) =>
        {
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == id);
            if (role is null)
                return Results.NotFound(new { message = "Role not found." });

            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
                return Results.Json(new { message = "Name is required (max 100 chars)." }, statusCode: 400);

            if (await db.Roles.AnyAsync(r => r.Name == request.Name && r.Id != id))
                return Results.Json(new { message = "Role name already exists." }, statusCode: 409);

            role.Name = request.Name;
            role.Description = request.Description;
            role.Permissions = request.Permissions ?? [];

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                id = role.Id.ToString(),
                name = role.Name,
                message = "Role updated successfully."
            });
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var role = await db.Roles.Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == id);
            if (role is null)
                return Results.NotFound(new { message = "Role not found." });

            if (role.Users.Any())
                return Results.Json(new { message = "Cannot delete role with assigned users." }, statusCode: 409);

            db.Roles.Remove(role);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Role deleted successfully." });
        });
    }
}

public record RoleDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public List<string> Permissions { get; init; } = [];
    public int UserCount { get; init; }
}

public record CreateRoleRequest(string Name, string? Description, List<string>? Permissions);
public record UpdateRoleRequest(string Name, string? Description, List<string>? Permissions);
