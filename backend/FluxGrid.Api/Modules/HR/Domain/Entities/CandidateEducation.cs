namespace FluxGrid.Api.Modules.HR.Domain.Entities;

public class CandidateEducation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CandidateId { get; set; }
    public string Institution { get; set; } = string.Empty;
    public string Degree { get; set; } = string.Empty;
    public string? FieldOfStudy { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Gpa { get; set; }

    public Candidate Candidate { get; set; } = null!;
}
