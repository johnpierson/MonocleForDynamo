using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Dynamo.Wpf.Extensions;

namespace MonocleViewExtension.StandardViews
{
    internal class StandardViewsCommand
    {
        /// <summary>
        /// Enable Standard Views in canvas
        /// </summary>
        /// <param name="p">our view loaded parameters for dynamo</param>
        public static void EnableStandardViews(ViewLoadedParams p)
        {
            var m = new StandardViewsModel(p);
            var vm = new StandardViewsViewModel(m);

            var v = new StandardViews() { MainGrid = { DataContext = vm } };
            vm.View = v;

            StackPanel statusBarPanel = FindVisualChildren<StackPanel>(m.dynamoView).First(s => s.Name == "viewControlPanel");

            statusBarPanel.Children.Insert(1,v);

            vm.ViewControlPanel = statusBarPanel;

        }


        #region Helpers
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
        #endregion
    }
}
