using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using MonocleViewExtension.About;

namespace MonocleViewExtension.Snippets
{
    internal class SnippetsCommand
    {
        /// <summary>
        /// Create the snippets menu
        /// </summary>
        /// <param name="menuItem">monocle menu item</param>
        /// <param name="p">our view loaded parameters for dynamo</param>
        public static void AddMenuItem(MenuItem menuItem, ViewLoadedParams p)
        {
            var dvm = p.DynamoWindow.DataContext as DynamoViewModel;

            var graphResizererMenu = new MenuItem { Header = $"snippets" };

            graphResizererMenu.Click += (sender, args) =>
            {
                var m = new SnippetsModel(dvm, p);
                var viewModel = new SnippetsViewModel(m);

                var window = new SnippetsView()
                {
                    // Set the data context for the main grid in the window.
                    MainGrid = { DataContext = viewModel },
                    // Set the owner of the window to the Dynamo window.
                    Owner = p.DynamoWindow
                };

                window.Show();
            };

            //add the graph resizerer menu
            menuItem.Items.Add(graphResizererMenu);
        }
    }
}