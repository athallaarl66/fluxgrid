namespace FluxGrid.Api.Modules.HR.Domain.Entities;

public class CandidateExperience
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CandidateId { get; set; }
    public string Company { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }

    public Candidate Candidate { get; set; } = null!;
}
