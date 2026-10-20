using Cleaner.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using Cleaner.Core;

namespace Cleaner.App.ViewModels;

public sealed partial class ScanItemViewModel : ObservableObject
{
    private readonly Action _onSelectionChanged;

    public ScanItemViewModel(ScanItem item, Action onSelectionChanged)
    {
        Item = item;
        _onSelectionChanged = onSelectionChanged;
        _isSelected = item.SelectedByDefault;
    }

    public ScanItem Item { get; }

    public string Title => Item.Title;

    public string Description => Item.Description;

    public string SizeText => SizeFormatter.Format(Item.TotalBytes);

    public string Detail => $"{Item.FileCount:N0} archivos · {SafetyLabel} · {Item.RootPath}";

    public string SafetyLabel => Item.SafetyLevel switch
    {
        SafetyLevel.Safe => "Seguro",
        SafetyLevel.Review => "Revisar",
        _ => "Avanzado",
    };

    public bool CanSelect => Item.SafetyLevel != SafetyLevel.Advanced;

    [ObservableProperty]
    private bool _isSelected;

    partial void OnIsSelectedChanged(bool value) => _onSelectionChanged();
}
