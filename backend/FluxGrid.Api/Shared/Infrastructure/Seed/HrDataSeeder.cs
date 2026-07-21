using FluxGrid.Api.Modules.HR.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Shared.Infrastructure.Seed;

public static class HrDataSeeder
{
    public static async Task SeedAsync(AppDbContext db, Guid tenantId)
    {
        if (!await db.Departments.AnyAsync(d => d.TenantId == tenantId))
        {
            db.Departments.AddRange(
                new Department { Id = Guid.NewGuid(), Name = "HR", TenantId = tenantId },
                new Department { Id = Guid.NewGuid(), Name = "IT", TenantId = tenantId },
                new Department { Id = Guid.NewGuid(), Name = "Finance", TenantId = tenantId }
            );
        }

        if (!await db.SalaryGrades.AnyAsync(g => g.TenantId == tenantId))
        {
            db.SalaryGrades.AddRange(
                new SalaryGrade { Id = Guid.NewGuid(), Grade = "Junior", MinSalary = 5_000_000m, MaxSalary = 10_000_000m, TenantId = tenantId },
                new SalaryGrade { Id = Guid.NewGuid(), Grade = "Mid", MinSalary = 10_000_000m, MaxSalary = 20_000_000m, TenantId = tenantId },
                new SalaryGrade { Id = Guid.NewGuid(), Grade = "Senior", MinSalary = 20_000_000m, MaxSalary = 35_000_000m, TenantId = tenantId },
                new SalaryGrade { Id = Guid.NewGuid(), Grade = "Lead", MinSalary = 35_000_000m, MaxSalary = 55_000_000m, TenantId = tenantId },
                new SalaryGrade { Id = Guid.NewGuid(), Grade = "Executive", MinSalary = 55_000_000m, MaxSalary = 100_000_000m, TenantId = tenantId }
            );
        }

        await db.SaveChangesAsync();

        await EmployeeDataSeeder.SeedAsync(db, tenantId);
        await PayrollDataSeeder.SeedAsync(db, tenantId);
        await RecruitmentDataSeeder.SeedAsync(db, tenantId);

        await SyncAdminPassword(db, tenantId);
    }

    private static async Task SyncAdminPassword(AppDbContext db, Guid tenantId)
    {
        var adminUser = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        if (adminUser is null) return;

        var seedPassword = Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD");
        if (!string.IsNullOrEmpty(seedPassword))
        {
            adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(seedPassword);
            adminUser.FailedLoginAttempts = 0;
            adminUser.LockoutEnd = null;
        }

        if (adminUser.TenantId == Guid.Empty)
            adminUser.TenantId = tenantId;

        await db.SaveChangesAsync();
    }
}
