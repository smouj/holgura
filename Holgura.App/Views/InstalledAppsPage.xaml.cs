using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using Holgura.App.Services;
using Holgura.App.ViewModels;

namespace Holgura.App.Views;

public partial class InstalledAppsPage : Page
{
    public InstalledAppsPage()
    {
        InitializeComponent();
        ViewModel = App.Services.InstalledApps;
        DataContext = ViewModel;
        Loaded += OnLoaded;
    }

    private InstalledAppsViewModel ViewModel { get; }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        PageAnimations.FadeIn(ContentRoot);
        await ViewModel.RefreshAsync();
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e) => await ViewModel.RefreshAsync();

    private void OnClearSelectionClick(object sender, RoutedEventArgs e) => ViewModel.SelectNone();

    private void OnOpenWindowsAppsSettingsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            _ = Process.Start(new ProcessStartInfo("ms-settings:appsfeatures") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo abrir Configuración de aplicaciones: {ex.Message}",
                "Configuración de Windows", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnUninstallClick(object sender, RoutedEventArgs e)
    {
        var selected = ViewModel.SelectedApplications;
        if (selected.Count != 1)
        {
            MessageBox.Show("Desinstala una aplicación cada vez para revisar su asistente oficial.",
                "Selecciona una aplicación", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var application = selected[0].Application;
        var question = $"Se abrirá el desinstalador oficial de:\n\n{application.DisplayName}\n{application.VersionText} · {application.PublisherText}\n\nHolgura no borrará manualmente los archivos del programa. Revisa el desinstalador antes de confirmar.";
        if (MessageBox.Show(question, "Confirmar desinstalación", MessageBoxButton.OKCancel,
                MessageBoxImage.Warning, MessageBoxResult.Cancel) != MessageBoxResult.OK)
        {
            return;
        }

        var result = ViewModel.StartSelectedUninstaller();
        MessageBox.Show(result, "Desinstalador", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
