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
    private static readonly string[] FileSortOptions =
    [
        "Tamaño: mayor a menor",
        "Tamaño: menor a mayor",
        "Nombre: A a Z",
    ];

    private readonly CleanerEngine _engine;
    private readonly IFileSystem _fileSystem;
    private readonly IHistoryStore _history;
    private readonly DriveSpaceService _driveSpace;
    private DateTimeOffset? _scanTimestamp;
    private CancellationTokenSource? _activeOperation;
    private int _selectionBatchDepth;
#if DEBUG
    private bool _isReadmePreview;
#endif

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

    public IReadOnlyList<string> FileSortModes => FileSortOptions;

    public IReadOnlyList<ScanFileViewModel> CurrentFiles
    {
        get
        {
            if (CurrentItem is null)
            {
                return NoFiles;
            }

            return FileSortMode switch
            {
                "Tamaño: menor a mayor" => CurrentItem.Files.OrderBy(file => file.Entry.Length)
                    .ThenBy(file => file.Name, StringComparer.CurrentCultureIgnoreCase).ToArray(),
                "Nombre: A a Z" => CurrentItem.Files.OrderBy(file => file.Name, StringComparer.CurrentCultureIgnoreCase).ToArray(),
                _ => CurrentItem.Files.OrderByDescending(file => file.Entry.Length)
                    .ThenBy(file => file.Name, StringComparer.CurrentCultureIgnoreCase).ToArray(),
            };
        }
    }

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
    private string _fileSortMode = FileSortOptions[0];

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

#if DEBUG
    public bool CanScan => !IsBusy && !_isReadmePreview;
#else
    public bool CanScan => !IsBusy;
#endif

    public bool CanCancel => IsBusy;

#if DEBUG
    public bool CanClean => !_isReadmePreview && !IsBusy && SelectedFileCount > 0;
#else
    public bool CanClean => !IsBusy && SelectedFileCount > 0;
#endif

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

    partial void OnFileSortModeChanged(string value) => OnPropertyChanged(nameof(CurrentFiles));

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
#if DEBUG
        if (_isReadmePreview)
        {
            return;
        }
#endif

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
#if DEBUG
        if (_isReadmePreview)
        {
            return "La vista previa usa datos de ejemplo y no permite borrar archivos.";
        }
#endif

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

#if DEBUG
    internal void LoadReadmePreview()
    {
        _isReadmePreview = true;
        OnPropertyChanged(nameof(CanScan));
        OnPropertyChanged(nameof(CanClean));

        Drives.Clear();
        Drives.Add(new DriveSpaceSnapshot("C:", "Windows", 512L * 1024 * 1024 * 1024, 171L * 1024 * 1024 * 1024));
        SelectedDrive = Drives[0];

        var now = DateTime.UtcNow;
        var previewItems = new[]
        {
            CreateReadmePreviewItem(
                "browser-cache", "Caché de navegador", "Recursos descargables que el navegador puede volver a crear.",
                @"C:\Holgura Demo\Caches\Navegador", ModuleCategory.Applications, SafetyLevel.Safe, now,
                ("render-cache-014.bin", 1_240_000_000L), ("media-preview-082.dat", 768_000_000L),
                ("web-assets-031.cache", 512_000_000L), ("image-cache-209.bin", 128_000_000L)),
            CreateReadmePreviewItem(
                "user-temp", "Temporales de usuario", "Restos temporales de aplicaciones cerradas.",
                @"C:\Holgura Demo\Temporales", ModuleCategory.System, SafetyLevel.Safe, now,
                ("session-cache-052.tmp", 128_000_000L), ("installer-fragment-008.tmp", 97_000_000L),
                ("thumbnail-work-041.tmp", 54_000_000L), ("update-stage-003.tmp", 33_000_000L)),
            CreateReadmePreviewItem(
                "error-reports", "Informes de errores", "Informes locales de errores de Windows.",
                @"C:\Holgura Demo\Informes", ModuleCategory.System, SafetyLevel.Review, now,
                ("application-crash-006.dmp", 220_000_000L), ("diagnostic-report-017.wer", 60_000_000L)),
        };

        Items.Clear();
        foreach (var item in previewItems.OrderByDescending(item => item.TotalBytes))
        {
            Items.Add(new ScanItemViewModel(item, RequestSelectionUpdate));
        }

        _scanTimestamp = now;
        CurrentItem = Items.FirstOrDefault();
        TotalText = SizeFormatter.Format(previewItems.Sum(item => item.TotalBytes));
        TotalFilesText = $"{previewItems.Sum(item => item.FileCount):N0} archivos revisados en {previewItems.Length:N0} zonas";
        LastScanText = "Vista previa con datos de ejemplo";
        WarningText = string.Empty;
        OperationSummary = "Datos de demostración · las acciones de limpieza están deshabilitadas.";
        StatusText = "Vista previa para documentación. No se han leído ni borrado archivos.";
        SelectNone();
        if (CurrentItem?.Files.FirstOrDefault() is { } sampleFile)
        {
            sampleFile.IsSelected = true;
        }
    }

    private static ScanItem CreateReadmePreviewItem(
        string id,
        string title,
        string description,
        string rootPath,
        ModuleCategory category,
        SafetyLevel safetyLevel,
        DateTime timestampUtc,
        params (string Name, long Length)[] files)
    {
        var entries = files.Select((file, index) => new FileEntry(
            Path.Combine(rootPath, file.Name),
            file.Length,
            timestampUtc.AddDays(-index - 2))).ToArray();
        return new ScanItem
        {
            Id = id,
            ModuleId = id,
            Title = title,
            Description = description,
            Category = category,
            SafetyLevel = safetyLevel,
            RootPath = rootPath,
            FileCount = entries.Length,
            TotalBytes = entries.Sum(entry => entry.Length),
            LastWriteUtc = entries.Max(entry => entry.LastWriteUtc),
            SampleFiles = entries.Select(entry => entry.FullName).ToArray(),
            FileDetails = entries,
        };
    }
#endif

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
