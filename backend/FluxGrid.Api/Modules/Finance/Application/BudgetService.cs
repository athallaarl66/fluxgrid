using FluxGrid.Api.Modules.Finance.API;
using FluxGrid.Api.Modules.Finance.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.Finance.Application;

public class BudgetService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public BudgetService(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<PaginatedResult<BudgetResponse>> GetListAsync(Guid tenantId, Guid? periodId, Guid? accountId, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var query = _db.Budgets
            .Include(b => b.Account)
            .Include(b => b.Period)
            .Where(b => b.TenantId == tenantId);

        if (periodId.HasValue)
            query = query.Where(b => b.PeriodId == periodId.Value);

        if (accountId.HasValue)
            query = query.Where(b => b.AccountId == accountId.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var mapped = items.Select(MapToResponse).ToList();
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        return new PaginatedResult<BudgetResponse>(mapped, total, page, pageSize, totalPages);
    }

    public async Task<BudgetResponse> CreateAsync(Guid tenantId, CreateBudgetRequest request, Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        var account = await _db.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.TenantId == tenantId)
            ?? throw new InvalidOperationException("Account not found");

        if (!account.IsActive)
            throw new InvalidOperationException("Account is not active");

        var period = await _db.AccountingPeriods
            .FirstOrDefaultAsync(p => p.Id == request.PeriodId && p.TenantId == tenantId)
            ?? throw new InvalidOperationException("Period not found");

        if (period.Status == "CLOSED")
            throw new InvalidOperationException("Cannot create budget for a closed period");

        var exists = await _db.Budgets.AnyAsync(b =>
            b.TenantId == tenantId && b.AccountId == request.AccountId && b.PeriodId == request.PeriodId);

        if (exists)
            throw new InvalidOperationException("Budget already exists for this account and period");

        var budget = new Budget
        {
            Id = Guid.NewGuid(),
            AccountId = request.AccountId,
            PeriodId = request.PeriodId,
            PlannedAmount = request.PlannedAmount,
            Notes = request.Notes,
            TenantId = tenantId
        };

        _db.Budgets.Add(budget);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, tenantId, "CREATE", "budgets", budget.Id, ipAddress, userAgent, null, new { budget.Id, budget.PlannedAmount });

        return MapToResponse(budget);
    }

    public async Task<BudgetResponse> UpdateAsync(Guid id, Guid tenantId, UpdateBudgetRequest request, Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        var budget = await _db.Budgets
            .Include(b => b.Account)
            .Include(b => b.Period)
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId)
            ?? throw new InvalidOperationException("Budget not found");

        var before = MapToResponse(budget);

        if (request.PlannedAmount.HasValue)
            budget.PlannedAmount = request.PlannedAmount.Value;
        if (request.Notes is not null)
            budget.Notes = request.Notes;

        budget.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var after = MapToResponse(budget);
        await _audit.LogAsync(userId, tenantId, "UPDATE", "budgets", budget.Id, ipAddress, userAgent, before, after);

        return after;
    }

    public async Task DeleteAsync(Guid id, Guid tenantId, Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        var budget = await _db.Budgets
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId)
            ?? throw new InvalidOperationException("Budget not found");

        _db.Budgets.Remove(budget);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, tenantId, "DELETE", "budgets", id, ipAddress, userAgent, new { budget.PlannedAmount }, null);
    }

    public async Task<List<BudgetVsActualRow>> GetBudgetVsActualAsync(Guid tenantId, Guid periodId)
    {
        var budgets = await _db.Budgets
            .Include(b => b.Account)
            .Where(b => b.TenantId == tenantId && b.PeriodId == periodId)
            .ToListAsync();

        if (budgets.Count == 0)
            return [];

        var accountIds = budgets.Select(b => b.AccountId).ToHashSet();

        var period = await _db.AccountingPeriods
            .FirstOrDefaultAsync(p => p.Id == periodId && p.TenantId == tenantId);

        if (period is null)
            return [];

        var actuals = await (
            from jel in _db.JournalEntryLines
            join je in _db.JournalEntries on jel.EntryId equals je.Id
            where accountIds.Contains(jel.AccountId)
                  && je.TenantId == tenantId
                  && je.TransactionDate >= period.StartDate
                  && je.TransactionDate <= period.EndDate
                  && (je.Status == "POSTED" || je.Status == "APPROVED")
            group jel by jel.AccountId into g
            select new { AccountId = g.Key, TotalAmount = g.Sum(x => x.Debit + x.Credit) }
        ).ToListAsync();

        var actualDict = actuals.ToDictionary(x => x.AccountId, x => x.TotalAmount);

        var rows = budgets.Select(b =>
        {
            var actual = actualDict.TryGetValue(b.AccountId, out var val) ? val : 0m;
            var variance = b.PlannedAmount - actual;
            var variancePct = b.PlannedAmount != 0
                ? Math.Round(variance / b.PlannedAmount * 100, 1)
                : 0m;
            var isFlagged = Math.Abs(variancePct) > 20m;

            return new BudgetVsActualRow(
                b.Account!.Code,
                b.Account.Name,
                b.PlannedAmount,
                actual,
                variance,
                variancePct,
                isFlagged
            );
        }).ToList();

        return rows;
    }

    private static BudgetResponse MapToResponse(Budget b)
    {
        return new BudgetResponse(
            b.Id,
            b.AccountId,
            b.Account?.Code ?? "",
            b.Account?.Name ?? "",
            b.PeriodId,
            b.Period?.Name ?? "",
            b.PlannedAmount,
            b.Notes,
            b.TenantId,
            b.CreatedAt,
            b.UpdatedAt
        );
    }
}
