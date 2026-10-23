using Holgura.Engine;
using Holgura.Contracts;

namespace Holgura.Engine.Tests;

/// <summary>A module over a fake root; the only difference from a real one is the root path.</summary>
public sealed class SyntheticCleaner(
    string root,
    TimeSpan? minimumAge = null,
    string? includeSuffix = null,
    bool recursive = true) : DirectoryCleanerModule
{
    public override string Id => "test.synthetic";

    public override string DisplayName => "Synthetic cleaner";

    protected override IEnumerable<ScanRoot> GetRoots(ScanContext context)
    {
        yield return new ScanRoot(root, "Fake root", "Test root", SafetyLevel.Safe, true, minimumAge, recursive);
    }

    protected override bool Include(FileEntry file, ScanRoot root, DateTimeOffset asOfUtc) =>
        base.Include(file, root, asOfUtc) &&
        (includeSuffix is null || file.FullName.EndsWith(includeSuffix, StringComparison.OrdinalIgnoreCase));
}
