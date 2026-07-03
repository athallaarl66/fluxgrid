using FluxGrid.Api.Shared.RBAC;

namespace FluxGrid.Api.Tests;

public class PermissionsTests
{
    [Fact]
    public void All_ContainsExpectedCount()
    {
        Assert.Equal(14, Permissions.All.Length);
    }

    [Fact]
    public void All_ContainsAllDefinedConstants()
    {
        var expected = new[]
        {
            Permissions.DashboardRead,
            Permissions.WmsRead, Permissions.WmsWrite, Permissions.WmsAdmin,
            Permissions.FinanceRead, Permissions.FinanceWrite, Permissions.FinanceAdmin,
            Permissions.HrRead, Permissions.HrWrite, Permissions.HrPayrollProcess,
            Permissions.TaskRead, Permissions.TaskWrite
        };

        foreach (var permission in expected)
        {
            Assert.Contains(permission, Permissions.All);
        }
    }

    [Fact]
    public void All_HasNoDuplicates()
    {
        Assert.Equal(Permissions.All.Length, Permissions.All.Distinct().Count());
    }
}
