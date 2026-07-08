namespace FluxGrid.Api.Modules.Finance.Domain.Entities;

public class Budget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public ChartOfAccount? Account { get; set; }
    public Guid PeriodId { get; set; }
    public AccountingPeriod? Period { get; set; }
    public decimal PlannedAmount { get; set; }
    public string? Notes { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
