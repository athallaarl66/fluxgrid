namespace FluxGrid.Api.Shared.RBAC;

public static class Permissions
{
    public const string DashboardRead = "Dashboard:Read";

    public const string WmsRead = "WMS:Read";
    public const string WmsWrite = "WMS:Write";
    public const string WmsAdmin = "WMS:Admin";
    public const string WmsInboundCreate = "wms.inbound.create";
    public const string WmsInboundApprove = "wms.inbound.approve";
    public const string WmsOutboundProcess = "wms.outbound.process";

    public const string FinanceRead = "Finance:Read";
    public const string FinanceWrite = "Finance:Write";
    public const string FinanceAdmin = "Finance:Admin";
    public const string FinanceCoaRead = "finance.coa.read";
    public const string FinanceCoaManage = "finance.coa.manage";
    public const string FinanceJournalView = "finance.journal.view";
    public const string FinanceJournalCreate = "finance.journal.create";
    public const string FinanceJournalApprove = "finance.journal.approve";
    public const string FinancePeriodRead = "finance.period.read";
    public const string FinancePeriodAdmin = "finance.period.admin";
    public const string FinanceBudgetRead = "finance.budget.read";
    public const string FinanceBudgetManage = "finance.budget.manage";
    public const string FinanceReportRead = "finance.report.read";

    public const string HrRead = "HR:Read";
    public const string HrWrite = "HR:Write";
    public const string HrPayrollProcess = "HR:PayrollProcess";

    public const string TaskRead = "Task:Read";
    public const string TaskWrite = "Task:Write";

    public static readonly string[] All = [
        DashboardRead,
        WmsRead, WmsWrite, WmsAdmin, WmsInboundCreate, WmsInboundApprove, WmsOutboundProcess,
        FinanceRead, FinanceWrite, FinanceAdmin, FinanceCoaRead, FinanceCoaManage,
        FinanceJournalView, FinanceJournalCreate, FinanceJournalApprove,
        FinancePeriodRead, FinancePeriodAdmin, FinanceBudgetRead, FinanceBudgetManage, FinanceReportRead,
        HrRead, HrWrite, HrPayrollProcess,
        TaskRead, TaskWrite
    ];
}
