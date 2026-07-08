using FluxGrid.Api.Modules.Finance.Application;
using FluxGrid.Api.Shared.RBAC;

namespace FluxGrid.Api.Modules.Finance.API;

public static class FinanceDashboardEndpoints
{
    public static void MapFinanceDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/finance/dashboard", async (
            FinanceDashboardService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            try
            {
                var dashboard = await service.GetDashboardAsync(tenantId);
                return Results.Ok(dashboard);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.FinanceRead);
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
