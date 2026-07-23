namespace FluxGrid.Api.Modules.HR.Domain.Entities;

public class CandidateJobMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CandidateId { get; set; }
    public Guid JobId { get; set; }
    public double Score { get; set; }
    public bool IsManual { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Candidate Candidate { get; set; } = null!;
    public JobPosting JobPosting { get; set; } = null!;
}
