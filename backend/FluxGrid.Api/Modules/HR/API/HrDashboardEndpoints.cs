using FluxGrid.Api.Modules.HR.Application;
using FluxGrid.Api.Shared.RBAC;

namespace FluxGrid.Api.Modules.HR.API;

public static class HrDashboardEndpoints
{
    public static void MapHrDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/hr/dashboard", async (
            HrDashboardService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var dashboard = await service.GetDashboardAsync(tenantId);
            return Results.Ok(dashboard);
        })
        .RequireAuthorization(Permissions.HrRead);
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
