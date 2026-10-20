using Cleaner.Contracts;
using Cleaner.Core;
using Xunit;

namespace Cleaner.Engine.Tests;

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
