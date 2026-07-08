namespace FluxGrid.Api.Modules.Finance.API;

public sealed record DashboardResponse(
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal TotalEquity,
    decimal RevenueMtd,
    decimal ExpensesMtd,
    decimal NetIncomeMtd,
    int JournalEntryCount,
    Guid PeriodId,
    List<RecentEntryRow> RecentEntries,
    List<MonthlyTrendRow> MonthlyTrend
);

public sealed record RecentEntryRow(
    Guid Id,
    string EntryNo,
    string Description,
    DateTime TransactionDate,
    decimal TotalDebit,
    decimal TotalCredit,
    string Status
);

public sealed record MonthlyTrendRow(
    int Month,
    decimal Revenue,
    decimal Expenses
);
