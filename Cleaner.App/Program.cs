using System;
using System.Windows;

namespace Cleaner.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
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
