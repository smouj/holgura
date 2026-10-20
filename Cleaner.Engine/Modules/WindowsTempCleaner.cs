using Cleaner.Contracts;

namespace Cleaner.Engine.Modules;

/// <summary>C:\Windows\Temp. Needs elevation for part of its contents, so it is reported as REVIEW.</summary>
public sealed class WindowsTempCleaner : DirectoryCleanerModule
{
    public override string Id => "system.windowstemp";

    public override string DisplayName => "Temporales de Windows";

    protected override IEnumerable<ScanRoot> GetRoots(ScanContext context)
    {
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (string.IsNullOrWhiteSpace(windows))
        {
            yield break;
        }

        yield return new ScanRoot(
            Path.Combine(windows, "Temp"),
            "Carpeta temporal de Windows",
            "Temporales creados por instaladores y servicios. Algunos archivos están en uso y se omitirán.",
            SafetyLevel.Review,
            SelectedByDefault: false);
    }
}
