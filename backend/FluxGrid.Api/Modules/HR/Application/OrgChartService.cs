using FluxGrid.Api.Modules.HR.API;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.HR.Application;

public class OrgChartService
{
    private readonly AppDbContext _db;

    public OrgChartService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<OrgChartNode>> GetOrgChartAsync(Guid tenantId)
    {
        return await _db.Employees
            .Where(e => e.TenantId == tenantId && e.Status == "ACTIVE")
            .OrderBy(e => e.EmployeeNo)
            .Select(e => new OrgChartNode(
                e.Id, e.EmployeeNo, e.FirstName, e.LastName,
                e.JobTitle, e.DepartmentId, e.ManagerId))
            .ToListAsync();
    }
}
