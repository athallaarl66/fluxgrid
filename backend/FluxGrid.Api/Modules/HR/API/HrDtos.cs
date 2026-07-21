namespace FluxGrid.Api.Modules.HR.API;

public sealed record CreateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? Address,
    DateTime? DateOfBirth,
    string? Nik,
    string? EmergencyContact,
    Guid? DepartmentId,
    Guid? ManagerId,
    string? JobTitle,
    DateTime HireDate
);

public sealed record UpdateEmployeeRequest(
    string? FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? Address,
    DateTime? DateOfBirth,
    string? EmergencyContact,
    Guid? DepartmentId,
    Guid? ManagerId,
    string? JobTitle
);

public sealed record EmployeeResponse(
    Guid Id,
    Guid? UserId,
    string EmployeeNo,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    Guid? DepartmentId,
    Guid? ManagerId,
    string? JobTitle,
    string Status,
    DateTime HireDate,
    DateTime? TerminationDate,
    Guid TenantId
);

public sealed record EmployeeDetailResponse(
    Guid Id,
    Guid? UserId,
    string EmployeeNo,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? Address,
    DateTime? DateOfBirth,
    string? Nik,
    string? EmergencyContact,
    Guid? DepartmentId,
    Guid? ManagerId,
    string? JobTitle,
    decimal? BaseSalary,
    string? BankName,
    string? BankAccount,
    string? TaxId,
    string Status,
    DateTime HireDate,
    DateTime? TerminationDate,
    Guid TenantId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public sealed record CreateDepartmentRequest(
    string Name,
    Guid? ParentId
);

public sealed record UpdateDepartmentRequest(
    string? Name,
    Guid? ParentId,
    bool? IsActive
);

public sealed record DepartmentResponse(
    Guid Id,
    string Name,
    Guid? ParentId,
    bool IsActive,
    Guid TenantId
);

public sealed record OrgChartNode(
    Guid Id,
    string EmployeeNo,
    string FirstName,
    string LastName,
    string? JobTitle,
    Guid? DepartmentId,
    Guid? ManagerId
);

public sealed record HrDashboardResponse(
    int TotalEmployees,
    int ActiveEmployees,
    int TotalCandidates,
    CandidatePipelineCounts CandidatePipeline,
    int TotalJobs,
    int PublishedJobs,
    int DraftJobs,
    decimal PayrollMtd,
    int PayrollCountMtd,
    List<RecentHireRow> RecentHires
);

public sealed record CandidatePipelineCounts(
    int Active,
    int Parsed,
    int Rejected
);

public sealed record RecentHireRow(
    Guid Id,
    string FirstName,
    string LastName,
    string JobTitle,
    string Department,
    DateTime HireDate
);
