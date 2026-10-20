using System.Windows;
using Cleaner.App.Views;

namespace Cleaner.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        App.Log("MainWindow: before InitializeComponent");
        InitializeComponent();
        App.Log("MainWindow: after InitializeComponent");
        NavFrame.Navigate(new HomePage());
        App.Log("MainWindow: navigated");
    }

    private void OnNav(object sender, RoutedEventArgs e)
    {
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
