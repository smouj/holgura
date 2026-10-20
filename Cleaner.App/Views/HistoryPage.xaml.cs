using System.Windows.Controls;
using Cleaner.App.ViewModels;

namespace Cleaner.App.Views;

public partial class HistoryPage : Page
{
    public HistoryPage()
    {
        InitializeComponent();
        ViewModel = App.Services.Main;
        DataContext = ViewModel;
        Loaded += async (_, _) => await ViewModel.LoadHistoryAsync();
    }

    public MainViewModel ViewModel { get; }
}
