using FluxGrid.Api.Modules.Finance.Application;
using FluxGrid.Api.Shared.RBAC;
using Microsoft.AspNetCore.Mvc;

namespace FluxGrid.Api.Modules.Finance.API;

public static class JournalEntryEndpoints
{
    public static void MapJournalEntryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/finance/journal-entries");

        group.MapGet("/", async (
            [FromQuery] string? status,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            JournalEntryService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var entries = await service.GetListAsync(tenantId, status, page ?? 1, pageSize ?? 20);
            return Results.Ok(entries);
        })
        .RequireAuthorization(Permissions.FinanceJournalView);

        group.MapGet("/{id:guid}", async (
            Guid id,
            JournalEntryService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var entry = await service.GetByIdAsync(id, tenantId);
            if (entry == null) return Results.NotFound();
            return Results.Ok(entry);
        })
        .RequireAuthorization(Permissions.FinanceJournalView);

        group.MapPost("/", async (
            CreateJournalEntryRequest request,
            JournalEntryService service,
            HttpContext http) =>
        {
            var (tenantId, userId, _, _) = GetAuditContext(http);
            try
            {
                var entry = await service.CreateAsync(tenantId, request, userId);
                return Results.Created($"/api/v1/finance/journal-entries/{entry.Id}", entry);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.FinanceJournalCreate);

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateJournalEntryRequest request,
            JournalEntryService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            try
            {
                var entry = await service.UpdateDraftAsync(id, tenantId, request);
                return Results.Ok(entry);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.FinanceJournalCreate);

        group.MapPost("/{id:guid}/approve", async (
            Guid id,
            JournalEntryService service,
            HttpContext http) =>
        {
            var (tenantId, userId, _, _) = GetAuditContext(http);
            try
            {
                var entry = await service.ApproveAsync(id, tenantId, userId);
                return Results.Ok(entry);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.FinanceJournalApprove);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            JournalEntryService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            try
            {
                await service.DeleteDraftAsync(id, tenantId);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.FinanceJournalCreate);
    }

    private static (Guid tenantId, Guid userId, string? ip, string? ua) GetAuditContext(HttpContext http)
    {
        var tenantId = Guid.Empty;
        var userId = Guid.Empty;

        var tenantClaim = http.User.FindFirst("tenant_id")?.Value;
        if (Guid.TryParse(tenantClaim, out var tid)) tenantId = tid;

        var userClaim = http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userClaim, out var uid)) userId = uid;

        var ip = http.Connection.RemoteIpAddress?.ToString();
        var ua = http.Request.Headers.UserAgent.ToString();

        return (tenantId, userId, ip, ua);
    }
}
