namespace FluxGrid.Api.Modules.Finance.Domain.Enums;

public static class PeriodStatus
{
    public const string Open = "OPEN";
    public const string Closed = "CLOSED";

    public static readonly string[] All = [Open, Closed];

    public static bool IsValid(string status) => All.Contains(status);
}
