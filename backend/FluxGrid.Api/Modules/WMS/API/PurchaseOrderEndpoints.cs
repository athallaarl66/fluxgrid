using FluxGrid.Api.Modules.WMS.Application;
using FluxGrid.Api.Shared.RBAC;
using Microsoft.AspNetCore.Mvc;

namespace FluxGrid.Api.Modules.WMS.API;

public static class PurchaseOrderEndpoints
{
    public static void MapPurchaseOrderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/wms/purchase-orders");

        group.MapPost("/", async (
            PoCreateRequest request,
            PurchaseOrderService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            var lines = request.Lines.Select(l => new PoLineInput(l.ItemId, l.OrderedQty)).ToList();
            var result = await service.CreatePoAsync(tenantId, request.PoNumber, request.SupplierName, request.PoDate, lines, userId, ip, ua);
            if (!result.Success)
                return Results.Problem(result.Error, statusCode: 400);
            return Results.Created($"/api/v1/wms/purchase-orders/{result.PoId}", result);
        })
        .RequireAuthorization(Permissions.WmsInboundCreate);

        group.MapGet("/", async (
            [FromQuery] string? search,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            PurchaseOrderService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var result = await service.GetPoListAsync(tenantId, search, page, pageSize);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.WmsRead);

        group.MapGet("/{id:guid}", async (
            Guid id,
            PurchaseOrderService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var result = await service.GetPoByIdAsync(tenantId, id);
            if (result is null)
                return Results.NotFound();
            return Results.Ok(result);
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

public sealed record PoCreateRequest(
    string PoNumber,
    string SupplierName,
    DateTime PoDate,
    List<PoCreateLineDto> Lines
);

public sealed record PoCreateLineDto(
    Guid ItemId,
    decimal OrderedQty
);
