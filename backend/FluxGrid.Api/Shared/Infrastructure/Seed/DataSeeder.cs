using FluxGrid.Api.Shared.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.RBAC;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Shared.Infrastructure.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Roles.AnyAsync()) return;

        var adminRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = "Admin",
            Description = "Full system access",
            Permissions = Permissions.All.ToList()
        };

        var managerRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = "Manager",
            Description = "Department-level access",
            Permissions = [
                Permissions.DashboardRead,
                Permissions.WmsRead, Permissions.WmsWrite,
                Permissions.FinanceRead, Permissions.FinanceWrite,
                Permissions.HrRead, Permissions.HrWrite,
                Permissions.TaskRead, Permissions.TaskWrite
            ]
        };

        var staffRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = "Staff",
            Description = "Basic operational access",
            Permissions = [
                Permissions.DashboardRead,
                Permissions.WmsRead,
                Permissions.FinanceRead,
                Permissions.HrRead,
                Permissions.TaskRead, Permissions.TaskWrite
            ]
        };

        db.Roles.AddRange(adminRole, managerRole, staffRole);

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Email = "admin@fluxgrid.com",
            IsActive = true,
            Roles = [adminRole]
        };

        db.Users.Add(adminUser);
        await db.SaveChangesAsync();
    }
}
