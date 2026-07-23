namespace FluxGrid.Api.Modules.HR.API;

public sealed record UploadUrlRequest(
    string FileName,
    string FileType,
    long FileSize,
    string FileHash
);

public sealed record UploadUrlResponse(
    string PresignedUrl,
    string ObjectKey,
    string FileHash
);

public sealed record CreateCandidateRequest(
    string Name,
    string Email,
    string? Phone,
    string? Location,
    string? LinkedInUrl,
    string? GitHubUrl,
    string? PortfolioUrl,
    string? Summary,
    int? TotalExperienceMonths,
    decimal? ExpectedSalaryMin,
    decimal? ExpectedSalaryMax,
    int? NoticePeriodDays,
    string FileUrl,
    string FileHash,
    string OriginalFilename,
    string FileType,
    long FileSizeBytes
);

public sealed record CandidateResponse(
    Guid Id,
    string Name,
    string Email,
    string Status,
    string? OriginalFilename,
    string? FileType,
    DateTime CreatedAt,
    Guid TenantId
);

public sealed record CandidateDetailResponse(
    Guid Id,
    string Name,
    string Email,
    string? Phone,
    string? Location,
    string? LinkedInUrl,
    string? GitHubUrl,
    string? PortfolioUrl,
    string? Summary,
    int? TotalExperienceMonths,
    decimal? ExpectedSalaryMin,
    decimal? ExpectedSalaryMax,
    int? NoticePeriodDays,
    string Status,
    string? FileUrl,
    string? OriginalFilename,
    string? FileType,
    long? FileSizeBytes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<CandidateEducationResponse> Education,
    List<CandidateExperienceResponse> Experience,
    List<CandidateSkillResponse> Skills,
    List<CandidateDocumentResponse> Documents
);

public sealed record CandidateListItem(
    Guid Id,
    string Name,
    string Email,
    string Status,
    string? OriginalFilename,
    string? FileType,
    DateTime CreatedAt
);

public sealed record CandidateEducationResponse(
    Guid Id,
    string Institution,
    string Degree,
    string? FieldOfStudy,
    DateTime? StartDate,
    DateTime? EndDate,
    decimal? Gpa
);

public sealed record CandidateExperienceResponse(
    Guid Id,
    string Company,
    string Role,
    DateTime? StartDate,
    DateTime? EndDate,
    bool IsCurrent,
    string? Description,
    string? Location
);

public sealed record CandidateSkillResponse(
    Guid Id,
    string SkillName,
    string? SkillCategory,
    string? ProficiencyLevel,
    int? YearsExperience
);

public sealed record CandidateDocumentResponse(
    Guid Id,
    string FileName,
    string? FileType,
    string? FileUrl,
    long? FileSizeBytes,
    bool IsPrimary,
    DateTime UploadedAt
);

public sealed record ApproveCandidateResponse(
    Guid Id,
    string Status,
    string Message
);

public sealed record RejectCandidateResponse(
    Guid Id,
    string Status,
    string Message
);

public sealed record PaginatedResponse<T>(
    List<T> Items,
    int Total,
    int Page,
    int PageSize
);

public sealed record CandidateUpdateRequest(
    string Name,
    string Email,
    string? Phone,
    string? Location,
    string? LinkedInUrl,
    string? GitHubUrl,
    string? PortfolioUrl,
    string? Summary,
    int? TotalExperienceMonths,
    decimal? ExpectedSalaryMin,
    decimal? ExpectedSalaryMax,
    int? NoticePeriodDays,
    List<UpdateEducationEntry>? Education,
    List<UpdateExperienceEntry>? Experience,
    List<string>? Skills
);

public sealed record UpdateEducationEntry(
    Guid? Id,
    string Institution,
    string Degree,
    string? FieldOfStudy,
    DateTime? StartDate,
    DateTime? EndDate,
    decimal? Gpa
);

public sealed record UpdateExperienceEntry(
    Guid? Id,
    string Company,
    string Role,
    DateTime? StartDate,
    DateTime? EndDate,
    bool IsCurrent,
    string? Description,
    string? Location
);

public sealed record ActivityLogResponse(
    Guid Id,
    string Action,
    Guid PerformedBy,
    string? Details,
    DateTime CreatedAt
);

public sealed record AddNoteRequest(string Note);
