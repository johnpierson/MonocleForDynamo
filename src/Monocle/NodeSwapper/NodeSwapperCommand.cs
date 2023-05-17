using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using MonocleViewExtension.About;

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
            
            var NodeSwapperMenu = new MenuItem { Header = $"node swapper" };

            NodeSwapperMenu.Click += (sender, args) =>
            {
                var m = new NodeSwapperModel(dvm, p);
                var viewModel = new NodeSwapperViewModel(m);

                var window = new NodeSwapperView()
                {
                    // Set the data context for the main grid in the window.
                    MainGrid = { DataContext = viewModel },
                    // Set the owner of the window to the Dynamo window.
                    Owner = p.DynamoWindow
                };

                window.Show();
            };

            //add the graph resizerer menu
            menuItem.Items.Add(NodeSwapperMenu);
        }
    }
}
