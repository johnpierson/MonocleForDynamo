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
                Header = "Freeze Action Nodes",
                ToolTip = "Freezes all nodes in the graph that perform actions (nodes with side effects)"
            };

            freezeActionNodesMenu.Click += (sender, args) =>
            {
                var m = new FreezeActionNodesModel(dvm, p);
                int frozenCount = m.FreezeActionNodes();
                
                dvm.Model.Logger.LogNotification(
                    "Monocle",
                    "FreezeActionNodes",
                    $"Frozen {frozenCount} action node(s)",
                    $"Successfully froze {frozenCount} action node(s) in the graph."
                );
            };

            //add the freeze action nodes menu
            menuItem.Items.Add(freezeActionNodesMenu);
        }
    }
}

