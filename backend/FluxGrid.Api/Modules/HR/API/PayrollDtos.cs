namespace FluxGrid.Api.Modules.HR.API;

public sealed record CreatePayrollRequest(
    string PeriodName,
    DateTime StartDate,
    DateTime EndDate
);

public sealed record PayrollRunResponse(
    Guid Id,
    string PeriodName,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    decimal? TotalGross,
    decimal? TotalNet,
    string ProcessedBy,
    Guid TenantId,
    DateTime CreatedAt
);

public sealed record PayrollRecordResponse(
    Guid Id,
    Guid RunId,
    Guid EmployeeId,
    string EmployeeNo,
    string EmployeeName,
    decimal? BaseSalary,
    decimal? OvertimePay,
    decimal? LatenessDeduction,
    decimal? GrossPay,
    decimal? TaxDeduction,
    decimal? NetPay,
    Guid TenantId
);

public sealed record PayrollRunDetailResponse(
    PayrollRunResponse Run,
    int TotalRecords,
    List<PayrollRecordResponse> Records
);

public sealed record PaginatedPayrollRunListResponse(
    List<PayrollRunResponse> Items,
    int Total,
    int Page,
    int PageSize
);
