namespace FluxGrid.Api.Modules.HR.Domain.Entities;

public class PayrollRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "DRAFT";
    public decimal TotalGross { get; set; }
    public decimal TotalNet { get; set; }
    public string ProcessedBy { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PayrollRecord> Records { get; set; } = new List<PayrollRecord>();
}
