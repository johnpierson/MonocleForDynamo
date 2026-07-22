using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace MonocleViewExtension.LocalGroupNaming
{
    internal sealed class LocalGroupNamingIndicator : Window
    {
        private readonly Window ownerWindow;
        private bool isDismissing;

        public LocalGroupNamingIndicator(Window owner)
        {
            ownerWindow = owner ?? throw new ArgumentNullException(nameof(owner));
            Owner = owner;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            SizeToContent = SizeToContent.WidthAndHeight;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            ShowActivated = false;
            Focusable = false;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Opacity = 0;

            var progressBar = new ProgressBar
            {
                Width = 52,
                Height = 3,
                IsIndeterminate = true,
                Foreground = new SolidColorBrush(Color.FromRgb(83, 184, 232)),
                Background = new SolidColorBrush(Color.FromRgb(68, 68, 68)),
                Margin = new Thickness(10, 1, 0, 0)
            };
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                IsHitTestVisible = false
            };
            row.Children.Add(new TextBlock
            {
                Text = "Naming group...",
                Foreground = new SolidColorBrush(Color.FromRgb(238, 238, 238)),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            });
            row.Children.Add(progressBar);

            Content = new Border
            {
                Child = row,
                Padding = new Thickness(11, 7, 11, 7),
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromArgb(232, 38, 38, 38)),
                Effect = new DropShadowEffect
                {
                    BlurRadius = 8,
                    ShadowDepth = 1,
                    Opacity = 0.28
                },
                IsHitTestVisible = false
            };

            Loaded += OnLoaded;
            Closed += OnClosed;
            ownerWindow.LocationChanged += OwnerBoundsChanged;
            ownerWindow.SizeChanged += OwnerBoundsChanged;
        }

        public void Dismiss()
        {
            if (isDismissing || !IsLoaded) return;
            isDismissing = true;

            var fade = new DoubleAnimation
            {
                From = Opacity,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(120)
            };
            fade.Completed += (sender, args) => Close();
            BeginAnimation(OpacityProperty, fade);
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            Reposition();
            BeginAnimation(OpacityProperty, new DoubleAnimation(
                0,
                0.9,
                TimeSpan.FromMilliseconds(140)));
        }

        private void OwnerBoundsChanged(object sender, EventArgs args)
        {
            Reposition();
        }

        private void Reposition()
        {
            if (!IsLoaded) return;

            const double inset = 22;
            Left = Math.Max(ownerWindow.Left + inset,
                ownerWindow.Left + ownerWindow.ActualWidth - ActualWidth - inset);
            Top = Math.Max(ownerWindow.Top + inset,
                ownerWindow.Top + ownerWindow.ActualHeight - ActualHeight - inset);
        }

        private void OnClosed(object sender, EventArgs args)
        {
            ownerWindow.LocationChanged -= OwnerBoundsChanged;
            ownerWindow.SizeChanged -= OwnerBoundsChanged;
        }
    }
}
