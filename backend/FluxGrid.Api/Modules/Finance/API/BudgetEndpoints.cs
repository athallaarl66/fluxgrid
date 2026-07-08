using FluxGrid.Api.Modules.Finance.Application;
using FluxGrid.Api.Shared.RBAC;
using Microsoft.AspNetCore.Mvc;

namespace FluxGrid.Api.Modules.Finance.API;

public static class BudgetEndpoints
{
    public static void MapBudgetEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/finance/budgets");

        group.MapGet("/", async (
            [FromQuery] Guid? periodId,
            [FromQuery] Guid? accountId,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            BudgetService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var result = await service.GetListAsync(tenantId, periodId, accountId, page, pageSize);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.FinanceBudgetRead);

        group.MapPost("/", async (
            CreateBudgetRequest request,
            BudgetService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var budget = await service.CreateAsync(tenantId, request, userId, ip, ua);
                return Results.Created($"/api/v1/finance/budgets/{budget.Id}", budget);
            }
            catch (InvalidOperationException ex)
            {
                var statusCode = ex.Message.Contains("already exists") ? 409 : 400;
                return Results.Problem(ex.Message, statusCode: statusCode);
            }
        })
        .RequireAuthorization(Permissions.FinanceBudgetManage);

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateBudgetRequest request,
            BudgetService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var budget = await service.UpdateAsync(id, tenantId, request, userId, ip, ua);
                return Results.Ok(budget);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.FinanceBudgetManage);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            BudgetService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                await service.DeleteAsync(id, tenantId, userId, ip, ua);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 404);
            }
        })
        .RequireAuthorization(Permissions.FinanceBudgetManage);

        group.MapGet("/report", async (
            [FromQuery] Guid? periodId,
            BudgetService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            if (periodId is null)
                return Results.Problem("periodId is required", statusCode: 400);
            var report = await service.GetBudgetVsActualAsync(tenantId, periodId.Value);
            return Results.Ok(report);
        })
        .RequireAuthorization(Permissions.FinanceBudgetRead);
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
