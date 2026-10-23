using System.Collections.ObjectModel;
using Holgura.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Holgura.App.ViewModels;

public sealed partial class InstalledAppsViewModel : ObservableObject
{
    private const string AllDrivesFilter = "Todas las unidades";

    private static readonly string[] ApplicationSortOptions =
    [
        "Tamaño: mayor a menor",
        "Tamaño: menor a mayor",
        "Nombre: A a Z",
    ];

    private readonly InstalledAppsService _service;
    private readonly UninstallerService _uninstaller;
#if DEBUG
    private bool _isReadmePreview;
#endif

    public InstalledAppsViewModel(InstalledAppsService service, UninstallerService uninstaller)
    {
        _service = service;
        _uninstaller = uninstaller;
    }

    public ObservableCollection<InstalledApplicationViewModel> Applications { get; } = [];

    public ObservableCollection<InstalledApplicationViewModel> VisibleApplications { get; } = [];

    public IReadOnlyList<string> SortModes => ApplicationSortOptions;

    public IReadOnlyList<string> DriveFilterOptions => new[] { AllDrivesFilter }
        .Concat(Applications.Select(application => application.DriveText)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(drive => drive, StringComparer.CurrentCultureIgnoreCase))
        .ToArray();

    [ObservableProperty]
    private bool _isBusy;

#if DEBUG
    public bool CanRefresh => !IsBusy && !_isReadmePreview;
#else
    public bool CanRefresh => !IsBusy;
#endif

    [ObservableProperty]
    private string _statusText = "Actualiza la lista para detectar aplicaciones instaladas.";

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private string _sortMode = ApplicationSortOptions[0];

    [ObservableProperty]
    private string _driveFilter = AllDrivesFilter;

    [ObservableProperty]
    private string _totalText = "—";

    [ObservableProperty]
    private string _selectionText = "Ninguna aplicación seleccionada";

    [ObservableProperty]
    private string _selectedDetailsText = "Selecciona una aplicación para revisar sus datos.";

    [ObservableProperty]
    private InstalledApplicationViewModel? _currentApplication;

    [ObservableProperty]
    private bool _canUninstall;

    [ObservableProperty]
    private string _uninstallButtonText = "Desinstalar aplicación";

    private string _scanWarningText = string.Empty;

    public IReadOnlyList<InstalledApplicationViewModel> SelectedApplications =>
        Applications.Where(application => application.IsSelected).ToArray();

    partial void OnFilterTextChanged(string value) => ApplyFilter();

    partial void OnSortModeChanged(string value) => ApplyFilter();

    partial void OnDriveFilterChanged(string value)
    {
        foreach (var application in Applications.Where(application => application.IsSelected && !MatchesDrive(application)))
        {
            application.IsSelected = false;
        }

        ApplyFilter();
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanRefresh));
        UpdateSelection();
    }

    partial void OnCurrentApplicationChanged(InstalledApplicationViewModel? value)
    {
        SelectedDetailsText = value is null
            ? "Selecciona una aplicación para revisar sus datos."
            : BuildDetails(value);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
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

        IsBusy = true;
        StatusText = "Leyendo aplicaciones registradas en Windows…";
        try
        {
            var scan = await Task.Run(_service.ReadInstalledApplications, cancellationToken);
            Applications.Clear();
            VisibleApplications.Clear();
            foreach (var application in scan.Applications)
            {
                Applications.Add(new InstalledApplicationViewModel(application, UpdateSelection));
            }

            OnPropertyChanged(nameof(DriveFilterOptions));
            if (!DriveFilterOptions.Contains(DriveFilter, StringComparer.CurrentCultureIgnoreCase))
            {
                DriveFilter = AllDrivesFilter;
            }

            _scanWarningText = scan.Warnings.Count == 0
                ? string.Empty
                : $" No se pudieron leer {scan.Warnings.Count} entrada(s) del registro.";
            TotalText = $"{Applications.Count:N0} aplicaciones encontradas";
            StatusText = $"Lista actualizada en este equipo.{_scanWarningText}";
            CurrentApplication = Applications.FirstOrDefault();
            ApplyFilter();
            UpdateSelection();
        }
        catch (OperationCanceledException)
        {
            StatusText = "Lectura cancelada.";
        }
        catch (Exception ex)
        {
            StatusText = $"No se pudo leer la lista: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public bool TryStartUninstaller(InstalledApplicationViewModel application, out string status)
    {
#if DEBUG
        if (_isReadmePreview)
        {
            status = "La vista previa usa datos de ejemplo y no permite desinstalar aplicaciones.";
            return false;
        }
#endif

        if (application is null || !Applications.Contains(application) || !application.IsSelected || !application.CanSelect || IsBusy)
        {
            status = "La aplicación ya no está disponible para desinstalar. Actualiza la lista y vuelve a seleccionarla.";
            return false;
        }

        try
        {
            using var process = _uninstaller.Start(application.Application);
            status = $"Se abrió el desinstalador oficial de {application.DisplayName}. Sigue sus instrucciones.";
            StatusText = status;
            return true;
        }
        catch (Exception ex)
        {
            status = $"No se pudo iniciar el desinstalador de {application.DisplayName}: {ex.Message}";
            StatusText = status;
            return false;
        }
    }

    public void SelectNone()
    {
        foreach (var application in Applications)
        {
            application.IsSelected = false;
        }

        UpdateSelection();
    }

#if DEBUG
    internal void LoadReadmePreview()
    {
        _isReadmePreview = true;
        OnPropertyChanged(nameof(CanRefresh));
        Applications.Clear();
        VisibleApplications.Clear();

        var examples = new[]
        {
            new InstalledApplication(
                "DEMO\\VideoEditor", "Editor de vídeo de ejemplo", "4.2", "Estudio de ejemplo",
                @"C:\Holgura Demo\Apps\VideoEditor", string.Empty, 4_800L * 1024 * 1024,
                "Ejemplo", false, false, "Deshabilitado en la vista de ejemplo"),
            new InstalledApplication(
                "DEMO\\PhotoViewer", "Visor de imágenes de ejemplo", "2.8", "Estudio de ejemplo",
                @"D:\Holgura Demo\Apps\PhotoViewer", string.Empty, 1_240L * 1024 * 1024,
                "Ejemplo", false, false, "Deshabilitado en la vista de ejemplo"),
            new InstalledApplication(
                "DEMO\\PuzzleGame", "Juego de ejemplo", "1.0", "Desarrollador de ejemplo",
                @"D:\Holgura Demo\Apps\PuzzleGame", string.Empty, 620L * 1024 * 1024,
                "Ejemplo", false, false, "Deshabilitado en la vista de ejemplo"),
            new InstalledApplication(
                "DEMO\\SmallTool", "Herramienta pequeña de ejemplo", "3.1", "Holgura Demo",
                @"C:\Holgura Demo\Apps\SmallTool", string.Empty, 84L * 1024 * 1024,
                "Ejemplo", false, false, "Deshabilitado en la vista de ejemplo"),
        };

        foreach (var application in examples)
        {
            Applications.Add(new InstalledApplicationViewModel(application, UpdateSelection));
        }

        OnPropertyChanged(nameof(DriveFilterOptions));
        DriveFilter = AllDrivesFilter;
        TotalText = $"{Applications.Count:N0} aplicaciones de ejemplo";
        StatusText = "Vista previa para documentación. Lista ficticia; desinstalación deshabilitada.";
        FilterText = string.Empty;
        SortMode = ApplicationSortOptions[0];
        ApplyFilter();
        CurrentApplication = VisibleApplications.FirstOrDefault();
        UpdateSelection();
    }
#endif

    private void ApplyFilter()
    {
        VisibleApplications.Clear();
        var query = FilterText.Trim();
        var matchingApplications = Applications.Where(application =>
                     MatchesDrive(application) &&
                     (query.Length == 0 ||
                      application.DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                      application.PublisherText.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                      application.VersionText.Contains(query, StringComparison.CurrentCultureIgnoreCase)));
        matchingApplications = SortMode switch
        {
            "Tamaño: menor a mayor" => matchingApplications
                .OrderBy(application => application.Application.EstimatedSizeBytes ?? long.MaxValue)
                .ThenBy(application => application.DisplayName, StringComparer.CurrentCultureIgnoreCase),
            "Nombre: A a Z" => matchingApplications
                .OrderBy(application => application.DisplayName, StringComparer.CurrentCultureIgnoreCase),
            _ => matchingApplications
                .OrderByDescending(application => application.Application.EstimatedSizeBytes ?? -1)
                .ThenBy(application => application.DisplayName, StringComparer.CurrentCultureIgnoreCase),
        };

        foreach (var application in matchingApplications)
        {
            VisibleApplications.Add(application);
        }

        if (CurrentApplication is not null && !VisibleApplications.Contains(CurrentApplication))
        {
            CurrentApplication = VisibleApplications.FirstOrDefault();
        }

        OnPropertyChanged(nameof(VisibleCountText));
    }

    private void UpdateSelection()
    {
        var selected = SelectedApplications;
        SelectionText = selected.Count switch
        {
            0 => "Ninguna aplicación seleccionada",
            1 => $"1 aplicación · {selected[0].DisplayName}",
            _ => $"{selected.Count:N0} seleccionadas · se abrirán una a una",
        };
        CanUninstall = selected.Count > 0 && selected.All(application => application.CanSelect) && !IsBusy;
        UninstallButtonText = selected.Count > 1
            ? $"Desinstalar {selected.Count:N0} aplicaciones"
            : "Desinstalar aplicación";
    }

    public string VisibleCountText => FilterText.Trim().Length == 0 && DriveFilter == AllDrivesFilter
        ? $"{Applications.Count:N0} aplicaciones"
        : $"{VisibleApplications.Count:N0} de {Applications.Count:N0} aplicaciones";

    private bool MatchesDrive(InstalledApplicationViewModel application) =>
        DriveFilter == AllDrivesFilter ||
        string.Equals(application.DriveText, DriveFilter, StringComparison.CurrentCultureIgnoreCase);

    private static string BuildDetails(InstalledApplicationViewModel application)
    {
        var model = application.Application;
        var location = string.IsNullOrWhiteSpace(model.InstallLocation) ? "Windows no informó la carpeta de instalación." : model.InstallLocation;
        var publisher = string.IsNullOrWhiteSpace(model.Publisher) ? "Editor no informado" : model.Publisher;
        return $"{model.DisplayName}\n{model.VersionText} · {publisher}\n{model.SizeText} · {application.DriveText} · {model.Scope}\n\nUbicación\n{location}\n\n{application.ProtectionText}";
    }
}
