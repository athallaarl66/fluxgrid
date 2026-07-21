namespace FluxGrid.Api.Modules.HR.Domain.Entities;

public class JobPosting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Requirements { get; set; }
    public string[] RequiredSkills { get; set; } = [];
    public int? MinExperienceYears { get; set; }
    public int? MaxExperienceYears { get; set; }
    public string? Location { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string Status { get; set; } = Domain.Enums.JobPostingStatus.Draft;
    public float[]? Embedding { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
