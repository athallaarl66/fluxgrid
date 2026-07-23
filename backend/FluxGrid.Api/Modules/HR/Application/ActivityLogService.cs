using System.Text.Json;
using System.Text.Json.Serialization;
using FluxGrid.Api.Modules.HR.API;
using FluxGrid.Api.Modules.HR.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.HR.Application;

public class ActivityLogService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    private readonly AppDbContext _db;

    public ActivityLogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(Guid candidateId, string action, Guid performedBy, object? details = null)
    {
        var log = new CandidateActivityLog
        {
            Id = Guid.NewGuid(),
            CandidateId = candidateId,
            Action = action,
            PerformedBy = performedBy,
            Details = details is not null
                ? JsonDocument.Parse(JsonSerializer.Serialize(details, _jsonOptions))
                : null,
            CreatedAt = DateTime.UtcNow
        };

        _db.CandidateActivityLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    public async Task<PaginatedResponse<ActivityLogResponse>> GetActivitiesAsync(
        Guid candidateId, Guid tenantId, int page = 1, int pageSize = 20)
    {
        var query = _db.CandidateActivityLogs
            .Where(a => a.CandidateId == candidateId)
            .OrderByDescending(a => a.CreatedAt);

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ActivityLogResponse(
                a.Id,
                a.Action,
                a.PerformedBy,
                a.Details != null ? a.Details.RootElement.GetRawText() : null,
                a.CreatedAt))
            .ToListAsync();

        return new PaginatedResponse<ActivityLogResponse>(items, total, page, pageSize);
    }
}
