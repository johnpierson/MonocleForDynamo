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
        internal static DynamoViewModel dvm;
        internal static ViewLoadedParams vp;
        /// <summary>
        /// Create the snippets menu
        /// </summary>
        /// <param name="menuItem">monocle menu item</param>
        /// <param name="p">our view loaded parameters for dynamo</param>
        public static void AddMenuItem(MenuItem menuItem, ViewLoadedParams p, MonocleViewExtension monocle)
        {
            vp = p;
            dvm = p.DynamoWindow.DataContext as DynamoViewModel;

            var snippetsMenu = new MenuItem { Header = $"snippets", IsCheckable = true};

            snippetsMenu.Checked += (sender, args) =>
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
                p.AddToExtensionsSideBar(monocle, window);
            };
            snippetsMenu.Unchecked += (sender, args) =>
            {
                p.CloseExtensioninInSideBar(monocle);
            };

            //add the graph resizerer menu
                menuItem.Items.Add(snippetsMenu);
        }

    }
}