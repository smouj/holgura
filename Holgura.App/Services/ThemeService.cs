using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Holgura.App.Services;

public static class ThemeService
{
    public static void Apply()
    {
        if (Application.Current is null)
        {
            return;
        }

        var resources = Application.Current.Resources;
        var isDark = IsSystemDark();
        resources["BrandLogo"] = LoadLogo(isDark ? "logo-light.png" : "logo.png");
        if (!isDark)
        {
            return;
        }

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
        resources["AccentSoft"] = Brush("#202D45");
        resources["Warning"] = Brush("#F6C177");
        resources["Danger"] = Brush("#CF6679");
        resources["DangerHover"] = Brush("#B75568");
    }

    private static BitmapImage LoadLogo(string assetName)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri($"pack://application:,,,/Holgura.App;component/Assets/{assetName}", UriKind.Absolute);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
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
