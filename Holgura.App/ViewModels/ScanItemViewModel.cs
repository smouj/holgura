using System.Collections.ObjectModel;
using Holgura.Contracts;
using Holgura.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Holgura.App.ViewModels;

public sealed partial class ScanItemViewModel : ObservableObject
{
    private readonly Action _onSelectionChanged;

    public ScanItemViewModel(ScanItem item, Action onSelectionChanged)
    {
        Item = item;
        _onSelectionChanged = onSelectionChanged;
        foreach (var file in item.FileDetails)
        {
            Files.Add(new ScanFileViewModel(file, this));
        }
    }

    public ScanItem Item { get; }

    public ObservableCollection<ScanFileViewModel> Files { get; } = [];

    public string Title => Item.Title;

    public string Description => Item.Description;

    public string SizeText => SizeFormatter.Format(Item.TotalBytes);

    public string Detail => $"{Item.FileCount:N0} archivos · {SafetyLabel}";

    public string SafetyLabel => Item.SafetyLevel switch
    {
        SafetyLevel.Safe => "Seguro",
        SafetyLevel.Review => "Revisar",
        _ => "Avanzado",
    };

    public bool CanSelect => Item.SafetyLevel != SafetyLevel.Advanced;

    public bool CanSelectIndividualFiles => CanSelect && Item.DeletionScope == DeletionScope.FileSystem && Files.Count > 0;

    public string PreviewLimitText => Item.FileDetailsTruncated
        ? $"Mostrando {Files.Count:N0} de {Item.FileCount:N0} archivos (los de mayor tamaño). Puedes seleccionar el grupo completo para incluir el resto."
        : $"{Item.FileCount:N0} archivos · lista completa";

    public long SelectedFileCount => IsSelected ? Item.FileCount : Files.LongCount(file => file.IsSelected);

    public long SelectedBytes => IsSelected ? Item.TotalBytes : Files.Where(file => file.IsSelected).Sum(file => file.Entry.Length);

    public IReadOnlyDictionary<string, FileEntry> SelectedFileDetails => Files
        .Where(file => file.IsSelected && !IsSelected)
        .ToDictionary(file => file.Entry.FullName, file => file.Entry, StringComparer.OrdinalIgnoreCase);

    [ObservableProperty]
    private bool _isSelected;

    partial void OnIsSelectedChanged(bool value)
    {
        if (value)
        {
            foreach (var file in Files)
            {
                file.ClearIndividualSelection();
            }
        }

        foreach (var file in Files)
        {
            file.NotifyParentSelectionChanged();
        }

        OnPropertyChanged(nameof(CanSelectIndividualFiles));
        OnPropertyChanged(nameof(SelectedFileCount));
        OnPropertyChanged(nameof(SelectedBytes));
        _onSelectionChanged();
    }

    internal void OnFileSelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedFileCount));
        OnPropertyChanged(nameof(SelectedBytes));
        _onSelectionChanged();
    }
}
