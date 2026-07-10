namespace FluxGrid.Api.Modules.HR.Domain.Entities;

public class Candidate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? GitHubUrl { get; set; }
    public string? PortfolioUrl { get; set; }
    public string? Summary { get; set; }
    public int? TotalExperienceMonths { get; set; }
    public decimal? ExpectedSalaryMin { get; set; }
    public decimal? ExpectedSalaryMax { get; set; }
    public int? NoticePeriodDays { get; set; }
    public string Status { get; set; } = "DRAFT";
    public string? FileUrl { get; set; }
    public string? FileHash { get; set; }
    public string? OriginalFilename { get; set; }
    public string? FileType { get; set; }
    public long? FileSizeBytes { get; set; }
    public Guid UploadedBy { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<CandidateEducation> Education { get; set; } = [];
    public List<CandidateExperience> Experience { get; set; } = [];
    public List<CandidateSkill> Skills { get; set; } = [];
    public List<CandidateDocument> Documents { get; set; } = [];
}
