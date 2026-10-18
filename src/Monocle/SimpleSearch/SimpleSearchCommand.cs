using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Dynamo.Controls;
using Dynamo.Logging;
using Dynamo.UI.Controls;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using Dynamo.Wpf.Interfaces;
using MonocleViewExtension.Utilities;

namespace MonocleViewExtension.SimpleSearch
{
    internal class SimpleSearchCommand
    {
        internal static DynamoViewModel dvm;

        internal static ViewLoadedParams vp;

        internal static MenuItem ssMenuItem;
        internal static Popup SimpleSearchPopup;
        internal static string Header = Properties.Resources.SimpleSearchMenuItemHeader;
        /// <summary>
        /// Create the simple search menu
        /// </summary>
        /// <param name="menuItem">monocle menu item</param>
        /// <param name="p">our view loaded parameters for dynamo</param>
        public static void AddMenuItem(ViewLoadedParams p, MenuItem menuItem, MonocleViewExtension m)
        {
            vp = p;
            dvm = p.DynamoWindow.DataContext as DynamoViewModel;

            ssMenuItem = new MenuItem
            {
                Header = $"{Header} 🔍",
                IsCheckable = true
            };

            ssMenuItem.Checked += (sender, args) =>
            {
#if D25_OR_GREATER
 SimpleSearchSideBar(p, m);
#endif
#if !D25_OR_GREATER
                SimpleSearchWindow(p, m);
#endif
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

            if (Globals.InCanvasSearchEnabled)
            {
                // Only registers the Shift+Space binding; the popup (and its sorted copy of the whole library)
                // is created the first time it is actually used, not during Dynamo startup.
                BuildPopup(p);
            }
            

        }

        public static void RegisterKeyboardShortcuts(ViewLoadedParams p)
        {
            var view = p.DynamoWindow as DynamoView;

            //bind full search to UI
            try
            {
                var bindingSearch = new CommandBinding(new RoutedUICommand(Header, "SimpleSearchCommand",
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
            catch (Exception)
            {
                //suppress
            }
        }
        private static void CloseSimpleSearch(ViewLoadedParams p, MonocleViewExtension m)
        {
            try
            {
#if D25_OR_GREATER
                 p.CloseExtensioninInSideBar(m);
#endif
            }
            catch (Exception)
            {
                //suppress this for now. this run is in a pretty old dynamo.
            }
        }
#if D25_OR_GREATER
                private static void SimpleSearchSideBar(ViewLoadedParams p, MonocleViewExtension m)
        {
            var dvm = p.DynamoWindow.DataContext as DynamoViewModel;
            ssView = new SimpleSearchView(dvm);

            //ssView.Unloaded += ViewOnUnloaded;

            p.AddToExtensionsSideBar(m, ssView);
        }
#endif



        private static Window ssWindow;
        private static void SimpleSearchWindow(ViewLoadedParams p, MonocleViewExtension m)
        {
            var dvm = p.DynamoWindow.DataContext as DynamoViewModel;
            ssView = new SimpleSearchView(dvm);

            //ssView.Unloaded += ViewOnUnloaded;

            ssWindow = new Window
            {
                Title = Header,
                Content = ssView,
                Width = 500,
                Owner = p.DynamoWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            ssWindow.Show();
        }

        internal static void BuildPopup(ViewLoadedParams p)
        {
            var view = p.DynamoWindow as DynamoView;

            var dvm = p.DynamoWindow.DataContext as DynamoViewModel;

            try
            {
                var bindingInCanvs = new CommandBinding(new RoutedUICommand(Header, "SimpleSearchCanvasCommand",
                    typeof(ResourceNames.MainWindow), new InputGestureCollection
                    {
                        new KeyGesture(Key.Space, ModifierKeys.Shift)
                    }));
                bindingInCanvs.Executed += (sender, args) =>
                {
                    //don't fire off command if the user is editing a code block
                    //(walk up from the focused element instead of searching the whole window on every keypress)
                    if ((Keyboard.FocusedElement as DependencyObject)?.FindVisualAncestor<CodeBlockEditor>() != null) return;


                    SimpleSearchInCanvas(p, dvm);
                };
                view.CommandBindings.Add(bindingInCanvs);
            }
            catch (Exception e)
            {
                dvm.Model.Logger.LogWarning($"Monocle- {e.Message}", WarningLevel.Mild);
            }
        }

        


        private static void SimpleSearchInCanvas(ViewLoadedParams p, DynamoViewModel viewModel)
        {
            //you at least need one node placed for this to work (for now)
            if (!viewModel.CurrentSpaceViewModel.Nodes.Any())
            {
                return;
            }

            if (SimpleSearchPopup == null)
            {
                SimpleSearchPopup = new Popup
                {
                    Child = new SimpleSearchView(viewModel),
                    Placement = PlacementMode.MousePoint,
                    IsOpen = false,
                    StaysOpen = false,
                    MaxWidth = 250,
                    MaxHeight = 400,
                    MinWidth = 250,
                    MinHeight = 400
                };
            }

            Dispatcher.CurrentDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
            {
                (SimpleSearchPopup.Child as SimpleSearchView)?.RefreshSelection();
                SimpleSearchPopup.Child.Visibility = Visibility.Visible;
                SimpleSearchPopup.Child.UpdateLayout();
                SimpleSearchPopup.IsOpen = true;
                SimpleSearchPopup.CustomPopupPlacementCallback = null;
            }));
        }

        private static SimpleSearchView ssView;
        private static void ViewOnUnloaded(object sender, RoutedEventArgs e)
        {
            ssMenuItem.IsChecked = false;
        }
    }
}
