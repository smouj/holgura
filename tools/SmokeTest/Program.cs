using Cleaner.Contracts;
using Cleaner.Core;
using Cleaner.Engine;
using Cleaner.Platform.Windows;

var fileSystem = new WindowsFileSystem();
var history = new JsonHistoryStore(Path.Combine(Path.GetTempPath(), "cleaner-smoke-history.jsonl"));

var engine = new CleanerEngine(
    ModuleRegistry.CreateDefault(new WslCliBridge()),
    fileSystem,
    PathSafetyValidator.CreateDefault(),
    new ExclusionService(),
    history);

Console.WriteLine("== SCAN ==");
var scan = await engine.ScanAsync(new ScanContext { FileSystem = fileSystem });
Console.WriteLine($"items={scan.Items.Count} files={scan.TotalFiles:N0} bytes={scan.TotalBytes:N0} ({scan.Duration.TotalSeconds:0.0}s) warnings={scan.Warnings.Count}");
foreach (var warning in scan.Warnings) Console.WriteLine("  warning: " + warning);

foreach (var item in scan.Items.OrderByDescending(i => i.TotalBytes).Take(15))
{
    Console.WriteLine($"  {item.TotalBytes / 1048576.0,9:0.0} MB | {item.SafetyLevel,-8} | {item.DeletionScope,-9} | {item.Title} | {item.RootPath}");
}

Console.WriteLine("== DRY RUN ==");
var dry = await engine.CleanAsync(scan.Items, scan.TimestampUtc, dryRun: true);
Console.WriteLine($"wouldDelete={dry.Results.Sum(r => r.WouldDelete):N0} skipped={dry.FilesSkipped:N0} errors={dry.Errors} bytesFreed={dry.BytesFreed} ({dry.Duration.TotalSeconds:0.0}s)");

foreach (var result in dry.Results)
{
    var outcomes = result.Items.GroupBy(i => i.Outcome).Select(g => $"{g.Key}={g.Count()}").OrderBy(s => s);
    Console.WriteLine($"  {result.ItemId}: {string.Join(" ", outcomes)}");
}

Console.WriteLine("== SAFETY CHECKS ==");
var validator = PathSafetyValidator.CreateDefault();
foreach (var probe in new[] { "C:\\", "C:\\Windows", "C:\\Program Files", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) })
{
    try
    {
        validator.EnsureSafeRoot(probe);
        Console.WriteLine($"  FALLO: aceptó {probe}");
    }
    catch (ProtectedPathException)
    {
        Console.WriteLine($"  ok: rechaza {probe}");
    }
}
