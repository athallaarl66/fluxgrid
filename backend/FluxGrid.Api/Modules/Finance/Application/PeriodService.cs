using FluxGrid.Api.Modules.Finance.API;
using FluxGrid.Api.Modules.Finance.Domain.Entities;
using FluxGrid.Api.Modules.Finance.Domain.Events;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.Finance.Application;

public class PeriodService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private readonly DomainEventDispatcher _events;

    public PeriodService(AppDbContext db, AuditService audit, DomainEventDispatcher events)
    {
        _db = db;
        _audit = audit;
        _events = events;
    }

    public async Task<List<PeriodResponse>> GetListAsync(Guid tenantId)
    {
        var periods = await _db.AccountingPeriods
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();

        return periods.Select(MapToResponse).ToList();
    }

    public async Task<ValidateCloseResponse> ValidateCloseAsync(Guid id, Guid tenantId)
    {
        var period = await _db.AccountingPeriods
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId)
            ?? throw new InvalidOperationException("Period not found");

        if (period.Status == "CLOSED")
            throw new InvalidOperationException("Period is already closed");

        var blockingEntries = await _db.JournalEntries
            .Where(je => je.TenantId == tenantId
                && je.TransactionDate >= period.StartDate
                && je.TransactionDate <= period.EndDate
                && (je.Status == "DRAFT" || je.Status == "PENDING_APPROVAL"))
            .Select(je => je.Id)
            .ToListAsync();

        if (blockingEntries.Any())
        {
            return new ValidateCloseResponse(
                false,
                blockingEntries,
                $"Cannot close period: {blockingEntries.Count} journal entries are pending"
            );
        }

        return new ValidateCloseResponse(true, new List<Guid>(), "Period can be closed");
    }

    public async Task<PeriodResponse> CloseAsync(Guid id, Guid tenantId, ClosePeriodRequest request, Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        if (request.Confirmation != "CLOSE")
            throw new InvalidOperationException("Confirmation text must be 'CLOSE'");

        var period = await _db.AccountingPeriods
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId)
            ?? throw new InvalidOperationException("Period not found");

        if (period.Status == "CLOSED")
            throw new InvalidOperationException("Period is already closed");

        // Re-run validation to prevent race conditions
        var validation = await ValidateCloseAsync(id, tenantId);
        if (!validation.CanClose)
            throw new InvalidOperationException(validation.Message ?? "Cannot close period due to validation failures");

        var before = MapToResponse(period);

        period.Status = "CLOSED";
        period.ClosedBy = userId;
        period.ClosedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var after = MapToResponse(period);
        await _audit.LogAsync(userId, tenantId, "CLOSE", "accounting_periods", period.Id, ipAddress, userAgent, before, after);

        _events.Raise(new PeriodClosed(period.Id, period.Name, period.StartDate, period.EndDate, userId, tenantId));

        return after;
    }

    public async Task<PeriodResponse> ReopenAsync(Guid id, Guid tenantId, ReopenPeriodRequest request, Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length < 10)
            throw new InvalidOperationException("Reason is required and must be at least 10 characters");

        var period = await _db.AccountingPeriods
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId)
            ?? throw new InvalidOperationException("Period not found");

        if (period.Status == "OPEN")
            throw new InvalidOperationException("Period is already open");

        var before = MapToResponse(period);

        period.Status = "OPEN";
        period.ClosedBy = null;
        period.ClosedAt = null;

        await _db.SaveChangesAsync();

        var after = MapToResponse(period);
        await _audit.LogAsync(userId, tenantId, "REOPEN", "accounting_periods", period.Id, ipAddress, userAgent, before, new { period = after, reason = request.Reason });

        _events.Raise(new PeriodReopened(period.Id, period.Name, period.StartDate, period.EndDate, request.Reason, userId, tenantId));

        return after;
    }

    private static PeriodResponse MapToResponse(AccountingPeriod p)
    {
        return new PeriodResponse(p.Id, p.Name, p.StartDate, p.EndDate, p.Status, p.ClosedBy, p.ClosedAt, p.TenantId, p.CreatedAt);
    }
}
