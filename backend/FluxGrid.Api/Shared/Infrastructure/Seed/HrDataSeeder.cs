using FluxGrid.Api.Modules.HR.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Shared.Infrastructure.Seed;

public static class HrDataSeeder
{
    public static async Task SeedAsync(AppDbContext db, Guid tenantId)
    {
        if (await db.Departments.AnyAsync(d => d.TenantId == tenantId))
            return;

        var departments = new List<Department>
        {
            new() { Id = Guid.NewGuid(), Name = "HR", TenantId = tenantId },
            new() { Id = Guid.NewGuid(), Name = "IT", TenantId = tenantId },
            new() { Id = Guid.NewGuid(), Name = "Finance", TenantId = tenantId }
        };
        db.Departments.AddRange(departments);

        var salaryGrades = new List<SalaryGrade>
        {
            new() { Id = Guid.NewGuid(), Grade = "Junior", MinSalary = 5_000_000m, MaxSalary = 10_000_000m, TenantId = tenantId },
            new() { Id = Guid.NewGuid(), Grade = "Mid", MinSalary = 10_000_000m, MaxSalary = 20_000_000m, TenantId = tenantId },
            new() { Id = Guid.NewGuid(), Grade = "Senior", MinSalary = 20_000_000m, MaxSalary = 35_000_000m, TenantId = tenantId },
            new() { Id = Guid.NewGuid(), Grade = "Lead", MinSalary = 35_000_000m, MaxSalary = 55_000_000m, TenantId = tenantId },
            new() { Id = Guid.NewGuid(), Grade = "Executive", MinSalary = 55_000_000m, MaxSalary = 100_000_000m, TenantId = tenantId }
        };
        db.SalaryGrades.AddRange(salaryGrades);
        await db.SaveChangesAsync();

        var adminUser = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        if (adminUser is not null && !await db.Employees.AnyAsync(e => e.TenantId == tenantId))
        {
            var hrDept = departments[0];
            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                UserId = adminUser.Id,
                EmployeeNo = "EMP-001",
                FirstName = "System",
                LastName = "Admin",
                Email = adminUser.Email,
                DepartmentId = hrDept.Id,
                JobTitle = "CEO",
                Status = "ACTIVE",
                HireDate = DateTime.UtcNow,
                TenantId = tenantId
            };
            db.Employees.Add(employee);
            await db.SaveChangesAsync();
        }
    }
}
