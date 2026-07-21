using FluxGrid.Api.Modules.HR.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Shared.Infrastructure.Seed;

public static class PayrollDataSeeder
{
    public static async Task SeedAsync(AppDbContext db, Guid tenantId)
    {
        if (await db.PayrollRuns.AnyAsync(r => r.TenantId == tenantId))
            return;

        var employees = await db.Employees
            .Where(e => e.TenantId == tenantId && e.Status == "ACTIVE")
            .ToListAsync();

        if (employees.Count == 0) return;

        var rng = new Random(42);
        var now = DateTime.UtcNow;

        for (var month = 1; month <= now.Month; month++)
        {
            var startDate = new DateTime(now.Year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            var periodName = $"{now.Year}-{month:D2}";

            var run = new PayrollRun
            {
                Id = Guid.NewGuid(),
                PeriodName = periodName,
                StartDate = startDate,
                EndDate = endDate,
                Status = "COMPLETED",
                ProcessedBy = "System",
                TenantId = tenantId,
                CreatedAt = endDate.AddDays(1)
            };

            var records = employees.Select(emp =>
            {
                var baseSalary = emp.BaseSalary ?? 8_000_000m;
                var overtimePay = Math.Round(baseSalary * 0.05m * (decimal)(rng.NextDouble() * 0.5 + 0.75), 2);
                var latenessDed = Math.Round(baseSalary * 0.02m * (decimal)(rng.NextDouble() * 0.5), 2);
                var grossPay = baseSalary + overtimePay - latenessDed;
                var taxDed = Math.Round(grossPay * 0.10m, 2);
                var netPay = grossPay - taxDed;

                return new PayrollRecord
                {
                    Id = Guid.NewGuid(),
                    RunId = run.Id,
                    EmployeeId = emp.Id,
                    BaseSalary = baseSalary,
                    OvertimePay = overtimePay,
                    LatenessDeduction = latenessDed,
                    GrossPay = grossPay,
                    TaxDeduction = taxDed,
                    NetPay = netPay,
                    TenantId = tenantId
                };
            }).ToList();

            run.TotalGross = records.Sum(r => r.GrossPay);
            run.TotalNet = records.Sum(r => r.NetPay);
            run.Records = records;

            db.PayrollRuns.Add(run);
        }

        await db.SaveChangesAsync();
    }
}
