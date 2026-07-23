namespace FluxGrid.Api.Modules.HR.Domain.Enums;

public static class ActivityAction
{
    public const string StatusChanged = "STATUS_CHANGED";
    public const string AssignedToJob = "ASSIGNED_TO_JOB";
    public const string RemovedFromJob = "REMOVED_FROM_JOB";
    public const string NoteAdded = "NOTE_ADDED";
    public const string CvUploaded = "CV_UPLOADED";
    public const string ParseCompleted = "PARSE_COMPLETED";
    public const string DataEdited = "DATA_EDITED";

    public static readonly string[] All =
    [
        StatusChanged, AssignedToJob, RemovedFromJob,
        NoteAdded, CvUploaded, ParseCompleted, DataEdited
    ];

    public static bool IsValid(string action) => All.Contains(action);
}
