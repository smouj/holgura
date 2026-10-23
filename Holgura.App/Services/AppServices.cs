using Holgura.App.ViewModels;
using Holgura.Contracts;
using Holgura.Core;
using Holgura.Engine;
using Holgura.Platform.Windows;

namespace Holgura.App.Services;

/// <summary>Composition root; the only place that knows about concrete Windows implementations.</summary>
public sealed class AppServices
{
    private AppServices(
        CleanerEngine engine,
        ExclusionService exclusions,
        IHistoryStore history,
        MainViewModel main,
        InstalledAppsViewModel installedApps)
    {
        Engine = engine;
        Exclusions = exclusions;
        History = history;
        Main = main;
        InstalledApps = installedApps;
    }

    public CleanerEngine Engine { get; }

    public ExclusionService Exclusions { get; }

    public IHistoryStore History { get; }

    public MainViewModel Main { get; }

    public InstalledAppsViewModel InstalledApps { get; }

    public static AppServices Create()
    {
        var fileSystem = new WindowsFileSystem();
        var exclusions = ExclusionStore.Load();
        var history = new JsonHistoryStore();
        var validator = PathSafetyValidator.CreateDefault();
        var bridge = new WslCliBridge();
        var recycleBin = new RecycleBinService();
        var driveSpace = new DriveSpaceService();
        var installedAppsService = new InstalledAppsService();
        var uninstaller = new UninstallerService();

        var engine = new CleanerEngine(
            ModuleRegistry.CreateDefault(bridge, recycleBin),
            fileSystem,
            validator,
            exclusions,
            history);

        var main = new MainViewModel(engine, fileSystem, history, driveSpace);
        var installedApps = new InstalledAppsViewModel(installedAppsService, uninstaller);
        return new AppServices(engine, exclusions, history, main, installedApps);
    }
}
