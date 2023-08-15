using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
