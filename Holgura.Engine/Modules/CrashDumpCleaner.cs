using Holgura.Contracts;

namespace Holgura.Engine.Modules;

/// <summary>Application crash dumps. Useful while debugging, disposable afterwards.</summary>
public sealed class CrashDumpCleaner : DirectoryCleanerModule
{
    public override string Id => "system.crashdumps";

    public override string DisplayName => "Volcados de fallos";

    public CrashDumpCleaner()
    {
        SafetyLevel = SafetyLevel.Safe;
    }

    protected override IEnumerable<ScanRoot> GetRoots(ScanContext context)
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(local))
        {
            yield return new ScanRoot(
                Path.Combine(local, "CrashDumps"),
                "Volcados de aplicaciones",
                "Memoria volcada cuando un programa se cierra de golpe. Solo sirve para depurar el fallo.");
        }

        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (!string.IsNullOrWhiteSpace(windows))
        {
            yield return new ScanRoot(
                Path.Combine(windows, "Minidump"),
                "Minivolcados del sistema",
                "Volcados pequeños de paradas del sistema. Requiere administrador para borrarlos.",
                SafetyLevel.Review,
                SelectedByDefault: false);
        }
    }
}
