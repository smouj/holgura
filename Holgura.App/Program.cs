using System;
using System.Linq;
using System.Windows;

namespace Holgura.App;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
#if DEBUG
        App.IsReadmeScreenshotMode = args.Contains("--capture-readme-screenshots", StringComparer.OrdinalIgnoreCase);
#endif
        AppDomain.CurrentDomain.UnhandledException += (_, args) => App.Log("FATAL: " + args.ExceptionObject);

        try
        {
            App.Log("Program: creating application");
            var app = new App();
            App.Log("Program: InitializeComponent");
            app.InitializeComponent();
            App.Log("Program: Run");
            app.Run();
            App.Log("Program: exited normally");
        }
        catch (Exception ex)
        {
            App.Log("MAIN EXCEPTION: " + ex);
            throw;
        }
    }
}
