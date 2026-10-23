using Holgura.Contracts;
using Holgura.Core;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Holgura.App.ViewModels;

public sealed partial class ScanFileViewModel : ObservableObject
{
    private readonly ScanItemViewModel _parent;
    private bool _isIndividuallySelected;

    public ScanFileViewModel(FileEntry entry, ScanItemViewModel parent)
    {
        Entry = entry;
        _parent = parent;
    }

    public FileEntry Entry { get; }

    public string Name => Path.GetFileName(Entry.FullName);

    public string DirectoryPath => Path.GetDirectoryName(Entry.FullName) ?? Entry.FullName;

    public string FullPath => Entry.FullName;

    public string SizeText => SizeFormatter.Format(Entry.Length);

    public string ModifiedText => Entry.LastWriteUtc.ToLocalTime().ToString("g");

    public bool CanSelect => _parent.CanSelectIndividualFiles && !_parent.IsSelected &&
                             !ProtectedPathPolicy.IsProjectInfestPath(Entry.FullName);

    public bool IsSelected
    {
        get => _parent.IsSelected || _isIndividuallySelected;
        set
        {
            if (_parent.IsSelected || _isIndividuallySelected == value || (value && !CanSelect))
            {
                return;
            }

            _isIndividuallySelected = value;
            OnPropertyChanged();
            _parent.OnFileSelectionChanged();
        }
    }

    internal void NotifyParentSelectionChanged()
    {
        OnPropertyChanged(nameof(CanSelect));
        OnPropertyChanged(nameof(IsSelected));
    }

    internal void ClearIndividualSelection()
    {
        if (!_isIndividuallySelected)
        {
            return;
        }

        _isIndividuallySelected = false;
        OnPropertyChanged(nameof(IsSelected));
    }
}
