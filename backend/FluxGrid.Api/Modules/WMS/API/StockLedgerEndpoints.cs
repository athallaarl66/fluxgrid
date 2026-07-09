using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Modules.WMS.Application;
using FluxGrid.Api.Shared.RBAC;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.WMS.API;

public static class StockLedgerEndpoints
{
    public static void MapStockLedgerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/wms/stock-ledger");

        group.MapGet("/", async (
            [FromQuery] string? sku,
            [FromQuery] Guid? locationId,
            [FromQuery] string? locationCode,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            StockLedgerService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var result = await service.GetLedgerAsync(tenantId, sku, locationId, locationCode, startDate, endDate, page, pageSize);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.WmsRead);

        group.MapPost("/", async (
            CreateMovementRequest request,
            StockLedgerService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            var dtoEntries = request.Entries.Select(e => new CreateMovementEntry(e.ItemId, e.LocationId, e.Quantity, e.UnitCost, e.ReferenceType, e.ReferenceId)).ToList();
            var result = await service.CreateMovementAsync(tenantId, dtoEntries, userId, ip, ua);
            if (!result.Success)
                return Results.Problem(result.Error, statusCode: 400);
            return Results.Created($"/api/v1/wms/stock-ledger?transactionId={result.TransactionId}", result);
        })
        .RequireAuthorization(Permissions.WmsWrite);
    }

    public static void MapInventoryBalanceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/wms/inventory");

        group.MapGet("/balance", async (
            [FromQuery] Guid itemId,
            [FromQuery] Guid locationId,
            StockLedgerService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var balance = await service.GetBalanceAsync(tenantId, itemId, locationId);
            if (balance == null)
                return Results.NotFound();
            return Results.Ok(balance);
        })
        .RequireAuthorization(Permissions.WmsRead);
    }

    public static void MapWmsDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/wms/dashboard", async (AppDbContext db, HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var itemCount = await db.InventoryItems.CountAsync(i => i.TenantId == tenantId);
            var locationCount = await db.Locations.CountAsync(l => l.TenantId == tenantId);
            var inboundMtd = await db.StockLedgerEntries
                .Where(e => e.TenantId == tenantId && e.Quantity > 0 && e.CreatedAt.Month == DateTime.UtcNow.Month)
                .SumAsync(e => e.Quantity);
            var outboundMtd = await db.StockLedgerEntries
                .Where(e => e.TenantId == tenantId && e.Quantity < 0 && e.CreatedAt.Month == DateTime.UtcNow.Month)
                .SumAsync(e => Math.Abs(e.Quantity));
            return Results.Ok(new { itemCount, locationCount, inboundMtd, outboundMtd });
        })
        .RequireAuthorization(Permissions.WmsRead);
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
