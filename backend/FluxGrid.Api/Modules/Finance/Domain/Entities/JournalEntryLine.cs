namespace FluxGrid.Api.Modules.Finance.Domain.Entities;

public class JournalEntryLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EntryId { get; set; }
    public JournalEntry Entry { get; set; } = null!;
    public Guid AccountId { get; set; }
    public ChartOfAccount Account { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}
