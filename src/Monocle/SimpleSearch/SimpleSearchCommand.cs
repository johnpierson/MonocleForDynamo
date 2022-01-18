using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Dynamo.Controls;
using Dynamo.Logging;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using Dynamo.Wpf.Interfaces;
using MonocleViewExtension.Utilities;
using ProtoCore.AST.AssociativeAST;

namespace MonocleViewExtension.SimpleSearch
{
    internal class SimpleSearchCommand
    {
        internal static DynamoViewModel dvm;

        internal static ViewLoadedParams vp;

        internal static MenuItem ssMenuItem;
        public static void AddMenuItem(ViewLoadedParams p, MenuItem menuItem, MonocleViewExtension m)
        {
            vp = p;
            dvm = p.DynamoWindow.DataContext as DynamoViewModel;

            ssMenuItem = new MenuItem
            {
                Header = "simple search 🔍",
                IsCheckable = true
            };

            ssMenuItem.Checked += (sender, args) =>
            {
                if (Globals.DynamoVersion.CompareTo(Globals.SidebarMinVersion) >= 0)
                {
                    SimpleSearchSideBar(p, m);
                }
                else
                {
                    SimpleSearchWindow(p, m);
                }
            };

            ssMenuItem.Unchecked += (sender, args) =>
            {
                if (Globals.DynamoVersion.CompareTo(Globals.SidebarMinVersion) >= 0)
                {
                    CloseSimpleSearch(p, m);
                }
                else
                {
                    CloseSimpleSearch();
                }

            };

            menuItem.Items.Add(ssMenuItem);

            RegisterKeyboardShortcuts(p);
        }

        public static void RegisterKeyboardShortcuts(ViewLoadedParams p)
        {
            var view = p.DynamoWindow as DynamoView;
            try
            {
                var bindingSearch = new CommandBinding(new RoutedUICommand("SimpleSearch", "SimpleSearchCommand",
                    typeof(ResourceNames.MainWindow), new InputGestureCollection
                    {
                        new KeyGesture(Key.F, ModifierKeys.Control)
                    }));
                bindingSearch.Executed += (sender, args) =>
                {
                    ssMenuItem.IsChecked = true;
                    ssView.RefreshSelection();
                };
                view.CommandBindings.Add(bindingSearch);
            }
            catch (Exception e)
            {
                dvm.Model.Logger.LogWarning($"Monocle- {e.Message}", WarningLevel.Mild);
            }
        }

        private static void CloseSimpleSearch()
        {
            try
            {
                ssWindow?.Close();
            }
            catch (Exception e)
            {
                //
            }
        }
        private static void CloseSimpleSearch(ViewLoadedParams p, MonocleViewExtension m)
        {
            try
            {
                p.CloseExtensioninInSideBar(m);
            }
            catch (Exception)
            {
                //suppress this for now. this run is in a pretty old dynamo.
            }
        }

        private static void SimpleSearchSideBar(ViewLoadedParams p, MonocleViewExtension m)
        {
            var dvm = p.DynamoWindow.DataContext as DynamoViewModel;
            ssView = new SimpleSearchView(dvm);

            ssView.Unloaded += ViewOnUnloaded;
            p.AddToExtensionsSideBar(m, ssView);
        }

        private static Window ssWindow;
        private static void SimpleSearchWindow(ViewLoadedParams p, MonocleViewExtension m)
        {
            var dvm = p.DynamoWindow.DataContext as DynamoViewModel;
            ssView = new SimpleSearchView(dvm);

            ssView.Unloaded += ViewOnUnloaded;

            ssWindow = new Window
            {
                Title = "Simple Search",
                Content = ssView,
                Width = 500,
                Owner = p.DynamoWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            ssWindow.Show();
        }

        private static SimpleSearchView ssView;
        private static void ViewOnUnloaded(object sender, RoutedEventArgs e)
        {
            ssMenuItem.IsChecked = false;
        }
    }
}
