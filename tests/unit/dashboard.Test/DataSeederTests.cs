using FluxGrid.Api.Shared.Domain.Entities;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Seed;
using FluxGrid.Api.Shared.RBAC;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Tests;

public class DataSeederTests
{
    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task SeedAsync_CreatesAdminUser()
    {
        using var db = CreateDbContext();
        await DataSeeder.SeedAsync(db);

        var admin = await db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Username == "admin");

        Assert.NotNull(admin);
        Assert.Equal("admin@fluxgrid.com", admin.Email);
        Assert.True(admin.IsActive);
        Assert.True(BCrypt.Net.BCrypt.Verify("admin123", admin.PasswordHash));
    }

    [Fact]
    public async Task SeedAsync_CreatesThreeRoles()
    {
        using var db = CreateDbContext();
        await DataSeeder.SeedAsync(db);

        var roles = await db.Roles.ToListAsync();
        Assert.Equal(3, roles.Count);
        Assert.Contains(roles, r => r.Name == "Admin");
        Assert.Contains(roles, r => r.Name == "Manager");
        Assert.Contains(roles, r => r.Name == "Staff");
    }

    [Fact]
    public async Task SeedAsync_AdminRoleHasAllPermissions()
    {
        using var db = CreateDbContext();
        await DataSeeder.SeedAsync(db);

        var adminRole = await db.Roles.FirstAsync(r => r.Name == "Admin");
        Assert.Equal(Permissions.All.Length, adminRole.Permissions.Count);
        foreach (var permission in Permissions.All)
        {
            Assert.Contains(permission, adminRole.Permissions);
        }
    }

    [Fact]
    public async Task SeedAsync_ManagerRoleHasExpectedPermissions()
    {
        using var db = CreateDbContext();
        await DataSeeder.SeedAsync(db);

        var managerRole = await db.Roles.FirstAsync(r => r.Name == "Manager");
        Assert.Contains(Permissions.DashboardRead, managerRole.Permissions);
        Assert.Contains(Permissions.WmsRead, managerRole.Permissions);
        Assert.DoesNotContain(Permissions.WmsAdmin, managerRole.Permissions);
        Assert.DoesNotContain(Permissions.HrPayrollProcess, managerRole.Permissions);
    }

    [Fact]
    public async Task SeedAsync_StaffRoleHasLimitedPermissions()
    {
        using var db = CreateDbContext();
        await DataSeeder.SeedAsync(db);

        var staffRole = await db.Roles.FirstAsync(r => r.Name == "Staff");
        Assert.Contains(Permissions.DashboardRead, staffRole.Permissions);
        Assert.Contains(Permissions.WmsRead, staffRole.Permissions);
        Assert.DoesNotContain(Permissions.WmsWrite, staffRole.Permissions);
        Assert.DoesNotContain(Permissions.FinanceWrite, staffRole.Permissions);
        Assert.DoesNotContain(Permissions.HrWrite, staffRole.Permissions);
    }

    [Fact]
    public async Task SeedAsync_AdminUserHasAdminRole()
    {
        using var db = CreateDbContext();
        await DataSeeder.SeedAsync(db);

        var admin = await db.Users
            .Include(u => u.Roles)
            .FirstAsync(u => u.Username == "admin");

        Assert.Contains(admin.Roles, r => r.Name == "Admin");
    }

    [Fact]
    public async Task SeedAsync_Idempotent_DoesNotDuplicateOnSecondCall()
    {
        using var db = CreateDbContext();
        await DataSeeder.SeedAsync(db);
        await DataSeeder.SeedAsync(db);

        Assert.Equal(3, await db.Roles.CountAsync());
        Assert.Equal(1, await db.Users.CountAsync());
    }
}
