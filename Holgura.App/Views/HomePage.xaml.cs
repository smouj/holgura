using System.Windows;
using System.Windows.Controls;
using Holgura.App.Services;
using Holgura.App.ViewModels;

namespace Holgura.App.Views;

public partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();
        ViewModel = App.Services.Main;
        DataContext = ViewModel;
        Loaded += OnLoaded;
    }

    public MainViewModel ViewModel { get; }

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

        await ViewModel.InitializeAsync();
        if (!ViewModel.HasScanned)
        {
            await ViewModel.ScanAsync();
        }
    }

    private async void OnScanClick(object sender, RoutedEventArgs e) => await ViewModel.ScanAsync();

    private void OnCancelClick(object sender, RoutedEventArgs e) => ViewModel.CancelOperation();

    private void OnSelectSafeZonesClick(object sender, RoutedEventArgs e) => ViewModel.SelectSafeZones();

    private void OnSelectVisibleFilesClick(object sender, RoutedEventArgs e) => ViewModel.SelectCurrentFiles();

    private void OnClearCurrentSelectionClick(object sender, RoutedEventArgs e) => ViewModel.ClearCurrentSelection();

    private async void OnDryRunClick(object sender, RoutedEventArgs e) => _ = await ViewModel.CleanAsync(dryRun: true);

    private async void OnCleanClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedFileCount == 0)
        {
            MessageBox.Show("Selecciona una zona o varios archivos.", "Nada seleccionado", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var question = $"Se procesarán los {ViewModel.SelectedFileCount:N0} archivos de la selección.\n\n" +
                       $"Espacio estimado: {ViewModel.SelectedBytesText}. Los archivos en uso o modificados se conservarán.\n\n" +
                       "Project Infest y sus archivos están protegidos permanentemente. ¿Quieres continuar?";
        if (MessageBox.Show(question, "Confirmar eliminación", MessageBoxButton.OKCancel,
                MessageBoxImage.Warning, MessageBoxResult.Cancel) != MessageBoxResult.OK)
        {
            return;
        }

        var result = await ViewModel.CleanAsync(dryRun: false);
        MessageBox.Show(result, "Resultado de Holgura", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
