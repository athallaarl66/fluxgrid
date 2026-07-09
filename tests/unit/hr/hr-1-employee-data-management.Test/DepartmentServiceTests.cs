using System.Text.Json;
using FluxGrid.Api.Modules.HR.API;
using FluxGrid.Api.Modules.HR.Application;
using FluxGrid.Api.Modules.HR.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Tests.HR;

public class DepartmentServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly DepartmentService _service;
    private readonly AuditService _audit;
    private readonly DomainEventDispatcher _dispatcher;
    private readonly Guid _tenantId = Guid.NewGuid();

    public DepartmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _audit = new AuditService(_db);
        _dispatcher = new DomainEventDispatcher();
        _service = new DepartmentService(_db, _audit, _dispatcher);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    // ─── GetAllAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllDepartments()
    {
        SeedDepartments(3);
        await _db.SaveChangesAsync();

        var result = await _service.GetAllAsync(_tenantId);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_RespectsTenantIsolation()
    {
        var otherTenant = Guid.NewGuid();
        SeedDepartments(3);
        _db.Departments.Add(new Department
        {
            Id = Guid.NewGuid(), Name = "Other Dept", TenantId = otherTenant
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetAllAsync(_tenantId);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByName()
    {
        _db.Departments.Add(new Department { Id = Guid.NewGuid(), Name = "Z Dept", TenantId = _tenantId });
        _db.Departments.Add(new Department { Id = Guid.NewGuid(), Name = "A Dept", TenantId = _tenantId });
        _db.Departments.Add(new Department { Id = Guid.NewGuid(), Name = "M Dept", TenantId = _tenantId });
        await _db.SaveChangesAsync();

        var result = await _service.GetAllAsync(_tenantId);

        Assert.Equal("A Dept", result[0].Name);
        Assert.Equal("M Dept", result[1].Name);
        Assert.Equal("Z Dept", result[2].Name);
    }

    // ─── CreateAsync ──────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_CreatesTopLevelDepartment()
    {
        var request = new CreateDepartmentRequest("Engineering", null);
        var result = await _service.CreateAsync(_tenantId, request, Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Equal("Engineering", result.Name);
        Assert.Null(result.ParentId);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_CreatesChildDepartment()
    {
        var parent = new Department { Id = Guid.NewGuid(), Name = "Parent", TenantId = _tenantId };
        _db.Departments.Add(parent);
        await _db.SaveChangesAsync();

        var request = new CreateDepartmentRequest("Child", parent.Id);
        var result = await _service.CreateAsync(_tenantId, request, Guid.NewGuid());

        Assert.Equal("Child", result.Name);
        Assert.Equal(parent.Id, result.ParentId);
    }

    [Fact]
    public async Task CreateAsync_ThrowsOnDuplicateName()
    {
        _db.Departments.Add(new Department { Id = Guid.NewGuid(), Name = "Duplicate", TenantId = _tenantId });
        await _db.SaveChangesAsync();

        var request = new CreateDepartmentRequest("Duplicate", null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(_tenantId, request, Guid.NewGuid()));
        Assert.Contains("name already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenParentNotFound()
    {
        var request = new CreateDepartmentRequest("Orphan", Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(_tenantId, request, Guid.NewGuid()));
        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_ThrowsOnMaxDepthExceeded()
    {
        // Build chain of 5 departments: L0 → L1 → L2 → L3 → L4
        var ids = new Guid[5];
        for (int i = 0; i < 5; i++)
        {
            ids[i] = Guid.NewGuid();
            _db.Departments.Add(new Department
            {
                Id = ids[i], Name = $"L{i}", ParentId = i > 0 ? ids[i - 1] : null, TenantId = _tenantId
            });
        }
        await _db.SaveChangesAsync();

        var request = new CreateDepartmentRequest("Too Deep", ids[4]);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(_tenantId, request, Guid.NewGuid()));
        Assert.Contains("Maximum hierarchy depth", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_AllowsDepth4()
    {
        // Build chain of 4 departments: L0 → L1 → L2 → L3
        var ids = new Guid[4];
        for (int i = 0; i < 4; i++)
        {
            ids[i] = Guid.NewGuid();
            _db.Departments.Add(new Department
            {
                Id = ids[i], Name = $"L{i}", ParentId = i > 0 ? ids[i - 1] : null, TenantId = _tenantId
            });
        }
        await _db.SaveChangesAsync();

        var request = new CreateDepartmentRequest("Level 4 Child", ids[3]);
        var result = await _service.CreateAsync(_tenantId, request, Guid.NewGuid());

        Assert.Equal("Level 4 Child", result.Name);
    }

    // ─── UpdateAsync ──────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_UpdatesDepartmentName()
    {
        var id = Guid.NewGuid();
        _db.Departments.Add(new Department { Id = id, Name = "Old Name", TenantId = _tenantId });
        await _db.SaveChangesAsync();

        var request = new UpdateDepartmentRequest("New Name", null, null);
        var result = await _service.UpdateAsync(id, _tenantId, request, Guid.NewGuid());

        Assert.Equal("New Name", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsWhenNotFound()
    {
        var request = new UpdateDepartmentRequest("New", null, null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(Guid.NewGuid(), _tenantId, request, Guid.NewGuid()));
        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_RejectsSelfParent()
    {
        var id = Guid.NewGuid();
        _db.Departments.Add(new Department { Id = id, Name = "Self", TenantId = _tenantId });
        await _db.SaveChangesAsync();

        var request = new UpdateDepartmentRequest(null, id, null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(id, _tenantId, request, Guid.NewGuid()));
        Assert.Contains("cannot be its own parent", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_RejectsCircularReference()
    {
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();
        _db.Departments.Add(new Department { Id = aId, Name = "Parent", TenantId = _tenantId });
        _db.Departments.Add(new Department { Id = bId, Name = "Child", ParentId = aId, TenantId = _tenantId });
        await _db.SaveChangesAsync();

        // Move parent under child -> circular
        var request = new UpdateDepartmentRequest(null, bId, null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(aId, _tenantId, request, Guid.NewGuid()));
        Assert.Contains("Circular reference", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_RejectsDuplicateName()
    {
        _db.Departments.Add(new Department { Id = Guid.NewGuid(), Name = "Existing", TenantId = _tenantId });
        var targetId = Guid.NewGuid();
        _db.Departments.Add(new Department { Id = targetId, Name = "Target", TenantId = _tenantId });
        await _db.SaveChangesAsync();

        var request = new UpdateDepartmentRequest("Existing", null, null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(targetId, _tenantId, request, Guid.NewGuid()));
        Assert.Contains("name already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_SetsInactive()
    {
        var id = Guid.NewGuid();
        _db.Departments.Add(new Department { Id = id, Name = "Active Dept", TenantId = _tenantId });
        await _db.SaveChangesAsync();

        var request = new UpdateDepartmentRequest(null, null, false);
        var result = await _service.UpdateAsync(id, _tenantId, request, Guid.NewGuid());

        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_RejectsMaxDepthExceeded()
    {
        var ids = new Guid[5];
        for (int i = 0; i < 5; i++)
        {
            ids[i] = Guid.NewGuid();
            _db.Departments.Add(new Department
            {
                Id = ids[i], Name = $"L{i}", ParentId = i > 0 ? ids[i - 1] : null, TenantId = _tenantId
            });
        }
        var otherId = Guid.NewGuid();
        _db.Departments.Add(new Department
        {
            Id = otherId, Name = "Other", TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        // Move "Other" under L4 (depth 4) — new depth would be 5 (6 levels) → exceeds max 5
        var request = new UpdateDepartmentRequest(null, ids[4], null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(otherId, _tenantId, request, Guid.NewGuid()));
        Assert.Contains("Maximum hierarchy depth", ex.Message);
    }

    // ─── DeleteAsync ──────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesDepartment()
    {
        var id = Guid.NewGuid();
        _db.Departments.Add(new Department { Id = id, Name = "To Delete", TenantId = _tenantId });
        await _db.SaveChangesAsync();

        await _service.DeleteAsync(id, _tenantId, Guid.NewGuid());

        var deleted = await _db.Departments.FindAsync(id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsWhenNotFound()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteAsync(Guid.NewGuid(), _tenantId, Guid.NewGuid()));
        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsWhenEmployeesAssigned()
    {
        var deptId = Guid.NewGuid();
        _db.Departments.Add(new Department { Id = deptId, Name = "Has Staff", TenantId = _tenantId });
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(), EmployeeNo = "EMP-001", FirstName = "A", LastName = "B",
            Email = "a@b.com", DepartmentId = deptId, Status = "ACTIVE",
            HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteAsync(deptId, _tenantId, Guid.NewGuid()));
        Assert.Contains("employees are assigned", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsWhenHasChildren()
    {
        var parentId = Guid.NewGuid();
        _db.Departments.Add(new Department { Id = parentId, Name = "Parent", TenantId = _tenantId });
        _db.Departments.Add(new Department { Id = Guid.NewGuid(), Name = "Child", ParentId = parentId, TenantId = _tenantId });
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteAsync(parentId, _tenantId, Guid.NewGuid()));
        Assert.Contains("child departments", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ─── Helpers ──────────────────────────────────────────────

    private void SeedDepartments(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            _db.Departments.Add(new Department
            {
                Id = Guid.NewGuid(),
                Name = $"Dept{i}",
                TenantId = _tenantId
            });
        }
    }
}
