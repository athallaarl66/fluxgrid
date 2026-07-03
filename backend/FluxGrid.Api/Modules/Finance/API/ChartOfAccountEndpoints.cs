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
            [FromQuery] bool? flat,
            ChartOfAccountService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var tree = await service.GetTreeAsync(tenantId, flat ?? false);
            return Results.Ok(tree);
        })
        .RequireAuthorization(Permissions.FinanceCoaRead);

        group.MapPost("/", async (
            CreateAccountRequest request,
            ChartOfAccountService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var account = await service.CreateAsync(tenantId, request, userId, ip, ua);
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
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var account = await service.UpdateAsync(id, tenantId, request, userId, ip, ua);
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
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var account = await service.DeactivateAsync(id, tenantId, userId, ip, ua);
                return Results.Ok(account);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.FinanceCoaManage);
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
