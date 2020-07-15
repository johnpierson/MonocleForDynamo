using System;
using System.Linq;
using System.Windows;
using System.Windows.Navigation;
using System.Diagnostics;
using System.Reflection;

namespace MonocleViewExtension.About
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            this.Title = $"Monocle v.{Globals.Version}";
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
