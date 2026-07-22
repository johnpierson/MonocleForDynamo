using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace MonocleViewExtension.LocalGroupNaming
{
    internal sealed class LocalModelSetupWindow : Window
    {
        private readonly TextBlock statusText;
        private readonly TextBlock detailText;
        private readonly ProgressBar progressBar;
        private readonly Button cancelButton;
        private bool canClose;
        private bool cancellationRequested;

        public LocalModelSetupWindow(Window owner)
        {
            Title = "Monocle local group naming";
            Owner = owner;
            Width = 500;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var content = new StackPanel { Margin = new Thickness(24) };
            statusText = new TextBlock
            {
                Text = "Preparing local group naming...",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            };
            detailText = new TextBlock
            {
                Margin = new Thickness(0, 6, 0, 12),
                TextWrapping = TextWrapping.Wrap
            };
            progressBar = new ProgressBar
            {
                Height = 16,
                IsIndeterminate = true,
                Minimum = 0,
                Maximum = 100
            };
            cancelButton = new Button
            {
                Content = "Cancel",
                MinWidth = 88,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };
            cancelButton.Click += RequestCancellation;
            content.Children.Add(statusText);
            content.Children.Add(detailText);
            content.Children.Add(progressBar);
            content.Children.Add(cancelButton);
            Content = content;
            Closing += OnClosing;
        }

        public event EventHandler CancelRequested;

        public void UpdateDownload(LocalModelDownloadProgress progress)
        {
            statusText.Text = $"Downloading {progress.Component}...";
            detailText.Text = progress.TotalBytes.HasValue
                ? $"{FormatBytes(progress.BytesReceived)} of {FormatBytes(progress.TotalBytes.Value)}"
                : FormatBytes(progress.BytesReceived);

            if (progress.TotalBytes.GetValueOrDefault() > 0)
            {
                progressBar.IsIndeterminate = false;
                progressBar.Value = Math.Min(100, progress.BytesReceived * 100d / progress.TotalBytes.Value);
            }
            else
            {
                progressBar.IsIndeterminate = true;
            }
        }

        public void ShowLoadingModel()
        {
            statusText.Text = "Loading the local group naming model...";
            detailText.Text = "This can take a moment on the first start.";
            progressBar.IsIndeterminate = true;
        }

        public void CompleteAndClose()
        {
            canClose = true;
            Close();
        }

        private void RequestCancellation(object sender, RoutedEventArgs args)
        {
            if (cancellationRequested) return;
            cancellationRequested = true;
            cancelButton.IsEnabled = false;
            statusText.Text = "Cancelling setup...";
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnClosing(object sender, CancelEventArgs args)
        {
            if (canClose) return;
            args.Cancel = true;
            RequestCancellation(sender, new RoutedEventArgs());
        }

        private static string FormatBytes(long bytes)
        {
            const double gigabyte = 1024d * 1024d * 1024d;
            const double megabyte = 1024d * 1024d;
            return bytes >= gigabyte
                ? $"{bytes / gigabyte:0.00} GB"
                : $"{bytes / megabyte:0.0} MB";
        }
    }
}
