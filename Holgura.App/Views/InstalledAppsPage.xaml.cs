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
#if DEBUG
        if (App.IsReadmeScreenshotMode)
        {
            ViewModel.LoadReadmePreview();
            return;
        }
#endif

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
        if (selected.Count == 0 || selected.Any(application => !application.CanSelect))
        {
            MessageBox.Show("Selecciona una o más aplicaciones con desinstalador disponible.",
                "Selecciona aplicaciones", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var preview = string.Join("\n", selected.Take(8).Select(item => $"• {item.DisplayName}"));
        if (selected.Count > 8)
        {
            preview += $"\n… y {selected.Count - 8:N0} más";
        }

        var question = $"Se abrirá el desinstalador oficial de {selected.Count:N0} aplicación(es), una a una:\n\n{preview}\n\nHolgura no borrará manualmente las carpetas. Tendrás que completar cada asistente y confirmar antes de abrir el siguiente. Revisa cada desinstalador antes de continuar.";
        if (MessageBox.Show(question, "Confirmar desinstalación", MessageBoxButton.OKCancel,
                MessageBoxImage.Warning, MessageBoxResult.Cancel) != MessageBoxResult.OK)
        {
            return;
        }

        for (var index = 0; index < selected.Count; index++)
        {
            if (index > 0)
            {
                var proceed = MessageBox.Show(
                    $"Cuando hayas terminado y cerrado el asistente de {selected[index - 1].DisplayName}, vuelve aquí.\n\nPulsa Sí para abrir el siguiente desinstalador oficial:\n{selected[index].DisplayName}\n\nPulsa No para detener el resto.",
                    "Continuar con la siguiente aplicación", MessageBoxButton.YesNo,
                    MessageBoxImage.Question, MessageBoxResult.No);
                if (proceed != MessageBoxResult.Yes)
                {
                    ViewModel.StatusText = $"Proceso detenido por el usuario. Se iniciaron {index:N0} de {selected.Count:N0} desinstaladores.";
                    return;
                }
            }

            if (!ViewModel.TryStartUninstaller(selected[index], out var result))
            {
                MessageBox.Show(result, "Desinstalador", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        if (selected.Count == 1)
        {
            MessageBox.Show(ViewModel.StatusText, "Desinstalador", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            ViewModel.StatusText = $"Se iniciaron los {selected.Count:N0} asistentes oficiales; confirma uno a uno y revisa su resultado.";
        }
    }
}
