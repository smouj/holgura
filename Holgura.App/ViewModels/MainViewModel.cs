using System.Collections.ObjectModel;
using System.IO;
using Holgura.App.Services;
using Holgura.Contracts;
using Holgura.Core;
using Holgura.Engine;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Holgura.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private static readonly IReadOnlyList<ScanFileViewModel> NoFiles = Array.Empty<ScanFileViewModel>();

    private readonly CleanerEngine _engine;
    private readonly IFileSystem _fileSystem;
    private readonly IHistoryStore _history;
    private readonly DriveSpaceService _driveSpace;
    private DateTimeOffset? _scanTimestamp;
    private CancellationTokenSource? _activeOperation;
    private int _selectionBatchDepth;

    public MainViewModel(
        CleanerEngine engine,
        IFileSystem fileSystem,
        IHistoryStore history,
        DriveSpaceService driveSpace)
    {
        _engine = engine;
        _fileSystem = fileSystem;
        _history = history;
        _driveSpace = driveSpace;
    }

    public ObservableCollection<ScanItemViewModel> Items { get; } = [];

    public ObservableCollection<HistoryEntryViewModel> History { get; } = [];

    public ObservableCollection<DriveSpaceSnapshot> Drives { get; } = [];

    public IReadOnlyList<ScanItem> SelectedItems => Items
        .Where(item => item.IsSelected || item.Files.Any(file => file.IsSelected))
        .Select(item => item.Item)
        .ToArray();

    public bool HasScanned => _scanTimestamp is not null;

    public IReadOnlyList<ScanFileViewModel> CurrentFiles => CurrentItem?.Files ?? NoFiles;

    public string CurrentItemTitle => CurrentItem?.Title ?? "Elige una zona";

    public string CurrentItemDescription => CurrentItem?.Description ?? "Analiza el equipo para ver los archivos que Holgura puede revisar.";

    public string CurrentItemPreviewText => CurrentItem?.PreviewLimitText ?? "La lista de archivos aparecerá aquí después del análisis.";

    public string DriveFreeText => SelectedDrive?.FreeText ?? "—";

    public string DriveCapacityText => SelectedDrive?.CapacityText ?? "No hay unidades fijas disponibles.";

    public string DriveUsageText => SelectedDrive?.UsageText ?? string.Empty;

    public double DriveUsedPercent => SelectedDrive?.UsedPercent ?? 0;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "Analiza el equipo para revisar las zonas seguras.";

    [ObservableProperty]
    private string _warningText = string.Empty;

    [ObservableProperty]
    private string _totalText = "—";

    [ObservableProperty]
    private string _totalFilesText = "Aún no hay análisis";

    [ObservableProperty]
    private string _lastScanText = "Nunca analizado";

    [ObservableProperty]
    private string _selectionText = "Nada seleccionado";

    [ObservableProperty]
    private string _selectedBytesText = "0 B";

    [ObservableProperty]
    private string _operationSummary = string.Empty;

    [ObservableProperty]
    private long _selectedBytes;

    [ObservableProperty]
    private long _selectedFileCount;

    [ObservableProperty]
    private DriveSpaceSnapshot? _selectedDrive;

    [ObservableProperty]
    private ScanItemViewModel? _currentItem;

    public bool CanScan => !IsBusy;

    public bool CanCancel => IsBusy;

    public bool CanClean => !IsBusy && SelectedFileCount > 0;

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanScan));
        OnPropertyChanged(nameof(CanCancel));
        OnPropertyChanged(nameof(CanClean));
    }

    partial void OnCurrentItemChanged(ScanItemViewModel? value)
    {
        OnPropertyChanged(nameof(CurrentFiles));
        OnPropertyChanged(nameof(CurrentItemTitle));
        OnPropertyChanged(nameof(CurrentItemDescription));
        OnPropertyChanged(nameof(CurrentItemPreviewText));
    }

    partial void OnSelectedDriveChanged(DriveSpaceSnapshot? value)
    {
        OnPropertyChanged(nameof(DriveFreeText));
        OnPropertyChanged(nameof(DriveCapacityText));
        OnPropertyChanged(nameof(DriveUsageText));
        OnPropertyChanged(nameof(DriveUsedPercent));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.WhenAll(LoadHistoryAsync(cancellationToken), RefreshDriveSpaceAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
            StatusText = "Carga inicial cancelada.";
        }
        catch (Exception ex)
        {
            StatusText = $"La interfaz está lista, pero algunos datos no se pudieron cargar: {ex.Message}";
        }
    }

    public async Task ScanAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _activeOperation = linked;
        IsBusy = true;
        OperationSummary = string.Empty;
        try
        {
            await ScanCoreAsync(linked.Token);
        }
        catch (OperationCanceledException)
        {
            StatusText = "Análisis cancelado. No se ha eliminado nada.";
        }
        catch (Exception ex)
        {
            StatusText = $"No se pudo completar el análisis: {ex.Message}";
        }
        finally
        {
            _activeOperation = null;
            IsBusy = false;
        }
    }

    public async Task<string> CleanAsync(bool dryRun, CancellationToken cancellationToken = default)
    {
        var selected = SelectedItems;
        if (selected.Count == 0)
        {
            return "Selecciona una categoría o varios archivos.";
        }

        if (_scanTimestamp is null)
        {
            return "Analiza el equipo antes de limpiar.";
        }

        if (IsBusy)
        {
            return "Espera a que termine la operación actual.";
        }

        var selectedFilesByItem = Items
            .Where(item => !item.IsSelected && item.Files.Any(file => file.IsSelected))
            .ToDictionary(
                item => item.Item.Id,
                item => item.SelectedFileDetails,
                StringComparer.OrdinalIgnoreCase);
        var plannedBytes = SelectedBytes;
        var plannedFiles = SelectedFileCount;

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _activeOperation = linked;
        IsBusy = true;
        try
        {
            StatusText = dryRun ? "Simulando la selección…" : "Aplicando la selección…";
            var report = await Task.Run(
                () => _engine.CleanAsync(
                    selected,
                    _scanTimestamp.Value,
                    dryRun,
                    cancellationToken: linked.Token,
                    selectedFilesByItem: selectedFilesByItem),
                linked.Token);

            var affected = report.Results.Sum(result => dryRun ? result.WouldDelete : result.Deleted);
            var skipped = report.FilesSkipped;
            var amount = dryRun ? plannedBytes : report.BytesFreed;
            OperationSummary = dryRun
                ? $"Vista previa: se podrían eliminar {affected:N0} archivos y liberar hasta {SizeFormatter.Format(amount)}. No se borró nada."
                : $"Completado: {report.FilesDeleted:N0} archivos eliminados, {SizeFormatter.Format(report.BytesFreed)} liberados y {skipped:N0} conservados u omitidos.";
            StatusText = dryRun ? "Vista previa lista." : "Limpieza terminada; actualizando el análisis.";

            await LoadHistoryAsync(linked.Token);
            if (!dryRun)
            {
                await ScanCoreAsync(linked.Token);
            }

            if (affected == 0 && plannedFiles > 0)
            {
                OperationSummary += " No se eliminó nada; los archivos pueden haber cambiado, estar protegidos o seguir en uso.";
            }

            return OperationSummary;
        }
        catch (OperationCanceledException)
        {
            OperationSummary = "Operación cancelada. Los archivos que no se procesaron permanecen intactos.";
            StatusText = OperationSummary;
            return OperationSummary;
        }
        catch (Exception ex)
        {
            OperationSummary = $"No se pudo completar la operación: {ex.Message}";
            StatusText = "La operación terminó con un error.";
            return OperationSummary;
        }
        finally
        {
            _activeOperation = null;
            IsBusy = false;
        }
    }

    public async Task LoadHistoryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = await _history.LoadAsync(50, cancellationToken);
            History.Clear();
            foreach (var entry in entries)
            {
                History.Add(new HistoryEntryViewModel(entry));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            StatusText = $"No se pudo cargar el historial local: {ex.Message}";
        }
    }

    public async Task RefreshDriveSpaceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var drives = await Task.Run(_driveSpace.ReadReadyFixedDrives, cancellationToken);
            ApplyDriveSpace(drives);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            OnPropertyChanged(nameof(DriveFreeText));
            StatusText = $"No se pudo leer el espacio de las unidades: {ex.Message}";
        }
    }

    public void SelectSafeZones()
    {
        RunSelectionBatch(() =>
        {
            foreach (var item in Items)
            {
                item.IsSelected = item.Item.SafetyLevel == SafetyLevel.Safe;
                if (!item.IsSelected)
                {
                    foreach (var file in item.Files)
                    {
                        file.IsSelected = false;
                    }
                }
            }
        });
    }

    public void SelectNone()
    {
        RunSelectionBatch(() =>
        {
            foreach (var item in Items)
            {
                item.IsSelected = false;
                foreach (var file in item.Files)
                {
                    file.IsSelected = false;
                }
            }
        });
    }

    public void SelectCurrentFiles()
    {
        if (CurrentItem is not { CanSelectIndividualFiles: true } item)
        {
            return;
        }

        RunSelectionBatch(() =>
        {
            item.IsSelected = false;
            foreach (var file in item.Files.Where(file => file.CanSelect))
            {
                file.IsSelected = true;
            }
        });
    }

    public void ClearCurrentSelection()
    {
        if (CurrentItem is not { } item)
        {
            return;
        }

        RunSelectionBatch(() =>
        {
            item.IsSelected = false;
            foreach (var file in item.Files)
            {
                file.IsSelected = false;
            }
        });
    }

    public void CancelOperation() => _activeOperation?.Cancel();

    private async Task ScanCoreAsync(CancellationToken cancellationToken)
    {
        StatusText = "Analizando zonas seguras del equipo…";
        var report = await Task.Run(
            () => _engine.ScanAsync(new ScanContext { FileSystem = _fileSystem }, cancellationToken),
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        _scanTimestamp = report.TimestampUtc;
        Items.Clear();
        foreach (var item in report.Items.OrderByDescending(item => item.TotalBytes).ThenBy(item => item.Title, StringComparer.CurrentCultureIgnoreCase))
        {
            Items.Add(new ScanItemViewModel(item, RequestSelectionUpdate));
        }

        CurrentItem = Items.FirstOrDefault(item => item.CanSelectIndividualFiles && item.Files.Count > 0) ?? Items.FirstOrDefault();
        TotalText = SizeFormatter.Format(report.TotalBytes);
        TotalFilesText = $"{report.TotalFiles:N0} archivos revisados en {report.Items.Count:N0} zonas";
        LastScanText = $"Último análisis · {report.TimestampUtc.ToLocalTime():g}";
        WarningText = report.Warnings.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, report.Warnings.Take(4)) + (report.Warnings.Count > 4 ? $"{Environment.NewLine}y {report.Warnings.Count - 4:N0} aviso(s) más." : string.Empty);
        StatusText = report.Warnings.Count == 0
            ? $"Análisis completado en {report.Duration.TotalSeconds:0.0} s. Revisa cada archivo antes de borrarlo."
            : $"Análisis completado con {report.Warnings.Count:N0} aviso(s). Revisa el resumen antes de continuar.";
        SelectNone();
        await RefreshDriveSpaceAsync(cancellationToken);
    }

    private void ApplyDriveSpace(IReadOnlyList<DriveSpaceSnapshot> snapshots)
    {
        var currentName = SelectedDrive?.Name;
        Drives.Clear();
        foreach (var drive in snapshots)
        {
            Drives.Add(drive);
        }

        var systemDrive = Path.GetPathRoot(Environment.SystemDirectory)?.TrimEnd('\\');
        SelectedDrive = Drives.FirstOrDefault(drive => drive.Name.Equals(currentName, StringComparison.OrdinalIgnoreCase))
                        ?? Drives.FirstOrDefault(drive => drive.Name.Equals(systemDrive, StringComparison.OrdinalIgnoreCase))
                        ?? Drives.FirstOrDefault();
    }

    private void UpdateSelection()
    {
        var categories = Items.Where(item => item.IsSelected).ToArray();
        var individualItems = Items.Where(item => !item.IsSelected && item.Files.Any(file => file.IsSelected)).ToArray();
        var categoryCount = categories.Length;
        var individualFileCount = individualItems.Sum(item => item.Files.LongCount(file => file.IsSelected));
        SelectedFileCount = categories.Sum(item => item.Item.FileCount) + individualFileCount;
        SelectedBytes = categories.Sum(item => item.Item.TotalBytes) + individualItems.Sum(item => item.Files.Where(file => file.IsSelected).Sum(file => file.Entry.Length));
        SelectedBytesText = SizeFormatter.Format(SelectedBytes);
        SelectionText = SelectedFileCount == 0
            ? "Nada seleccionado · elige una categoría o varios archivos"
            : categoryCount > 0 && individualFileCount > 0
                ? $"{categoryCount:N0} zonas completas + {individualFileCount:N0} archivos"
                : categoryCount > 0
                    ? $"{categoryCount:N0} zonas completas · {SelectedFileCount:N0} archivos"
                    : $"{individualFileCount:N0} archivos seleccionados";
        OnPropertyChanged(nameof(CanClean));
    }

    private void RequestSelectionUpdate()
    {
        if (_selectionBatchDepth > 0)
        {
            return;
        }

        UpdateSelection();
    }

    private void RunSelectionBatch(Action action)
    {
        _selectionBatchDepth++;
        try
        {
            action();
        }
        finally
        {
            _selectionBatchDepth--;
            if (_selectionBatchDepth == 0)
            {
                UpdateSelection();
            }
        }
    }
}
