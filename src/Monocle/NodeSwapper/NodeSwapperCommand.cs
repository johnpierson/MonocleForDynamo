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
            
            var NodeSwapperMenu = new MenuItem { Header = Properties.Resources.NodeSwapperMenuItemHeader };

            NodeSwapperMenu.Click += (sender, args) =>
            {
                // The view model shows its own paintbrush window from the constructor.
                var m = new NodeSwapperModel(dvm, p);
                _ = new NodeSwapperViewModel(m);
            };

            //add the graph resizerer menu
            menuItem.Items.Add(NodeSwapperMenu);
        }
    }
}
