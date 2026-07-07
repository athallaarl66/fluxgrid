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
            if (existingAdmin is not null && !string.IsNullOrEmpty(seedPassword))
            {
                existingAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(seedPassword);
                existingAdmin.FailedLoginAttempts = 0;
                existingAdmin.LockoutEnd = null;
                await db.SaveChangesAsync();
                Console.WriteLine("Admin password synced from SEED_ADMIN_PASSWORD.");
            }

            await ChartOfAccountSeeder.SeedAsync(db, DefaultTenantId);
            await AccountingPeriodSeeder.SeedAsync(db, DefaultTenantId);
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
                Permissions.FinanceRead, Permissions.FinanceWrite, Permissions.FinanceCoaRead, Permissions.FinanceCoaManage,
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
                Permissions.FinanceRead, Permissions.FinanceCoaRead,
                Permissions.HrRead,
                Permissions.TaskRead, Permissions.TaskWrite
            ]
        };

        db.Roles.AddRange(adminRole, managerRole, staffRole);

        if (string.IsNullOrEmpty(seedPassword))
        {
            seedPassword = GenerateRandomPassword();
            Console.WriteLine($"=== SEED ADMIN PASSWORD: {seedPassword} ===");
            Console.WriteLine("Set SEED_ADMIN_PASSWORD env var for a custom password.");
        }

        Console.WriteLine("Change this password on first login.");

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(seedPassword),
            Email = "admin@fluxgrid.com",
            IsActive = true,
            MustChangePassword = true,
            Roles = [adminRole]
        };

        db.Users.Add(adminUser);
        await db.SaveChangesAsync();

        await ChartOfAccountSeeder.SeedAsync(db, DefaultTenantId);
        await AccountingPeriodSeeder.SeedAsync(db, DefaultTenantId);
    }

    private static string GenerateRandomPassword()
    {
        var random = new Random();
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()-_=+";

        var chars = new char[12];
        chars[0] = upper[random.Next(upper.Length)];
        chars[1] = lower[random.Next(lower.Length)];
        chars[2] = digits[random.Next(digits.Length)];
        chars[3] = special[random.Next(special.Length)];

        var all = upper + lower + digits + special;
        for (int i = 4; i < chars.Length; i++)
            chars[i] = all[random.Next(all.Length)];

        return new string(chars.OrderBy(_ => random.Next()).ToArray());
    }
}
