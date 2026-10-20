using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace Holgura.App.Services;

public static class ThemeService
{
    public static void Apply()
    {
        if (!IsSystemDark() || Application.Current is null)
        {
            return;
        }

        var resources = Application.Current.Resources;
        resources["AppBackground"] = Brush("#111214");
        resources["Surface"] = Brush("#18191C");
        resources["SurfaceAlt"] = Brush("#232529");
        resources["BorderSoft"] = Brush("#2C2E33");
        resources["TextPrimary"] = Brush("#F4F4F5");
        resources["TextSecondary"] = Brush("#A1A1AA");
        resources["TextTertiary"] = Brush("#71717A");
        resources["Accent"] = Brush("#4C8DFF");
        resources["AccentHover"] = Brush("#6BA0FF");
        resources["AccentText"] = Brush("#0B0C0E");
    }

    private static SolidColorBrush Brush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    private static bool IsSystemDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
