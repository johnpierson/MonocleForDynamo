using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace MonocleViewExtension.LocalGroupNaming
{
    internal sealed class LocalGroupNamingIndicator : Window
    {
        private const int ExtendedWindowStyle = -20;
        private const int TransparentWindowStyle = 0x00000020;
        private const int NoActivateWindowStyle = 0x08000000;
        private readonly Window ownerWindow;
        private readonly DispatcherTimer cursorTimer;
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
            cursorTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(33)
            };
            cursorTimer.Tick += CursorTimerTick;

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
            SourceInitialized += OnSourceInitialized;
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
            cursorTimer.Start();
            BeginAnimation(OpacityProperty, new DoubleAnimation(
                0,
                0.9,
                TimeSpan.FromMilliseconds(140)));
        }

        private void CursorTimerTick(object sender, EventArgs args)
        {
            Reposition();
        }

        private void Reposition()
        {
            if (!IsLoaded) return;

            if (!GetCursorPosition(out var nativeCursor)) return;

            var presentationSource = PresentationSource.FromVisual(ownerWindow);
            if (presentationSource?.CompositionTarget == null) return;

            var cursor = presentationSource.CompositionTarget.TransformFromDevice.Transform(
                new Point(nativeCursor.X, nativeCursor.Y));
            const double cursorOffset = 16;
            const double windowInset = 8;
            var ownerRight = ownerWindow.Left + ownerWindow.ActualWidth;
            var ownerBottom = ownerWindow.Top + ownerWindow.ActualHeight;

            var left = cursor.X + cursorOffset;
            if (left + ActualWidth > ownerRight - windowInset)
            {
                left = cursor.X - ActualWidth - cursorOffset;
            }

            var top = cursor.Y + cursorOffset;
            if (top + ActualHeight > ownerBottom - windowInset)
            {
                top = cursor.Y - ActualHeight - cursorOffset;
            }

            Left = Math.Max(ownerWindow.Left + windowInset,
                Math.Min(left, ownerRight - ActualWidth - windowInset));
            Top = Math.Max(ownerWindow.Top + windowInset,
                Math.Min(top, ownerBottom - ActualHeight - windowInset));
        }

        private void OnSourceInitialized(object sender, EventArgs args)
        {
            var handle = new WindowInteropHelper(this).Handle;
            var styles = GetWindowLongPtr(handle, ExtendedWindowStyle).ToInt64();
            styles |= TransparentWindowStyle | NoActivateWindowStyle;
            SetWindowLongPtr(handle, ExtendedWindowStyle, new IntPtr(styles));
        }

        private void OnClosed(object sender, EventArgs args)
        {
            cursorTimer.Stop();
            cursorTimer.Tick -= CursorTimerTick;
        }

        [DllImport("user32.dll", EntryPoint = "GetCursorPos")]
        private static extern bool GetCursorPosition(out NativePoint point);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern IntPtr GetWindowLongPtr(IntPtr windowHandle, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static extern IntPtr SetWindowLongPtr(IntPtr windowHandle, int index, IntPtr newValue);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }
    }
}
