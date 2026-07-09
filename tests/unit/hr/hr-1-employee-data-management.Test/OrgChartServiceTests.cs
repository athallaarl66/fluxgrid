using FluxGrid.Api.Modules.HR.Application;
using FluxGrid.Api.Modules.HR.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Tests.HR;

public class OrgChartServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly OrgChartService _service;
    private readonly Guid _tenantId = Guid.NewGuid();

    public OrgChartServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _service = new OrgChartService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task GetOrgChartAsync_ReturnsActiveEmployees()
    {
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(), EmployeeNo = "EMP-001", FirstName = "Active", LastName = "User",
            Email = "active@test.com", Status = "ACTIVE", HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(), EmployeeNo = "EMP-002", FirstName = "Terminated", LastName = "User",
            Email = "term@test.com", Status = "TERMINATED", TerminationDate = DateTime.UtcNow,
            HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetOrgChartAsync(_tenantId);

        Assert.Single(result);
        Assert.Equal("Active", result[0].FirstName);
    }

    [Fact]
    public async Task GetOrgChartAsync_ReturnsAllActiveEmployees()
    {
        for (int i = 1; i <= 5; i++)
        {
            _db.Employees.Add(new Employee
            {
                Id = Guid.NewGuid(), EmployeeNo = $"EMP-{i:D3}", FirstName = $"F{i}", LastName = $"L{i}",
                Email = $"u{i}@test.com", Status = "ACTIVE", HireDate = DateTime.UtcNow, TenantId = _tenantId
            });
        }
        await _db.SaveChangesAsync();

        var result = await _service.GetOrgChartAsync(_tenantId);

        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task GetOrgChartAsync_RespectsTenantIsolation()
    {
        var otherTenant = Guid.NewGuid();
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(), EmployeeNo = "EMP-001", FirstName = "Other", LastName = "Co",
            Email = "other@test.com", Status = "ACTIVE", HireDate = DateTime.UtcNow, TenantId = otherTenant
        });
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(), EmployeeNo = "EMP-002", FirstName = "Mine", LastName = "Co",
            Email = "mine@test.com", Status = "ACTIVE", HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetOrgChartAsync(_tenantId);

        Assert.Single(result);
        Assert.Equal("Mine", result[0].FirstName);
    }

    [Fact]
    public async Task GetOrgChartAsync_OrdersByEmployeeNo()
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

        var result = await _service.GetOrgChartAsync(_tenantId);

        Assert.Equal("EMP-001", result[0].EmployeeNo);
        Assert.Equal("EMP-002", result[1].EmployeeNo);
        Assert.Equal("EMP-003", result[2].EmployeeNo);
    }

    [Fact]
    public async Task GetOrgChartAsync_ReturnsEmptyWhenNoActiveEmployees()
    {
        var result = await _service.GetOrgChartAsync(_tenantId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetOrgChartAsync_ReturnsFlatListWithManagerId()
    {
        var mgrId = Guid.NewGuid();
        _db.Employees.Add(new Employee
        {
            Id = mgrId, EmployeeNo = "EMP-001", FirstName = "Manager", LastName = "A",
            Email = "mgr@test.com", Status = "ACTIVE", HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(), EmployeeNo = "EMP-002", FirstName = "Report", LastName = "B",
            Email = "rpt@test.com", Status = "ACTIVE", ManagerId = mgrId,
            HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetOrgChartAsync(_tenantId);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.ManagerId == null);
        Assert.Contains(result, e => e.ManagerId == mgrId);
    }
}
