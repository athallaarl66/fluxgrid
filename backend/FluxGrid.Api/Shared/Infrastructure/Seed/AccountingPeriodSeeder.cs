using FluxGrid.Api.Modules.Finance.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Shared.Infrastructure.Seed;

public static class AccountingPeriodSeeder
{
    public static async Task SeedAsync(AppDbContext db, Guid tenantId)
    {
        if (await db.AccountingPeriods.AnyAsync(p => p.TenantId == tenantId))
            return;

        var periods = Generate12MonthlyPeriods(tenantId);
        db.AccountingPeriods.AddRange(periods);
        await db.SaveChangesAsync();
    }

    public static List<AccountingPeriod> Generate12MonthlyPeriods(Guid tenantId)
    {
        var currentYear = DateTime.UtcNow.Year;
        var periods = new List<AccountingPeriod>();

        for (int month = 1; month <= 12; month++)
        {
            var startDate = new DateTime(currentYear, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            var monthName = startDate.ToString("MMMM yyyy");

            periods.Add(new AccountingPeriod
            {
                Id = Guid.NewGuid(),
                Name = monthName,
                StartDate = startDate,
                EndDate = endDate,
                Status = "OPEN",
                TenantId = tenantId,
                CreatedAt = DateTime.UtcNow
            });
        }

        return periods;
    }
}
