namespace FluxGrid.Api.Modules.HR.API;

public sealed record CreateJobRequest(
    string Title,
    string Description,
    string? Requirements,
    string[]? RequiredSkills,
    int? MinExperienceYears,
    int? MaxExperienceYears,
    string? Location,
    decimal? SalaryMin,
    decimal? SalaryMax
);

public sealed record UpdateJobRequest(
    string? Title,
    string? Description,
    string? Requirements,
    string[]? RequiredSkills,
    int? MinExperienceYears,
    int? MaxExperienceYears,
    string? Location,
    decimal? SalaryMin,
    decimal? SalaryMax
);

public sealed record JobResponse(
    Guid Id,
    string Title,
    string Description,
    string? Requirements,
    string[] RequiredSkills,
    int? MinExperienceYears,
    int? MaxExperienceYears,
    string? Location,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string Status,
    Guid TenantId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public sealed record PublishJobResponse(
    Guid Id,
    string Status,
    string Message
);

public sealed record JobMatchItem(
    Guid CandidateId,
    string CandidateName,
    string CandidateEmail,
    double MatchScore,
    double? SemanticSimilarity,
    double? SkillMatchScore,
    double? ExperienceMatchScore,
    string? Skills,
    DateTime CalculatedAt
);

public sealed record JobMatchResponse(
    Guid JobId,
    string JobTitle,
    List<JobMatchItem> Matches
);

public sealed record MatchReasoningResponse(
    Guid CandidateId,
    string CandidateName,
    double MatchScore,
    string Reasoning
);
