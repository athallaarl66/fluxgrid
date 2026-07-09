namespace FluxGrid.Api.Modules.HR.Domain.Entities;

public class Employee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string EmployeeNo { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Nik { get; set; }
    public string? EmergencyContact { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public string? JobTitle { get; set; }
    public decimal? BaseSalary { get; set; }
    public string? BankName { get; set; }
    public string? BankAccount { get; set; }
    public string? TaxId { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
