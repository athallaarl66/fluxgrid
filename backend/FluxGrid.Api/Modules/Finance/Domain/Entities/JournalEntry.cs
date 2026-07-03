namespace FluxGrid.Api.Modules.Finance.Domain.Entities;

public class JournalEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntryNo { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "DRAFT"; // DRAFT, PENDING_APPROVAL, POSTED
    public decimal TotalAmount { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<JournalEntryLine> Lines { get; set; } = [];
}
