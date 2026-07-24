using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.Dashboard.Application;

public class DashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db) => _db = db;

    public async Task<DashboardStats> GetStatsAsync(Guid tenantId)
    {
        var inventoryCount = await _db.InventoryItems.CountAsync(i => i.TenantId == tenantId);
        var locationCount = await _db.Locations.CountAsync(l => l.TenantId == tenantId);
        var employeeCount = await _db.Employees.CountAsync(e => e.TenantId == tenantId && e.Status == "ACTIVE");
        var journalCount = await _db.JournalEntries.CountAsync(e => e.TenantId == tenantId);
        var candidateCount = await _db.Candidates.CountAsync(c => c.TenantId == tenantId);
        var openJobCount = await _db.JobPostings.CountAsync(j => j.TenantId == tenantId && j.Status == "OPEN");
        var periodCount = await _db.AccountingPeriods.CountAsync(p => p.TenantId == tenantId);
        var coaCount = await _db.ChartOfAccounts.CountAsync(a => a.TenantId == tenantId);

        return new DashboardStats(
            inventoryCount, locationCount,
            employeeCount, candidateCount, openJobCount,
            journalCount, coaCount, periodCount
        );
    }

    public async Task<List<MonthlyData>> GetJournalTrendAsync(Guid tenantId, int months = 6)
    {
        var startDate = DateTime.UtcNow.AddMonths(-months);
        return await _db.JournalEntries
            .Where(e => e.TenantId == tenantId && e.CreatedAt >= startDate)
            .GroupBy(e => new { e.CreatedAt.Year, e.CreatedAt.Month })
            .Select(g => new MonthlyData(
                $"{g.Key.Month:D2}/{g.Key.Year % 100:D2}",
                g.Count()
            ))
            .OrderBy(d => d.Label)
            .ToListAsync();
    }

    public async Task<List<MonthlyData>> GetInboundTrendAsync(Guid tenantId, int months = 6)
    {
        var startDate = DateTime.UtcNow.AddMonths(-months);
        return await _db.StockLedgerEntries
            .Where(e => e.TenantId == tenantId && e.Quantity > 0 && e.CreatedAt >= startDate)
            .GroupBy(e => new { e.CreatedAt.Year, e.CreatedAt.Month })
            .Select(g => new MonthlyData(
                $"{g.Key.Month:D2}/{g.Key.Year % 100:D2}",
                (int)g.Sum(e => e.Quantity)
            ))
            .OrderBy(d => d.Label)
            .ToListAsync();
    }

    public async Task<List<MonthlyData>> GetOutboundTrendAsync(Guid tenantId, int months = 6)
    {
        var startDate = DateTime.UtcNow.AddMonths(-months);
        return await _db.StockLedgerEntries
            .Where(e => e.TenantId == tenantId && e.Quantity < 0 && e.CreatedAt >= startDate)
            .GroupBy(e => new { e.CreatedAt.Year, e.CreatedAt.Month })
            .Select(g => new MonthlyData(
                $"{g.Key.Month:D2}/{g.Key.Year % 100:D2}",
                (int)g.Sum(e => Math.Abs(e.Quantity))
            ))
            .OrderBy(d => d.Label)
            .ToListAsync();
    }

    public async Task<List<ModuleActivity>> GetModuleActivityAsync(Guid tenantId)
    {
        var journalThisMonth = await _db.JournalEntries
            .CountAsync(e => e.TenantId == tenantId && e.CreatedAt.Month == DateTime.UtcNow.Month && e.CreatedAt.Year == DateTime.UtcNow.Year);

        var stockThisMonth = await _db.StockLedgerEntries
            .CountAsync(e => e.TenantId == tenantId && e.CreatedAt.Month == DateTime.UtcNow.Month && e.CreatedAt.Year == DateTime.UtcNow.Year);

        var candidatesThisMonth = await _db.Candidates
            .CountAsync(c => c.TenantId == tenantId && c.CreatedAt.Month == DateTime.UtcNow.Month && c.CreatedAt.Year == DateTime.UtcNow.Year);

        return
        [
            new("WMS", stockThisMonth, "movements this month"),
            new("Finance", journalThisMonth, "entries this month"),
            new("HR", candidatesThisMonth, "candidates this month"),
        ];
    }
}

public record DashboardStats(
    int InventoryItems,
    int Locations,
    int ActiveEmployees,
    int Candidates,
    int OpenJobPostings,
    int JournalEntries,
    int ChartOfAccounts,
    int AccountingPeriods
);

public record MonthlyData(string Label, int Value);

public record ModuleActivity(string Module, int Count, string Unit);
