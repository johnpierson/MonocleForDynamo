using System.IO;
using System.Windows;
using System.Windows.Controls;

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
            // "tb" is a TextBox
            DataObject.AddPastingHandler(this.BasePath, OnPaste);
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            var isText = e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true);
            if (!isText) return;

            var text = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string;

            var vm = this.MainGrid.DataContext as NodeDocumentationViewModel;

            if (Directory.Exists(text))
            {
                vm.CanGetNode = true;
            }

           
            //vm.Path = text;

        }
    }
}
