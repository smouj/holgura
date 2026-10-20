using Holgura.App.ViewModels;
using Holgura.Contracts;
using Holgura.Core;
using Holgura.Engine;
using Holgura.Platform.Windows;

namespace Holgura.App.Services;

/// <summary>Composition root; the only place that knows about concrete Windows implementations.</summary>
public sealed class AppServices
{
    private AppServices(CleanerEngine engine, ExclusionService exclusions, IHistoryStore history, MainViewModel main)
    {
        Engine = engine;
        Exclusions = exclusions;
        History = history;
        Main = main;
    }

    public CleanerEngine Engine { get; }

    public ExclusionService Exclusions { get; }

    public IHistoryStore History { get; }

    public MainViewModel Main { get; }

    public static AppServices Create()
    {
        var fileSystem = new WindowsFileSystem();
        var exclusions = ExclusionStore.Load();
        var history = new JsonHistoryStore();
        var validator = PathSafetyValidator.CreateDefault();
        var bridge = new WslCliBridge();
        var recycleBin = new RecycleBinService();

        var engine = new CleanerEngine(
            ModuleRegistry.CreateDefault(bridge, recycleBin),
            fileSystem,
            validator,
            exclusions,
            history);

        var main = new MainViewModel(engine, fileSystem, history);
        return new AppServices(engine, exclusions, history, main);
    }
}
