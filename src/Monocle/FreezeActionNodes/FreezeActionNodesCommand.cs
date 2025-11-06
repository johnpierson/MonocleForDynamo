using System.Windows.Controls;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;

namespace MonocleViewExtension.FreezeActionNodes
{
    internal class FreezeActionNodesCommand
    {
        /// <summary>
        /// Create the freeze action nodes menu item
        /// </summary>
        /// <param name="menuItem">monocle menu item</param>
        /// <param name="p">our view loaded parameters for dynamo</param>
        public static void AddMenuItem(MenuItem menuItem, ViewLoadedParams p)
        {
            var dvm = p.DynamoWindow.DataContext as DynamoViewModel;
            
            var freezeActionNodesMenu = new MenuItem 
            { 
                Header = "freeze/thaw action nodes",
                ToolTip = "toggles the frozen state of all action nodes in the graph (freezes if unfrozen, thaws if frozen). yes, this is an autocad reference with freeze/thaw."
            };

            freezeActionNodesMenu.Click += (sender, args) =>
            {
                var m = new FreezeActionNodesModel(dvm, p);
                int toggledCount = m.FreezeActionNodes();
                
                dvm.Model.Logger.LogNotification(
                    "Monocle",
                    "FreezeActionNodes",
                    $"Toggled {toggledCount} action node(s)",
                    $"Successfully toggled frozen state for {toggledCount} action node(s) in the graph."
                );
            };

            //add the freeze action nodes menu
            menuItem.Items.Add(freezeActionNodesMenu);
        }
    }
}

