using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Dynamo.Wpf.Extensions;
using MonocleViewExtension.Utilities;

namespace MonocleViewExtension.StandardViews
{
    internal class StandardViewsCommand
    {
        /// <summary>
        /// Enable Standard Views in canvas
        /// </summary>
        /// <param name="p">our view loaded parameters for dynamo</param>
        public static StandardViewsViewModel EnableStandardViews(ViewLoadedParams p)
        {
            var m = new StandardViewsModel(p);
            var vm = new StandardViewsViewModel(m);

            var v = new StandardViews() { MainGrid = { DataContext = vm } };
            vm.View = v;

            StackPanel statusBarPanel = m.dynamoView.FindVisualChild<StackPanel>(s => s.Name == "viewControlPanel");
            if (statusBarPanel == null) return vm;

            statusBarPanel.Children.Insert(1,v);

            vm.ViewControlPanel = statusBarPanel;

            return vm;
        }

    }
}
