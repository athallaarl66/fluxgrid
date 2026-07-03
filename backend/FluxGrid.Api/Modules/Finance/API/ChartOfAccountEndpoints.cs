using FluxGrid.Api.Modules.Finance.Application;
using FluxGrid.Api.Shared.RBAC;
using Microsoft.AspNetCore.Mvc;

namespace FluxGrid.Api.Modules.Finance.API;

public static class ChartOfAccountEndpoints
{
    public static void MapChartOfAccountEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/finance/chart-of-accounts");

        group.MapGet("/", async (
            [FromQuery] bool flat,
            ChartOfAccountService service,
            HttpContext http) =>
        {
            var tenantId = GetTenantId(http);
            var tree = await service.GetTreeAsync(tenantId, flat);
            return Results.Ok(tree);
        })
        .RequireAuthorization(Permissions.FinanceCoaRead);

        group.MapPost("/", async (
            CreateAccountRequest request,
            ChartOfAccountService service,
            HttpContext http) =>
        {
            var tenantId = GetTenantId(http);
            try
            {
                var account = await service.CreateAsync(tenantId, request);
                return Results.Created($"/api/v1/finance/chart-of-accounts/{account.Id}", account);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.FinanceCoaManage);

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateAccountRequest request,
            ChartOfAccountService service,
            HttpContext http) =>
        {
            var tenantId = GetTenantId(http);
            try
            {
                var account = await service.UpdateAsync(id, tenantId, request);
                return Results.Ok(account);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.FinanceCoaManage);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ChartOfAccountService service,
            HttpContext http) =>
        {
            var tenantId = GetTenantId(http);
            try
            {
                var account = await service.DeactivateAsync(id, tenantId);
                return Results.Ok(account);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.FinanceCoaManage);
    }

    private static Guid GetTenantId(HttpContext http)
    {
        var tenantIdClaim = http.User.FindFirst("tenant_id")?.Value;
        if (Guid.TryParse(tenantIdClaim, out var tenantId))
            return tenantId;
        return Guid.Empty;
    }
}
