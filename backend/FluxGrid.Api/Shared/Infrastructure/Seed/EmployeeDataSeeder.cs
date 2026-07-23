using FluxGrid.Api.Modules.HR.Domain.Entities;
using FluxGrid.Api.Modules.HR.Domain.Enums;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Shared.Infrastructure.Seed;

public static class EmployeeDataSeeder
{
    private static DateTime U(int y, int m, int d) => new(y, m, d, 0, 0, 0, DateTimeKind.Utc);

    public static async Task<List<Employee>> SeedAsync(AppDbContext db, Guid tenantId)
    {
        if (await db.Employees.AnyAsync(e => e.TenantId == tenantId))
            return await db.Employees.Where(e => e.TenantId == tenantId).ToListAsync();

        var depts = await db.Departments.Where(d => d.TenantId == tenantId).ToListAsync();
        var grades = await db.SalaryGrades.Where(g => g.TenantId == tenantId).ToListAsync();

        var hr = depts.First(d => d.Name == "HR");
        var it = depts.First(d => d.Name == "IT");
        var finance = depts.First(d => d.Name == "Finance");

        var mid = grades.First(g => g.Grade == "Mid");
        var sr = grades.First(g => g.Grade == "Senior");
        var lead = grades.First(g => g.Grade == "Lead");
        var exec = grades.First(g => g.Grade == "Executive");

        var admin = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        var adminId = admin?.Id ?? Guid.NewGuid();

        var employees = new List<Employee>
        {
            new() { Id = Guid.NewGuid(), EmployeeNo = "EMP-001", FirstName = "Budi", LastName = "Santoso", Email = "budi@fluxgrid.com", Phone = "081234567890", JobTitle = "Chief Executive Officer", DepartmentId = it.Id, Status = EmployeeStatus.Active, BaseSalary = 85_000_000, HireDate = U(2020, 1, 1), TenantId = tenantId },
            new() { Id = Guid.NewGuid(), EmployeeNo = "EMP-002", FirstName = "Siti", LastName = "Rahayu", Email = "siti@fluxgrid.com", Phone = "081234567891", JobTitle = "HR Director", DepartmentId = hr.Id, ManagerId = null, Status = EmployeeStatus.Active, BaseSalary = 45_000_000, HireDate = U(2020, 3, 15), TenantId = tenantId },
            new() { Id = Guid.NewGuid(), EmployeeNo = "EMP-003", FirstName = "Ahmad", LastName = "Hidayat", Email = "ahmad@fluxgrid.com", Phone = "081234567892", JobTitle = "IT Director", DepartmentId = it.Id, Status = EmployeeStatus.Active, BaseSalary = 50_000_000, HireDate = U(2020, 2, 1), TenantId = tenantId },
            new() { Id = Guid.NewGuid(), EmployeeNo = "EMP-004", FirstName = "Dewi", LastName = "Kurniawan", Email = "dewi@fluxgrid.com", Phone = "081234567893", JobTitle = "Finance Director", DepartmentId = finance.Id, Status = EmployeeStatus.Active, BaseSalary = 48_000_000, HireDate = U(2020, 4, 1), TenantId = tenantId },
            new() { Id = Guid.NewGuid(), EmployeeNo = "EMP-005", FirstName = "Rudi", LastName = "Hartono", Email = "rudi@fluxgrid.com", Phone = "081234567894", JobTitle = "Senior HR Specialist", DepartmentId = hr.Id, Status = EmployeeStatus.Active, BaseSalary = 18_000_000, HireDate = U(2021, 6, 1), TenantId = tenantId },
            new() { Id = Guid.NewGuid(), EmployeeNo = "EMP-006", FirstName = "Maya", LastName = "Anggraini", Email = "maya@fluxgrid.com", Phone = "081234567895", JobTitle = "HR Staff", DepartmentId = hr.Id, Status = EmployeeStatus.Active, BaseSalary = 8_500_000, HireDate = U(2022, 8, 15), TenantId = tenantId },
            new() { Id = Guid.NewGuid(), EmployeeNo = "EMP-007", FirstName = "Dimas", LastName = "Prasetyo", Email = "dimas@fluxgrid.com", Phone = "081234567896", JobTitle = "Senior Software Engineer", DepartmentId = it.Id, Status = EmployeeStatus.Active, BaseSalary = 22_000_000, HireDate = U(2021, 3, 1), TenantId = tenantId },
            new() { Id = Guid.NewGuid(), EmployeeNo = "EMP-008", FirstName = "Rina", LastName = "Wijaya", Email = "rina@fluxgrid.com", Phone = "081234567897", JobTitle = "Software Engineer", DepartmentId = it.Id, Status = EmployeeStatus.Active, BaseSalary = 14_000_000, HireDate = U(2022, 1, 10), TenantId = tenantId },
            new() { Id = Guid.NewGuid(), EmployeeNo = "EMP-009", FirstName = "Agus", LastName = "Nugroho", Email = "agus@fluxgrid.com", Phone = "081234567898", JobTitle = "Junior Software Engineer", DepartmentId = it.Id, Status = EmployeeStatus.Active, BaseSalary = 8_000_000, HireDate = U(2023, 7, 1), TenantId = tenantId },
            new() { Id = Guid.NewGuid(), EmployeeNo = "EMP-010", FirstName = "Linda", LastName = "Susanti", Email = "linda@fluxgrid.com", Phone = "081234567899", JobTitle = "QA Engineer", DepartmentId = it.Id, Status = EmployeeStatus.Active, BaseSalary = 12_000_000, HireDate = U(2022, 5, 20), TenantId = tenantId },
            new() { Id = Guid.NewGuid(), EmployeeNo = "EMP-011", FirstName = "Hendra", LastName = "Gunawan", Email = "hendra@fluxgrid.com", Phone = "081234567800", JobTitle = "Senior Accountant", DepartmentId = finance.Id, Status = EmployeeStatus.Active, BaseSalary = 16_000_000, HireDate = U(2021, 4, 1), TenantId = tenantId },
            new() { Id = Guid.NewGuid(), EmployeeNo = "EMP-012", FirstName = "Nina", LastName = "Puspita", Email = "nina@fluxgrid.com", Phone = "081234567801", JobTitle = "Accountant", DepartmentId = finance.Id, Status = EmployeeStatus.Active, BaseSalary = 9_500_000, HireDate = U(2022, 9, 1), TenantId = tenantId },
            new() { Id = Guid.NewGuid(), EmployeeNo = "EMP-013", FirstName = "Adi", LastName = "Saputra", Email = "adi@fluxgrid.com", Phone = "081234567802", JobTitle = "Finance Staff", DepartmentId = finance.Id, Status = EmployeeStatus.Active, BaseSalary = 6_500_000, HireDate = U(2023, 1, 15), TenantId = tenantId },
            new() { Id = Guid.NewGuid(), EmployeeNo = "EMP-014", FirstName = "Tari", LastName = "Ramadhani", Email = "tari@fluxgrid.com", Phone = "081234567803", JobTitle = "DevOps Engineer", DepartmentId = it.Id, Status = EmployeeStatus.Active, BaseSalary = 20_000_000, HireDate = U(2022, 3, 1), TenantId = tenantId },
            new() { Id = Guid.NewGuid(), EmployeeNo = "EMP-015", FirstName = "Eko", LastName = "Wahyudi", Email = "eko@fluxgrid.com", Phone = "081234567804", JobTitle = "UI/UX Designer", DepartmentId = it.Id, Status = EmployeeStatus.Active, BaseSalary = 13_000_000, HireDate = U(2022, 11, 1), TenantId = tenantId },
            new() { Id = Guid.NewGuid(), EmployeeNo = "EMP-016", FirstName = "Fitri", LastName = "Handayani", Email = "fitri@fluxgrid.com", Phone = "081234567805", JobTitle = "Payroll Specialist", DepartmentId = hr.Id, Status = EmployeeStatus.Active, BaseSalary = 11_000_000, HireDate = U(2022, 6, 1), TenantId = tenantId },
            new() { Id = Guid.NewGuid(), EmployeeNo = "EMP-017", FirstName = "Bambang", LastName = "Susilo", Email = "bambang@fluxgrid.com", Phone = "081234567806", JobTitle = "Senior IT Support", DepartmentId = it.Id, Status = EmployeeStatus.Terminated, BaseSalary = 10_000_000, HireDate = U(2021, 8, 1), TerminationDate = U(2024, 2, 28), TenantId = tenantId },
            new() { Id = Guid.NewGuid(), EmployeeNo = "EMP-018", FirstName = "Dian", LastName = "Permata", Email = "dian@fluxgrid.com", Phone = "081234567807", JobTitle = "Marketing Staff", DepartmentId = hr.Id, Status = EmployeeStatus.Terminated, BaseSalary = 7_000_000, HireDate = U(2022, 4, 1), TerminationDate = U(2024, 1, 15), TenantId = tenantId },
        };

        // Set manager hierarchy
        employees[0].UserId = adminId; // CEO = admin user
        employees[1].ManagerId = employees[0].Id; // HR Dir → CEO
        employees[2].ManagerId = employees[0].Id; // IT Dir → CEO
        employees[3].ManagerId = employees[0].Id; // Finance Dir → CEO
        employees[4].ManagerId = employees[1].Id; // Sr HR Spec → HR Dir
        employees[5].ManagerId = employees[4].Id; // HR Staff → Sr HR Spec
        employees[6].ManagerId = employees[2].Id; // Sr SWE → IT Dir
        employees[7].ManagerId = employees[6].Id; // SWE → Sr SWE
        employees[8].ManagerId = employees[6].Id; // Jr SWE → Sr SWE
        employees[9].ManagerId = employees[2].Id; // QA → IT Dir
        employees[10].ManagerId = employees[3].Id; // Sr Acct → Finance Dir
        employees[11].ManagerId = employees[10].Id; // Acct → Sr Acct
        employees[12].ManagerId = employees[10].Id; // Finance Staff → Sr Acct
        employees[13].ManagerId = employees[2].Id; // DevOps → IT Dir
        employees[14].ManagerId = employees[2].Id; // UI/UX → IT Dir
        employees[15].ManagerId = employees[1].Id; // Payroll Spec → HR Dir

        db.Employees.AddRange(employees);
        await db.SaveChangesAsync();

        return employees;
    }
}
