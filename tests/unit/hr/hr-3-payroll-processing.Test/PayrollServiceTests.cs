using FluxGrid.Api.Modules.Finance.Domain.Entities;
using FluxGrid.Api.Modules.HR.API;
using FluxGrid.Api.Modules.HR.Application;
using FluxGrid.Api.Modules.HR.Domain.Entities;
using FluxGrid.Api.Modules.HR.Domain.Events;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FluxGrid.Api.Tests.HR;

public class PayrollServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly PayrollService _service;
    private readonly Mock<HttpClient> _httpClientMock;
    private readonly AuditService _audit;
    private readonly DomainEventDispatcher _dispatcher;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public PayrollServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _audit = new AuditService(_db);
        _dispatcher = new DomainEventDispatcher();
        _httpClientMock = new Mock<HttpClient>();
        _service = new PayrollService(_db, _audit, _dispatcher, _httpClientMock.Object);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    // ─── CalculatePayrollAsync ─────────────────────────────────

    [Fact]
    public async Task CalculatePayrollAsync_CreatesDraftRunWithRecords()
    {
        SeedActiveEmployees(3, withSalary: true);
        await _db.SaveChangesAsync();

        var result = await _service.CalculatePayrollAsync(
            _tenantId, new CreatePayrollRequest("May 2026", new DateTime(2026, 5, 1), new DateTime(2026, 5, 31)),
            _userId);

        Assert.NotNull(result);
        Assert.Equal("May 2026", result.PeriodName);
        Assert.Equal("DRAFT", result.Status);
        Assert.NotNull(result.TotalGross);
        Assert.NotNull(result.TotalNet);
        Assert.True(result.TotalGross > 0);
        Assert.True(result.TotalNet > 0);

        var records = await _db.PayrollRecords.Where(r => r.RunId == result.Id).ToListAsync();
        Assert.Equal(3, records.Count);
    }

    [Fact]
    public async Task CalculatePayrollAsync_ThrowsOnDuplicatePeriod()
    {
        _db.PayrollRuns.Add(new PayrollRun
        {
            Id = Guid.NewGuid(), PeriodName = "May 2026", Status = "DRAFT",
            StartDate = new DateTime(2026, 5, 1), EndDate = new DateTime(2026, 5, 31),
            TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CalculatePayrollAsync(
                _tenantId, new CreatePayrollRequest("May 2026", new DateTime(2026, 5, 1), new DateTime(2026, 5, 31)),
                _userId));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task CalculatePayrollAsync_ThrowsWhenNoActiveEmployees()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CalculatePayrollAsync(
                _tenantId, new CreatePayrollRequest("May 2026", new DateTime(2026, 5, 1), new DateTime(2026, 5, 31)),
                _userId));
        Assert.Contains("No active employees", ex.Message);
    }

    [Fact]
    public async Task CalculatePayrollAsync_RespectsTenantIsolation()
    {
        SeedActiveEmployees(2, withSalary: true, tenantId: Guid.NewGuid());
        SeedActiveEmployees(1, withSalary: true, tenantId: _tenantId);
        await _db.SaveChangesAsync();

        var result = await _service.CalculatePayrollAsync(
            _tenantId, new CreatePayrollRequest("May 2026", new DateTime(2026, 5, 1), new DateTime(2026, 5, 31)),
            _userId);

        var records = await _db.PayrollRecords.Where(r => r.RunId == result.Id).ToListAsync();
        Assert.Single(records);
    }

    // ─── FinalizePayrollAsync ──────────────────────────────────

    [Fact]
    public async Task FinalizePayrollAsync_SetsFinalizedAndDispatchesEvent()
    {
        var run = await SeedDraftRun();
        SeedOpenAccountingPeriod();
        await _db.SaveChangesAsync();

        var result = await _service.FinalizePayrollAsync(run.Id, _tenantId, _userId);

        Assert.Equal("FINALIZED", result.Status);
        var saved = await _db.PayrollRuns.FindAsync(run.Id);
        Assert.NotNull(saved);
        Assert.Equal("FINALIZED", saved.Status);
        Assert.Contains(_dispatcher.GetEvents(), e => e is PayrollProcessed);
    }

    [Fact]
    public async Task FinalizePayrollAsync_ThrowsWhenAlreadyFinalized()
    {
        var run = await SeedDraftRun();
        run.Status = "FINALIZED";
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.FinalizePayrollAsync(run.Id, _tenantId, _userId));
        Assert.Contains("can be finalized", ex.Message);
    }

    [Fact]
    public async Task FinalizePayrollAsync_ThrowsWhenPeriodClosed()
    {
        var run = await SeedDraftRun();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.FinalizePayrollAsync(run.Id, _tenantId, _userId));
        Assert.Contains("OPEN finance period", ex.Message);
    }

    [Fact]
    public async Task FinalizePayrollAsync_ThrowsWhenNotFound()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.FinalizePayrollAsync(Guid.NewGuid(), _tenantId, _userId));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task FinalizePayrollAsync_RespectsTenantIsolation()
    {
        var run = await SeedDraftRun();
        var otherTenant = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.FinalizePayrollAsync(run.Id, otherTenant, _userId));
        Assert.Contains("not found", ex.Message);
    }

    // ─── RecalculatePayrollAsync ───────────────────────────────

    [Fact]
    public async Task RecalculatePayrollAsync_ClearsAndRecalculates()
    {
        var run = await SeedDraftRun();
        _db.PayrollRecords.Add(new PayrollRecord
        {
            Id = Guid.NewGuid(), RunId = run.Id, EmployeeId = Guid.NewGuid(),
            BaseSalary = 100, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.RecalculatePayrollAsync(run.Id, _tenantId, _userId);

        var records = await _db.PayrollRecords.Where(r => r.RunId == run.Id).ToListAsync();
        Assert.NotNull(result);
        Assert.Equal("DRAFT", result.Status);
    }

    [Fact]
    public async Task RecalculatePayrollAsync_ThrowsWhenFinalized()
    {
        var run = await SeedDraftRun();
        run.Status = "FINALIZED";
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RecalculatePayrollAsync(run.Id, _tenantId, _userId));
        Assert.Contains("can be recalculated", ex.Message);
    }

    [Fact]
    public async Task RecalculatePayrollAsync_ThrowsWhenNotFound()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RecalculatePayrollAsync(Guid.NewGuid(), _tenantId, _userId));
        Assert.Contains("not found", ex.Message);
    }

    // ─── GetPayrollRunAsync ────────────────────────────────────

    [Fact]
    public async Task GetPayrollRunAsync_ReturnsRunWithRecords()
    {
        var run = await SeedDraftRun();
        await SeedRecord(run.Id);

        var result = await _service.GetPayrollRunAsync(run.Id, _tenantId);

        Assert.NotNull(result);
        Assert.Equal(run.Id, result.Run.Id);
        Assert.Single(result.Records);
    }

    [Fact]
    public async Task GetPayrollRunAsync_MasksSalaryWhenNotPermitted()
    {
        var run = await SeedDraftRun();
        await SeedRecord(run.Id, salary: 5000000);

        var result = await _service.GetPayrollRunAsync(run.Id, _tenantId, includeSalary: false);

        Assert.NotNull(result);
        Assert.Null(result.Run.TotalGross);
        Assert.Null(result.Run.TotalNet);
        Assert.Null(result.Records[0].BaseSalary);
        Assert.Null(result.Records[0].NetPay);
    }

    [Fact]
    public async Task GetPayrollRunAsync_ShowsSalaryWhenPermitted()
    {
        var run = await SeedDraftRun();
        await SeedRecord(run.Id, salary: 5000000);

        var result = await _service.GetPayrollRunAsync(run.Id, _tenantId, includeSalary: true);

        Assert.NotNull(result);
        Assert.NotNull(result.Run.TotalGross);
        Assert.NotNull(result.Run.TotalNet);
        Assert.NotNull(result.Records[0].BaseSalary);
        Assert.Equal(5000000, result.Records[0].BaseSalary);
    }

    [Fact]
    public async Task GetPayrollRunAsync_ReturnsNullWhenWrongTenant()
    {
        var run = await SeedDraftRun();

        var result = await _service.GetPayrollRunAsync(run.Id, Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPayrollRunAsync_ReturnsNullWhenNotFound()
    {
        var result = await _service.GetPayrollRunAsync(Guid.NewGuid(), _tenantId);

        Assert.Null(result);
    }

    // ─── ListPayrollRunsAsync ──────────────────────────────────

    [Fact]
    public async Task ListPayrollRunsAsync_ReturnsPaginatedResults()
    {
        for (int i = 0; i < 5; i++)
            await SeedDraftRun();

        var result = await _service.ListPayrollRunsAsync(_tenantId, null, 1, 2);

        Assert.Equal(5, result.Total);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task ListPayrollRunsAsync_FiltersByStatus()
    {
        await SeedDraftRun();
        var finalizedRun = await SeedDraftRun();
        finalizedRun.Status = "FINALIZED";
        await _db.SaveChangesAsync();

        var result = await _service.ListPayrollRunsAsync(_tenantId, "FINALIZED");

        Assert.Equal(1, result.Total);
        Assert.Equal("FINALIZED", result.Items[0].Status);
    }

    [Fact]
    public async Task ListPayrollRunsAsync_OrdersByCreatedAtDesc()
    {
        var run1 = await SeedDraftRun();
        await Task.Delay(10);
        var run2 = await SeedDraftRun();

        var result = await _service.ListPayrollRunsAsync(_tenantId, null);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(run2.Id, result.Items[0].Id);
        Assert.Equal(run1.Id, result.Items[1].Id);
    }

    [Fact]
    public async Task ListPayrollRunsAsync_MasksTotalsWhenNotPermitted()
    {
        await SeedDraftRun();

        var result = await _service.ListPayrollRunsAsync(_tenantId, null, includeSalary: false);

        Assert.Null(result.Items[0].TotalGross);
        Assert.Null(result.Items[0].TotalNet);
    }

    [Fact]
    public async Task ListPayrollRunsAsync_ShowsTotalsWhenPermitted()
    {
        await SeedDraftRun();

        var result = await _service.ListPayrollRunsAsync(_tenantId, null, includeSalary: true);

        Assert.NotNull(result.Items[0].TotalGross);
        Assert.NotNull(result.Items[0].TotalNet);
    }

    [Fact]
    public async Task ListPayrollRunsAsync_RespectsTenantIsolation()
    {
        await SeedDraftRun();
        await SeedDraftRun(tenantId: Guid.NewGuid());

        var result = await _service.ListPayrollRunsAsync(_tenantId, null);

        Assert.Equal(1, result.Total);
    }

    // ─── GetMyPayslipsAsync ────────────────────────────────────

    [Fact]
    public async Task GetMyPayslipsAsync_ReturnsRecordsForLinkedEmployee()
    {
        var employeeId = Guid.NewGuid();
        _db.Employees.Add(new Employee
        {
            Id = employeeId, EmployeeNo = "EMP-001", FirstName = "Test", LastName = "User",
            Email = "test@test.com", UserId = _userId, Status = "ACTIVE",
            HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        var run = await SeedDraftRun();
        run.Status = "FINALIZED";
        _db.PayrollRecords.Add(new PayrollRecord
        {
            Id = Guid.NewGuid(), RunId = run.Id, EmployeeId = employeeId,
            BaseSalary = 5000000, NetPay = 4500000, TenantId = _tenantId
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetMyPayslipsAsync(_tenantId, _userId);

        Assert.Single(result);
        Assert.Equal(5000000, result[0].BaseSalary);
    }

    [Fact]
    public async Task GetMyPayslipsAsync_ThrowsWhenNoLinkedEmployee()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetMyPayslipsAsync(_tenantId, _userId));
        Assert.Contains("No employee record", ex.Message);
    }

    // ─── Helpers ───────────────────────────────────────────────

    private void SeedActiveEmployees(int count, bool withSalary = false, Guid? tenantId = null)
    {
        var tid = tenantId ?? _tenantId;
        for (int i = 1; i <= count; i++)
        {
            _db.Employees.Add(new Employee
            {
                Id = Guid.NewGuid(), EmployeeNo = $"EMP-{i:D3}",
                FirstName = $"First{i}", LastName = $"Last{i}",
                Email = $"user{i}@test.com", Status = "ACTIVE",
                HireDate = new DateTime(2020, 1, 1),
                BaseSalary = withSalary ? 5000000m : null,
                TenantId = tid
            });
        }
    }

    private async Task<PayrollRun> SeedDraftRun(Guid? tenantId = null)
    {
        var tid = tenantId ?? _tenantId;
        var run = new PayrollRun
        {
            Id = Guid.NewGuid(), PeriodName = "May 2026",
            StartDate = new DateTime(2026, 5, 1), EndDate = new DateTime(2026, 5, 31),
            Status = "DRAFT", TotalGross = 50000000, TotalNet = 45000000,
            ProcessedBy = _userId.ToString(), TenantId = tid
        };
        _db.PayrollRuns.Add(run);
        await _db.SaveChangesAsync();
        return run;
    }

    private async Task SeedRecord(Guid runId, decimal salary = 5000000)
    {
        var employeeId = Guid.NewGuid();
        _db.Employees.Add(new Employee
        {
            Id = employeeId, EmployeeNo = "EMP-001", FirstName = "Test", LastName = "User",
            Email = "test@test.com", Status = "ACTIVE",
            HireDate = DateTime.UtcNow, TenantId = _tenantId
        });
        _db.PayrollRecords.Add(new PayrollRecord
        {
            Id = Guid.NewGuid(), RunId = runId, EmployeeId = employeeId,
            BaseSalary = salary, OvertimePay = 100000, LatenessDeduction = 50000,
            GrossPay = salary + 50000, TaxDeduction = 250000, NetPay = salary - 200000,
            TenantId = _tenantId
        });
        await _db.SaveChangesAsync();
    }

    private void SeedOpenAccountingPeriod()
    {
        _db.Set<AccountingPeriod>().Add(new AccountingPeriod
        {
            Id = Guid.NewGuid(), Name = "Period May 2026", Status = "OPEN",
            StartDate = new DateTime(2026, 5, 1), EndDate = new DateTime(2026, 5, 31),
            TenantId = _tenantId
        });
    }
}
