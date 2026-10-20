using System.IO;
using System.Windows;
using System.Windows.Threading;
using Cleaner.App.Services;
using Cleaner.Platform.Windows;

namespace Cleaner.App;

public partial class App : Application
{
    private static readonly string LogPath = Path.Combine(WindowsPaths.AppDataDirectory, "app.log");

    public App()
    {
        Services = AppServices.Create();
        ThemeService.Apply();

        DispatcherUnhandledException += (_, args) =>
        {
            Log("DispatcherUnhandledException: " + args.Exception);
            args.Handled = false;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Log("UnhandledException: " + args.ExceptionObject);
    }

    public static AppServices Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        Log("startup");
        base.OnStartup(e);
        Log("startup: base done, windows=" + Windows.Count);
    }

    public static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath, DateTimeOffset.Now.ToString("s") + " " + message + Environment.NewLine);
        }
        catch (Exception)
        {
            // Logging must never take the app down.
        }
    }
}
