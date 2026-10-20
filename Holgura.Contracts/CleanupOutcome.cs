namespace Holgura.Contracts;

public enum CleanupOutcome
{
    Deleted = 0,
    WouldDelete = 1,
    SkippedLocked = 2,
    SkippedAccessDenied = 3,
    Excluded = 4,
    ChangedSinceScan = 5,
    NotFound = 6,
    Failed = 7,
}
