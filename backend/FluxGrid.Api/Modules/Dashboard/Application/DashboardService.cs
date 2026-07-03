namespace FluxGrid.Api.Modules.Dashboard.Application;

public class DashboardService
{
    public Task<ModuleInfo[]> GetModulesAsync()
    {
        var modules = new[]
        {
            new ModuleInfo("WMS", "/wms", "Warehouse Management System — inventory, picking, shipping", "package", "1,234"),
            new ModuleInfo("Finance", "/finance", "Financial management — invoices, budgets, reporting", "wallet", "$892K"),
            new ModuleInfo("HR", "/hr", "Human Resources — payroll, attendance, employee records", "users", "156"),
            new ModuleInfo("Projects", "/projects", "Task & Project management — timelines, milestones, tasks", "clipboard", "23"),
        };

        return Task.FromResult(modules);
    }
}

public record ModuleInfo(string Name, string Path, string Description, string Icon, string Metric);
