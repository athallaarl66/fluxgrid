using FluxGrid.Api.Modules.HR.Application;
using FluxGrid.Api.Shared.RBAC;
using Microsoft.AspNetCore.Mvc;

namespace FluxGrid.Api.Modules.HR.API;

public static class HrEndpoints
{
    public static void MapHrEndpoints(this WebApplication app)
    {
        var employees = app.MapGroup("/api/v1/hr/employees");
        var departments = app.MapGroup("/api/v1/hr/departments");
        var orgChart = app.MapGroup("/api/v1/hr/org-chart");

        employees.MapGet("/", async (
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] Guid? departmentId,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            EmployeeService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var includeSalary = http.User.HasClaim("permissions", Permissions.HrPayrollRead);
            var result = await service.GetListAsync(tenantId, search, status, departmentId, page ?? 1, pageSize ?? 20, includeSalary);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.HrRead);

        employees.MapGet("/{id:guid}", async (
            Guid id,
            EmployeeService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var includeSalary = http.User.HasClaim("permissions", Permissions.HrPayrollRead);
            var employee = await service.GetByIdAsync(id, tenantId, includeSalary);
            return employee is null ? Results.NotFound() : Results.Ok(employee);
        })
        .RequireAuthorization(Permissions.HrRead);

        employees.MapPost("/", async (
            CreateEmployeeRequest request,
            EmployeeService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var employee = await service.CreateAsync(tenantId, request, userId, ip, ua);
                return Results.Created($"/api/v1/hr/employees/{employee.Id}", employee);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.HrWrite);

        employees.MapPut("/{id:guid}", async (
            Guid id,
            UpdateEmployeeRequest request,
            EmployeeService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var employee = await service.UpdateAsync(id, tenantId, request, userId, ip, ua);
                return Results.Ok(employee);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.HrWrite);

        employees.MapPost("/{id:guid}/terminate", async (
            Guid id,
            EmployeeService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var employee = await service.TerminateAsync(id, tenantId, userId, ip, ua);
                return Results.Ok(employee);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.HrWrite);

        departments.MapGet("/", async (
            DepartmentService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var result = await service.GetAllAsync(tenantId);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.HrRead);

        departments.MapPost("/", async (
            CreateDepartmentRequest request,
            DepartmentService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var department = await service.CreateAsync(tenantId, request, userId, ip, ua);
                return Results.Created($"/api/v1/hr/departments/{department.Id}", department);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.HrWrite);

        departments.MapPut("/{id:guid}", async (
            Guid id,
            UpdateDepartmentRequest request,
            DepartmentService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var department = await service.UpdateAsync(id, tenantId, request, userId, ip, ua);
                return Results.Ok(department);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.HrWrite);

        departments.MapDelete("/{id:guid}", async (
            Guid id,
            DepartmentService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                await service.DeleteAsync(id, tenantId, userId, ip, ua);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization(Permissions.HrWrite);

        orgChart.MapGet("/", async (
            OrgChartService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var chart = await service.GetOrgChartAsync(tenantId);
            return Results.Ok(chart);
        })
        .RequireAuthorization(Permissions.HrRead);
    }

    private static (Guid tenantId, Guid userId, string? ip, string? ua) GetAuditContext(HttpContext http)
    {
        var tenantId = Guid.Empty;
        var userId = Guid.Empty;

        var tenantClaim = http.User.FindFirst("tenant_id")?.Value;
        if (Guid.TryParse(tenantClaim, out var tid)) tenantId = tid;

        var userClaim = http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userClaim, out var uid)) userId = uid;

        var ip = http.Connection.RemoteIpAddress?.ToString();
        var ua = http.Request.Headers.UserAgent.ToString();

        return (tenantId, userId, ip, ua);
    }
}
