using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace GroceryStoreManagement.Helpers
{
    public static class AnimationHelper
    {
        public static void FadeIn(UIElement element, double durationSeconds = 0.5)
        {
            if (element == null) return;
            element.Visibility = Visibility.Visible;
            DoubleAnimation animation = new()
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(durationSeconds),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            element.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        public static void FadeOut(UIElement element, double durationSeconds = 0.5, bool hideAtEnd = true)
        {
            if (element == null) return;
            DoubleAnimation animation = new()
            {
                From = element.Opacity,
                To = 0,
                Duration = TimeSpan.FromSeconds(durationSeconds),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            if (hideAtEnd)
            {
                animation.Completed += (s, e) => element.Visibility = Visibility.Collapsed;
            }
            element.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        public static void SlideIn(UIElement element, double fromY = 20, double durationSeconds = 0.5)
        {
            if (element == null) return;

            ThicknessAnimation animation = new()
            {
                From = new Thickness(0, fromY, 0, -fromY),
                To = new Thickness(0),
                Duration = TimeSpan.FromSeconds(durationSeconds),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            element.BeginAnimation(FrameworkElement.MarginProperty, animation);
            FadeIn(element, durationSeconds);
        }
    }
}
