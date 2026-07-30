using FluxGrid.Api.Shared.RBAC;

namespace FluxGrid.Api.Modules.Admin.API;

public static class PermissionsEndpoints
{
    public static void MapPermissionsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/admin/permissions", () =>
        {
            var permissions = Permissions.All.Select(p => new
            {
                permission = p,
                module = p.Split(':')[0] ?? p.Split('.')[0],
                description = GetDescription(p)
            });

            return Results.Ok(permissions);
        })
        .RequireAuthorization()
        .RequireAuthorization("AdminOnly");
    }

    private static string GetDescription(string permission) => permission switch
    {
        "Dashboard:Read" => "View dashboard and KPIs",
        "WMS:Read" => "View warehouse data",
        "WMS:Write" => "Modify warehouse data",
        "WMS:Admin" => "Warehouse admin operations",
        "wms.inbound.create" => "Create inbound receipts",
        "wms.inbound.approve" => "Approve inbound receipts",
        "wms.outbound.process" => "Process outbound orders",
        "Finance:Read" => "View financial data",
        "Finance:Write" => "Modify financial data",
        "Finance:Admin" => "Finance admin operations",
        "finance.coa.read" => "View chart of accounts",
        "finance.coa.manage" => "Manage chart of accounts",
        "finance.journal.view" => "View journal entries",
        "finance.journal.create" => "Create journal entries",
        "finance.journal.approve" => "Approve journal entries",
        "finance.period.read" => "View accounting periods",
        "finance.period.admin" => "Manage accounting periods",
        "finance.budget.read" => "View budgets",
        "finance.budget.manage" => "Manage budgets",
        "finance.report.read" => "View financial reports",
        "HR:Read" => "View HR data",
        "HR:Write" => "Modify HR data",
        "HR:PayrollProcess" => "Process payroll",
        "HR:EmployeeRead" => "View employee data",
        "HR:EmployeeManage" => "Manage employees",
        "HR:PayrollRead" => "View payroll data",
        "HR:RecruitmentManage" => "Manage recruitment",
        "HR:JobRead" => "View job postings",
        "HR:JobManage" => "Manage job postings",
        "Task:Read" => "View tasks",
        "Task:Write" => "Modify tasks",
        _ => permission
    };
}
