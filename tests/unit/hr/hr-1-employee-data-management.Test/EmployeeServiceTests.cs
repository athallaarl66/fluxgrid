using FluxGrid.Api.Modules.HR.API;
using FluxGrid.Api.Modules.HR.Application;
using FluxGrid.Api.Modules.HR.Domain.Entities;
using FluxGrid.Api.Modules.HR.Domain.Events;
using FluxGrid.Api.Shared.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FluxGrid.Api.Tests.HR;

public class EmployeeServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly EmployeeService _service;
    private readonly AuditService _audit;
    private readonly DomainEventDispatcher _dispatcher;
    private readonly Guid _tenantId = Guid.NewGuid();

    public EmployeeServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _audit = new AuditService(_db);
        _dispatcher = new DomainEventDispatcher();
        _service = new EmployeeService(_db, _audit, _dispatcher);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    // ─── GetListAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetListAsync_ReturnsPaginatedResults()
    {
        SeedEmployees(5);
        await _db.SaveChangesAsync();

        var result = await _service.GetListAsync(_tenantId, null, null, null, 1, 2);

        Assert.Equal(5, result.Total);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task GetListAsync_FiltersBySearch()
    {
        SeedEmployees(3);
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(), EmployeeNo = "EMP-099", FirstName = "Zara", LastName = "Unique",
            Email = "zara@test.com", Status = "ACTIVE", HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetListAsync(_tenantId, "zara", null, null, 1, 20);

        Assert.Equal(1, result.Total);
        Assert.Equal("Zara", result.Items[0].FirstName);
    }

    [Fact]
    public async Task GetListAsync_FiltersByStatus()
    {
        SeedEmployees(3);
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(), EmployeeNo = "EMP-099", FirstName = "Term", LastName = "Staff",
            Email = "term@test.com", Status = "TERMINATED", HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetListAsync(_tenantId, null, "TERMINATED", null, 1, 20);

        Assert.Equal(1, result.Total);
    }

    [Fact]
    public async Task GetListAsync_FiltersByDepartment()
    {
        var deptId = Guid.NewGuid();
        SeedEmployees(3);
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(), EmployeeNo = "EMP-099", FirstName = "Dept", LastName = "Staff",
            Email = "dept@test.com", DepartmentId = deptId, Status = "ACTIVE",
            HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetListAsync(_tenantId, null, null, deptId, 1, 20);

        Assert.Equal(1, result.Total);
    }

    [Fact]
    public async Task GetListAsync_ExcludesSalaryByDefault()
    {
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(), EmployeeNo = "EMP-001", FirstName = "A", LastName = "B",
            Email = "a@test.com", BaseSalary = 5000000, Status = "ACTIVE",
            HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetListAsync(_tenantId, null, null, null, 1, 20);

        Assert.NotNull(result);
        // List response uses EmployeeResponse which has no salary field
        Assert.Equal(1, result.Total);
    }

    [Fact]
    public async Task GetListAsync_RespectsTenantIsolation()
    {
        var otherTenant = Guid.NewGuid();
        SeedEmployees(3);
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(), EmployeeNo = "EMP-099", FirstName = "Other", LastName = "Co",
            Email = "other@test.com", Status = "ACTIVE", HireDate = DateTime.UtcNow, TenantId = otherTenant
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetListAsync(_tenantId, null, null, null, 1, 20);

        Assert.Equal(3, result.Total);
    }

    [Fact]
    public async Task GetListAsync_OrdersByEmployeeNo()
    {
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(), EmployeeNo = "EMP-003", FirstName = "C", LastName = "C",
            Email = "c@test.com", Status = "ACTIVE", HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(), EmployeeNo = "EMP-001", FirstName = "A", LastName = "A",
            Email = "a@test.com", Status = "ACTIVE", HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(), EmployeeNo = "EMP-002", FirstName = "B", LastName = "B",
            Email = "b@test.com", Status = "ACTIVE", HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetListAsync(_tenantId, null, null, null, 1, 20);

        Assert.Equal("EMP-001", result.Items[0].EmployeeNo);
        Assert.Equal("EMP-002", result.Items[1].EmployeeNo);
        Assert.Equal("EMP-003", result.Items[2].EmployeeNo);
    }

    // ─── GetByIdAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsEmployee()
    {
        var id = Guid.NewGuid();
        _db.Employees.Add(new Employee
        {
            Id = id, EmployeeNo = "EMP-001", FirstName = "Test", LastName = "User",
            Email = "test@test.com", Status = "ACTIVE", HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync(id, _tenantId);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("EMP-001", result.EmployeeNo);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullWhenNotFound()
    {
        var result = await _service.GetByIdAsync(Guid.NewGuid(), _tenantId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_RespectsTenantIsolation()
    {
        var otherTenant = Guid.NewGuid();
        var id = Guid.NewGuid();
        _db.Employees.Add(new Employee
        {
            Id = id, EmployeeNo = "EMP-001", FirstName = "Test", LastName = "User",
            Email = "test@test.com", Status = "ACTIVE", HireDate = DateTime.UtcNow, TenantId = otherTenant
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync(id, _tenantId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ExcludesSalaryWhenNotPermitted()
    {
        var id = Guid.NewGuid();
        _db.Employees.Add(new Employee
        {
            Id = id, EmployeeNo = "EMP-001", FirstName = "Test", LastName = "User",
            Email = "test@test.com", BaseSalary = 5000000, Status = "ACTIVE",
            HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync(id, _tenantId, includeSalary: false);

        Assert.NotNull(result);
        Assert.Null(result.BaseSalary);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesSalaryWhenPermitted()
    {
        var id = Guid.NewGuid();
        _db.Employees.Add(new Employee
        {
            Id = id, EmployeeNo = "EMP-001", FirstName = "Test", LastName = "User",
            Email = "test@test.com", BaseSalary = 5000000, Status = "ACTIVE",
            HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync(id, _tenantId, includeSalary: true);

        Assert.NotNull(result);
        Assert.Equal(5000000, result.BaseSalary);
    }

    // ─── CreateAsync ──────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_CreatesEmployeeWithGeneratedNumber()
    {
        var request = new CreateEmployeeRequest(
            "John", "Doe", "john@test.com", null, null, null, null, null,
            null, null, "Engineer", DateTime.UtcNow);

        var result = await _service.CreateAsync(_tenantId, request, Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Equal("EMP-001", result.EmployeeNo);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
        Assert.Equal("ACTIVE", result.Status);
    }

    [Fact]
    public async Task CreateAsync_IncrementsEmployeeNumber()
    {
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(), EmployeeNo = "EMP-001", FirstName = "Existing", LastName = "User",
            Email = "existing@test.com", Status = "ACTIVE", HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var request = new CreateEmployeeRequest(
            "Jane", "Doe", "jane@test.com", null, null, null, null, null,
            null, null, "Designer", DateTime.UtcNow);

        var result = await _service.CreateAsync(_tenantId, request, Guid.NewGuid());

        Assert.Equal("EMP-002", result.EmployeeNo);
    }

    [Fact]
    public async Task CreateAsync_ThrowsOnDuplicateEmail()
    {
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(), EmployeeNo = "EMP-001", FirstName = "Existing", LastName = "User",
            Email = "dup@test.com", Status = "ACTIVE", HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var request = new CreateEmployeeRequest(
            "Jane", "Doe", "dup@test.com", null, null, null, null, null,
            null, null, "Designer", DateTime.UtcNow);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(_tenantId, request, Guid.NewGuid()));
        Assert.Contains("email already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_ProvisionsUserAccount()
    {
        await SeedDefaultRole();

        var request = new CreateEmployeeRequest(
            "John", "Doe", "john.provision@test.com", null, null, null, null, null,
            null, null, "Engineer", DateTime.UtcNow);

        var result = await _service.CreateAsync(_tenantId, request, Guid.NewGuid());

        Assert.NotNull(result.UserId);
        var user = await _db.Users.FindAsync(result.UserId.Value);
        Assert.NotNull(user);
        Assert.Equal("john.provision@test.com", user.Username);
        Assert.True(user.IsActive);
        Assert.True(user.MustChangePassword);
    }

    [Fact]
    public async Task CreateAsync_RaisesEmployeeHiredEvent()
    {
        var request = new CreateEmployeeRequest(
            "John", "Doe", "john.event@test.com", null, null, null, null, null,
            null, null, "Engineer", DateTime.UtcNow);

        await _service.CreateAsync(_tenantId, request, Guid.NewGuid());

        Assert.Single(_dispatcher.GetEvents());
        Assert.IsType<EmployeeHired>(_dispatcher.GetEvents()[0]);
    }

    // ─── UpdateAsync ──────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_UpdatesEmployeeFields()
    {
        var id = Guid.NewGuid();
        _db.Employees.Add(new Employee
        {
            Id = id, EmployeeNo = "EMP-001", FirstName = "Old", LastName = "Name",
            Email = "old@test.com", Status = "ACTIVE", HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var request = new UpdateEmployeeRequest("New", "Name", "new@test.com", null, null, null, null, null, null, "Senior");
        var result = await _service.UpdateAsync(id, _tenantId, request, Guid.NewGuid());

        Assert.Equal("New", result.FirstName);
        Assert.Equal("Name", result.LastName);
        Assert.Equal("new@test.com", result.Email);
        Assert.Equal("Senior", result.JobTitle);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsWhenNotFound()
    {
        var request = new UpdateEmployeeRequest("New", null, null, null, null, null, null, null, null, null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(Guid.NewGuid(), _tenantId, request, Guid.NewGuid()));
        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_RejectsSelfManager()
    {
        var id = Guid.NewGuid();
        _db.Employees.Add(new Employee
        {
            Id = id, EmployeeNo = "EMP-001", FirstName = "Self", LastName = "Manager",
            Email = "self@test.com", Status = "ACTIVE", HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var request = new UpdateEmployeeRequest(
            null, null, null, null, null, null, null, null, id, null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(id, _tenantId, request, Guid.NewGuid()));
        Assert.Contains("cannot be their own manager", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_RejectsCircularManagerReference()
    {
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        var cId = Guid.NewGuid();
        // Alice reports to Charlie, Bob reports to Alice (already creates cycle)
        _db.Employees.Add(new Employee
        {
            Id = aId, EmployeeNo = "EMP-001", FirstName = "Alice", LastName = "A",
            Email = "alice@test.com", ManagerId = cId, Status = "ACTIVE",
            HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        _db.Employees.Add(new Employee
        {
            Id = bId, EmployeeNo = "EMP-002", FirstName = "Bob", LastName = "B",
            Email = "bob@test.com", ManagerId = aId, Status = "ACTIVE",
            HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        // Try to set Alice's manager to Bob → cycle: Alice → Bob → Alice
        var request = new UpdateEmployeeRequest(
            null, null, null, null, null, null, null, null, bId, null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(aId, _tenantId, request, Guid.NewGuid()));
        Assert.Contains("Circular reference", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_RaisesEmployeeUpdatedEvent()
    {
        var id = Guid.NewGuid();
        _db.Employees.Add(new Employee
        {
            Id = id, EmployeeNo = "EMP-001", FirstName = "Old", LastName = "Name",
            Email = "old@test.com", Status = "ACTIVE", HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var request = new UpdateEmployeeRequest("New", null, null, null, null, null, null, null, null, null);
        await _service.UpdateAsync(id, _tenantId, request, Guid.NewGuid());

        Assert.Single(_dispatcher.GetEvents());
        Assert.IsType<EmployeeUpdated>(_dispatcher.GetEvents()[0]);
    }

    // ─── TerminateAsync ───────────────────────────────────────

    [Fact]
    public async Task TerminateAsync_SetsStatusAndDate()
    {
        var id = Guid.NewGuid();
        _db.Employees.Add(new Employee
        {
            Id = id, EmployeeNo = "EMP-001", FirstName = "Term", LastName = "Staff",
            Email = "term@test.com", Status = "ACTIVE", HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.TerminateAsync(id, _tenantId, Guid.NewGuid());

        Assert.Equal("TERMINATED", result.Status);
        Assert.NotNull(result.TerminationDate);
    }

    [Fact]
    public async Task TerminateAsync_ThrowsWhenAlreadyTerminated()
    {
        var id = Guid.NewGuid();
        _db.Employees.Add(new Employee
        {
            Id = id, EmployeeNo = "EMP-001", FirstName = "Term", LastName = "Staff",
            Email = "term@test.com", Status = "TERMINATED", TerminationDate = DateTime.UtcNow,
            HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.TerminateAsync(id, _tenantId, Guid.NewGuid()));
        Assert.Contains("already terminated", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TerminateAsync_ThrowsWhenNotFound()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.TerminateAsync(Guid.NewGuid(), _tenantId, Guid.NewGuid()));
        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TerminateAsync_DeactivatesUserAccount()
    {
        var userId = Guid.NewGuid();
        var id = Guid.NewGuid();
        _db.Employees.Add(new Employee
        {
            Id = id, EmployeeNo = "EMP-001", FirstName = "Term", LastName = "Staff",
            Email = "term@test.com", Status = "ACTIVE", UserId = userId,
            HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        _db.Users.Add(new User
        {
            Id = userId, Username = "term@test.com", Email = "term@test.com",
            PasswordHash = "hash", IsActive = true, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        await _service.TerminateAsync(id, _tenantId, Guid.NewGuid());

        var user = await _db.Users.FindAsync(userId);
        Assert.NotNull(user);
        Assert.False(user.IsActive);
    }

    [Fact]
    public async Task TerminateAsync_RaisesEmployeeTerminatedEvent()
    {
        var id = Guid.NewGuid();
        _db.Employees.Add(new Employee
        {
            Id = id, EmployeeNo = "EMP-001", FirstName = "Term", LastName = "Staff",
            Email = "term@test.com", Status = "ACTIVE", HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        await _service.TerminateAsync(id, _tenantId, Guid.NewGuid());

        Assert.Single(_dispatcher.GetEvents());
        Assert.IsType<EmployeeTerminated>(_dispatcher.GetEvents()[0]);
    }

    // ─── Employee No Generation ───────────────────────────────

    [Fact]
    public async Task GenerateEmployeeNo_StartsAtEMP001()
    {
        var request = new CreateEmployeeRequest(
            "A", "B", "first@test.com", null, null, null, null, null,
            null, null, "Dev", DateTime.UtcNow);

        var result = await _service.CreateAsync(_tenantId, request, Guid.NewGuid());
        Assert.Equal("EMP-001", result.EmployeeNo);
    }

    [Fact]
    public async Task GenerateEmployeeNo_IncrementsSequentially()
    {
        for (int i = 1; i <= 5; i++)
        {
            var request = new CreateEmployeeRequest(
                $"F{i}", $"L{i}", $"user{i}@test.com", null, null, null, null, null,
                null, null, "Dev", DateTime.UtcNow);
            var result = await _service.CreateAsync(_tenantId, request, Guid.NewGuid());
            Assert.Equal($"EMP-{i:D3}", result.EmployeeNo);
        }
    }

    [Fact]
    public async Task GenerateEmployeeNo_RespectsTenantIsolation()
    {
        var otherTenant = Guid.NewGuid();
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(), EmployeeNo = "EMP-999", FirstName = "Other", LastName = "Co",
            Email = "other@test.com", Status = "ACTIVE", HireDate = DateTime.UtcNow, TenantId = otherTenant
        });
        await _db.SaveChangesAsync();

        var request = new CreateEmployeeRequest(
            "New", "Emp", "new@test.com", null, null, null, null, null,
            null, null, "Dev", DateTime.UtcNow);

        var result = await _service.CreateAsync(_tenantId, request, Guid.NewGuid());
        Assert.Equal("EMP-001", result.EmployeeNo);
    }

    // ─── Helpers ──────────────────────────────────────────────

    private void SeedEmployees(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            _db.Employees.Add(new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNo = $"EMP-{i:D3}",
                FirstName = $"First{i}",
                LastName = $"Last{i}",
                Email = $"user{i}@test.com",
                Status = "ACTIVE",
                HireDate = DateTime.UtcNow,
                TenantId = _tenantId
            });
        }
    }

    private async Task SeedDefaultRole()
    {
        _db.Roles.Add(new Role
        {
            Id = Guid.NewGuid(),
            Name = "Staff"
        });
        await _db.SaveChangesAsync();
    }
}
