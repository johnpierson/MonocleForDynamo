using Dynamo.Controls;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using Dynamo.Wpf.Interfaces;
using MonocleViewExtension.FancyPaste;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using MonocleViewExtension.Utilities;

namespace MonocleViewExtension.InlineNodeConnectomatic
{
    internal class InlineNodeConnectomaticCommand
    {
        public static void AddMenuItem(ViewLoadedParams p, MenuItem menuItem)
        {
            var dvm = p.DynamoWindow.DataContext as DynamoViewModel;
            var m = new InlineNodeConnectomaticModel(dvm, p);
            
            var connectoMenuItem = new MenuItem
            {
                IsCheckable = true,
                Header = "inline node connect-o-matic",
                ToolTip = "this tool allows you to drag a node over the center of a wire to connect it.",
                IsChecked = Globals.IsConnectoEnabled
            };


            connectoMenuItem.Checked += (sender, args) =>
            {
                Globals.IsConnectoEnabled = true;
            };
            connectoMenuItem.Unchecked += (sender, args) =>
            {
                Globals.IsConnectoEnabled = false;
            };

            menuItem.Items.Add(connectoMenuItem);
        }

       
    }
}
