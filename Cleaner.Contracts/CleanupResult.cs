namespace Cleaner.Contracts;

public sealed class CleanupResult
{
    public required string ModuleId { get; init; }

    public required string ItemId { get; init; }

    public IReadOnlyList<CleanupItemResult> Items { get; init; } = [];

    public TimeSpan Duration { get; init; }

    public bool DryRun { get; init; }

    public long BytesFreed { get; init; }

    public int Deleted => Items.Count(i => i.Outcome == CleanupOutcome.Deleted);

    public int WouldDelete => Items.Count(i => i.Outcome == CleanupOutcome.WouldDelete);

    public int Skipped => Items.Count(i => i.Outcome is not (CleanupOutcome.Deleted or CleanupOutcome.WouldDelete));
}
