using Dynamo.Search.SearchElements;
using MonocleViewExtension.SimpleSearch;
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
