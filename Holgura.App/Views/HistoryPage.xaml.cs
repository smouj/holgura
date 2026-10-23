using System.Windows.Controls;
using Holgura.App.Services;
using Holgura.App.ViewModels;

namespace Holgura.App.Views;

public partial class HistoryPage : Page
{
    public HistoryPage()
    {
        InitializeComponent();
        ViewModel = App.Services.Main;
        DataContext = ViewModel;
        Loaded += async (_, _) =>
        {
            PageAnimations.FadeIn(this);
            await ViewModel.LoadHistoryAsync();
        };
    }

    public MainViewModel ViewModel { get; }
}
