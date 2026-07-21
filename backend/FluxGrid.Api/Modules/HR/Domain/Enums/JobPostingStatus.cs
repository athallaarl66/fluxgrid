namespace FluxGrid.Api.Modules.HR.Domain.Enums;

public static class JobPostingStatus
{
    public const string Draft = "DRAFT";
    public const string Published = "PUBLISHED";
    public const string Closed = "CLOSED";

    public static readonly string[] All = [Draft, Published, Closed];

    public static bool IsValid(string status) => All.Contains(status);
}
