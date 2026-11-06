using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace MonocleViewExtension.LocalGroupNaming
{
    internal sealed class LocalModelAgreementWindow : Window
    {
        private readonly CheckBox runtimeAgreement;
        private readonly CheckBox modelAgreement;
        private readonly Button acceptButton;

        public LocalModelAgreementWindow(Window owner, LocalModelManifest manifest)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));

            Title = "Set up Monocle local group naming";
            Owner = owner;
            Width = 560;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var content = new StackPanel { Margin = new Thickness(24) };
            content.Children.Add(new TextBlock
            {
                Text = $"Monocle will download about {manifest.ApproximateDownloadSize} of third-party software for fully local group naming.",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            });
            content.Children.Add(new TextBlock
            {
                Text = $"The llama.cpp runtime ({manifest.RuntimeVersion}) and the {manifest.ModelDisplayName} model are stored under your local application data folder. " +
                       "Dynamo node names stay on this computer and are never sent to an external AI service.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });
            content.Children.Add(new TextBlock
            {
                Text = "The downloaded files and license acceptance persist, but the model is enabled only for the current Dynamo session or until you uncheck the menu item.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 16)
            });

            runtimeAgreement = CreateAgreement(
                $"I accept the llama.cpp {manifest.RuntimeLicenseName}",
                manifest.RuntimeLicenseUrl);
            modelAgreement = CreateAgreement(
                $"I accept the {manifest.ModelDisplayName} {manifest.ModelLicenseName}",
                manifest.ModelLicenseUrl);
            runtimeAgreement.Checked += AgreementChanged;
            runtimeAgreement.Unchecked += AgreementChanged;
            modelAgreement.Checked += AgreementChanged;
            modelAgreement.Unchecked += AgreementChanged;
            content.Children.Add(runtimeAgreement);
            content.Children.Add(modelAgreement);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 18, 0, 0)
            };
            var cancelButton = new Button
            {
                Content = "Cancel",
                MinWidth = 88,
                Margin = new Thickness(0, 0, 8, 0),
                IsCancel = true
            };
            acceptButton = new Button
            {
                Content = "Accept and download",
                MinWidth = 148,
                IsDefault = true,
                IsEnabled = false
            };
            acceptButton.Click += (sender, args) =>
            {
                DialogResult = true;
                Close();
            };
            buttons.Children.Add(cancelButton);
            buttons.Children.Add(acceptButton);
            content.Children.Add(buttons);
            Content = content;
        }

        private static CheckBox CreateAgreement(string text, string licenseUrl)
        {
            var checkBox = new CheckBox { Margin = new Thickness(0, 5, 0, 5) };
            var textBlock = new TextBlock { TextWrapping = TextWrapping.Wrap };
            textBlock.Inlines.Add(new Run(text + " ("));
            var link = new Hyperlink(new Run("review license"))
            {
                NavigateUri = new Uri(licenseUrl)
            };
            link.RequestNavigate += OpenLink;
            textBlock.Inlines.Add(link);
            textBlock.Inlines.Add(new Run(")"));
            checkBox.Content = textBlock;
            return checkBox;
        }

        private void AgreementChanged(object sender, RoutedEventArgs args)
        {
            acceptButton.IsEnabled = runtimeAgreement.IsChecked == true && modelAgreement.IsChecked == true;
        }

        private static void OpenLink(object sender, RequestNavigateEventArgs args)
        {
            args.Handled = true;
            try
            {
                Process.Start(new ProcessStartInfo(args.Uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch (Exception)
            {
                // No browser, or one blocked by policy (common on locked-down hosts): an
                // exception escaping a routed event handler would take Dynamo down.
                MessageBox.Show(
                    "The license could not be opened in a browser. You can read it at:" + Environment.NewLine + Environment.NewLine + args.Uri.AbsoluteUri,
                    "Monocle local group naming",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }
}
