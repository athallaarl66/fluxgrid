using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.Finance.Application;

public class PeriodValidator
{
    private readonly AppDbContext _db;

    public PeriodValidator(AppDbContext db)
    {
        _db = db;
    }

    public async Task ValidateOpenPeriodAsync(DateTime transactionDate, Guid tenantId)
    {
        var period = await _db.AccountingPeriods
            .FirstOrDefaultAsync(p => p.TenantId == tenantId
                && p.StartDate <= transactionDate
                && p.EndDate >= transactionDate);

        if (period == null)
            throw new InvalidOperationException($"No accounting period found for date {transactionDate:yyyy-MM-dd}");

        if (period.Status == "CLOSED")
            throw new InvalidOperationException($"Cannot post to a closed period: {period.Name}");
    }
}
