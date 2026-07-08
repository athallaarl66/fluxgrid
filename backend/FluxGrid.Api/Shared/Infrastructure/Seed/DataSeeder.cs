using FluxGrid.Api.Shared.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.RBAC;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Shared.Infrastructure.Seed;

public static class DataSeeder
{
    public static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(AppDbContext db)
    {
        var seedPassword = Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD");

        if (await db.Roles.AnyAsync())
        {
            var existingAdmin = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");
            if (existingAdmin is not null)
            {
                if (!string.IsNullOrEmpty(seedPassword))
                {
                    existingAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(seedPassword);
                    existingAdmin.FailedLoginAttempts = 0;
                    existingAdmin.LockoutEnd = null;
                    Console.WriteLine("Admin password synced from SEED_ADMIN_PASSWORD.");
                }

                if (existingAdmin.TenantId == Guid.Empty)
                {
                    existingAdmin.TenantId = DefaultTenantId;
                    Console.WriteLine("Admin TenantId synced to default.");
                }

                await db.SaveChangesAsync();
            }

            await FinanceDataSeeder.SeedAsync(db, DefaultTenantId);
            return;
        }

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
                Permissions.FinanceRead, Permissions.FinanceWrite, Permissions.FinanceCoaRead, Permissions.FinanceCoaManage, Permissions.FinanceReportRead,
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
                Permissions.FinanceRead, Permissions.FinanceCoaRead, Permissions.FinanceReportRead,
                Permissions.HrRead,
                Permissions.TaskRead, Permissions.TaskWrite
            ]
        };

        db.Roles.AddRange(adminRole, managerRole, staffRole);

        if (!string.IsNullOrEmpty(seedPassword))
        {
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(seedPassword),
                Email = "admin@fluxgrid.com",
                IsActive = true,
                MustChangePassword = false,
                TenantId = DefaultTenantId,
                Roles = [adminRole]
            };

            db.Users.Add(adminUser);
        }
        else
        {
            Console.WriteLine("WARNING: SEED_ADMIN_PASSWORD not set. Admin user will not be seeded.");
        }

        await db.SaveChangesAsync();
        await FinanceDataSeeder.SeedAsync(db, DefaultTenantId);
    }


}
