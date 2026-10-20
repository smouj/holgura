using Holgura.Contracts;

namespace Holgura.Engine.Modules;

/// <summary>Crash reports and WER queues. Nothing here is needed after it has been sent.</summary>
public sealed class WindowsErrorReportingCleaner : DirectoryCleanerModule
{
    public override string Id => "system.wer";

    public override string DisplayName => "Informes de errores";

    public WindowsErrorReportingCleaner()
    {
        SafetyLevel = SafetyLevel.Safe;
    }

    protected override IEnumerable<ScanRoot> GetRoots(ScanContext context)
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var common = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        if (!string.IsNullOrWhiteSpace(local))
        {
            yield return new ScanRoot(
                Path.Combine(local, "Microsoft", "Windows", "WER"),
                "Informes de errores del usuario",
                "Volcados y colas de Windows Error Reporting generados por aplicaciones que fallaron.");
        }

        if (!string.IsNullOrWhiteSpace(common))
        {
            yield return new ScanRoot(
                Path.Combine(common, "Microsoft", "Windows", "WER"),
                "Informes de errores del sistema",
                "Los mismos informes, en la cola compartida. Parte puede requerir permisos de administrador.",
                SafetyLevel.Review,
                SelectedByDefault: false);
        }
    }
}
