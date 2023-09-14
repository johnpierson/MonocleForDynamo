using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace MonocleViewExtension.About
{
    /// <summary>
    /// Interaction logic for ContributorUserControl.xaml
    /// </summary>
    public partial class ContributorUserControl : UserControl
    {
        public ContributorUserControl()
        {
            InitializeComponent();
            this.DataContext = this;
        }
        public string UserName { get; set; }
        public string UserImageSource { get; set; }
        public string NavigateUri { get; set; }
        public string Contribution { get; set; }
        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
