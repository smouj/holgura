using System.Collections.ObjectModel;
using Holgura.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Holgura.App.ViewModels;

public sealed partial class InstalledAppsViewModel : ObservableObject
{
    private readonly InstalledAppsService _service;
    private readonly UninstallerService _uninstaller;

    public InstalledAppsViewModel(InstalledAppsService service, UninstallerService uninstaller)
    {
        _service = service;
        _uninstaller = uninstaller;
    }

    public ObservableCollection<InstalledApplicationViewModel> Applications { get; } = [];

    public ObservableCollection<InstalledApplicationViewModel> VisibleApplications { get; } = [];

    [ObservableProperty]
    private bool _isBusy;

    public bool CanRefresh => !IsBusy;

    [ObservableProperty]
    private string _statusText = "Actualiza la lista para detectar aplicaciones instaladas.";

    [ObservableProperty]
    private string _filterText = string.Empty;

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

    public string StartSelectedUninstaller()
    {
        var selected = SelectedApplications;
        if (selected.Count != 1 || !selected[0].CanSelect)
        {
            return "Selecciona una aplicación con desinstalador disponible.";
        }

        try
        {
            using var process = _uninstaller.Start(selected[0].Application);
            StatusText = $"Se abrió el desinstalador oficial de {selected[0].DisplayName}. Sigue sus instrucciones.";
            return StatusText;
        }
        catch (Exception ex)
        {
            StatusText = $"No se pudo iniciar el desinstalador: {ex.Message}";
            return StatusText;
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

    private void ApplyFilter()
    {
        VisibleApplications.Clear();
        var query = FilterText.Trim();
        foreach (var application in Applications.Where(application =>
                     query.Length == 0 ||
                     application.DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                     application.PublisherText.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                     application.VersionText.Contains(query, StringComparison.CurrentCultureIgnoreCase)))
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
            _ => $"{selected.Count:N0} seleccionadas · desinstala una a una para revisar cada asistente",
        };
        CanUninstall = selected.Count == 1 && selected[0].CanSelect && !IsBusy;
        UninstallButtonText = selected.Count > 1 ? "Elige una para desinstalar" : "Desinstalar aplicación";
    }

    public string VisibleCountText => FilterText.Trim().Length == 0
        ? $"{Applications.Count:N0} aplicaciones"
        : $"{VisibleApplications.Count:N0} de {Applications.Count:N0} aplicaciones";

    private static string BuildDetails(InstalledApplicationViewModel application)
    {
        var model = application.Application;
        var location = string.IsNullOrWhiteSpace(model.InstallLocation) ? "Windows no informó la carpeta de instalación." : model.InstallLocation;
        var publisher = string.IsNullOrWhiteSpace(model.Publisher) ? "Editor no informado" : model.Publisher;
        return $"{model.DisplayName}\n{model.VersionText} · {publisher}\n{model.SizeText} · {model.Scope}\n\nUbicación\n{location}\n\n{application.ProtectionText}";
    }
}
