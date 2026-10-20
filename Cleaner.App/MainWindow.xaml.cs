using System.Windows;
using Cleaner.App.Views;

namespace Cleaner.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        App.Log("MainWindow: xaml loaded");
        NavFrame.Navigate(new HomePage());
        App.Log("MainWindow: navigated to HomePage, visible=" + IsVisible);
    }

    private void OnNav(object sender, RoutedEventArgs e)
    {
        // IsChecked="True" on the first item raises Checked while the XAML tree is still being built,
        // so NavFrame can legitimately be null here. The constructor does the initial navigation.
        if (NavFrame is null)
        {
            return;
        }

        if (sender == NavHistory)
        {
            NavFrame.Navigate(new HistoryPage());
        }
        else if (sender == NavExclusions)
        {
            NavFrame.Navigate(new ExclusionsPage());
        }
        else if (sender == NavHome)
        {
            NavFrame.Navigate(new HomePage());
        }
    }
}
