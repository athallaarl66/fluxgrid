using FluxGrid.Api.Shared.Domain.Events;

namespace FluxGrid.Api.Modules.HR.Domain.Events;

public sealed record CandidateUploaded(
    Guid CandidateId,
    string CandidateName,
    string Email,
    string FileName,
    string FileHash,
    long FileSizeBytes,
    Guid UploadedBy,
    Guid TenantId
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
