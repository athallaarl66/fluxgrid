namespace FluxGrid.Api.Modules.Finance.Domain.Enums;

public static class AccountTypes
{
    public const string Asset = "ASSET";
    public const string Liability = "LIABILITY";
    public const string Equity = "EQUITY";
    public const string Revenue = "REVENUE";
    public const string Expense = "EXPENSE";

    public static readonly string[] All = [Asset, Liability, Equity, Revenue, Expense];

    public static bool IsValid(string type) => All.Contains(type);
}
