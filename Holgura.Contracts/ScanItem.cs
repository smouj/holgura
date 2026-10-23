namespace Holgura.Contracts;

/// <summary>
/// One selectable group of disposable files, produced by a module during a scan.
/// A group is a rule over a root directory; the file list is only sampled, never fully materialized.
/// </summary>
public sealed class ScanItem
{
    public required string Id { get; init; }

    public required string ModuleId { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public required ModuleCategory Category { get; init; }

    public required SafetyLevel SafetyLevel { get; init; }

    /// <summary>Directory the items live under. Every deleted file must stay inside it.</summary>
    public required string RootPath { get; init; }

    /// <summary>Whether scanning and cleanup recurse into child directories.</summary>
    public bool Recursive { get; init; } = true;

    /// <summary>Minimum age policy applied when the item was scanned.</summary>
    public TimeSpan? MinimumAge { get; init; }

    public long FileCount { get; init; }

    public long TotalBytes { get; init; }

    public DateTimeOffset? LastWriteUtc { get; init; }

    /// <summary>A capped preview used by the "show files" panel.</summary>
    public IReadOnlyList<string> SampleFiles { get; init; } = [];

    /// <summary>The largest matching files retained for a detailed, selectable preview.</summary>
    public IReadOnlyList<FileEntry> FileDetails { get; init; } = [];

    /// <summary>True when the detailed preview contains only the largest matching files.</summary>
    public bool FileDetailsTruncated { get; init; }

    public bool SelectedByDefault { get; init; }

    public DeletionScope DeletionScope { get; init; } = DeletionScope.FileSystem;

    public bool HasFiles => FileCount > 0;
}
