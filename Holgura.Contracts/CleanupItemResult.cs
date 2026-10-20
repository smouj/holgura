namespace Holgura.Contracts;

public sealed record CleanupItemResult(string Path, CleanupOutcome Outcome, string? Message = null);
