using FluxGrid.Api.Modules.Finance.Application;
using FluxGrid.Api.Shared.RBAC;
using Microsoft.AspNetCore.Mvc;

namespace FluxGrid.Api.Modules.Finance.API;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/finance/reports");

        group.MapGet("/trial-balance", async (
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] bool includeDrafts,
            ReportService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var report = await service.GetTrialBalanceAsync(tenantId, startDate, endDate, includeDrafts);
            return Results.Ok(report);
        })
        .RequireAuthorization(Permissions.FinanceReportRead);

        group.MapGet("/pl", async (
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] bool includeDrafts,
            ReportService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var report = await service.GetProfitLossAsync(tenantId, startDate, endDate, includeDrafts);
            return Results.Ok(report);
        })
        .RequireAuthorization(Permissions.FinanceReportRead);

        group.MapGet("/balance-sheet", async (
            [FromQuery] DateTime asOfDate,
            [FromQuery] bool includeDrafts,
            [FromQuery] decimal? netIncome,
            ReportService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var report = await service.GetBalanceSheetAsync(tenantId, asOfDate, includeDrafts, netIncome);
            return Results.Ok(report);
        })
        .RequireAuthorization(Permissions.FinanceReportRead);

        group.MapGet("/{accountId:guid}/ledger", async (
            Guid accountId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] bool includeDrafts,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            ReportService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var (rows, total) = await service.GetAccountLedgerAsync(accountId, tenantId, startDate, endDate, includeDrafts, page, pageSize);
            return Results.Ok(new { rows, total, page, pageSize });
        })
        .RequireAuthorization(Permissions.FinanceReportRead);
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
