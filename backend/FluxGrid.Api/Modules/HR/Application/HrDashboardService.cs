using FluxGrid.Api.Modules.HR.API;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.HR.Application;

public class HrDashboardService
{
    private readonly AppDbContext _db;

    public HrDashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<HrDashboardResponse> GetDashboardAsync(Guid tenantId)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalEmployees = await _db.Employees.CountAsync(e => e.TenantId == tenantId);
        var activeEmployees = await _db.Employees.CountAsync(e => e.TenantId == tenantId && e.Status == "ACTIVE");
        var totalCandidates = await _db.Candidates.CountAsync(c => c.TenantId == tenantId);
        var candidatesActive = await _db.Candidates.CountAsync(c => c.TenantId == tenantId && c.Status == "ACTIVE");
        var candidatesParsed = await _db.Candidates.CountAsync(c => c.TenantId == tenantId && c.Status == "PARSED");
        var candidatesRejected = await _db.Candidates.CountAsync(c => c.TenantId == tenantId && c.Status == "REJECTED");
        var totalJobs = await _db.JobPostings.CountAsync(j => j.TenantId == tenantId);
        var publishedJobs = await _db.JobPostings.CountAsync(j => j.TenantId == tenantId && j.Status == "PUBLISHED");
        var draftJobs = await _db.JobPostings.CountAsync(j => j.TenantId == tenantId && j.Status == "DRAFT");

        var payrollMtd = await _db.PayrollRecords
            .Where(r => r.TenantId == tenantId && r.Run.StartDate >= monthStart)
            .SumAsync(r => (decimal?)r.NetPay);

        var payrollCountMtd = await _db.PayrollRecords
            .CountAsync(r => r.TenantId == tenantId && r.Run.StartDate >= monthStart);

        var recentHires = await (from e in _db.Employees
            join d in _db.Departments on e.DepartmentId equals d.Id into deptJoin
            from d in deptJoin.DefaultIfEmpty()
            where e.TenantId == tenantId && e.Status == "ACTIVE"
            orderby e.HireDate descending
            select new RecentHireRow(e.Id, e.FirstName, e.LastName, e.JobTitle ?? "", d.Name, e.HireDate))
            .Take(5)
            .ToListAsync();

        return new HrDashboardResponse(
            totalEmployees, activeEmployees,
            totalCandidates,
            new CandidatePipelineCounts(candidatesActive, candidatesParsed, candidatesRejected),
            totalJobs, publishedJobs, draftJobs,
            payrollMtd ?? 0, payrollCountMtd,
            recentHires
        );
    }
}
