using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Holgura.App.Services;
using Holgura.App.Views;

namespace Holgura.App;

public partial class MainWindow : Window
{
    private const uint MonitorDefaultToNearest = 2;
    private const double WindowMargin = 24;
    private bool _applyingMonitorBounds;
    private bool _initialPlacementApplied;

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
        LocationChanged += OnWindowLocationChanged;
#if DEBUG
        Loaded += CaptureReadmeScreenshotsOnLoaded;
#endif
        ThemeService.Apply();
        App.Log("App: theme and brand logo applied");
        App.Log("MainWindow: xaml loaded");
        NavFrame.Navigate(new HomePage());
        App.Log("MainWindow: navigated to HomePage, visible=" + IsVisible);
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        ApplyMonitorBounds(centerWindow: true);
    }

    private void OnWindowLocationChanged(object? sender, EventArgs e)
    {
        ApplyMonitorBounds(centerWindow: false);
    }

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (SidebarColumn is null || SidebarContent is null)
        {
            return;
        }

        var compact = ActualWidth < 1140;
        SidebarColumn.Width = new GridLength(compact ? 68 : 228);
        SidebarContent.Margin = compact
            ? new Thickness(10, 22, 10, 12)
            : new Thickness(16, 26, 16, 16);
        BrandHeader.Margin = compact ? new Thickness(0, 0, 0, 20) : new Thickness(8, 0, 0, 20);
        BrandHeader.HorizontalAlignment = compact ? HorizontalAlignment.Center : HorizontalAlignment.Left;
        BrandName.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        PrivacyNote.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;

        ConfigureNavigationItem(NavHome, NavHomeIcon, NavHomeLabel, compact);
        ConfigureNavigationItem(NavApps, NavAppsIcon, NavAppsLabel, compact);
        ConfigureNavigationItem(NavHistory, NavHistoryIcon, NavHistoryLabel, compact);
        ConfigureNavigationItem(NavExclusions, NavExclusionsIcon, NavExclusionsLabel, compact);
    }

    private static void ConfigureNavigationItem(
        System.Windows.Controls.RadioButton button,
        System.Windows.Controls.TextBlock icon,
        System.Windows.Controls.TextBlock label,
        bool compact)
    {
        button.Padding = compact ? new Thickness(10) : new Thickness(12, 9, 12, 9);
        button.Width = compact ? 44 : double.NaN;
        button.HorizontalContentAlignment = compact ? HorizontalAlignment.Center : HorizontalAlignment.Left;
        icon.Margin = compact ? new Thickness(0) : new Thickness(0, 0, 12, 0);
        label.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ApplyMonitorBounds(bool centerWindow)
    {
        if (_applyingMonitorBounds)
        {
            return;
        }

        _applyingMonitorBounds = true;
        try
        {
            var monitor = IntPtr.Zero;
            if (!centerWindow)
            {
                var handle = new WindowInteropHelper(this).Handle;
                if (handle != IntPtr.Zero)
                {
                    monitor = MonitorFromWindow(handle, MonitorDefaultToNearest);
                }
            }

            if (monitor == IntPtr.Zero && GetCursorPos(out var cursor))
            {
                monitor = MonitorFromPoint(cursor, MonitorDefaultToNearest);
            }

            if (monitor == IntPtr.Zero)
            {
                return;
            }

            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(monitor, ref info))
            {
                return;
            }

            var dpiX = 96u;
            var dpiY = 96u;
            try
            {
                _ = GetDpiForMonitor(monitor, MonitorDpiType.Effective, out dpiX, out dpiY);
            }
            catch (DllNotFoundException)
            {
                // Windows 10 includes Shcore; 96 DPI is a safe fallback for unusual hosts.
            }
            catch (EntryPointNotFoundException)
            {
                // Windows 10 includes this API; 96 DPI is a safe fallback for unusual hosts.
            }

            var scaleX = Math.Max(1u, dpiX) / 96d;
            var scaleY = Math.Max(1u, dpiY) / 96d;
            var workLeft = info.Work.Left / scaleX;
            var workTop = info.Work.Top / scaleY;
            var workWidth = (info.Work.Right - info.Work.Left) / scaleX;
            var workHeight = (info.Work.Bottom - info.Work.Top) / scaleY;
            var availableWidth = Math.Max(1, workWidth - WindowMargin);
            var availableHeight = Math.Max(1, workHeight - WindowMargin);

            MinWidth = Math.Min(760, availableWidth);
            MinHeight = Math.Min(560, availableHeight);
            MaxWidth = availableWidth;
            MaxHeight = availableHeight;

            if (centerWindow || !_initialPlacementApplied)
            {
                Width = Math.Min(1360, availableWidth);
                Height = Math.Min(860, availableHeight);
                Left = workLeft + Math.Max(0, (workWidth - Width) / 2);
                Top = workTop + Math.Max(0, (workHeight - Height) / 2);
                _initialPlacementApplied = true;
                App.Log($"MainWindow: fit to monitor work area {workWidth:0}x{workHeight:0} DIP at {dpiX}x{dpiY} DPI");
                return;
            }

            if (Width > availableWidth)
            {
                Width = availableWidth;
            }

            if (Height > availableHeight)
            {
                Height = availableHeight;
            }

            var maxLeft = workLeft + Math.Max(0, workWidth - Width);
            var maxTop = workTop + Math.Max(0, workHeight - Height);
            Left = Math.Clamp(Left, workLeft, maxLeft);
            Top = Math.Clamp(Top, workTop, maxTop);
        }
        catch (Exception ex)
        {
            App.Log("MainWindow: monitor sizing fallback: " + ex.Message);
        }
        finally
        {
            _applyingMonitorBounds = false;
        }
    }

#if DEBUG
    private async void CaptureReadmeScreenshotsOnLoaded(object sender, RoutedEventArgs e)
    {
        if (!App.IsReadmeScreenshotMode)
        {
            return;
        }

        try
        {
            var outputDirectory = Path.Combine(Environment.CurrentDirectory, "docs", "screenshots");
            Directory.CreateDirectory(outputDirectory);
            await WaitForScreenshotRenderAsync();
            SaveWindowScreenshot(Path.Combine(outputDirectory, "inicio.png"));

            NavApps.IsChecked = true;
            await WaitForScreenshotRenderAsync();
            SaveWindowScreenshot(Path.Combine(outputDirectory, "aplicaciones.png"));
            App.Log("Readme screenshots saved to " + outputDirectory);
        }
        catch (Exception ex)
        {
            App.Log("Readme screenshot capture failed: " + ex);
        }
        finally
        {
            Close();
        }
    }

    private async Task WaitForScreenshotRenderAsync()
    {
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
        await Task.Delay(650);
    }

    private void SaveWindowScreenshot(string path)
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        var pixelWidth = Math.Max(1, (int)Math.Ceiling(ActualWidth * dpi.DpiScaleX));
        var pixelHeight = Math.Max(1, (int)Math.Ceiling(ActualHeight * dpi.DpiScaleY));
        var bitmap = new RenderTargetBitmap(
            pixelWidth,
            pixelHeight,
            dpi.PixelsPerInchX,
            dpi.PixelsPerInchY,
            PixelFormats.Pbgra32);
        bitmap.Render(this);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
#endif

    private enum MonitorDpiType
    {
        Effective = 0,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(NativePoint point, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr monitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

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
        else if (sender == NavApps)
        {
            NavFrame.Navigate(new InstalledAppsPage());
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
