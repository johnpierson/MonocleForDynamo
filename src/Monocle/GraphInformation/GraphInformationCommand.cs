using System.Windows.Controls;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;

namespace MonocleViewExtension.GraphInformation
{
    internal static class GraphInformationCommand
    {
        public static void AddMenuItem(MenuItem menuItem, ViewLoadedParams p)
        {
            var graphInfoMenuItem = new MenuItem
            {
                Header = "graph information template",
                ToolTip = "Create a graph information group with notes"
            };

            graphInfoMenuItem.Click += (sender, args) =>
            {
                var dvm = p.DynamoWindow.DataContext as DynamoViewModel;
                if (dvm == null) return;

                var model = new GraphInformationModel(dvm);
                model.CreateGraphInformationTemplate();
            };

            menuItem.Items.Add(graphInfoMenuItem);
        }
    }
}
