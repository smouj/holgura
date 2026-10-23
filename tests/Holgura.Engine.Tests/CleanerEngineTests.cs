using Holgura.Contracts;
using Holgura.Core;
using Xunit;

namespace Holgura.Engine.Tests;

public sealed class CleanerEngineTests
{
    private const string Root = "C:\\Users\\Tester\\AppData\\Local\\Temp";

    private static (CleanerEngine Engine, InMemoryFileSystem Fs, FakeHistoryStore History) Create(
        ExclusionService? exclusions = null,
        params ICleanerModule[] modules)
    {
        var fs = new InMemoryFileSystem();
        fs.AddDirectory(Root);
        var history = new FakeHistoryStore();
        ICleanerModule[] list = modules.Length > 0 ? modules : [new SyntheticCleaner(Root)];
        var engine = new CleanerEngine(
            list,
            fs,
            new PathSafetyValidator(["C:\\Users", "C:\\Users\\Tester\\Documents"]),
            exclusions ?? new ExclusionService(),
            history);
        return (engine, fs, history);
    }

    private static ScanContext Context(InMemoryFileSystem fs) => new() { FileSystem = fs };

    [Fact]
    public async Task Scan_reports_size_and_count()
    {
        var (engine, fs, _) = Create();
        fs.AddFile($"{Root}\\a.tmp", 1000);
        fs.AddFile($"{Root}\\sub\\b.tmp", 2000);

        var report = await engine.ScanAsync(Context(fs));

        var item = Assert.Single(report.Items);
        Assert.Equal(2, item.FileCount);
        Assert.Equal(3000, item.TotalBytes);
    }

    [Fact]
    public async Task Scan_keeps_a_bounded_file_preview_ordered_by_size()
    {
        var (engine, fs, _) = Create();
        fs.AddFile($"{Root}\\small.tmp", 100);
        fs.AddFile($"{Root}\\large.tmp", 900);
        fs.AddFile($"{Root}\\medium.tmp", 500);

        var report = await engine.ScanAsync(new ScanContext { FileSystem = fs, MaxFileDetails = 2 });

        var item = Assert.Single(report.Items);
        Assert.Equal(3, item.FileCount);
        Assert.Equal(2, item.FileDetails.Count);
        Assert.True(item.FileDetailsTruncated);
        Assert.Equal([900L, 500L], item.FileDetails.Select(file => file.Length));
    }

    [Fact]
    public async Task Cleanup_deletes_only_individually_selected_files()
    {
        var (engine, fs, _) = Create();
        fs.AddFile($"{Root}\\first.tmp", 100);
        fs.AddFile($"{Root}\\second.tmp", 200);
        var scan = await engine.ScanAsync(Context(fs));
        var item = Assert.Single(scan.Items);
        var selected = item.FileDetails.Single(file => file.FullName.EndsWith("first.tmp", StringComparison.OrdinalIgnoreCase));
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, FileEntry>> selectedFiles =
            new Dictionary<string, IReadOnlyDictionary<string, FileEntry>>(StringComparer.OrdinalIgnoreCase)
            {
                [item.Id] = new Dictionary<string, FileEntry>(StringComparer.OrdinalIgnoreCase)
                {
                    [selected.FullName] = selected,
                },
            };

        var report = await engine.CleanAsync([item], scan.TimestampUtc, dryRun: false, selectedFilesByItem: selectedFiles);

        Assert.Equal(1, report.FilesDeleted);
        Assert.Equal(100, report.BytesFreed);
        Assert.False(fs.FileExists($"{Root}\\first.tmp"));
        Assert.True(fs.FileExists($"{Root}\\second.tmp"));
    }

    [Fact]
    public async Task Cleanup_preserves_an_individually_selected_file_if_it_changed_after_scan()
    {
        var (engine, fs, _) = Create();
        var path = $"{Root}\\changing.tmp";
        fs.AddFile(path, 100);
        var scan = await engine.ScanAsync(Context(fs));
        var item = Assert.Single(scan.Items);
        var selected = Assert.Single(item.FileDetails);
        fs.AddFile(path, 200, DateTime.UtcNow);
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, FileEntry>> selectedFiles =
            new Dictionary<string, IReadOnlyDictionary<string, FileEntry>>(StringComparer.OrdinalIgnoreCase)
            {
                [item.Id] = new Dictionary<string, FileEntry>(StringComparer.OrdinalIgnoreCase) { [path] = selected },
            };

        var report = await engine.CleanAsync([item], scan.TimestampUtc, dryRun: false, selectedFilesByItem: selectedFiles);

        Assert.Equal(0, report.FilesDeleted);
        Assert.Contains(report.Results.Single().Items, result => result.Outcome == CleanupOutcome.ChangedSinceScan);
        Assert.True(fs.FileExists(path));
    }

    [Fact]
    public async Task Cleanup_deletes_every_candidate()
    {
        var (engine, fs, history) = Create();
        fs.AddFile($"{Root}\\a.tmp", 1000);
        fs.AddFile($"{Root}\\b.tmp", 2000);
        var scan = await engine.ScanAsync(Context(fs));

        var report = await engine.CleanAsync(scan.Items, scan.TimestampUtc, dryRun: false);

        Assert.Equal(2, report.FilesDeleted);
        Assert.Equal(3000, report.BytesFreed);
        Assert.Empty(fs.Files);
        Assert.Single(history.Entries);
    }

    [Fact]
    public async Task Dry_run_reports_without_deleting()
    {
        var (engine, fs, _) = Create();
        fs.AddFile($"{Root}\\a.tmp", 500);
        var scan = await engine.ScanAsync(Context(fs));

        var report = await engine.CleanAsync(scan.Items, scan.TimestampUtc, dryRun: true);

        Assert.Equal(1, report.Results[0].WouldDelete);
        Assert.Equal(0, report.BytesFreed);
        Assert.Single(fs.Files);
    }

    [Fact]
    public async Task Cleanup_only_deletes_files_that_match_the_scan_filter()
    {
        var (engine, fs, _) = Create(modules: [new SyntheticCleaner(Root, includeSuffix: ".cache")]);
        fs.AddFile($"{Root}\\Browser\\Default\\Cache\\data.cache", 100);
        fs.AddFile($"{Root}\\Browser\\Default\\Cookies", 200);

        var scan = await engine.ScanAsync(Context(fs));
        var item = Assert.Single(scan.Items);
        Assert.Equal(1, item.FileCount);
        Assert.Equal(100, item.TotalBytes);

        var dryRun = await engine.CleanAsync(scan.Items, scan.TimestampUtc, dryRun: true);
        Assert.Equal(1, dryRun.Results.Single().WouldDelete);
        Assert.Equal(2, fs.Files.Count);

        var report = await engine.CleanAsync(scan.Items, scan.TimestampUtc, dryRun: false);

        Assert.Equal(1, report.FilesDeleted);
        Assert.False(fs.FileExists($"{Root}\\Browser\\Default\\Cache\\data.cache"));
        Assert.True(fs.FileExists($"{Root}\\Browser\\Default\\Cookies"));
    }

    [Fact]
    public async Task Cleanup_respects_non_recursive_scan_roots()
    {
        var (engine, fs, _) = Create(modules: [new SyntheticCleaner(Root, recursive: false)]);
        fs.AddFile($"{Root}\\thumbcache_1.db", 100);
        fs.AddFile($"{Root}\\sub\\unrelated.db", 200);

        var scan = await engine.ScanAsync(Context(fs));
        Assert.Equal(1, Assert.Single(scan.Items).FileCount);

        var report = await engine.CleanAsync(scan.Items, scan.TimestampUtc, dryRun: false);

        Assert.Equal(1, report.FilesDeleted);
        Assert.True(fs.FileExists($"{Root}\\sub\\unrelated.db"));
    }

    [Fact]
    public async Task Cleanup_respects_minimum_age_filters()
    {
        var (engine, fs, _) = Create(modules: [new SyntheticCleaner(Root, TimeSpan.FromDays(30))]);
        fs.AddFile($"{Root}\\old.tmp", 100, DateTime.UtcNow.AddDays(-40));
        fs.AddFile($"{Root}\\fresh.tmp", 200, DateTime.UtcNow.AddDays(-1));

        var scan = await engine.ScanAsync(Context(fs));
        Assert.Equal(1, Assert.Single(scan.Items).FileCount);

        var report = await engine.CleanAsync(scan.Items, scan.TimestampUtc, dryRun: false);

        Assert.Equal(1, report.FilesDeleted);
        Assert.True(fs.FileExists($"{Root}\\fresh.tmp"));
    }

    [Fact]
    public async Task Cleanup_never_deletes_files_inside_project_infest_paths()
    {
        const string projectRoot = "C:\\Users\\Tester\\Documents\\Playground\\PROJECT_INFEST";
        var (engine, fs, _) = Create(modules: [new SyntheticCleaner(projectRoot)]);
        fs.AddFile($"{projectRoot}\\keep-me.txt", 100);

        var scan = await engine.ScanAsync(Context(fs));
        var report = await engine.CleanAsync(scan.Items, scan.TimestampUtc, dryRun: false);

        Assert.Empty(scan.Items);
        Assert.Equal(0, report.FilesDeleted);
        Assert.True(fs.FileExists($"{projectRoot}\\keep-me.txt"));
    }

    [Fact]
    public async Task Scan_and_cleanup_skip_project_infest_subpaths()
    {
        var (engine, fs, _) = Create();
        fs.AddFile($"{Root}\\safe.tmp", 100);
        fs.AddFile($"{Root}\\PROJECT_INFEST\\preserve.tmp", 900);

        var scan = await engine.ScanAsync(Context(fs));
        var item = Assert.Single(scan.Items);
        Assert.Equal(1, item.FileCount);
        Assert.Equal(100, item.TotalBytes);

        await engine.CleanAsync(scan.Items, scan.TimestampUtc, dryRun: false);

        Assert.False(fs.FileExists($"{Root}\\safe.tmp"));
        Assert.True(fs.FileExists($"{Root}\\PROJECT_INFEST\\preserve.tmp"));
    }

    [Fact]
    public void Delete_rechecks_exclusions_immediately_before_removing_a_file()
    {
        var fs = new InMemoryFileSystem();
        var file = $"{Root}\\keep-at-last-check.tmp";
        fs.AddFile(file, 100);
        var exclusions = new ExclusionService();
        var gate = new CleanupGate(
            new PathSafetyValidator(["C:\\Users", "C:\\Users\\Tester\\Documents"]),
            exclusions,
            fs,
            Root);

        Assert.Equal(CleanupDecision.Allow, gate.Evaluate(file));
        exclusions.Add(ExclusionRule.Prefix(file));

        Assert.ThrowsAny<InvalidOperationException>(() => gate.Delete(file));
        Assert.True(fs.FileExists(file));
    }

    [Fact]
    public async Task Locked_files_are_skipped_not_fatal()
    {
        var (engine, fs, _) = Create();
        fs.AddFile($"{Root}\\free.tmp", 100);
        fs.AddFile($"{Root}\\locked.tmp", 100);
        fs.MarkLocked($"{Root}\\locked.tmp");
        var scan = await engine.ScanAsync(Context(fs));

        var report = await engine.CleanAsync(scan.Items, scan.TimestampUtc, dryRun: false);

        Assert.Equal(1, report.FilesDeleted);
        Assert.Equal(1, report.FilesSkipped);
    }

    [Fact]
    public async Task Excluded_files_are_never_touched()
    {
        var exclusions = new ExclusionService([ExclusionRule.Prefix($"{Root}\\keep")]);
        var (engine, fs, _) = Create(exclusions);
        fs.AddFile($"{Root}\\keep\\important.tmp", 100);
        fs.AddFile($"{Root}\\junk.tmp", 100);
        var scan = await engine.ScanAsync(Context(fs));

        var report = await engine.CleanAsync(scan.Items, scan.TimestampUtc, dryRun: false);

        Assert.Equal(1, report.FilesDeleted);
        Assert.True(fs.FileExists($"{Root}\\keep\\important.tmp"));
    }

    [Fact]
    public async Task Reparse_points_are_skipped()
    {
        var (engine, fs, _) = Create();
        fs.AddFile($"{Root}\\link.tmp", 100);
        fs.MarkReparsePoint($"{Root}\\link.tmp");
        var scan = await engine.ScanAsync(Context(fs));

        var report = await engine.CleanAsync(scan.Items, scan.TimestampUtc, dryRun: false);

        Assert.Equal(0, report.FilesDeleted);
        Assert.True(fs.FileExists($"{Root}\\link.tmp"));
    }

    [Fact]
    public async Task Orders_are_scanned_but_never_cleaned()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("C:\\Users\\Tester\\Documents\\important.docx", 10);
        var history = new FakeHistoryStore();

        var engine = new CleanerEngine(
            [new SyntheticCleaner("C:\\Users\\Tester\\Documents")],
            fs,
            new PathSafetyValidator(["C:\\Users\\Tester\\Documents"]),
            new ExclusionService(),
            history);

        var scan = await engine.ScanAsync(Context(fs));
        var report = await engine.CleanAsync(scan.Items, scan.TimestampUtc, dryRun: false);

        Assert.Equal(0, report.FilesDeleted);
        Assert.True(fs.FileExists("C:\\Users\\Tester\\Documents\\important.docx"));
    }

    [Fact]
    public async Task Age_filter_hides_recent_files()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile($"{Root}\\old.log", 100, DateTime.UtcNow.AddDays(-40));
        fs.AddFile($"{Root}\\fresh.log", 100, DateTime.UtcNow.AddDays(-1));
        var engine = new CleanerEngine(
            [new SyntheticCleaner(Root, TimeSpan.FromDays(30))],
            fs,
            new PathSafetyValidator(["C:\\Users"]),
            new ExclusionService(),
            new FakeHistoryStore());

        var report = await engine.ScanAsync(Context(fs));

        var item = Assert.Single(report.Items);
        Assert.Equal(1, item.FileCount);
    }

    [Fact]
    public async Task Cancellation_stops_the_scan()
    {
        var (engine, fs, _) = Create();
        fs.AddFile($"{Root}\\a.tmp", 1);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => engine.ScanAsync(Context(fs), cts.Token));
    }
}
