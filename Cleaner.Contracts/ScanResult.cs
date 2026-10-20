namespace Cleaner.Contracts;

public sealed class ScanResult
{
    public required string ModuleId { get; init; }

    public IReadOnlyList<ScanItem> Items { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public long TotalBytes => Items.Sum(item => item.TotalBytes);

    public long TotalFiles => Items.Sum(item => item.FileCount);
}
