namespace FluxGrid.Api.Modules.HR.Domain.Entities;

public class PayrollRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RunId { get; set; }
    public Guid EmployeeId { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal LatenessDeduction { get; set; }
    public decimal GrossPay { get; set; }
    public decimal TaxDeduction { get; set; }
    public decimal NetPay { get; set; }
    public Guid TenantId { get; set; }

    public PayrollRun Run { get; set; } = null!;
}
