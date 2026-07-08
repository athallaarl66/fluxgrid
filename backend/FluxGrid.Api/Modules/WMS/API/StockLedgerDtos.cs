namespace FluxGrid.Api.Modules.WMS.API;

public sealed record CreateMovementRequest(
    List<CreateMovementEntryDto> Entries
);

public sealed record CreateMovementEntryDto(
    Guid ItemId,
    Guid LocationId,
    decimal Quantity,
    decimal UnitCost,
    string ReferenceType,
    Guid ReferenceId
);

public sealed record LedgerEntryResponse(
    Guid Id,
    Guid TransactionId,
    Guid ItemId,
    Guid LocationId,
    decimal Quantity,
    decimal UnitCost,
    string ReferenceType,
    Guid ReferenceId,
    DateTime CreatedAt
);

public sealed record BalanceResponse(
    Guid ItemId,
    Guid LocationId,
    decimal BalanceQty,
    decimal BalanceValue
);

public sealed record ValuationResponse(
    string Method,
    decimal AverageCost,
    decimal TotalQuantity,
    decimal TotalValue,
    List<CostLayerDto>? Layers
);

public sealed record CostLayerDto(
    Guid EntryId,
    decimal Quantity,
    decimal UnitCost,
    DateTime CreatedAt
);
