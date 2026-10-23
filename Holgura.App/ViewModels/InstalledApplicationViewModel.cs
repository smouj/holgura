using Holgura.App.Services;
using Holgura.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Holgura.App.ViewModels;

public sealed partial class InstalledApplicationViewModel : ObservableObject
{
    private readonly Action _onSelectionChanged;

    public InstalledApplicationViewModel(InstalledApplication application, Action onSelectionChanged)
    {
        Application = application;
        _onSelectionChanged = onSelectionChanged;
    }

    public InstalledApplication Application { get; }

    public string DisplayName => Application.DisplayName;

    public string VersionText => Application.VersionText;

    public string PublisherText => Application.PublisherText;

    public string SizeText => Application.SizeText;

    public string Scope => Application.Scope;

    public string InstallLocation => Application.InstallLocation;

    public string ProtectionText => Application.IsProjectInfestProtected ? "Protegido · Project Infest" : Application.CanUninstall ? "Desinstalador disponible" : Application.UninstallBlockedReason ?? "No se puede desinstalar desde Holgura";

    public bool CanSelect => !Application.IsProjectInfestProtected && Application.CanUninstall;

    [ObservableProperty]
    private bool _isSelected;

    partial void OnIsSelectedChanged(bool value) => _onSelectionChanged();
}
