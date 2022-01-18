using System;
using System.Collections.Generic;
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

namespace MonocleViewExtension.StandardViews
{
    /// <summary>
    /// Interaction logic for StandardViews.xaml
    /// </summary>
    public partial class StandardViews : UserControl
    {
        public StandardViews()
        {
            InitializeComponent();
        }

        private void StandardViewsExpander_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Expander expander) expander.IsExpanded = true;
        }

        private void StandardViewsExpander_OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Expander expander) expander.IsExpanded = false;
        }
    }
}
