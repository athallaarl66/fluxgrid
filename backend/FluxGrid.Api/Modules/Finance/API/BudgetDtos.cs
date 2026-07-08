namespace FluxGrid.Api.Modules.Finance.API;

public sealed record CreateBudgetRequest(
    Guid AccountId,
    Guid PeriodId,
    decimal PlannedAmount,
    string? Notes
);

public sealed record UpdateBudgetRequest(
    decimal? PlannedAmount,
    string? Notes
);

public sealed record BudgetResponse(
    Guid Id,
    Guid AccountId,
    string AccountCode,
    string AccountName,
    Guid PeriodId,
    string PeriodName,
    decimal PlannedAmount,
    string? Notes,
    Guid TenantId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public sealed record BudgetVsActualRow(
    string AccountCode,
    string AccountName,
    decimal PlannedAmount,
    decimal ActualAmount,
    decimal Variance,
    decimal VariancePercentage,
    bool IsFlagged
);

public sealed record PaginatedResult<T>(
    List<T> Items,
    int Total,
    int Page,
    int PageSize,
    int TotalPages
);
