namespace Holgura.Core;

public sealed class HistoryEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;

    public bool DryRun { get; init; }

    public TimeSpan Duration { get; init; }

    public long BytesFreed { get; init; }

    public int FilesDeleted { get; init; }

    public int FilesSkipped { get; init; }

    public int Errors { get; init; }

    public IReadOnlyList<HistoryModuleEntry> Modules { get; init; } = [];
}

public sealed class HistoryModuleEntry
{
    public required string ModuleId { get; init; }

    public required string ItemId { get; init; }

    public required string Title { get; init; }

    public long BytesFreed { get; init; }

    public int Deleted { get; init; }

    public int Skipped { get; init; }
}
