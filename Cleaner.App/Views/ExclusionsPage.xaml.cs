using System.Windows;
using System.Windows.Controls;
using Cleaner.App.Services;
using Cleaner.Core;

namespace Cleaner.App.Views;

public partial class ExclusionsPage : Page
{
    public ExclusionsPage()
    {
        InitializeComponent();
        Reload();
    }

    private void Reload()
    {
        RuleList.ItemsSource = App.Services.Exclusions.Rules.Select(rule => rule.Value).ToList();
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        var value = RuleBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        try
        {
            var rule = value.Contains('*') || value.Contains('?')
                ? ExclusionRule.Pattern(value)
                : ExclusionRule.Prefix(value);

            App.Services.Exclusions.Add(rule);
            ExclusionStore.Save(App.Services.Exclusions);
            RuleBox.Text = string.Empty;
            Reload();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ruta no válida", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
