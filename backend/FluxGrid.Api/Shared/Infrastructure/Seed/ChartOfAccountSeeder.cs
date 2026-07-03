using FluxGrid.Api.Modules.Finance.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Shared.Infrastructure.Seed;

public static class ChartOfAccountSeeder
{
    private static readonly (string code, string name, string type, string? parentCode)[] Template = [
        // Assets (1000)
        ("1000", "Assets", "ASSET", null),
        ("1100", "Current Assets", "ASSET", "1000"),
        ("1110", "Cash in Bank", "ASSET", "1100"),
        ("1120", "Accounts Receivable", "ASSET", "1100"),
        ("1130", "Inventory", "ASSET", "1100"),
        ("1140", "Prepaid Expenses", "ASSET", "1100"),
        ("1200", "Fixed Assets", "ASSET", "1000"),
        ("1210", "Land", "ASSET", "1200"),
        ("1220", "Buildings", "ASSET", "1200"),
        ("1230", "Machinery & Equipment", "ASSET", "1200"),
        ("1240", "Accumulated Depreciation", "ASSET", "1200"),

        // Liabilities (2000)
        ("2000", "Liabilities", "LIABILITY", null),
        ("2100", "Current Liabilities", "LIABILITY", "2000"),
        ("2110", "Accounts Payable", "LIABILITY", "2100"),
        ("2120", "Accrued Expenses", "LIABILITY", "2100"),
        ("2130", "Short-term Debt", "LIABILITY", "2100"),
        ("2200", "Long-term Liabilities", "LIABILITY", "2000"),
        ("2210", "Long-term Debt", "LIABILITY", "2200"),
        ("2220", "Deferred Tax Liabilities", "LIABILITY", "2200"),

        // Equity (3000)
        ("3000", "Equity", "EQUITY", null),
        ("3100", "Share Capital", "EQUITY", "3000"),
        ("3200", "Retained Earnings", "EQUITY", "3000"),
        ("3300", "Current Year Earnings", "EQUITY", "3000"),

        // Revenue (4000)
        ("4000", "Revenue", "REVENUE", null),
        ("4100", "Sales Revenue", "REVENUE", "4000"),
        ("4110", "Product Sales", "REVENUE", "4100"),
        ("4120", "Service Revenue", "REVENUE", "4100"),
        ("4200", "Other Income", "REVENUE", "4000"),

        // Expenses (5000)
        ("5000", "Expenses", "EXPENSE", null),
        ("5100", "Cost of Goods Sold", "EXPENSE", "5000"),
        ("5200", "Operating Expenses", "EXPENSE", "5000"),
        ("5210", "Salaries Expense", "EXPENSE", "5200"),
        ("5220", "Rent Expense", "EXPENSE", "5200"),
        ("5230", "Utilities Expense", "EXPENSE", "5200"),
        ("5240", "Depreciation Expense", "EXPENSE", "5200"),
        ("5300", "Other Expenses", "EXPENSE", "5000"),
    ];

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
        var codeToId = Template.ToDictionary(t => t.code, _ => Guid.NewGuid());
        var codeToParentId = Template.ToDictionary(t => t.code, t => t.parentCode);

        return Template.Select(t => new ChartOfAccount
        {
            Id = codeToId[t.code],
            Code = t.code,
            Name = t.name,
            Type = t.type,
            ParentId = t.parentCode != null ? codeToId[t.parentCode] : null,
            IsActive = true,
            TenantId = tenantId,
        }).ToList();
    }
}
