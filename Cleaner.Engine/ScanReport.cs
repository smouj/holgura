using Cleaner.Contracts;

namespace Cleaner.Engine;

public sealed class ScanReport
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;

    public TimeSpan Duration { get; init; }

    public IReadOnlyList<ScanItem> Items { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public long TotalBytes => Items.Sum(item => item.TotalBytes);

    public long TotalFiles => Items.Sum(item => item.FileCount);
}
