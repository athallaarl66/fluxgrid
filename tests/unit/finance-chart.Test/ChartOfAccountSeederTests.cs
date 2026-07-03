using FluxGrid.Api.Modules.Finance.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Tests.Finance;

public class ChartOfAccountSeederTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task SeedAsync_Creates33Accounts()
    {
        using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();

        await ChartOfAccountSeeder.SeedAsync(db, tenantId);

        var count = await db.ChartOfAccounts.CountAsync(a => a.TenantId == tenantId);
        Assert.Equal(36, count);
    }

    [Fact]
    public async Task SeedAsync_Creates5TopLevelAccounts()
    {
        using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();

        await ChartOfAccountSeeder.SeedAsync(db, tenantId);

        var topLevel = await db.ChartOfAccounts
            .Where(a => a.TenantId == tenantId && a.ParentId == null)
            .ToListAsync();

        Assert.Equal(5, topLevel.Count);
        Assert.Contains(topLevel, a => a.Code == "1000" && a.Type == "ASSET");
        Assert.Contains(topLevel, a => a.Code == "2000" && a.Type == "LIABILITY");
        Assert.Contains(topLevel, a => a.Code == "3000" && a.Type == "EQUITY");
        Assert.Contains(topLevel, a => a.Code == "4000" && a.Type == "REVENUE");
        Assert.Contains(topLevel, a => a.Code == "5000" && a.Type == "EXPENSE");
    }

    [Fact]
    public async Task SeedAsync_AllAccountsAreActive()
    {
        using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();

        await ChartOfAccountSeeder.SeedAsync(db, tenantId);

        var inactive = await db.ChartOfAccounts
            .CountAsync(a => a.TenantId == tenantId && !a.IsActive);

        Assert.Equal(0, inactive);
    }

    [Fact]
    public async Task SeedAsync_ChildrenTypeMatchesParent()
    {
        using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();

        await ChartOfAccountSeeder.SeedAsync(db, tenantId);

        var children = await db.ChartOfAccounts
            .Where(a => a.TenantId == tenantId && a.ParentId != null)
            .Include(a => a.Parent)
            .ToListAsync();

        foreach (var child in children)
        {
            Assert.Equal(child.Parent!.Type, child.Type);
        }
    }

    [Fact]
    public async Task SeedAsync_HasCorrectHierarchy()
    {
        using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();

        await ChartOfAccountSeeder.SeedAsync(db, tenantId);

        var cashInBank = await db.ChartOfAccounts
            .FirstAsync(a => a.TenantId == tenantId && a.Code == "1110");

        var currentAssets = await db.ChartOfAccounts
            .FirstAsync(a => a.TenantId == tenantId && a.Code == "1100");

        var assets = await db.ChartOfAccounts
            .FirstAsync(a => a.TenantId == tenantId && a.Code == "1000");

        Assert.Equal(currentAssets.Id, cashInBank.ParentId);
        Assert.Equal(assets.Id, currentAssets.ParentId);
        Assert.Null(assets.ParentId);
    }

    [Fact]
    public async Task SeedAsync_Idempotent_DoesNotDuplicate()
    {
        using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();

        await ChartOfAccountSeeder.SeedAsync(db, tenantId);
        await ChartOfAccountSeeder.SeedAsync(db, tenantId);

        var count = await db.ChartOfAccounts.CountAsync(a => a.TenantId == tenantId);
        Assert.Equal(36, count);
    }

    [Fact]
    public async Task SeedAsync_MultipleTenants_AreIsolated()
    {
        using var db = CreateDbContext();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await ChartOfAccountSeeder.SeedAsync(db, tenantA);
        await ChartOfAccountSeeder.SeedAsync(db, tenantB);

        Assert.Equal(36, await db.ChartOfAccounts.CountAsync(a => a.TenantId == tenantA));
        Assert.Equal(36, await db.ChartOfAccounts.CountAsync(a => a.TenantId == tenantB));
    }

    [Fact]
    public void GetTemplate_Returns33Accounts()
    {
        var accounts = ChartOfAccountSeeder.GetTemplate(Guid.NewGuid());
        Assert.Equal(36, accounts.Count);
    }
}
