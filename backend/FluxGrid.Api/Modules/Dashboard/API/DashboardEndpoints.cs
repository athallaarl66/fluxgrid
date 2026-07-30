using System.Security.Claims;
using FluxGrid.Api.Modules.Dashboard.Application;

namespace FluxGrid.Api.Modules.Dashboard.API;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/dashboard/stats", async (DashboardService service, HttpContext http) =>
        {
            var tenantId = GetTenantId(http);
            var stats = await service.GetStatsAsync(tenantId);
            return Results.Ok(stats);
        })
        .RequireAuthorization("Dashboard:Read");

        app.MapGet("/api/dashboard/charts/journal-trend", async (DashboardService service, HttpContext http, int? months) =>
        {
            var tenantId = GetTenantId(http);
            var data = await service.GetJournalTrendAsync(tenantId, months ?? 6);
            return Results.Ok(data);
        })
        .RequireAuthorization("Dashboard:Read");

        app.MapGet("/api/dashboard/charts/inventory-trend", async (DashboardService service, HttpContext http, int? months) =>
        {
            var tenantId = GetTenantId(http);
            var inbound = await service.GetInboundTrendAsync(tenantId, months ?? 6);
            var outbound = await service.GetOutboundTrendAsync(tenantId, months ?? 6);

            var allLabels = inbound.Select(d => d.Label)
                .Union(outbound.Select(d => d.Label))
                .Distinct()
                .OrderBy(l => l)
                .ToList();

            var inMap = inbound.ToDictionary(d => d.Label, d => d.Value);
            var outMap = outbound.ToDictionary(d => d.Label, d => d.Value);

            var result = allLabels.Select(l => new
            {
                label = l,
                inbound = inMap.GetValueOrDefault(l, 0),
                outbound = outMap.GetValueOrDefault(l, 0),
            }).ToList();

            return Results.Ok(result);
        })
        .RequireAuthorization("Dashboard:Read");

        app.MapGet("/api/dashboard/activity", async (DashboardService service, HttpContext http) =>
        {
            var tenantId = GetTenantId(http);
            var activity = await service.GetModuleActivityAsync(tenantId);
            return Results.Ok(activity);
        })
        .RequireAuthorization("Dashboard:Read");
    }

    private static Guid GetTenantId(HttpContext http)
    {
        var tenantClaim = http.User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(tenantClaim, out var tid) ? tid : Guid.Empty;
    }
}
