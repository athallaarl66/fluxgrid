using FluxGrid.Api.Modules.HR.Application;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.RBAC;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.HR.API;

public static class PayrollEndpoints
{
    public static void MapPayrollEndpoints(this WebApplication app)
    {
        var payroll = app.MapGroup("/api/v1/hr/payroll");

        payroll.MapPost("/calculate", async (
            CreatePayrollRequest request,
            PayrollService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var result = await service.CalculatePayrollAsync(tenantId, request, userId, ip, ua);
                return Results.Created($"/api/v1/hr/payroll/{result.Id}", result);
            }
            catch (InvalidOperationException ex)
            {
                var code = ex.Message.Contains("already exists") ? "DUPLICATE_PERIOD" : null;
                return Results.Problem(detail: ex.Message, statusCode: code is not null ? 409 : 400, extensions: code is not null ? new Dictionary<string, object?> { ["code"] = code } : null);
            }
        })
        .RequireAuthorization(Permissions.HrPayrollProcess);

        payroll.MapPut("/{id:guid}/finalize", async (
            Guid id,
            PayrollService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var result = await service.FinalizePayrollAsync(id, tenantId, userId, ip, ua);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                var m = ex.Message;
                var code = m.Contains("can be finalized") ? "ALREADY_FINALIZED" : m.Contains("OPEN finance") ? "PERIOD_CLOSED" : null;
                return Results.Problem(detail: m, statusCode: code is not null ? 409 : 400, extensions: code is not null ? new Dictionary<string, object?> { ["code"] = code } : null);
            }
        })
        .RequireAuthorization(Permissions.HrPayrollProcess);

        payroll.MapPut("/{id:guid}/recalculate", async (
            Guid id,
            PayrollService service,
            HttpContext http) =>
        {
            var (tenantId, userId, ip, ua) = GetAuditContext(http);
            try
            {
                var result = await service.RecalculatePayrollAsync(id, tenantId, userId, ip, ua);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                var code = ex.Message.Contains("can be recalculated") ? "ALREADY_FINALIZED" : null;
                return Results.Problem(detail: ex.Message, statusCode: code is not null ? 409 : 400, extensions: code is not null ? new Dictionary<string, object?> { ["code"] = code } : null);
            }
        })
        .RequireAuthorization(Permissions.HrPayrollProcess);

        payroll.MapGet("/runs", async (
            [FromQuery] string? status,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            PayrollService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var includeSalary = http.User.HasClaim("permissions", Permissions.HrPayrollRead);
            var result = await service.ListPayrollRunsAsync(tenantId, status, page, pageSize, includeSalary);
            return Results.Ok(result);
        })
        .RequireAuthorization();

        payroll.MapGet("/{id:guid}", async (
            Guid id,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            PayrollService service,
            HttpContext http) =>
        {
            var (tenantId, _, _, _) = GetAuditContext(http);
            var includeSalary = http.User.HasClaim("permissions", Permissions.HrPayrollRead);
            var result = await service.GetPayrollRunAsync(id, tenantId, page, pageSize, includeSalary);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .RequireAuthorization();

        payroll.MapGet("/my-payslips", async (
            PayrollService service,
            HttpContext http) =>
        {
            var (tenantId, userId, _, _) = GetAuditContext(http);
            try
            {
                var result = await service.GetMyPayslipsAsync(tenantId, userId);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .RequireAuthorization();

        payroll.MapGet("/{runId:guid}/payslip/{employeeId:guid}", async (
            Guid runId,
            Guid employeeId,
            PayrollService service,
            HttpContext http) =>
        {
            var (tenantId, userId, _, _) = GetAuditContext(http);
            var detail = await service.GetPayrollRunAsync(runId, tenantId);
            if (detail is null) return Results.NotFound();

            var record = detail.Records.FirstOrDefault(r => r.EmployeeId == employeeId);
            if (record is null) return Results.NotFound();

            var isSelf = await IsSelfPayslip(http, employeeId, tenantId);
            var hasPayrollRead = http.User.HasClaim("permissions", Permissions.HrPayrollRead);
            if (!isSelf && !hasPayrollRead)
                return Results.Forbid();

            var html = GeneratePayslipHtml(record, detail.Run);
            return Results.Content(html, "text/html");
        })
        .RequireAuthorization();
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

    private static async Task<bool> IsSelfPayslip(HttpContext http, Guid employeeId, Guid tenantId)
    {
        var userIdClaim = http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return false;

        var db = http.RequestServices.GetRequiredService<Shared.Infrastructure.Data.AppDbContext>();
        return await db.Employees.AnyAsync(e => e.Id == employeeId && e.UserId == userId && e.TenantId == tenantId);
    }

    private static string GeneratePayslipHtml(PayrollRecordResponse record, PayrollRunResponse run)
    {
        return $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <style>
                body { font-family: Arial, sans-serif; max-width: 800px; margin: 0 auto; padding: 20px; }
                .header { text-align: center; margin-bottom: 30px; }
                table { width: 100%; border-collapse: collapse; }
                td { padding: 8px 12px; border-bottom: 1px solid #ddd; }
                td:last-child { text-align: right; font-variant-numeric: tabular-nums; }
                .total td { font-weight: bold; border-top: 2px solid #000; }
                .net { font-size: 1.4em; font-weight: bold; }
            </style>
        </head>
        <body>
            <div class="header">
                <h2>Payslip</h2>
                <p>{{run.PeriodName}} — {{run.StartDate:dd MMM yyyy}} to {{run.EndDate:dd MMM yyyy}}</p>
                <p>Employee: {{record.EmployeeNo}} — {{record.EmployeeName}}</p>
            </div>
            <table>
                <tr><td>Base Salary</td><td>{{record.BaseSalary:N2}}</td></tr>
                <tr><td>Overtime Pay</td><td>{{record.OvertimePay:N2}}</td></tr>
                <tr><td>Lateness Deduction</td><td>({{record.LatenessDeduction:N2}})</td></tr>
                <tr><td>Gross Pay</td><td>{{record.GrossPay:N2}}</td></tr>
                <tr><td>Tax Deduction (PPh 21)</td><td>({{record.TaxDeduction:N2}})</td></tr>
                <tr class="total"><td>Net Pay</td><td class="net">{{record.NetPay:N2}}</td></tr>
            </table>
        </body>
        </html>
        """;
    }
}
