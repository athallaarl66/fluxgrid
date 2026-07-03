using FluxGrid.Api.Modules.Dashboard.Application;

namespace FluxGrid.Api.Tests;

public class DashboardServiceTests
{
    private readonly DashboardService _sut = new();

    [Fact]
    public async Task GetModulesAsync_ReturnsFourModules()
    {
        var modules = await _sut.GetModulesAsync();
        Assert.Equal(4, modules.Length);
    }

    [Fact]
    public async Task GetModulesAsync_AllModulesHaveRequiredFields()
    {
        var modules = await _sut.GetModulesAsync();

        foreach (var m in modules)
        {
            Assert.False(string.IsNullOrWhiteSpace(m.Name));
            Assert.False(string.IsNullOrWhiteSpace(m.Path));
            Assert.False(string.IsNullOrWhiteSpace(m.Description));
            Assert.False(string.IsNullOrWhiteSpace(m.Icon));
            Assert.False(string.IsNullOrWhiteSpace(m.Metric));
        }
    }

    [Theory]
    [InlineData("WMS", "/wms")]
    [InlineData("Finance", "/finance")]
    [InlineData("HR", "/hr")]
    [InlineData("Projects", "/projects")]
    public async Task GetModulesAsync_ModuleHasCorrectPath(string name, string expectedPath)
    {
        var modules = await _sut.GetModulesAsync();
        var module = modules.FirstOrDefault(m => m.Name == name);
        Assert.NotNull(module);
        Assert.Equal(expectedPath, module.Path);
    }

    [Theory]
    [InlineData("WMS", "package")]
    [InlineData("Finance", "wallet")]
    [InlineData("HR", "users")]
    [InlineData("Projects", "clipboard")]
    public async Task GetModulesAsync_ModuleHasCorrectIcon(string name, string expectedIcon)
    {
        var modules = await _sut.GetModulesAsync();
        var module = modules.FirstOrDefault(m => m.Name == name);
        Assert.NotNull(module);
        Assert.Equal(expectedIcon, module.Icon);
    }
}
