using System.Text.Json;

namespace FluxGrid.Api.Modules.HR.Domain.Entities;

public class CandidateActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CandidateId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid PerformedBy { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Candidate Candidate { get; set; } = null!;
}
