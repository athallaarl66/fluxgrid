using FluxGrid.Api.Modules.Finance.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Shared.Infrastructure.Seed;

public static class ChartOfAccountSeeder
{
    public static async Task SeedAsync(AppDbContext db, Guid tenantId)
    {
        if (await db.ChartOfAccounts.AnyAsync(a => a.TenantId == tenantId))
            return;

        var accounts = GetTemplate(tenantId);
        db.ChartOfAccounts.AddRange(accounts);
        await db.SaveChangesAsync();
    }

    public static List<ChartOfAccount> GetTemplate(Guid tenantId)
    {
        var assets = new ChartOfAccount { Id = Guid.NewGuid(), Code = "1000", Name = "Assets", Type = "ASSET", IsActive = true, TenantId = tenantId };
        var liabilities = new ChartOfAccount { Id = Guid.NewGuid(), Code = "2000", Name = "Liabilities", Type = "LIABILITY", IsActive = true, TenantId = tenantId };
        var equity = new ChartOfAccount { Id = Guid.NewGuid(), Code = "3000", Name = "Equity", Type = "EQUITY", IsActive = true, TenantId = tenantId };
        var revenue = new ChartOfAccount { Id = Guid.NewGuid(), Code = "4000", Name = "Revenue", Type = "REVENUE", IsActive = true, TenantId = tenantId };
        var expenses = new ChartOfAccount { Id = Guid.NewGuid(), Code = "5000", Name = "Expenses", Type = "EXPENSE", IsActive = true, TenantId = tenantId };

        var currentAssets = NewChild("1100", "Current Assets", assets, tenantId);
        var cashInBank = NewChild("1110", "Cash in Bank", currentAssets, tenantId);
        var accountsReceivable = NewChild("1120", "Accounts Receivable", currentAssets, tenantId);
        var inventory = NewChild("1130", "Inventory", currentAssets, tenantId);
        var prepaidExpenses = NewChild("1140", "Prepaid Expenses", currentAssets, tenantId);

        var fixedAssets = NewChild("1200", "Fixed Assets", assets, tenantId);
        var land = NewChild("1210", "Land", fixedAssets, tenantId);
        var buildings = NewChild("1220", "Buildings", fixedAssets, tenantId);
        var machinery = NewChild("1230", "Machinery & Equipment", fixedAssets, tenantId);
        var accDepreciation = NewChild("1240", "Accumulated Depreciation", fixedAssets, tenantId);

        var currentLiabilities = NewChild("2100", "Current Liabilities", liabilities, tenantId);
        var accountsPayable = NewChild("2110", "Accounts Payable", currentLiabilities, tenantId);
        var accruedExpenses = NewChild("2120", "Accrued Expenses", currentLiabilities, tenantId);
        var shortTermDebt = NewChild("2130", "Short-term Debt", currentLiabilities, tenantId);

        var longTermLiabilities = NewChild("2200", "Long-term Liabilities", liabilities, tenantId);
        var longTermDebt = NewChild("2210", "Long-term Debt", longTermLiabilities, tenantId);
        var deferredTax = NewChild("2220", "Deferred Tax Liabilities", longTermLiabilities, tenantId);

        var shareCapital = NewChild("3100", "Share Capital", equity, tenantId);
        var retainedEarnings = NewChild("3200", "Retained Earnings", equity, tenantId);
        var currentYearEarnings = NewChild("3300", "Current Year Earnings", equity, tenantId);

        var salesRevenue = NewChild("4100", "Sales Revenue", revenue, tenantId);
        var productSales = NewChild("4110", "Product Sales", salesRevenue, tenantId);
        var serviceRevenue = NewChild("4120", "Service Revenue", salesRevenue, tenantId);
        var otherIncome = NewChild("4200", "Other Income", revenue, tenantId);

        var cogs = NewChild("5100", "Cost of Goods Sold", expenses, tenantId);
        var operatingExpenses = NewChild("5200", "Operating Expenses", expenses, tenantId);
        var salaries = NewChild("5210", "Salaries Expense", operatingExpenses, tenantId);
        var rent = NewChild("5220", "Rent Expense", operatingExpenses, tenantId);
        var utilities = NewChild("5230", "Utilities Expense", operatingExpenses, tenantId);
        var depreciation = NewChild("5240", "Depreciation Expense", operatingExpenses, tenantId);
        var otherExpenses = NewChild("5300", "Other Expenses", expenses, tenantId);

        return
        [
            assets, liabilities, equity, revenue, expenses,
            currentAssets, cashInBank, accountsReceivable, inventory, prepaidExpenses,
            fixedAssets, land, buildings, machinery, accDepreciation,
            currentLiabilities, accountsPayable, accruedExpenses, shortTermDebt,
            longTermLiabilities, longTermDebt, deferredTax,
            shareCapital, retainedEarnings, currentYearEarnings,
            salesRevenue, productSales, serviceRevenue, otherIncome,
            cogs, operatingExpenses, salaries, rent, utilities, depreciation, otherExpenses
        ];
    }

    private static ChartOfAccount NewChild(string code, string name, ChartOfAccount parent, Guid tenantId)
    {
        return new ChartOfAccount
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            ParentId = parent.Id,
            Type = parent.Type,
            IsActive = true,
            TenantId = tenantId
        };
    }
}
