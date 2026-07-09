using FluxGrid.Api.Modules.WMS.Application;
using FluxGrid.Api.Shared.RBAC;
using Microsoft.AspNetCore.Mvc;

namespace FluxGrid.Api.Modules.WMS.API;

public static class PurchaseReceiptEndpoints
{
    public static void MapPurchaseReceiptEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/wms/receipts");

        group.MapPost("/", async (
            ReceiptCreateRequest request,
            PurchaseReceiptService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            var lines = request.Lines.Select(l => new ReceiptLineInput(l.ItemId, l.QtyReceived, l.QtyPassed, l.QtyFailed)).ToList();
            var result = await service.CreateReceiptAsync(tenantId, request.PoReference, request.ReceivedBy, lines, userId, ip, ua);
            if (!result.Success)
                return Results.Problem(result.Error, statusCode: 400);
            return Results.Created($"/api/v1/wms/receipts/{result.ReceiptId}", result);
        })
        .RequireAuthorization(Permissions.WmsInboundCreate);

        group.MapGet("/", async (
            [FromQuery] string? status,
            [FromQuery] string? poReference,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            PurchaseReceiptService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var result = await service.GetReceiptListAsync(tenantId, status, poReference, startDate, endDate, page, pageSize);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.WmsRead);

        group.MapGet("/{id:guid}", async (
            Guid id,
            PurchaseReceiptService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var result = await service.GetReceiptAsync(tenantId, id);
            if (result is null)
                return Results.NotFound();
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.WmsRead);

        group.MapPost("/{id:guid}/confirm", async (
            Guid id,
            PurchaseReceiptService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            var result = await service.ConfirmReceiptAsync(tenantId, id, userId, ip, ua);
            if (!result.Success)
                return Results.Problem(result.Error, statusCode: 400);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.WmsInboundApprove);

        group.MapPost("/{id:guid}/putaway", async (
            Guid id,
            PutawaySubmitRequest request,
            PurchaseReceiptService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            var lines = request.Lines.Select(l => new PutawayLineInput(l.LineId, l.LocationId)).ToList();
            var result = await service.ProcessPutawayAsync(tenantId, id, lines, userId, ip, ua);
            if (!result.Success)
                return Results.Problem(result.Error, statusCode: 409);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.WmsInboundApprove);
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

public sealed record ReceiptCreateRequest(
    string PoReference,
    string ReceivedBy,
    List<ReceiptLineRequest> Lines
);

public sealed record ReceiptLineRequest(
    Guid ItemId,
    decimal QtyReceived,
    decimal QtyPassed,
    decimal QtyFailed
);

public sealed record PutawaySubmitRequest(
    List<PutawayLineRequest> Lines
);

public sealed record PutawayLineRequest(
    Guid LineId,
    Guid LocationId
);
