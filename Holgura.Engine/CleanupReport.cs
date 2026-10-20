using Holgura.Core;
using Holgura.Contracts;

namespace Holgura.Engine;

public sealed class CleanupReport
{
    public bool DryRun { get; init; }

    public TimeSpan Duration { get; init; }

    public IReadOnlyList<CleanupResult> Results { get; init; } = [];

    public long BytesFreed => Results.Sum(result => result.BytesFreed);

    public int FilesDeleted => Results.Sum(result => result.Deleted);

    public int FilesSkipped => Results.Sum(result => result.Skipped);

    public int Errors => Results.Sum(result => result.Items.Count(i =>
        i.Outcome is CleanupOutcome.Failed or CleanupOutcome.SkippedAccessDenied));

    public HistoryEntry ToHistoryEntry(IReadOnlyDictionary<string, string> titles) => new()
    {
        TimestampUtc = DateTimeOffset.UtcNow,
        DryRun = DryRun,
        Duration = Duration,
        BytesFreed = BytesFreed,
        FilesDeleted = FilesDeleted,
        FilesSkipped = FilesSkipped,
        Errors = Errors,
        Modules = Results.Select(result => new HistoryModuleEntry
        {
            ModuleId = result.ModuleId,
            ItemId = result.ItemId,
            Title = titles.TryGetValue(result.ItemId, out var title) ? title : result.ItemId,
            BytesFreed = result.BytesFreed,
            Deleted = result.Deleted,
            Skipped = result.Skipped,
        }).ToArray(),
    };
}
