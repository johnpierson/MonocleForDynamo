using System.Windows;
using System.Windows.Controls;
using Dynamo.Graph.Workspaces;

namespace MonocleViewExtension.Snippets
{
    /// <summary>
    /// Interaction logic for SnippetsView.xaml
    /// </summary>
    public partial class SnippetsView : Window
    {
        public SnippetsView()
        {
            InitializeComponent();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var hwm = button.DataContext as HomeWorkspaceModel;

 

        }
    }
}
