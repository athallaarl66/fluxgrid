using FluxGrid.Api.Shared.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.RBAC;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Shared.Infrastructure.Seed;

public static class DemoSeeder
{
    public const string DemoUsername = "demo";
    // ponypaper: fixed public password for demo-only tenant; swap to env var when prod matters.
    public const string DemoPassword = "Demo@12345";

    public static async Task SeedAsync(AppDbContext db)
    {
        var existingRole = await db.Roles.FirstOrDefaultAsync(r => r.TenantId == DataSeeder.DemoTenantId && r.Name == "Staff");
        Role staffRole;
        if (existingRole is null)
        {
            staffRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = "Staff",
                Description = "Basic operational access",
                TenantId = DataSeeder.DemoTenantId,
                Permissions =
                [
                    Permissions.DashboardRead,
                    Permissions.WmsRead,
                    Permissions.FinanceRead, Permissions.FinanceCoaRead, Permissions.FinanceReportRead,
                    Permissions.HrRead,
                    Permissions.TaskRead, Permissions.TaskWrite
                ]
            };
            db.Roles.Add(staffRole);
            await db.SaveChangesAsync();
        }
        else
        {
            staffRole = existingRole;
        }

        if (!await db.Users.AnyAsync(u => u.TenantId == DataSeeder.DemoTenantId && u.Username == DemoUsername))
        {
            var demoUser = new User
            {
                Id = Guid.NewGuid(),
                Username = DemoUsername,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword, workFactor: 12),
                Email = "demo@fluxgrid.com",
                IsActive = true,
                MustChangePassword = false,
                TenantId = DataSeeder.DemoTenantId,
                Roles = [staffRole]
            };

            db.Users.Add(demoUser);
            await db.SaveChangesAsync();
        }

        await FinanceDataSeeder.SeedAsync(db, DataSeeder.DemoTenantId);
        await WmsDataSeeder.SeedAsync(db, DataSeeder.DemoTenantId);
        await HrDataSeeder.SeedAsync(db, DataSeeder.DemoTenantId);
        await RecruitmentDataSeeder.SeedAsync(db, DataSeeder.DemoTenantId);
    }
}
