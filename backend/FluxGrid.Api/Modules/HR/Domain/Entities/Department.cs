namespace FluxGrid.Api.Modules.HR.Domain.Entities;

public class Department
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public Department? Parent { get; set; }
    public Guid TenantId { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Department> Children { get; set; } = [];
}
