using System.Windows;

namespace MonocleViewExtension.NodeDocumentation
{
    /// <summary>
    /// Interaction logic for NodeDocumentationView.xaml
    /// </summary>
    public partial class NodeDocumentationView
    {
        public NodeDocumentationView()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Window));
        }
    }
}
