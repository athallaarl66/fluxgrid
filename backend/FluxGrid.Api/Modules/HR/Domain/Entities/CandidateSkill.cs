namespace FluxGrid.Api.Modules.HR.Domain.Entities;

public class CandidateSkill
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CandidateId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string? SkillCategory { get; set; }
    public string? ProficiencyLevel { get; set; }
    public int? YearsExperience { get; set; }

    public Candidate Candidate { get; set; } = null!;
}
