using Holgura.Contracts;

namespace Holgura.Engine.Modules;

/// <summary>%TEMP% of the current user. Everything in there is disposable by contract.</summary>
public sealed class TempCleaner : DirectoryCleanerModule
{
    public override string Id => "system.temp";

    public override string DisplayName => "Archivos temporales";

    protected override IEnumerable<ScanRoot> GetRoots(ScanContext context)
    {
        yield return new ScanRoot(
            context.TempPath,
            "Temporales del usuario",
            "Archivos de trabajo que las aplicaciones dejan en %TEMP%. Windows y los programas los regeneran cuando hace falta.",
            SafetyLevel.Safe,
            SelectedByDefault: true);
    }
}
