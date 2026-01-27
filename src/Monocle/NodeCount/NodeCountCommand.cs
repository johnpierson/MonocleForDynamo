using System.Windows;
using System.Windows.Controls;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;

namespace MonocleViewExtension.NodeCount
{
    internal static class NodeCountCommand
    {
        public static void AddMenuItem(MenuItem menuItem, ViewLoadedParams p)
        {
            var countMenuItem = new MenuItem
            {
                Header = "node count",
                ToolTip = "Show the number of nodes in the current workspace"
            };

            countMenuItem.Click += (sender, args) =>
            {
                var dvm = p.DynamoWindow.DataContext as DynamoViewModel;
                var count = dvm?.CurrentSpaceViewModel?.Nodes?.Count ?? 0;
                var plural = count == 1 ? string.Empty : "s";
                MessageBox.Show(p.DynamoWindow, $"This workspace has {count} node{plural}.", "Monocle");
            };

            menuItem.Items.Add(countMenuItem);
        }
    }
}
