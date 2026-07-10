namespace FluxGrid.Api.Modules.HR.Domain.Enums;

public static class CandidateStatus
{
    public const string Draft = "DRAFT";
    public const string Parsed = "PARSED";
    public const string ParseFailed = "PARSE_FAILED";
    public const string Active = "ACTIVE";
    public const string Interview = "INTERVIEW";
    public const string Hired = "HIRED";
    public const string Rejected = "REJECTED";
    public const string Archived = "ARCHIVED";

    public static readonly string[] All = [Draft, Parsed, ParseFailed, Active, Interview, Hired, Rejected, Archived];

    public static bool IsValid(string status) => All.Contains(status);
}
