using FluxGrid.Api.Modules.Finance.Application;
using FluxGrid.Api.Shared.RBAC;
using Microsoft.AspNetCore.Mvc;

namespace FluxGrid.Api.Modules.Finance.API;

public static class PeriodEndpoints
{
    public static void MapPeriodEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/finance/periods");

        group.MapGet("/", async (
            PeriodService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var periods = await service.GetListAsync(tenantId);
            return Results.Ok(periods);
        })
        .RequireAuthorization(Permissions.FinancePeriodRead);

        group.MapGet("/{id:guid}/validate", async (
            Guid id,
            PeriodService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            try
            {
                var validation = await service.ValidateCloseAsync(id, tenantId);
                return Results.Ok(validation);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.FinancePeriodRead);

        group.MapPost("/{id:guid}/close", async (
            Guid id,
            ClosePeriodRequest request,
            PeriodService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var period = await service.CloseAsync(id, tenantId, request, userId, ip, ua);
                return Results.Ok(period);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.FinancePeriodAdmin);

        group.MapPost("/{id:guid}/reopen", async (
            Guid id,
            ReopenPeriodRequest request,
            PeriodService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var period = await service.ReopenAsync(id, tenantId, request, userId, ip, ua);
                return Results.Ok(period);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.FinancePeriodAdmin);
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
