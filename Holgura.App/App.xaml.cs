using System.IO;
using System.Windows;
using System.Windows.Threading;
using Holgura.App.Services;
using Holgura.Platform.Windows;

namespace Holgura.App;

public partial class App : Application
{
    private static string LogPath =>
        Path.Combine(WindowsPaths.AppDataDirectory, "app.log");

    public App()
    {
        Log("App: ctor begin");
        Services = AppServices.Create();
        Log("App: services created");

        DispatcherUnhandledException += (_, args) =>
        {
            Log("DispatcherUnhandledException: " + args.Exception);
            args.Handled = false;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Log("UnhandledException: " + args.ExceptionObject);
    }

    public static AppServices Services { get; private set; } = null!;

    internal static bool IsReadmeScreenshotMode { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        Log("OnStartup: begin");
        base.OnStartup(e);
        Log("OnStartup: base done, windows=" + Windows.Count);
    }

    public static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath, DateTimeOffset.Now.ToString("HH:mm:ss.fff") + " " + message + Environment.NewLine);
        }
        catch (Exception)
        {
            // Logging must never take the app down.
        }
    }
}
