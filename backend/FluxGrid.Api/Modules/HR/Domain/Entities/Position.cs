namespace FluxGrid.Api.Modules.HR.Domain.Entities;

public class Position
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public Guid? SalaryGradeId { get; set; }
    public Guid TenantId { get; set; }
}
