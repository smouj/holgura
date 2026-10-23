using System.Windows;
using System.Windows.Media.Animation;

namespace Holgura.App.Services;

public static class PageAnimations
{
    public static void FadeIn(FrameworkElement element)
    {
        element.Opacity = 0;
        element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(180),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        });
    }
}
