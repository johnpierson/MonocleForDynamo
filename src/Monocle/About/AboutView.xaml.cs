using System.Diagnostics;
using System.Security.Policy;
using System.Windows;
using System.Windows.Navigation;

namespace MonocleViewExtension.About
{
    /// <summary>
    /// Interaction logic for AboutView.xaml
    /// </summary>
    public partial class AboutView : Window
    {
        public AboutView()
        {
            InitializeComponent();
        }

       
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start("explorer", e.Uri.AbsoluteUri);
            e.Handled = true;
        }

       

        
    }
}
