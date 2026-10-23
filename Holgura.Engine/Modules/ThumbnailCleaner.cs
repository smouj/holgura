using Holgura.Contracts;

namespace Holgura.Engine.Modules;

/// <summary>Thumbnail cache databases. Explorer rebuilds them on demand.</summary>
public sealed class ThumbnailCleaner : DirectoryCleanerModule
{
    public override string Id => "system.thumbnails";

    public override string DisplayName => "Miniaturas de Windows";

    protected override IEnumerable<ScanRoot> GetRoots(ScanContext context)
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(local))
        {
            yield break;
        }

        yield return new ScanRoot(
            Path.Combine(local, "Microsoft", "Windows", "Explorer"),
            "Caché de miniaturas",
            "Windows volverá a generar estas miniaturas cuando las necesite.",
            SafetyLevel.Safe,
            SelectedByDefault: true,
            Recursive: false);
    }

    protected override bool Include(FileEntry file, ScanRoot root, DateTimeOffset asOfUtc)
    {
        var fileName = Path.GetFileName(file.FullName);
        return ((fileName.StartsWith("thumbcache_", StringComparison.OrdinalIgnoreCase) ||
                 fileName.StartsWith("iconcache_", StringComparison.OrdinalIgnoreCase)) &&
                fileName.EndsWith(".db", StringComparison.OrdinalIgnoreCase)) ||
               string.Equals(fileName, "iconcache.db", StringComparison.OrdinalIgnoreCase);
    }
}
