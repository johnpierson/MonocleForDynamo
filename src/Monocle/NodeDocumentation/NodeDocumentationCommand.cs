using System.Windows.Controls;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;

namespace MonocleViewExtension.NodeDocumentation
{
    internal class NodeDocumentationCommand
    {
        /// <summary>
        /// Create the about menu
        /// </summary>
        /// <param name="menuItem">monocle menu item</param>
        /// <param name="p">our view loaded parameters for dynamo</param>
        public static void AddMenuItem(MenuItem menuItem, ViewLoadedParams p)
        {

            var dvm = p.DynamoWindow.DataContext as DynamoViewModel;

            var docMenu = new MenuItem { Header = Properties.Resources.NodeDocumentationMenuItemHeader };

            docMenu.Click += (sender, args) =>
            {
                var m = new NodeDocumentationModel(dvm, p);
                var vm = new NodeDocumentationViewModel(m);
                var window = new NodeDocumentationView()
                {
                    // Set the data context for the main grid in the window.
                    MainGrid = { DataContext = vm },
                    // Set the owner of the window to the Dynamo window.
                    Owner = p.DynamoWindow
                };

                window.Show();
            };

            //add the about menu and a separator
            menuItem.Items.Add(docMenu);
            menuItem.Items.Add(new Separator());

        }
    }
}
