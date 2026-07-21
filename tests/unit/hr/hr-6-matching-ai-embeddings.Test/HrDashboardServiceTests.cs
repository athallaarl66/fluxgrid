using FluxGrid.Api.Modules.HR.Application;
using FluxGrid.Api.Modules.HR.Domain.Entities;
using FluxGrid.Api.Modules.HR.Domain.Enums;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Tests.HR;

public class HrDashboardServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly HrDashboardService _service;
    private readonly Guid _tenantId = Guid.NewGuid();

    public HrDashboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _service = new HrDashboardService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetDashboardAsync_ReturnsZeroCountsWhenNoData()
    {
        var result = await _service.GetDashboardAsync(_tenantId);

        Assert.Equal(0, result.TotalEmployees);
        Assert.Equal(0, result.ActiveEmployees);
        Assert.Equal(0, result.TotalCandidates);
        Assert.Equal(0, result.TotalJobs);
        Assert.Equal(0, result.PayrollMtd);
        Assert.Empty(result.RecentHires);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsEmployeeCounts()
    {
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(), EmployeeNo = "EMP-001", FirstName = "A", LastName = "B",
            Email = "a@test.com", HireDate = DateTime.UtcNow, Status = "ACTIVE", TenantId = _tenantId
        });
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(), EmployeeNo = "EMP-002", FirstName = "C", LastName = "D",
            Email = "c@test.com", HireDate = DateTime.UtcNow, Status = "TERMINATED", TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetDashboardAsync(_tenantId);

        Assert.Equal(2, result.TotalEmployees);
        Assert.Equal(1, result.ActiveEmployees);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsCandidatePipeline()
    {
        _db.Candidates.Add(new Candidate { Id = Guid.NewGuid(), Name = "A", Email = "a@t.com", Status = CandidateStatus.Active, TenantId = _tenantId, UploadedBy = Guid.NewGuid() });
        _db.Candidates.Add(new Candidate { Id = Guid.NewGuid(), Name = "B", Email = "b@t.com", Status = CandidateStatus.Parsed, TenantId = _tenantId, UploadedBy = Guid.NewGuid() });
        _db.Candidates.Add(new Candidate { Id = Guid.NewGuid(), Name = "C", Email = "c@t.com", Status = CandidateStatus.Rejected, TenantId = _tenantId, UploadedBy = Guid.NewGuid() });
        _db.Candidates.Add(new Candidate { Id = Guid.NewGuid(), Name = "D", Email = "d@t.com", Status = CandidateStatus.Draft, TenantId = _tenantId, UploadedBy = Guid.NewGuid() });
        await _db.SaveChangesAsync();

        var result = await _service.GetDashboardAsync(_tenantId);

        Assert.Equal(4, result.TotalCandidates);
        Assert.Equal(1, result.CandidatePipeline.Active);
        Assert.Equal(1, result.CandidatePipeline.Parsed);
        Assert.Equal(1, result.CandidatePipeline.Rejected);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsJobCounts()
    {
        _db.JobPostings.Add(new JobPosting { Id = Guid.NewGuid(), Title = "A", Description = "D", Status = JobPostingStatus.Published, TenantId = _tenantId });
        _db.JobPostings.Add(new JobPosting { Id = Guid.NewGuid(), Title = "B", Description = "D", Status = JobPostingStatus.Draft, TenantId = _tenantId });
        _db.JobPostings.Add(new JobPosting { Id = Guid.NewGuid(), Title = "C", Description = "D", Status = JobPostingStatus.Closed, TenantId = _tenantId });
        await _db.SaveChangesAsync();

        var result = await _service.GetDashboardAsync(_tenantId);

        Assert.Equal(3, result.TotalJobs);
        Assert.Equal(1, result.PublishedJobs);
        Assert.Equal(1, result.DraftJobs);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsRecentHires()
    {
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(), EmployeeNo = "EMP-001", FirstName = "A", LastName = "B",
            Email = "a@test.com", Status = "ACTIVE", HireDate = DateTime.UtcNow.AddDays(-1), TenantId = _tenantId
        });
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(), EmployeeNo = "EMP-002", FirstName = "C", LastName = "D",
            Email = "c@test.com", Status = "ACTIVE", HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetDashboardAsync(_tenantId);

        Assert.Equal(2, result.RecentHires.Count);
        Assert.Equal("C", result.RecentHires[0].FirstName);
        Assert.Equal("A", result.RecentHires[1].FirstName);
    }

    [Fact]
    public async Task GetDashboardAsync_RespectsTenantIsolation()
    {
        var otherTenant = Guid.NewGuid();
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(), EmployeeNo = "EMP-001", FirstName = "Other", LastName = "Co",
            Email = "o@t.com", HireDate = DateTime.UtcNow, Status = "ACTIVE", TenantId = otherTenant
        });
        _db.Candidates.Add(new Candidate { Id = Guid.NewGuid(), Name = "X", Email = "x@t.com", Status = CandidateStatus.Active, TenantId = otherTenant, UploadedBy = Guid.NewGuid() });
        _db.JobPostings.Add(new JobPosting { Id = Guid.NewGuid(), Title = "T", Description = "D", Status = JobPostingStatus.Published, TenantId = otherTenant });
        await _db.SaveChangesAsync();

        var result = await _service.GetDashboardAsync(_tenantId);

        Assert.Equal(0, result.TotalEmployees);
        Assert.Equal(0, result.TotalCandidates);
        Assert.Equal(0, result.TotalJobs);
    }
}
