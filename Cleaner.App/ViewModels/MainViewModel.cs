using System.Collections.ObjectModel;
using Cleaner.Contracts;
using Cleaner.Core;
using Cleaner.Engine;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cleaner.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly CleanerEngine _engine;
    private readonly IFileSystem _fileSystem;
    private readonly IHistoryStore _history;

    public MainViewModel(CleanerEngine engine, IFileSystem fileSystem, IHistoryStore history)
    {
        _engine = engine;
        _fileSystem = fileSystem;
        _history = history;
    }

    public ObservableCollection<ScanItemViewModel> Items { get; } = [];

    public ObservableCollection<HistoryEntryViewModel> History { get; } = [];

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "Pulsa Analizar equipo para empezar.";

    [ObservableProperty]
    private string _totalText = "—";

    [ObservableProperty]
    private string _selectionText = "Sin elementos seleccionados";

    private DateTimeOffset _scanTimestamp = DateTimeOffset.UtcNow;

    public bool CanScan => !IsBusy;

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(CanScan));

    public IReadOnlyList<ScanItem> SelectedItems =>
        Items.Where(i => i.IsSelected).Select(i => i.Item).ToArray();

    public async Task ScanAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        StatusText = "Analizando…";
        try
        {
            var report = await _engine.ScanAsync(new ScanContext { FileSystem = _fileSystem }, cancellationToken);
            _scanTimestamp = report.TimestampUtc;

            Items.Clear();
            foreach (var item in report.Items.OrderBy(i => i.Category).ThenByDescending(i => i.TotalBytes))
            {
                Items.Add(new ScanItemViewModel(item, UpdateSelection));
            }

            TotalText = SizeFormatter.Format(report.TotalBytes);
            StatusText = report.Warnings.Count == 0
                ? $"Análisis completado en {report.Duration.TotalSeconds:0.0} s"
                : $"Análisis completado con {report.Warnings.Count} aviso(s): {string.Join(" · ", report.Warnings)}";
            UpdateSelection();
        }
        catch (OperationCanceledException)
        {
            StatusText = "Análisis cancelado.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<string> CleanAsync(bool dryRun, CancellationToken cancellationToken = default)
    {
        var selected = SelectedItems;
        if (selected.Count == 0)
        {
            return "No hay elementos seleccionados.";
        }

        var wouldDeleteBytes = selected.Sum(i => i.TotalBytes);

        IsBusy = true;
        StatusText = dryRun ? "Simulando…" : "Limpiando…";
        try
        {
            var report = await _engine.CleanAsync(selected, _scanTimestamp, dryRun, cancellationToken: cancellationToken);
            StatusText = dryRun
                ? $"Simulación: se liberarían {SizeFormatter.Format(wouldDeleteBytes)}"
                : $"Listo: {SizeFormatter.Format(report.BytesFreed)} liberados";

            await LoadHistoryAsync(cancellationToken);

            return dryRun
                ? $"Se eliminarían {report.Results.Sum(r => r.WouldDelete):N0} archivos ({SizeFormatter.Format(wouldDeleteBytes)}). No se ha borrado nada."
                : $"Se han eliminado {report.FilesDeleted:N0} archivos y se han liberado {SizeFormatter.Format(report.BytesFreed)}. Omitidos: {report.FilesSkipped:N0}.";
        }
        catch (OperationCanceledException)
        {
            return "Operación cancelada.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task LoadHistoryAsync(CancellationToken cancellationToken = default)
    {
        History.Clear();
        foreach (var entry in await _history.LoadAsync(50, cancellationToken))
        {
            History.Add(new HistoryEntryViewModel(entry));
        }
    }

    private void UpdateSelection()
    {
        var selected = Items.Where(i => i.IsSelected).ToList();
        var bytes = selected.Sum(i => i.Item.TotalBytes);
        SelectionText = selected.Count == 0
            ? "Sin elementos seleccionados"
            : $"{selected.Count} categorías · {SizeFormatter.Format(bytes)} recuperables";
    }
}
