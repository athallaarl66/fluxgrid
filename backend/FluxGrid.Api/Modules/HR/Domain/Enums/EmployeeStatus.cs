namespace FluxGrid.Api.Modules.HR.Domain.Enums;

public static class EmployeeStatus
{
    public const string Active = "ACTIVE";
    public const string OnLeave = "ON_LEAVE";
    public const string Terminated = "TERMINATED";

    public static readonly string[] All = [Active, OnLeave, Terminated];

    public static bool IsValid(string status) => All.Contains(status);
}
