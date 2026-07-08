using FluxGrid.Api.Modules.Finance.API;
using FluxGrid.Api.Modules.Finance.Application;
using FluxGrid.Api.Modules.Finance.Domain.Entities;
using FluxGrid.Api.Modules.Finance.Domain.Enums;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Tests.Finance;

public class ReportServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ReportService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _otherTenantId = Guid.NewGuid();

    // Fixed account IDs for predictable seeding
    private static readonly Guid AssetAccountId = Guid.NewGuid();
    private static readonly Guid CurrentAssetId = Guid.NewGuid();
    private static readonly Guid CashBankId = Guid.NewGuid();
    private static readonly Guid LiabilityAccountId = Guid.NewGuid();
    private static readonly Guid EquityAccountId = Guid.NewGuid();
    private static readonly Guid RevenueAccountId = Guid.NewGuid();
    private static readonly Guid ExpenseAccountId = Guid.NewGuid();
    private static readonly Guid OrphanAccountId = Guid.NewGuid();

    public ReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _service = new ReportService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    // ─── Helpers ──────────────────────────────────────────────────────

    private async Task SeedAccounts()
    {
        _db.ChartOfAccounts.AddRange(
            new ChartOfAccount { Id = AssetAccountId, Code = "1000", Name = "Assets", Type = AccountTypes.Asset, IsActive = true, TenantId = _tenantId },
            new ChartOfAccount { Id = CurrentAssetId, Code = "1100", Name = "Current Assets", Type = AccountTypes.Asset, IsActive = true, TenantId = _tenantId, ParentId = AssetAccountId },
            new ChartOfAccount { Id = CashBankId, Code = "1110", Name = "Cash in Bank", Type = AccountTypes.Asset, IsActive = true, TenantId = _tenantId, ParentId = CurrentAssetId },
            new ChartOfAccount { Id = LiabilityAccountId, Code = "2000", Name = "Liabilities", Type = AccountTypes.Liability, IsActive = true, TenantId = _tenantId },
            new ChartOfAccount { Id = EquityAccountId, Code = "3000", Name = "Equity", Type = AccountTypes.Equity, IsActive = true, TenantId = _tenantId },
            new ChartOfAccount { Id = RevenueAccountId, Code = "4000", Name = "Revenue", Type = AccountTypes.Revenue, IsActive = true, TenantId = _tenantId },
            new ChartOfAccount { Id = ExpenseAccountId, Code = "5000", Name = "Expenses", Type = AccountTypes.Expense, IsActive = true, TenantId = _tenantId },
            new ChartOfAccount { Id = OrphanAccountId, Code = "9999", Name = "Orphan", Type = AccountTypes.Asset, IsActive = true, TenantId = _tenantId, ParentId = Guid.NewGuid() }
        );
        await _db.SaveChangesAsync();
    }

    private async Task<Guid> SeedPostedEntry(decimal amount, DateTime date, Guid? debitAccountId = null, Guid? creditAccountId = null)
    {
        debitAccountId ??= CashBankId;
        creditAccountId ??= LiabilityAccountId;
        var entry = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = date,
            Description = "Test posted entry",
            Status = "POSTED",
            TotalAmount = amount,
            TenantId = _tenantId,
            CreatedBy = Guid.NewGuid()
        };
        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();

        _db.JournalEntryLines.AddRange(
            new JournalEntryLine { EntryId = entry.Id, AccountId = debitAccountId.Value, Debit = amount, Credit = 0 },
            new JournalEntryLine { EntryId = entry.Id, AccountId = creditAccountId.Value, Debit = 0, Credit = amount }
        );
        await _db.SaveChangesAsync();
        return entry.Id;
    }

    private async Task<Guid> SeedDraftEntry(decimal amount, DateTime date)
    {
        var entry = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = date,
            Description = "Test draft entry",
            Status = "DRAFT",
            TotalAmount = amount,
            TenantId = _tenantId,
            CreatedBy = Guid.NewGuid()
        };
        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();

        _db.JournalEntryLines.AddRange(
            new JournalEntryLine { EntryId = entry.Id, AccountId = CashBankId, Debit = amount, Credit = 0 },
            new JournalEntryLine { EntryId = entry.Id, AccountId = LiabilityAccountId, Debit = 0, Credit = amount }
        );
        await _db.SaveChangesAsync();
        return entry.Id;
    }

    private async Task<Guid> SeedPendingApprovalEntry(decimal amount, DateTime date)
    {
        var entry = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = date,
            Description = "Test pending approval entry",
            Status = "PENDING_APPROVAL",
            TotalAmount = amount,
            TenantId = _tenantId,
            CreatedBy = Guid.NewGuid()
        };
        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();

        _db.JournalEntryLines.AddRange(
            new JournalEntryLine { EntryId = entry.Id, AccountId = CashBankId, Debit = amount, Credit = 0 },
            new JournalEntryLine { EntryId = entry.Id, AccountId = LiabilityAccountId, Debit = 0, Credit = amount }
        );
        await _db.SaveChangesAsync();
        return entry.Id;
    }

    private async Task SeedOtherTenantEntry(decimal amount, DateTime date)
    {
        var entry = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = date,
            Description = "Other tenant entry",
            Status = "POSTED",
            TotalAmount = amount,
            TenantId = _otherTenantId,
            CreatedBy = Guid.NewGuid()
        };
        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();

        _db.JournalEntryLines.AddRange(
            new JournalEntryLine { EntryId = entry.Id, AccountId = CashBankId, Debit = amount, Credit = 0 },
            new JournalEntryLine { EntryId = entry.Id, AccountId = LiabilityAccountId, Debit = 0, Credit = amount }
        );
        await _db.SaveChangesAsync();
    }

    // ══════════════════════════════════════════════════════════════════
    //  TRIAL BALANCE
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetTrialBalanceAsync_ReturnsAllAccountsWithBalances()
    {
        await SeedAccounts();
        await SeedPostedEntry(1_000_000, new DateTime(2026, 1, 15));

        var report = await _service.GetTrialBalanceAsync(_tenantId,
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), false);

        Assert.NotNull(report);
        Assert.Equal(1_000_000, report.TotalDebit);
        Assert.Equal(1_000_000, report.TotalCredit);
    }

    [Fact]
    public async Task GetTrialBalanceAsync_TotalDebitEqualsTotalCredit()
    {
        await SeedAccounts();
        await SeedPostedEntry(2_500_000, new DateTime(2026, 2, 15));
        await SeedPostedEntry(3_000_000, new DateTime(2026, 2, 20));

        var report = await _service.GetTrialBalanceAsync(_tenantId,
            new DateTime(2026, 2, 1), new DateTime(2026, 2, 28), false);

        Assert.Equal(5_500_000, report.TotalDebit);
        Assert.Equal(5_500_000, report.TotalCredit);
    }

    [Fact]
    public async Task GetTrialBalanceAsync_ExcludesDraftsWhenIncludeDraftsFalse()
    {
        await SeedAccounts();
        await SeedPostedEntry(1_000_000, new DateTime(2026, 3, 15));
        await SeedDraftEntry(500_000, new DateTime(2026, 3, 16));

        var report = await _service.GetTrialBalanceAsync(_tenantId,
            new DateTime(2026, 3, 1), new DateTime(2026, 3, 31), false);

        Assert.Equal(1_000_000, report.TotalDebit);
    }

    [Fact]
    public async Task GetTrialBalanceAsync_IncludesDraftsWhenIncludeDraftsTrue()
    {
        await SeedAccounts();
        await SeedPostedEntry(1_000_000, new DateTime(2026, 3, 15));
        await SeedDraftEntry(500_000, new DateTime(2026, 3, 16));

        var report = await _service.GetTrialBalanceAsync(_tenantId,
            new DateTime(2026, 3, 1), new DateTime(2026, 3, 31), true);

        Assert.Equal(1_500_000, report.TotalDebit);
    }

    [Fact]
    public async Task GetTrialBalanceAsync_IncludesPendingApprovalWhenIncludeDraftsFalse()
    {
        await SeedAccounts();
        await SeedPendingApprovalEntry(750_000, new DateTime(2026, 4, 10));

        var report = await _service.GetTrialBalanceAsync(_tenantId,
            new DateTime(2026, 4, 1), new DateTime(2026, 4, 30), false);

        Assert.Equal(0, report.TotalDebit);
    }

    [Fact]
    public async Task GetTrialBalanceAsync_IncludesPendingApprovalWhenIncludeDraftsTrue()
    {
        await SeedAccounts();
        await SeedPendingApprovalEntry(750_000, new DateTime(2026, 4, 10));

        var report = await _service.GetTrialBalanceAsync(_tenantId,
            new DateTime(2026, 4, 1), new DateTime(2026, 4, 30), true);

        Assert.Equal(750_000, report.TotalDebit);
    }

    [Fact]
    public async Task GetTrialBalanceAsync_FiltersByDateRange()
    {
        await SeedAccounts();
        await SeedPostedEntry(1_000_000, new DateTime(2026, 1, 15));
        await SeedPostedEntry(2_000_000, new DateTime(2026, 2, 15));

        var report = await _service.GetTrialBalanceAsync(_tenantId,
            new DateTime(2026, 2, 1), new DateTime(2026, 2, 28), false);

        Assert.Equal(2_000_000, report.TotalDebit);
    }

    [Fact]
    public async Task GetTrialBalanceAsync_ReturnsHierarchicalTree()
    {
        await SeedAccounts();
        await SeedPostedEntry(1_000_000, new DateTime(2026, 5, 15));

        var report = await _service.GetTrialBalanceAsync(_tenantId,
            new DateTime(2026, 5, 1), new DateTime(2026, 5, 31), false);

        var assets = report.Rows.Single(r => r.Code == "1000");
        Assert.Equal(0, assets.Depth);

        var currentAssets = Assert.Single(assets.Children);
        Assert.Equal("1100", currentAssets.Code);
        Assert.Equal(1, currentAssets.Depth);

        var cashBank = Assert.Single(currentAssets.Children);
        Assert.Equal("1110", cashBank.Code);
        Assert.Equal(2, cashBank.Depth);
    }

    [Fact]
    public async Task GetTrialBalanceAsync_ParentAggregatesChildBalances()
    {
        await SeedAccounts();
        await SeedPostedEntry(5_000_000, new DateTime(2026, 6, 15));

        var report = await _service.GetTrialBalanceAsync(_tenantId,
            new DateTime(2026, 6, 1), new DateTime(2026, 6, 30), false);

        var assets = report.Rows[0];
        var currentAssets = assets.Children[0];
        var cashBank = currentAssets.Children[0];

        Assert.Equal(5_000_000, cashBank.Debit);
        Assert.Equal(5_000_000, currentAssets.Debit);
        Assert.Equal(5_000_000, assets.Debit);
    }

    [Fact]
    public async Task GetTrialBalanceAsync_ReturnsEmptyWhenNoAccounts()
    {
        var report = await _service.GetTrialBalanceAsync(_tenantId,
            new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), false);

        Assert.Empty(report.Rows);
        Assert.Equal(0, report.TotalDebit);
        Assert.Equal(0, report.TotalCredit);
    }

    [Fact]
    public async Task GetTrialBalanceAsync_ReturnsZeroBalancesWhenNoEntries()
    {
        await SeedAccounts();

        var report = await _service.GetTrialBalanceAsync(_tenantId,
            new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), false);

        Assert.NotEmpty(report.Rows);
        Assert.Equal(0, report.TotalDebit);
        Assert.Equal(0, report.TotalCredit);
    }

    [Fact]
    public async Task GetTrialBalanceAsync_RespectsTenantIsolation()
    {
        await SeedAccounts();
        await SeedPostedEntry(1_000_000, new DateTime(2026, 7, 15));
        await SeedOtherTenantEntry(5_000_000, new DateTime(2026, 7, 15));

        var report = await _service.GetTrialBalanceAsync(_tenantId,
            new DateTime(2026, 7, 1), new DateTime(2026, 7, 31), false);

        Assert.Equal(1_000_000, report.TotalDebit);
    }

    [Fact]
    public async Task GetTrialBalanceAsync_OnlyIncludesActiveAccounts()
    {
        await SeedAccounts();
        // Deactivate one account
        var cash = await _db.ChartOfAccounts.FindAsync(CashBankId);
        cash!.IsActive = false;
        await _db.SaveChangesAsync();
        await SeedPostedEntry(1_000_000, new DateTime(2026, 8, 15), CashBankId, LiabilityAccountId);

        var report = await _service.GetTrialBalanceAsync(_tenantId,
            new DateTime(2026, 8, 1), new DateTime(2026, 8, 31), false);

        // CashBank is inactive so its balance won't appear
        Assert.Equal(0, report.TotalDebit);
    }

    [Fact]
    public async Task GetTrialBalanceAsync_UsesCorrectBalanceSign_DebitNormal()
    {
        await SeedAccounts();
        // Asset (debit-normal): debit 10M, credit 2M → balance 8M
        await SeedPostedEntry(8_000_000, new DateTime(2026, 9, 15), CashBankId, LiabilityAccountId);
        // Manually add reversing entry: credit cash, debit liability
        var reversingEntry = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = new DateTime(2026, 9, 20),
            Description = "Reversing",
            Status = "POSTED",
            TotalAmount = 2_000_000,
            TenantId = _tenantId,
            CreatedBy = Guid.NewGuid()
        };
        _db.JournalEntries.Add(reversingEntry);
        await _db.SaveChangesAsync();
        _db.JournalEntryLines.AddRange(
            new JournalEntryLine { EntryId = reversingEntry.Id, AccountId = LiabilityAccountId, Debit = 2_000_000, Credit = 0 },
            new JournalEntryLine { EntryId = reversingEntry.Id, AccountId = CashBankId, Debit = 0, Credit = 2_000_000 }
        );
        await _db.SaveChangesAsync();

        var report = await _service.GetTrialBalanceAsync(_tenantId,
            new DateTime(2026, 9, 1), new DateTime(2026, 9, 30), false);

        var cashRow = FindRow(report.Rows, CashBankId);
        Assert.NotNull(cashRow);
        Assert.Equal(6_000_000, cashRow.Balance); // 8M debit - 2M credit
    }

    [Fact]
    public async Task GetTrialBalanceAsync_UsesCorrectBalanceSign_CreditNormal()
    {
        await SeedAccounts();
        // Liability (credit-normal): credit 5M, debit 1M → balance 4M
        var entry = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = new DateTime(2026, 10, 15),
            Description = "Liability test",
            Status = "POSTED",
            TotalAmount = 5_000_000,
            TenantId = _tenantId,
            CreatedBy = Guid.NewGuid()
        };
        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();
        _db.JournalEntryLines.AddRange(
            new JournalEntryLine { EntryId = entry.Id, AccountId = CashBankId, Debit = 5_000_000, Credit = 0 },
            new JournalEntryLine { EntryId = entry.Id, AccountId = LiabilityAccountId, Debit = 0, Credit = 5_000_000 }
        );
        await _db.SaveChangesAsync();

        // Partial repayment: debit liability, credit cash
        var repayEntry = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = new DateTime(2026, 10, 20),
            Description = "Repayment",
            Status = "POSTED",
            TotalAmount = 1_000_000,
            TenantId = _tenantId,
            CreatedBy = Guid.NewGuid()
        };
        _db.JournalEntries.Add(repayEntry);
        await _db.SaveChangesAsync();
        _db.JournalEntryLines.AddRange(
            new JournalEntryLine { EntryId = repayEntry.Id, AccountId = LiabilityAccountId, Debit = 1_000_000, Credit = 0 },
            new JournalEntryLine { EntryId = repayEntry.Id, AccountId = CashBankId, Debit = 0, Credit = 1_000_000 }
        );
        await _db.SaveChangesAsync();

        var report = await _service.GetTrialBalanceAsync(_tenantId,
            new DateTime(2026, 10, 1), new DateTime(2026, 10, 31), false);

        var liabilityRow = FindRow(report.Rows, LiabilityAccountId);
        Assert.NotNull(liabilityRow);
        Assert.Equal(4_000_000, liabilityRow.Balance); // 5M credit - 1M debit
    }

    [Fact]
    public async Task GetTrialBalanceAsync_NetIncomeIsNull()
    {
        await SeedAccounts();
        await SeedPostedEntry(1_000_000, new DateTime(2026, 11, 15));

        var report = await _service.GetTrialBalanceAsync(_tenantId,
            new DateTime(2026, 11, 1), new DateTime(2026, 11, 30), false);

        Assert.Null(report.NetIncome);
    }

    // ══════════════════════════════════════════════════════════════════
    //  PROFIT & LOSS
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetProfitLossAsync_OnlyIncludesRevenueAndExpense()
    {
        await SeedAccounts();
        // Revenue: credit 10M
        // Expense: debit 6M
        // Both are posted to Revenue and Expense accounts
        var revEntry = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = new DateTime(2026, 1, 15),
            Description = "Revenue entry",
            Status = "POSTED",
            TotalAmount = 10_000_000,
            TenantId = _tenantId,
            CreatedBy = Guid.NewGuid()
        };
        _db.JournalEntries.Add(revEntry);
        await _db.SaveChangesAsync();
        _db.JournalEntryLines.AddRange(
            new JournalEntryLine { EntryId = revEntry.Id, AccountId = CashBankId, Debit = 10_000_000, Credit = 0 },
            new JournalEntryLine { EntryId = revEntry.Id, AccountId = RevenueAccountId, Debit = 0, Credit = 10_000_000 }
        );

        var expEntry = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = new DateTime(2026, 1, 20),
            Description = "Expense entry",
            Status = "POSTED",
            TotalAmount = 6_000_000,
            TenantId = _tenantId,
            CreatedBy = Guid.NewGuid()
        };
        _db.JournalEntries.Add(expEntry);
        await _db.SaveChangesAsync();
        _db.JournalEntryLines.AddRange(
            new JournalEntryLine { EntryId = expEntry.Id, AccountId = ExpenseAccountId, Debit = 6_000_000, Credit = 0 },
            new JournalEntryLine { EntryId = expEntry.Id, AccountId = CashBankId, Debit = 0, Credit = 6_000_000 }
        );
        await _db.SaveChangesAsync();

        var report = await _service.GetProfitLossAsync(_tenantId,
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), false);

        Assert.Equal(2, report.Rows.Count); // Only Revenue + Expense
        Assert.Contains(report.Rows, r => r.Type == AccountTypes.Revenue);
        Assert.Contains(report.Rows, r => r.Type == AccountTypes.Expense);
    }

    [Fact]
    public async Task GetProfitLossAsync_ExcludesAssetLiabilityEquity()
    {
        await SeedAccounts();
        // Post to CashBank (ASSET) and Liability — these should NOT appear in P&L
        await SeedPostedEntry(1_000_000, new DateTime(2026, 2, 15));

        var report = await _service.GetProfitLossAsync(_tenantId,
            new DateTime(2026, 2, 1), new DateTime(2026, 2, 28), false);

        Assert.All(report.Rows, r => Assert.Contains(r.Type, new[] { AccountTypes.Revenue, AccountTypes.Expense }));
        Assert.DoesNotContain(report.Rows, r => r.Type == AccountTypes.Asset);
        Assert.DoesNotContain(report.Rows, r => r.Type == AccountTypes.Liability);
        Assert.DoesNotContain(report.Rows, r => r.Type == AccountTypes.Equity);
    }

    [Fact]
    public async Task GetProfitLossAsync_NetIncomeEqualsRevenueMinusExpenses()
    {
        await SeedAccounts();
        // Revenue 10M, Expense 6M → NI = 4M
        var revEntry = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = new DateTime(2026, 3, 15),
            Description = "Revenue",
            Status = "POSTED",
            TotalAmount = 10_000_000,
            TenantId = _tenantId,
            CreatedBy = Guid.NewGuid()
        };
        _db.JournalEntries.Add(revEntry);
        await _db.SaveChangesAsync();
        _db.JournalEntryLines.AddRange(
            new JournalEntryLine { EntryId = revEntry.Id, AccountId = CashBankId, Debit = 10_000_000, Credit = 0 },
            new JournalEntryLine { EntryId = revEntry.Id, AccountId = RevenueAccountId, Debit = 0, Credit = 10_000_000 }
        );

        var expEntry = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = new DateTime(2026, 3, 20),
            Description = "Expense",
            Status = "POSTED",
            TotalAmount = 6_000_000,
            TenantId = _tenantId,
            CreatedBy = Guid.NewGuid()
        };
        _db.JournalEntries.Add(expEntry);
        await _db.SaveChangesAsync();
        _db.JournalEntryLines.AddRange(
            new JournalEntryLine { EntryId = expEntry.Id, AccountId = ExpenseAccountId, Debit = 6_000_000, Credit = 0 },
            new JournalEntryLine { EntryId = expEntry.Id, AccountId = CashBankId, Debit = 0, Credit = 6_000_000 }
        );
        await _db.SaveChangesAsync();

        var report = await _service.GetProfitLossAsync(_tenantId,
            new DateTime(2026, 3, 1), new DateTime(2026, 3, 31), false);

        Assert.Equal(4_000_000, report.NetIncome);
    }

    [Fact]
    public async Task GetProfitLossAsync_ReturnsZeroNetIncomeWhenNoEntries()
    {
        await SeedAccounts();

        var report = await _service.GetProfitLossAsync(_tenantId,
            new DateTime(2026, 4, 1), new DateTime(2026, 4, 30), false);

        Assert.Equal(0, report.NetIncome);
    }

    [Fact]
    public async Task GetProfitLossAsync_NetIncomeNegativeWhenExpensesExceedRevenue()
    {
        await SeedAccounts();
        // Revenue 3M, Expense 5M → NI = -2M
        var revEntry = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = new DateTime(2026, 5, 15),
            Description = "Revenue",
            Status = "POSTED",
            TotalAmount = 3_000_000,
            TenantId = _tenantId,
            CreatedBy = Guid.NewGuid()
        };
        _db.JournalEntries.Add(revEntry);
        await _db.SaveChangesAsync();
        _db.JournalEntryLines.AddRange(
            new JournalEntryLine { EntryId = revEntry.Id, AccountId = CashBankId, Debit = 3_000_000, Credit = 0 },
            new JournalEntryLine { EntryId = revEntry.Id, AccountId = RevenueAccountId, Debit = 0, Credit = 3_000_000 }
        );

        var expEntry = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = new DateTime(2026, 5, 20),
            Description = "Expense",
            Status = "POSTED",
            TotalAmount = 5_000_000,
            TenantId = _tenantId,
            CreatedBy = Guid.NewGuid()
        };
        _db.JournalEntries.Add(expEntry);
        await _db.SaveChangesAsync();
        _db.JournalEntryLines.AddRange(
            new JournalEntryLine { EntryId = expEntry.Id, AccountId = ExpenseAccountId, Debit = 5_000_000, Credit = 0 },
            new JournalEntryLine { EntryId = expEntry.Id, AccountId = CashBankId, Debit = 0, Credit = 5_000_000 }
        );
        await _db.SaveChangesAsync();

        var report = await _service.GetProfitLossAsync(_tenantId,
            new DateTime(2026, 5, 1), new DateTime(2026, 5, 31), false);

        Assert.Equal(-2_000_000, report.NetIncome);
    }

    [Fact]
    public async Task GetProfitLossAsync_RevenueBalanceIsCreditNormal()
    {
        await SeedAccounts();
        // Revenue account: credit 10M, debit 2M → credit-normal → Balance = 8M
        var entry = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = new DateTime(2026, 6, 15),
            Description = "Revenue entry",
            Status = "POSTED",
            TotalAmount = 10_000_000,
            TenantId = _tenantId,
            CreatedBy = Guid.NewGuid()
        };
        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();
        _db.JournalEntryLines.AddRange(
            new JournalEntryLine { EntryId = entry.Id, AccountId = CashBankId, Debit = 10_000_000, Credit = 0 },
            new JournalEntryLine { EntryId = entry.Id, AccountId = RevenueAccountId, Debit = 0, Credit = 10_000_000 }
        );

        var revEntry2 = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = new DateTime(2026, 6, 20),
            Description = "Revenue reversal",
            Status = "POSTED",
            TotalAmount = 2_000_000,
            TenantId = _tenantId,
            CreatedBy = Guid.NewGuid()
        };
        _db.JournalEntries.Add(revEntry2);
        await _db.SaveChangesAsync();
        _db.JournalEntryLines.AddRange(
            new JournalEntryLine { EntryId = revEntry2.Id, AccountId = RevenueAccountId, Debit = 2_000_000, Credit = 0 },
            new JournalEntryLine { EntryId = revEntry2.Id, AccountId = CashBankId, Debit = 0, Credit = 2_000_000 }
        );
        await _db.SaveChangesAsync();

        var report = await _service.GetProfitLossAsync(_tenantId,
            new DateTime(2026, 6, 1), new DateTime(2026, 6, 30), false);

        var revenueRow = FindRow(report.Rows, RevenueAccountId);
        Assert.NotNull(revenueRow);
        Assert.Equal(8_000_000, revenueRow.Balance);
    }

    [Fact]
    public async Task GetProfitLossAsync_ExpenseBalanceIsDebitNormal()
    {
        await SeedAccounts();
        // Expense account: debit 7M, credit 1M → debit-normal → Balance = 6M
        var entry = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = new DateTime(2026, 7, 15),
            Description = "Expense entry",
            Status = "POSTED",
            TotalAmount = 7_000_000,
            TenantId = _tenantId,
            CreatedBy = Guid.NewGuid()
        };
        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();
        _db.JournalEntryLines.AddRange(
            new JournalEntryLine { EntryId = entry.Id, AccountId = ExpenseAccountId, Debit = 7_000_000, Credit = 0 },
            new JournalEntryLine { EntryId = entry.Id, AccountId = CashBankId, Debit = 0, Credit = 7_000_000 }
        );

        var expEntry2 = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = new DateTime(2026, 7, 20),
            Description = "Expense refund",
            Status = "POSTED",
            TotalAmount = 1_000_000,
            TenantId = _tenantId,
            CreatedBy = Guid.NewGuid()
        };
        _db.JournalEntries.Add(expEntry2);
        await _db.SaveChangesAsync();
        _db.JournalEntryLines.AddRange(
            new JournalEntryLine { EntryId = expEntry2.Id, AccountId = CashBankId, Debit = 1_000_000, Credit = 0 },
            new JournalEntryLine { EntryId = expEntry2.Id, AccountId = ExpenseAccountId, Debit = 0, Credit = 1_000_000 }
        );
        await _db.SaveChangesAsync();

        var report = await _service.GetProfitLossAsync(_tenantId,
            new DateTime(2026, 7, 1), new DateTime(2026, 7, 31), false);

        var expenseRow = FindRow(report.Rows, ExpenseAccountId);
        Assert.NotNull(expenseRow);
        Assert.Equal(6_000_000, expenseRow.Balance);
    }

    // ══════════════════════════════════════════════════════════════════
    //  BALANCE SHEET
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetBalanceSheetAsync_OnlyIncludesAssetLiabilityEquity()
    {
        await SeedAccounts();
        await SeedPostedEntry(1_000_000, new DateTime(2026, 1, 15));

        var report = await _service.GetBalanceSheetAsync(_tenantId,
            new DateTime(2026, 1, 31), false, null);

        Assert.All(report.Rows, r =>
            Assert.Contains(r.Type, new[] { AccountTypes.Asset, AccountTypes.Liability, AccountTypes.Equity }));
    }

    [Fact]
    public async Task GetBalanceSheetAsync_InjectsCurrentYearEarnings()
    {
        await SeedAccounts();
        await SeedPostedEntry(1_000_000, new DateTime(2026, 1, 15));

        var report = await _service.GetBalanceSheetAsync(_tenantId,
            new DateTime(2026, 1, 31), false, 500_000);

        var equityRow = FindRow(report.Rows, EquityAccountId);
        Assert.NotNull(equityRow);
        var cyeRow = equityRow.Children.FirstOrDefault(c => c.Code == "CYE");
        Assert.NotNull(cyeRow);
        Assert.Equal("Current Year Earnings", cyeRow.Name);
    }

    [Fact]
    public async Task GetBalanceSheetAsync_CurrentYearEarningsHasCorrectAmount()
    {
        await SeedAccounts();

        var report = await _service.GetBalanceSheetAsync(_tenantId,
            new DateTime(2026, 1, 31), false, 750_000);

        var equityRow = FindRow(report.Rows, EquityAccountId);
        var cyeRow = equityRow!.Children.First(c => c.Code == "CYE");
        Assert.Equal(750_000, cyeRow.Credit);
        Assert.Equal(0, cyeRow.Debit);
    }

    [Fact]
    public async Task GetBalanceSheetAsync_DoesNotInjectWhenNetIncomeNull()
    {
        await SeedAccounts();

        var report = await _service.GetBalanceSheetAsync(_tenantId,
            new DateTime(2026, 1, 31), false, null);

        var equityRow = FindRow(report.Rows, EquityAccountId);
        Assert.DoesNotContain(equityRow!.Children, c => c.Code == "CYE");
    }

    [Fact]
    public async Task GetBalanceSheetAsync_HandlesNegativeNetIncome()
    {
        await SeedAccounts();

        var report = await _service.GetBalanceSheetAsync(_tenantId,
            new DateTime(2026, 1, 31), false, -300_000);

        var equityRow = FindRow(report.Rows, EquityAccountId);
        var cyeRow = equityRow!.Children.First(c => c.Code == "CYE");
        Assert.Equal(300_000, cyeRow.Debit);
        Assert.Equal(0, cyeRow.Credit);
    }

    [Fact]
    public async Task GetBalanceSheetAsync_UsesAsOfDateSnapshot()
    {
        await SeedAccounts();
        await SeedPostedEntry(1_000_000, new DateTime(2026, 6, 15));
        await SeedPostedEntry(500_000, new DateTime(2026, 7, 15));

        var report = await _service.GetBalanceSheetAsync(_tenantId,
            new DateTime(2026, 6, 30), false, null);

        // Only the June transaction (1M) should be included
        Assert.Equal(1_000_000, report.TotalDebit);
    }

    [Fact]
    public async Task GetBalanceSheetAsync_DoesNotInjectWhenNoTopLevelEquity()
    {
        _db.ChartOfAccounts.AddRange(
            new ChartOfAccount { Id = Guid.NewGuid(), Code = "1000", Name = "Assets", Type = AccountTypes.Asset, IsActive = true, TenantId = _tenantId },
            new ChartOfAccount { Id = Guid.NewGuid(), Code = "2000", Name = "Liabilities", Type = AccountTypes.Liability, IsActive = true, TenantId = _tenantId }
        );
        await _db.SaveChangesAsync();

        var report = await _service.GetBalanceSheetAsync(_tenantId,
            new DateTime(2026, 1, 31), false, 500_000);

        Assert.DoesNotContain(report.Rows, r => r.Code == "CYE");
    }

    // ══════════════════════════════════════════════════════════════════
    //  LEDGER DRILL-DOWN
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAccountLedgerAsync_ReturnsLinesForSpecificAccount()
    {
        await SeedAccounts();
        await SeedPostedEntry(1_000_000, new DateTime(2026, 1, 15));

        var (rows, total) = await _service.GetAccountLedgerAsync(CashBankId, _tenantId,
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), false, 1, 20);

        Assert.Single(rows);
        Assert.Equal(1, total);
        Assert.All(rows, r => Assert.Equal(1_000_000, r.Debit));
    }

    [Fact]
    public async Task GetAccountLedgerAsync_RespectsPagination()
    {
        await SeedAccounts();
        for (int i = 0; i < 5; i++)
            await SeedPostedEntry(1_000_000, new DateTime(2026, 2, i + 1));

        var (rows, total) = await _service.GetAccountLedgerAsync(CashBankId, _tenantId,
            new DateTime(2026, 2, 1), new DateTime(2026, 2, 28), false, 1, 2);

        Assert.Equal(2, rows.Count);
        Assert.Equal(5, total);
    }

    [Fact]
    public async Task GetAccountLedgerAsync_FiltersByDateRange()
    {
        await SeedAccounts();
        await SeedPostedEntry(1_000_000, new DateTime(2026, 3, 15));
        await SeedPostedEntry(2_000_000, new DateTime(2026, 4, 15));

        var (rows, total) = await _service.GetAccountLedgerAsync(CashBankId, _tenantId,
            new DateTime(2026, 3, 1), new DateTime(2026, 3, 31), false, 1, 20);

        Assert.Single(rows);
        Assert.Equal(1, total);
        Assert.All(rows, r => Assert.True(r.TransactionDate >= new DateTime(2026, 3, 1)));
    }

    [Fact]
    public async Task GetAccountLedgerAsync_ExcludesDraftsWhenIncludeDraftsFalse()
    {
        await SeedAccounts();
        await SeedPostedEntry(1_000_000, new DateTime(2026, 4, 15));
        await SeedDraftEntry(500_000, new DateTime(2026, 4, 16));

        var (rows, total) = await _service.GetAccountLedgerAsync(CashBankId, _tenantId,
            new DateTime(2026, 4, 1), new DateTime(2026, 4, 30), false, 1, 20);

        Assert.Equal(1, total);
    }

    [Fact]
    public async Task GetAccountLedgerAsync_IncludesDraftsWhenIncludeDraftsTrue()
    {
        await SeedAccounts();
        await SeedPostedEntry(1_000_000, new DateTime(2026, 4, 15));
        await SeedDraftEntry(500_000, new DateTime(2026, 4, 16));

        var (rows, total) = await _service.GetAccountLedgerAsync(CashBankId, _tenantId,
            new DateTime(2026, 4, 1), new DateTime(2026, 4, 30), true, 1, 20);

        Assert.Equal(2, total);
    }

    [Fact]
    public async Task GetAccountLedgerAsync_ReturnsEmptyForNonExistentAccount()
    {
        await SeedAccounts();
        await SeedPostedEntry(1_000_000, new DateTime(2026, 5, 15));

        var (rows, total) = await _service.GetAccountLedgerAsync(Guid.NewGuid(), _tenantId,
            new DateTime(2026, 5, 1), new DateTime(2026, 5, 31), false, 1, 20);

        Assert.Empty(rows);
        Assert.Equal(0, total);
    }

    [Fact]
    public async Task GetAccountLedgerAsync_OrdersByTransactionDateDescending()
    {
        await SeedAccounts();
        await SeedPostedEntry(1_000_000, new DateTime(2026, 6, 10));
        await SeedPostedEntry(2_000_000, new DateTime(2026, 6, 20));

        var (rows, _) = await _service.GetAccountLedgerAsync(CashBankId, _tenantId,
            new DateTime(2026, 6, 1), new DateTime(2026, 6, 30), false, 1, 20);

        Assert.True(rows[0].TransactionDate >= rows[1].TransactionDate);
    }

    [Fact]
    public async Task GetAccountLedgerAsync_RespectsTenantIsolation()
    {
        await SeedAccounts();
        await SeedPostedEntry(1_000_000, new DateTime(2026, 7, 15));
        await SeedOtherTenantEntry(5_000_000, new DateTime(2026, 7, 15));

        var (rows, total) = await _service.GetAccountLedgerAsync(CashBankId, _tenantId,
            new DateTime(2026, 7, 1), new DateTime(2026, 7, 31), false, 1, 20);

        Assert.Equal(1, total);
    }

    [Fact]
    public async Task GetAccountLedgerAsync_ReturnsCorrectTotalCount()
    {
        await SeedAccounts();
        for (int i = 0; i < 7; i++)
            await SeedPostedEntry(1_000_000, new DateTime(2026, 8, i + 1));

        var (_, total) = await _service.GetAccountLedgerAsync(CashBankId, _tenantId,
            new DateTime(2026, 8, 1), new DateTime(2026, 8, 31), false, 1, 3);

        Assert.Equal(7, total);
    }

    [Fact]
    public async Task GetAccountLedgerAsync_ReturnsEmptyWhenNoEntries()
    {
        await SeedAccounts();

        var (rows, total) = await _service.GetAccountLedgerAsync(CashBankId, _tenantId,
            new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), false, 1, 20);

        Assert.Empty(rows);
        Assert.Equal(0, total);
    }

    // ══════════════════════════════════════════════════════════════════
    //  TREE BUILDING (internal behavior via public methods)
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task BuildTree_ExcludesAccountsWithInvalidParent()
    {
        await SeedAccounts();
        await SeedPostedEntry(1_000_000, new DateTime(2026, 9, 15));

        var report = await _service.GetTrialBalanceAsync(_tenantId,
            new DateTime(2026, 9, 1), new DateTime(2026, 9, 30), false);

        // Orphan (Code=9999) has ParentId pointing to non-existent parent so it's excluded
        Assert.DoesNotContain(report.Rows, r => r.Code == "9999");
    }

    [Fact]
    public async Task BuildTree_MultipleChildrenPerParent()
    {
        var parentId = Guid.NewGuid();
        var child1Id = Guid.NewGuid();
        var child2Id = Guid.NewGuid();

        _db.ChartOfAccounts.AddRange(
            new ChartOfAccount { Id = parentId, Code = "1000", Name = "Parent", Type = AccountTypes.Asset, IsActive = true, TenantId = _tenantId },
            new ChartOfAccount { Id = child1Id, Code = "1100", Name = "Child 1", Type = AccountTypes.Asset, IsActive = true, TenantId = _tenantId, ParentId = parentId },
            new ChartOfAccount { Id = child2Id, Code = "1200", Name = "Child 2", Type = AccountTypes.Asset, IsActive = true, TenantId = _tenantId, ParentId = parentId }
        );
        await _db.SaveChangesAsync();

        // Post entries to both children
        var entry1 = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = new DateTime(2026, 10, 15),
            Description = "Entry 1",
            Status = "POSTED",
            TotalAmount = 3_000_000,
            TenantId = _tenantId,
            CreatedBy = Guid.NewGuid()
        };
        _db.JournalEntries.Add(entry1);
        var entry2 = new JournalEntry
        {
            EntryNo = $"JE-{Guid.NewGuid():N}"[..16],
            TransactionDate = new DateTime(2026, 10, 20),
            Description = "Entry 2",
            Status = "POSTED",
            TotalAmount = 4_000_000,
            TenantId = _tenantId,
            CreatedBy = Guid.NewGuid()
        };
        _db.JournalEntries.Add(entry2);
        await _db.SaveChangesAsync();
        _db.JournalEntryLines.AddRange(
            new JournalEntryLine { EntryId = entry1.Id, AccountId = child1Id, Debit = 3_000_000, Credit = 0 },
            new JournalEntryLine { EntryId = entry1.Id, AccountId = parentId, Debit = 0, Credit = 3_000_000 },
            new JournalEntryLine { EntryId = entry2.Id, AccountId = child2Id, Debit = 4_000_000, Credit = 0 },
            new JournalEntryLine { EntryId = entry2.Id, AccountId = parentId, Debit = 0, Credit = 4_000_000 }
        );
        await _db.SaveChangesAsync();

        var report = await _service.GetTrialBalanceAsync(_tenantId,
            new DateTime(2026, 10, 1), new DateTime(2026, 10, 31), false);

        var parent = Assert.Single(report.Rows);
        Assert.Equal(2, parent.Children.Count);
        Assert.Equal(7_000_000, parent.Debit); // Aggregate of both children
        Assert.Equal(7_000_000, parent.Credit);
    }

    [Fact]
    public async Task BuildTree_EmptyChildrenWhenNoSubAccounts()
    {
        _db.ChartOfAccounts.Add(
            new ChartOfAccount { Id = Guid.NewGuid(), Code = "1000", Name = "Solo", Type = AccountTypes.Asset, IsActive = true, TenantId = _tenantId }
        );
        await _db.SaveChangesAsync();

        var report = await _service.GetTrialBalanceAsync(_tenantId,
            new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), false);

        var row = Assert.Single(report.Rows);
        Assert.Empty(row.Children);
    }

    // ─── Helper: Find row by ID in nested tree ──────────────────────

    private static ReportRow? FindRow(List<ReportRow> rows, Guid accountId)
    {
        foreach (var row in rows)
        {
            if (row.AccountId == accountId) return row;
            var found = FindRow(row.Children, accountId);
            if (found != null) return found;
        }
        return null;
    }
}
