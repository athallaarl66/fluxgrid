using FluxGrid.Api.Modules.WMS.Application;
using FluxGrid.Api.Shared.RBAC;
using Microsoft.AspNetCore.Mvc;

namespace FluxGrid.Api.Modules.WMS.API;

public static class OutboundEndpoints
{
    public static void MapOutboundEndpoints(this WebApplication app)
    {
        var salesOrders = app.MapGroup("/api/v1/wms/sales-orders");
        var pickLists = app.MapGroup("/api/v1/wms/pick-lists");
        var shipments = app.MapGroup("/api/v1/wms/shipments");

        salesOrders.MapPost("/", async (
            SoCreateRequest request,
            SalesOrderService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            var lines = request.Lines.Select(l => new SoLineInput(l.ItemId, l.QtyOrdered)).ToList();
            var result = await service.CreateOrderAsync(tenantId, request.OrderNo, request.CustomerId, request.CustomerName, request.Notes, lines, userId, ip, ua);
            if (!result.Success)
                return Results.Problem(result.Error, statusCode: 400);
            return Results.Created($"/api/v1/wms/sales-orders/{result.OrderId}", result);
        })
        .RequireAuthorization(Permissions.WmsOutboundProcess);

        salesOrders.MapGet("/", async (
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            SalesOrderService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var result = await service.GetOrderListAsync(tenantId, search, status, page, pageSize);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.WmsRead);

        salesOrders.MapGet("/{id:guid}", async (
            Guid id,
            SalesOrderService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var result = await service.GetOrderAsync(tenantId, id);
            if (result is null)
                return Results.NotFound();
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.WmsRead);

        salesOrders.MapPost("/{id:guid}/cancel", async (
            Guid id,
            SalesOrderService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            var result = await service.CancelOrderAsync(tenantId, id, userId, ip, ua);
            if (!result.Success)
                return Results.Problem(result.Error, statusCode: 400);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.WmsOutboundProcess);

        pickLists.MapPost("/", async (
            PickListGenerateRequest request,
            PickListService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            var result = await service.GeneratePickListAsync(tenantId, request.OrderId, userId, ip, ua);
            if (!result.Success)
                return Results.Problem(result.Error, statusCode: result.Error switch
                {
                    "ORDER_NOT_FOUND" => 404,
                    "INSUFFICIENT_STOCK" => 422,
                    _ => 409
                });
            return Results.Created($"/api/v1/wms/pick-lists/{result.PickListId}", result);
        })
        .RequireAuthorization(Permissions.WmsOutboundProcess);

        pickLists.MapGet("/{id:guid}", async (
            Guid id,
            PickListService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var result = await service.GetPickListAsync(tenantId, id);
            if (result is null)
                return Results.NotFound();
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.WmsRead);

        pickLists.MapPut("/{id:guid}/items", async (
            Guid id,
            PickExecuteRequest request,
            PickListService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            var items = request.Items.Select(i => new PickExecutionInput(i.ItemId, i.QtyPicked, i.ShortPickReason)).ToList();
            var result = await service.ExecutePickItemsAsync(tenantId, id, items, userId, ip, ua);
            if (!result.Success)
                return Results.Problem(result.Error, statusCode: 400);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.WmsOutboundProcess);

        shipments.MapPost("/verify", async (
            VerifyRequest request,
            ShipmentService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            var lines = request.Lines.Select(l => new VerifyLineInput(l.ItemId, l.VerifiedQty)).ToList();
            var result = await service.VerifyPackingAsync(tenantId, request.OrderId, lines, userId, ip, ua);
            if (!result.Success)
                return Results.Problem(result.ErrorDetail, statusCode: 400);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.WmsOutboundProcess);

        shipments.MapPost("/", async (
            ShipConfirmRequest request,
            ShipmentService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            var result = await service.ConfirmShipmentAsync(tenantId, request.OrderId, userId, ip, ua);
            if (!result.Success)
                return Results.Problem(result.Error, statusCode: 400);
            return Results.Created($"/api/v1/wms/shipments/{result.ShipmentId}", result);
        })
        .RequireAuthorization(Permissions.WmsOutboundProcess);

        shipments.MapGet("/", async (
            [FromQuery] Guid? orderId,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            ShipmentService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var result = await service.GetShipmentListAsync(tenantId, orderId, page, pageSize);
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

public sealed record SoCreateRequest(string OrderNo, Guid CustomerId, string CustomerName, string? Notes, List<SoCreateLineDto> Lines);
public sealed record SoCreateLineDto(Guid ItemId, decimal QtyOrdered);
public sealed record PickListGenerateRequest(Guid OrderId);
public sealed record PickExecuteRequest(List<PickExecuteItemDto> Items);
public sealed record PickExecuteItemDto(Guid ItemId, decimal QtyPicked, string? ShortPickReason);
public sealed record VerifyRequest(Guid OrderId, List<VerifyLineDto> Lines);
public sealed record VerifyLineDto(Guid ItemId, decimal VerifiedQty);
public sealed record ShipConfirmRequest(Guid OrderId);
