using System.Windows;
using System.Windows.Controls;
using Cleaner.App.ViewModels;

namespace Cleaner.App.Views;

public partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();
        ViewModel = App.Services.Main;
        DataContext = ViewModel;
        Loaded += async (_, _) => await ViewModel.LoadHistoryAsync();
    }

    public MainViewModel ViewModel { get; }

    private async void OnScanClick(object sender, RoutedEventArgs e) => await ViewModel.ScanAsync();

    private async void OnDryRunClick(object sender, RoutedEventArgs e)
    {
        var message = await ViewModel.CleanAsync(dryRun: true);
        MessageBox.Show(message, "Simulación de limpieza", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void OnCleanClick(object sender, RoutedEventArgs e)
    {
        var selected = ViewModel.SelectedItems;
        if (selected.Count == 0)
        {
            MessageBox.Show("Selecciona al menos una categoría.", "Nada que limpiar", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var question = "Se eliminarán los archivos de " + selected.Count + " categorías."
            + Environment.NewLine + Environment.NewLine
            + "Las rutas protegidas y tus exclusiones se respetan siempre, y los archivos en uso se omiten.";

        var confirm = MessageBox.Show(question, "Confirmar limpieza", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
        if (confirm != MessageBoxResult.OK)
        {
            return;
        }

        var result = await ViewModel.CleanAsync(dryRun: false);
        MessageBox.Show(result, "Limpieza completada", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnPreviewClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ScanItemViewModel item })
        {
            return;
        }

        var body = item.Item.SampleFiles.Count == 0
            ? "Este elemento no expone una lista de archivos."
            : string.Join(Environment.NewLine, item.Item.SampleFiles.Take(60));

        MessageBox.Show(body, item.Title + " — muestra de archivos", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
