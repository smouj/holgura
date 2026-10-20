namespace Cleaner.Contracts;

public sealed record CleanupItemResult(string Path, CleanupOutcome Outcome, string? Message = null);
