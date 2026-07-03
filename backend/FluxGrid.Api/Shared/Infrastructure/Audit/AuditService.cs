using System.Text.Json;
using FluxGrid.Api.Shared.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Data;

namespace FluxGrid.Api.Shared.Infrastructure.Audit;

public class AuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(Guid userId, Guid tenantId, string action, string resourceType, Guid resourceId, string? ipAddress, string? userAgent, object? oldValue = null, object? newValue = null)
    {
        var changes = new Dictionary<string, object?>();
        if (oldValue is not null) changes["old_value"] = oldValue;
        if (newValue is not null) changes["new_value"] = newValue;

        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            UserId = userId,
            TenantId = tenantId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ChangesJson = changes.Count > 0 ? JsonSerializer.Serialize(changes) : null
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }
}
