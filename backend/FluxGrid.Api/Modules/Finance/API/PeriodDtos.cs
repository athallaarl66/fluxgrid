namespace FluxGrid.Api.Modules.Finance.API;

public sealed record PeriodResponse(
    Guid Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    Guid? ClosedBy,
    DateTime? ClosedAt,
    Guid TenantId,
    DateTime CreatedAt
);

public sealed record ClosePeriodRequest(
    string Confirmation
);

public sealed record ReopenPeriodRequest(
    string Reason
);

public sealed record ValidateCloseResponse(
    bool CanClose,
    List<Guid> BlockingEntryIds,
    string? Message
);
