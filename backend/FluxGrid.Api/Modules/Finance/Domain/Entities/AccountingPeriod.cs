using System.ComponentModel.DataAnnotations;

namespace FluxGrid.Api.Modules.Finance.Domain.Entities;

public class AccountingPeriod
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "OPEN"; // OPEN, CLOSED
    public Guid? ClosedBy { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
