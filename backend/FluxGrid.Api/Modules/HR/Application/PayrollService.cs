using System.Net.Http.Json;
using FluxGrid.Api.Modules.HR.API;
using FluxGrid.Api.Modules.HR.Domain.Entities;
using FluxGrid.Api.Modules.HR.Domain.Events;
using FluxGrid.Api.Modules.Notifications.Domain;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.HR.Application;

public class PayrollService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private readonly DomainEventDispatcher _events;
    private readonly HttpClient _httpClient;
    private readonly INotificationService _notif;

    public PayrollService(AppDbContext db, AuditService audit, DomainEventDispatcher events, HttpClient httpClient, INotificationService notif)
    {
        _db = db;
        _audit = audit;
        _events = events;
        _httpClient = httpClient;
        _notif = notif;
    }

    public async Task<PayrollRunResponse> CalculatePayrollAsync(
        Guid tenantId, CreatePayrollRequest request, Guid userId,
        string? ipAddress = null, string? userAgent = null)
    {
        if (await _db.PayrollRuns.AnyAsync(r => r.TenantId == tenantId && r.PeriodName == request.PeriodName))
            throw new InvalidOperationException("A payroll run for this period already exists");

        var employees = await _db.Employees
            .Where(e => e.TenantId == tenantId && e.Status == "ACTIVE")
            .ToListAsync();

        if (employees.Count == 0)
            throw new InvalidOperationException("No active employees found for this tenant");

        var periodDays = (request.EndDate - request.StartDate).Days + 1;

        var attendanceData = await FetchAttendanceSummaryAsync(tenantId, employees.Select(e => e.Id).ToList(), request.StartDate, request.EndDate);

        var run = new PayrollRun
        {
            Id = Guid.NewGuid(),
            PeriodName = request.PeriodName,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = "DRAFT",
            ProcessedBy = userId.ToString(),
            TenantId = tenantId
        };

        _db.PayrollRuns.Add(run);

        var totalGross = 0m;
        var totalNet = 0m;

        foreach (var employee in employees)
        {
            var workedDays = CalculateWorkedDays(employee, request.StartDate, request.EndDate, periodDays);
            var proratedBase = employee.BaseSalary.HasValue
                ? Math.Round(employee.BaseSalary.Value / periodDays * workedDays, 2)
                : 0m;

            var att = attendanceData?.FirstOrDefault(a => a.EmployeeId == employee.Id);
            var overtimePay = CalculateOvertimePay(proratedBase, att?.OvertimeHours ?? 0);
            var latenessDeduction = CalculateLatenessDeduction(proratedBase, att?.LateMinutes ?? 0);
            var grossPay = Math.Round(proratedBase + overtimePay - latenessDeduction, 2);
            var taxDeduction = Math.Round(grossPay * 0.05m, 2);
            var netPay = Math.Max(grossPay - taxDeduction, 0);

            var record = new PayrollRecord
            {
                Id = Guid.NewGuid(),
                RunId = run.Id,
                EmployeeId = employee.Id,
                BaseSalary = proratedBase,
                OvertimePay = overtimePay,
                LatenessDeduction = latenessDeduction,
                GrossPay = grossPay,
                TaxDeduction = taxDeduction,
                NetPay = netPay,
                TenantId = tenantId
            };

            _db.PayrollRecords.Add(record);

            totalGross += grossPay;
            totalNet += netPay;
        }

        run.TotalGross = totalGross;
        run.TotalNet = totalNet;

        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, tenantId, "CALCULATE", "payroll_runs", run.Id, ipAddress, userAgent, null, new { run.PeriodName, employeeCount = employees.Count });

        return MapRunToResponse(run);
    }

    public async Task<PayrollRunResponse> FinalizePayrollAsync(
        Guid id, Guid tenantId, Guid userId,
        string? ipAddress = null, string? userAgent = null)
    {
        var run = await _db.PayrollRuns
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId)
            ?? throw new InvalidOperationException("Payroll run not found");

        if (run.Status != "DRAFT")
            throw new InvalidOperationException("Only DRAFT payroll runs can be finalized");

        var periodOpen = await _db.AccountingPeriods
            .AnyAsync(p => p.TenantId == tenantId
                && p.Status == "OPEN"
                && p.StartDate <= run.StartDate
                && p.EndDate >= run.EndDate);

        if (!periodOpen)
            throw new InvalidOperationException("No OPEN finance period covers this payroll run. Open a period first.");

        run.Status = "FINALIZED";
        run.ProcessedBy = userId.ToString();

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("Payroll run was modified by another user. Please refresh and try again.");
        }

        await _audit.LogAsync(userId, tenantId, "FINALIZE", "payroll_runs", run.Id, ipAddress, userAgent,
            new { status = "DRAFT" }, new { status = "FINALIZED" });

        _events.Raise(new PayrollProcessed(
            run.Id, run.TotalGross,
            Math.Round(run.TotalGross * 0.05m, 2),
            run.TotalNet, run.PeriodName, run.TenantId, DateTime.UtcNow));

        await NotifyFinanceAsync($"Payroll for {run.PeriodName} has been finalized. Total net: {run.TotalNet:N0}");

        return MapRunToResponse(run);
    }

    public async Task<PayrollRunResponse> RecalculatePayrollAsync(
        Guid id, Guid tenantId, Guid userId,
        string? ipAddress = null, string? userAgent = null)
    {
        var run = await _db.PayrollRuns
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId)
            ?? throw new InvalidOperationException("Payroll run not found");

        if (run.Status != "DRAFT")
            throw new InvalidOperationException("Only DRAFT payroll runs can be recalculated");

        var existingRecords = await _db.PayrollRecords
            .Where(r => r.RunId == run.Id)
            .ToListAsync();

        _db.PayrollRecords.RemoveRange(existingRecords);

        var employees = await _db.Employees
            .Where(e => e.TenantId == tenantId && e.Status == "ACTIVE")
            .ToListAsync();

        var periodDays = (run.EndDate - run.StartDate).Days + 1;
        var attendanceData = await FetchAttendanceSummaryAsync(tenantId, employees.Select(e => e.Id).ToList(), run.StartDate, run.EndDate);

        var totalGross = 0m;
        var totalNet = 0m;

        foreach (var employee in employees)
        {
            var workedDays = CalculateWorkedDays(employee, run.StartDate, run.EndDate, periodDays);
            var proratedBase = employee.BaseSalary.HasValue
                ? Math.Round(employee.BaseSalary.Value / periodDays * workedDays, 2)
                : 0m;

            var att = attendanceData?.FirstOrDefault(a => a.EmployeeId == employee.Id);
            var overtimePay = CalculateOvertimePay(proratedBase, att?.OvertimeHours ?? 0);
            var latenessDeduction = CalculateLatenessDeduction(proratedBase, att?.LateMinutes ?? 0);
            var grossPay = Math.Round(proratedBase + overtimePay - latenessDeduction, 2);
            var taxDeduction = Math.Round(grossPay * 0.05m, 2);
            var netPay = Math.Max(grossPay - taxDeduction, 0);

            var record = new PayrollRecord
            {
                Id = Guid.NewGuid(),
                RunId = run.Id,
                EmployeeId = employee.Id,
                BaseSalary = proratedBase,
                OvertimePay = overtimePay,
                LatenessDeduction = latenessDeduction,
                GrossPay = grossPay,
                TaxDeduction = taxDeduction,
                NetPay = netPay,
                TenantId = tenantId
            };

            _db.PayrollRecords.Add(record);

            totalGross += grossPay;
            totalNet += netPay;
        }

        run.TotalGross = totalGross;
        run.TotalNet = totalNet;

        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, tenantId, "RECALCULATE", "payroll_runs", run.Id, ipAddress, userAgent, null, new { run.PeriodName, employeeCount = employees.Count });

        return MapRunToResponse(run);
    }

    public async Task<PayrollRunDetailResponse?> GetPayrollRunAsync(
        Guid id, Guid tenantId, int page = 1, int pageSize = 20, bool includeSalary = true)
    {
        var run = await _db.PayrollRuns
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);

        if (run is null) return null;

        var query = _db.PayrollRecords
            .Where(r => r.RunId == run.Id);

        var totalRecords = await query.CountAsync();

        var records = await query
            .OrderBy(r => r.EmployeeId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(_db.Employees,
                rec => rec.EmployeeId,
                emp => emp.Id,
                (rec, emp) => new { rec, emp })
            .ToListAsync();

        var recordResponses = records.Select(x => new PayrollRecordResponse(
            x.rec.Id, x.rec.RunId, x.rec.EmployeeId,
            x.emp.EmployeeNo,
            $"{x.emp.FirstName} {x.emp.LastName}",
            includeSalary ? x.rec.BaseSalary : null,
            includeSalary ? x.rec.OvertimePay : null,
            includeSalary ? x.rec.LatenessDeduction : null,
            includeSalary ? x.rec.GrossPay : null,
            includeSalary ? x.rec.TaxDeduction : null,
            includeSalary ? x.rec.NetPay : null,
            x.rec.TenantId
        )).ToList();

        return new PayrollRunDetailResponse(MapRunToResponse(run, includeSalary), totalRecords, recordResponses);
    }

    public async Task<PaginatedPayrollRunListResponse> ListPayrollRunsAsync(
        Guid tenantId, string? status, int page = 1, int pageSize = 20, bool includeSalary = true)
    {
        var query = _db.PayrollRuns.Where(r => r.TenantId == tenantId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var responses = items.Select(r => MapRunToResponse(r, includeSalary)).ToList();

        return new PaginatedPayrollRunListResponse(responses, total, page, pageSize);
    }

    public async Task<List<PayrollRecordResponse>> GetMyPayslipsAsync(Guid tenantId, Guid userId)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.UserId == userId)
            ?? throw new InvalidOperationException("No employee record linked to your account");

        var records = await _db.PayrollRecords
            .Where(r => r.TenantId == tenantId && r.EmployeeId == employee.Id)
            .OrderByDescending(r => r.RunId)
            .Join(_db.PayrollRuns,
                rec => rec.RunId,
                run => run.Id,
                (rec, run) => new { rec, run })
            .Where(x => x.run.Status == "FINALIZED")
            .Join(_db.Employees,
                x => x.rec.EmployeeId,
                emp => emp.Id,
                (x, emp) => new PayrollRecordResponse(
                    x.rec.Id, x.rec.RunId, x.rec.EmployeeId,
                    emp.EmployeeNo,
                    $"{emp.FirstName} {emp.LastName}",
                    x.rec.BaseSalary, x.rec.OvertimePay, x.rec.LatenessDeduction,
                    x.rec.GrossPay, x.rec.TaxDeduction, x.rec.NetPay, x.rec.TenantId))
            .ToListAsync();

        return records;
    }

    private static int CalculateWorkedDays(Employee employee, DateTime periodStart, DateTime periodEnd, int periodDays)
    {
        var effectiveStart = employee.HireDate > periodStart ? employee.HireDate : periodStart;
        var effectiveEnd = employee.TerminationDate.HasValue && employee.TerminationDate.Value < periodEnd
            ? employee.TerminationDate.Value
            : periodEnd;

        if (effectiveStart > effectiveEnd) return 0;

        return (effectiveEnd - effectiveStart).Days + 1;
    }

    private static decimal CalculateOvertimePay(decimal proratedBase, double overtimeHours)
    {
        if (overtimeHours <= 0 || proratedBase <= 0) return 0;
        var hourlyRate = proratedBase / 173;
        return Math.Round((decimal)overtimeHours * hourlyRate * 1.5m, 2);
    }

    private static decimal CalculateLatenessDeduction(decimal proratedBase, double lateMinutes)
    {
        if (lateMinutes <= 0 || proratedBase <= 0) return 0;
        var hourlyRate = proratedBase / 173;
        var minuteRate = hourlyRate / 60;
        return Math.Round((decimal)lateMinutes * minuteRate * 0.5m, 2);
    }

    private async Task<List<AttendanceSummary>?> FetchAttendanceSummaryAsync(
        Guid tenantId, List<Guid> employeeIds, DateTime from, DateTime to)
    {
        try
        {
            var ids = string.Join(",", employeeIds.Select(e => e.ToString()));
            var response = await _httpClient.GetAsync(
                $"/api/attendance/summary?employee_ids={ids}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<AttendanceSummary>>();
        }
        catch
        {
            return null;
        }
    }

    private static PayrollRunResponse MapRunToResponse(PayrollRun r, bool includeSalary = true) =>
        new PayrollRunResponse(
            r.Id, r.PeriodName, r.StartDate, r.EndDate,
            r.Status,
            includeSalary ? r.TotalGross : null,
            includeSalary ? r.TotalNet : null,
            r.ProcessedBy,
            r.TenantId, r.CreatedAt);

    private async Task NotifyFinanceAsync(string message)
    {
        var financeIds = await _db.Users
            .Where(u => u.IsActive && u.Roles.Any(r =>
                r.Name == "Admin" || r.Permissions.Contains("Finance:Read")))
            .Select(u => u.Id)
            .ToListAsync();
        foreach (var uid in financeIds)
            await _notif.CreateAsync(uid, "info", "Payroll Finalized", message);
    }
}

public sealed record AttendanceSummary(
    Guid EmployeeId,
    double OvertimeHours,
    double LateMinutes
);
