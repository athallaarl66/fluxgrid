using FluxGrid.Api.Modules.HR.API;
using FluxGrid.Api.Modules.HR.Domain.Entities;
using FluxGrid.Api.Modules.HR.Domain.Events;
using FluxGrid.Api.Shared.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.HR.Application;

public class EmployeeService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private readonly DomainEventDispatcher _events;

    public EmployeeService(AppDbContext db, AuditService audit, DomainEventDispatcher events)
    {
        _db = db;
        _audit = audit;
        _events = events;
    }

    public async Task<ListResult<EmployeeResponse>> GetListAsync(
        Guid tenantId, string? search, string? status, Guid? departmentId,
        int page = 1, int pageSize = 20, bool includeSalary = false)
    {
        var query = _db.Employees.Where(e => e.TenantId == tenantId);

        if (!string.IsNullOrEmpty(search))
        {
            var term = search.ToLower();
            query = query.Where(e =>
                e.FirstName.ToLower().Contains(term) ||
                e.LastName.ToLower().Contains(term) ||
                e.EmployeeNo.ToLower().Contains(term) ||
                e.Email.ToLower().Contains(term));
        }

        if (status is not null)
            query = query.Where(e => e.Status == status);

        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentId == departmentId);

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(e => e.EmployeeNo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeResponse(
                e.Id, e.UserId, e.EmployeeNo, e.FirstName, e.LastName, e.Email,
                e.Phone, e.DepartmentId, e.ManagerId, e.JobTitle, e.Status,
                e.HireDate, e.TerminationDate, e.TenantId))
            .ToListAsync();

        return new ListResult<EmployeeResponse>(items, total, page, pageSize);
    }

    public async Task<EmployeeDetailResponse?> GetByIdAsync(Guid id, Guid tenantId, bool includeSalary = false)
    {
        var query = _db.Employees.Where(e => e.Id == id && e.TenantId == tenantId);

        if (!includeSalary)
        {
            return await query
                .Select(e => new EmployeeDetailResponse(
                    e.Id, e.UserId, e.EmployeeNo, e.FirstName, e.LastName, e.Email,
                    e.Phone, e.Address, e.DateOfBirth, e.Nik, e.EmergencyContact,
                    e.DepartmentId, e.ManagerId, e.JobTitle,
                    null, null, null, null,
                    e.Status, e.HireDate, e.TerminationDate, e.TenantId,
                    e.CreatedAt, e.UpdatedAt))
                .FirstOrDefaultAsync();
        }

        return await query
            .Select(e => new EmployeeDetailResponse(
                e.Id, e.UserId, e.EmployeeNo, e.FirstName, e.LastName, e.Email,
                e.Phone, e.Address, e.DateOfBirth, e.Nik, e.EmergencyContact,
                e.DepartmentId, e.ManagerId, e.JobTitle,
                e.BaseSalary, e.BankName, e.BankAccount, e.TaxId,
                e.Status, e.HireDate, e.TerminationDate, e.TenantId,
                e.CreatedAt, e.UpdatedAt))
            .FirstOrDefaultAsync();
    }

    public async Task<EmployeeDetailResponse> CreateAsync(
        Guid tenantId, CreateEmployeeRequest request, Guid userId,
        string? ipAddress = null, string? userAgent = null)
    {
        if (await _db.Employees.AnyAsync(e => e.TenantId == tenantId && e.Email == request.Email))
            throw new InvalidOperationException("An employee with this email already exists");

        var employeeNo = await GenerateEmployeeNoAsync(tenantId);

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNo = employeeNo,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            DateOfBirth = request.DateOfBirth,
            Nik = request.Nik,
            EmergencyContact = request.EmergencyContact,
            DepartmentId = request.DepartmentId,
            ManagerId = request.ManagerId,
            JobTitle = request.JobTitle,
            HireDate = request.HireDate,
            Status = "ACTIVE",
            TenantId = tenantId
        };

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        var defaultRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Staff");
        var tempPassword = $"Emp{employee.EmployeeNo}!{(new Random().Next(100000, 999999))}";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = employee.Email,
            Email = employee.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
            IsActive = true,
            MustChangePassword = true,
            TenantId = tenantId,
            Roles = defaultRole is not null ? [defaultRole] : []
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        employee.UserId = user.Id;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, tenantId, "CREATE", "employees", employee.Id, ipAddress, userAgent, null, employee);

        _events.Raise(new EmployeeHired(
            employee.Id, employee.EmployeeNo, employee.FirstName, employee.LastName,
            employee.DepartmentId, employee.ManagerId, employee.JobTitle, userId, tenantId));

        return MapToDetail(employee);
    }

    public async Task<EmployeeDetailResponse> UpdateAsync(
        Guid id, Guid tenantId, UpdateEmployeeRequest request, Guid userId,
        string? ipAddress = null, string? userAgent = null)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId)
            ?? throw new InvalidOperationException("Employee not found");

        if (request.ManagerId.HasValue && request.ManagerId != employee.ManagerId)
        {
            if (request.ManagerId == id)
                throw new InvalidOperationException("An employee cannot be their own manager");

            if (await IsCircularReferenceAsync(id, request.ManagerId.Value))
                throw new InvalidOperationException("Circular reference detected: selected manager is a descendant of this employee");
        }

        var before = MapToDetail(employee);

        if (request.FirstName is not null) employee.FirstName = request.FirstName;
        if (request.LastName is not null) employee.LastName = request.LastName;
        if (request.Email is not null) employee.Email = request.Email;
        if (request.Phone is not null) employee.Phone = request.Phone;
        if (request.Address is not null) employee.Address = request.Address;
        if (request.DateOfBirth is not null) employee.DateOfBirth = request.DateOfBirth;
        if (request.EmergencyContact is not null) employee.EmergencyContact = request.EmergencyContact;
        if (request.DepartmentId is not null) employee.DepartmentId = request.DepartmentId;
        if (request.ManagerId is not null) employee.ManagerId = request.ManagerId;
        if (request.JobTitle is not null) employee.JobTitle = request.JobTitle;

        employee.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var after = MapToDetail(employee);
        await _audit.LogAsync(userId, tenantId, "UPDATE", "employees", employee.Id, ipAddress, userAgent, before, after);

        _events.Raise(new EmployeeUpdated(
            employee.Id, employee.EmployeeNo,
            before.JobTitle, before.DepartmentId, before.ManagerId,
            userId, tenantId));

        return after;
    }

    public async Task<EmployeeDetailResponse> TerminateAsync(
        Guid id, Guid tenantId, Guid userId,
        string? ipAddress = null, string? userAgent = null)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId)
            ?? throw new InvalidOperationException("Employee not found");

        if (employee.Status == "TERMINATED")
            throw new InvalidOperationException("Employee is already terminated");

        var before = MapToDetail(employee);

        employee.Status = "TERMINATED";
        employee.TerminationDate = DateTime.UtcNow;
        employee.UpdatedAt = DateTime.UtcNow;

        if (employee.UserId.HasValue)
        {
            var user = await _db.Users.FindAsync(employee.UserId.Value);
            if (user is not null)
            {
                user.IsActive = false;
            }
        }

        await _db.SaveChangesAsync();

        var after = MapToDetail(employee);
        await _audit.LogAsync(userId, tenantId, "TERMINATE", "employees", employee.Id, ipAddress, userAgent, before, after);

        _events.Raise(new EmployeeTerminated(
            employee.Id, employee.EmployeeNo, employee.UserId,
            employee.TerminationDate.Value, userId, tenantId));

        return after;
    }

    private async Task<bool> IsCircularReferenceAsync(Guid employeeId, Guid candidateManagerId)
    {
        var current = candidateManagerId;
        while (true)
        {
            if (current == employeeId) return true;
            var managerId = await _db.Employees
                .Where(e => e.Id == current)
                .Select(e => e.ManagerId)
                .FirstOrDefaultAsync();
            if (managerId is null) return false;
            current = managerId.Value;
        }
    }

    private async Task<string> GenerateEmployeeNoAsync(Guid tenantId)
    {
        var lastNo = await _db.Employees
            .Where(e => e.TenantId == tenantId)
            .OrderByDescending(e => e.EmployeeNo)
            .Select(e => e.EmployeeNo)
            .FirstOrDefaultAsync();

        if (lastNo is null)
            return "EMP-001";

        var parts = lastNo.Split('-');
        if (parts.Length == 2 && int.TryParse(parts[1], out var num))
            return $"EMP-{(num + 1):D3}";

        return "EMP-001";
    }

    private static EmployeeDetailResponse MapToDetail(Employee e)
    {
        return new EmployeeDetailResponse(
            e.Id, e.UserId, e.EmployeeNo, e.FirstName, e.LastName, e.Email,
            e.Phone, e.Address, e.DateOfBirth, e.Nik, e.EmergencyContact,
            e.DepartmentId, e.ManagerId, e.JobTitle,
            e.BaseSalary, e.BankName, e.BankAccount, e.TaxId,
            e.Status, e.HireDate, e.TerminationDate, e.TenantId,
            e.CreatedAt, e.UpdatedAt);
    }
}

public sealed record ListResult<T>(List<T> Items, int Total, int Page, int PageSize);
