using System.Windows.Controls;
using System.Windows.Input;

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
