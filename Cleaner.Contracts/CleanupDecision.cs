namespace Cleaner.Contracts;

public enum CleanupDecision
{
    Allow = 0,
    Excluded = 1,
    OutsideAllowedRoot = 2,
    ProtectedPath = 3,
    ReparsePoint = 4,
    NotFound = 5,
}
