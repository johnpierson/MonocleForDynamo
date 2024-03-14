using System.Windows.Controls;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;

namespace MonocleViewExtension.NodeSwapper
{
    internal class NodeSwapperCommand
    {
        /// <summary>
        /// Create the about menu
        /// </summary>
        /// <param name="menuItem">monocle menu item</param>
        /// <param name="p">our view loaded parameters for dynamo</param>
        public static void AddMenuItem(MenuItem menuItem, ViewLoadedParams p)
        {
            var dvm = p.DynamoWindow.DataContext as DynamoViewModel;
            
            var NodeSwapperMenu = new MenuItem { Header = Properties.Resources.ResourceManager.GetString("NodeSwapperMenuItemHeader") };

            NodeSwapperMenu.Click += (sender, args) =>
            {   
                var m = new NodeSwapperModel(dvm, p);
                var viewModel = new NodeSwapperViewModel(m);

                //var window = new NodeSwapperView()
                //{
                //    // Set the data context for the main grid in the window.
                //    MainGrid = { DataContext = viewModel },
                //    // Set the owner of the window to the Dynamo window.
                //    Owner = p.DynamoWindow
                //};
               
            };

            //add the graph resizerer menu
            menuItem.Items.Add(NodeSwapperMenu);
        }
    }
}
