namespace FluxGrid.Api.Modules.Finance.API;

public sealed record ReportRow(
    Guid AccountId,
    string Code,
    string Name,
    string Type,
    int Depth,
    decimal Debit,
    decimal Credit,
    decimal Balance,
    List<ReportRow> Children
);

public sealed record ReportResponse(
    List<ReportRow> Rows,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal? NetIncome
);

public sealed record LedgerDetailRow(
    Guid EntryId,
    string EntryNo,
    DateTime TransactionDate,
    string Description,
    decimal Debit,
    decimal Credit,
    DateTime CreatedAt
);
