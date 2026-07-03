using FluxGrid.Api.Modules.Dashboard.Application;

namespace FluxGrid.Api.Modules.Dashboard.API;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/dashboard", async (DashboardService service) =>
        {
            var modules = await service.GetModulesAsync();
            return Results.Ok(modules);
        })
        .RequireAuthorization("Dashboard:Read");
    }
}
