namespace FluxGrid.Api.Modules.HR.Domain.Entities;

public class CandidateDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CandidateId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public string? FileUrl { get; set; }
    public long? FileSizeBytes { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Candidate Candidate { get; set; } = null!;
}
