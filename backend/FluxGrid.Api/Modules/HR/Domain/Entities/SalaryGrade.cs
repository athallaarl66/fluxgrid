namespace FluxGrid.Api.Modules.HR.Domain.Entities;

public class SalaryGrade
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Grade { get; set; } = string.Empty;
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public Guid TenantId { get; set; }
}
