namespace Holgura.Contracts;

public sealed class CleanupContext
{
    public required ScanItem Item { get; init; }

    public required IFileSystem FileSystem { get; init; }

    public required ICleanupGate Gate { get; init; }

    /// <summary>When true nothing is deleted; each candidate is reported as WouldDelete.</summary>
    public bool DryRun { get; init; }

    /// <summary>Timestamp of the scan that produced <see cref="Item"/>.</summary>
    public DateTimeOffset ScanTimestampUtc { get; init; }

    /// <summary>
    /// Null means the user selected the whole category. A dictionary means the user selected only
    /// these scanned files; each file is revalidated against its scanned size and timestamp.
    /// </summary>
    public IReadOnlyDictionary<string, FileEntry>? SelectedFileDetails { get; init; }

    public int MaxParallelism { get; init; } = 4;
}
