using System;
using System.Windows.Controls;
using System.Windows.Input;
using Dynamo.Controls;
using Dynamo.Logging;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using Dynamo.Wpf.Interfaces;
using MonocleViewExtension.PackageUsage;
using SharpDX.Direct2D1;

namespace MonocleViewExtension.PackageUsage
{
    public class PackageUsageCommand
    {
        /// <summary>
        /// Create the package usage menu
        /// </summary>
        /// <param name="menuItem">monocle menu item</param>
        /// <param name="p">our view loaded parameters for dynamo</param>
        public static void AddMenuItem(MenuItem menuItem, ViewLoadedParams p)
        {
            //add the flyout first
            var packageFlyout = new MenuItem {Header = Properties.Resources.PackageUsageMenuItemHeader };
            menuItem.Items.Add(packageFlyout);

            var dvm = p.DynamoWindow.DataContext as DynamoViewModel;
            var m = new PackageUsageModel(dvm, p);

            #region DogeWindow

            var packageUsageDoge = new MenuItem { Header = Properties.Resources.PackageUsageDogeMenuItemHeader };

            packageUsageDoge.Click += (sender, args) =>
            {
                
                var vm = new PackageUsageViewModel(m);
                var dogeWindow = new PackageUsageDogeView
                {
                    // Set the data context for the main grid in the window.
                    MainGrid = { DataContext = vm },
                    // Set the owner of the window to the Dynamo window.
                    Owner = p.DynamoWindow,
                    Top = p.DynamoWindow.Top + 200,
                    Left = p.DynamoWindow.Left + 200,
                };
                dogeWindow.Show();
                //unsubscribe from events
                dogeWindow.Closing += (o, eventArgs) => vm.Dispose();
            };

            packageFlyout.Items.Add(packageUsageDoge);
            #endregion

            #region BoringWindow
            
            var packageUsageBoring = new MenuItem { Header = Properties.Resources.PackageUsageBoringMenuItemHeader };

            packageUsageBoring.Click += (sender, args) =>
            {

                var vm = new PackageUsageViewModel(m);

                var boringWindow = new PackageUsageView
                {
                    // Set the data context for the main grid in the window.
                    MainGrid = { DataContext = vm },
                    // Set the owner of the window to the Dynamo window.
                    Owner = p.DynamoWindow,
                    Top = p.DynamoWindow.Top + 200,
                    Left = p.DynamoWindow.Left + 200
                };

                boringWindow.Show();

                //unsubscribe from events
                boringWindow.Closing += (o, eventArgs) => vm.Dispose();
            };

            packageUsageBoring.InputGestureText = "Ctrl + Shift + P";
            
            packageFlyout.Items.Add(packageUsageBoring);
            #endregion

            RegisterKeyboardShortcuts(p,m);
            AddCustomNodeHighlighter(menuItem, m);
        }
        /// <summary>
        /// Create the custom node highlighter flyout menu
        /// </summary>
        /// <param name="menuItem">monocle menu item</param>
        /// <param name="p">our view loaded parameters for dynamo</param>
        private static void AddCustomNodeHighlighter(MenuItem menuItem, PackageUsageModel m)
        {
            //add the flyout first
            var highlightFlyout = new MenuItem { Header = Properties.Resources.NodeIndentifierMenuItemHeader };
            menuItem.Items.Add(highlightFlyout);


            //add highlight
            var highlightCustomNodes = new MenuItem { Header = Properties.Resources.HighlightEnableMenuItemHeader };
            highlightCustomNodes.Click += (sender, args) =>
            {
                m.HighlightCustomNodes();
            };
            highlightCustomNodes.InputGestureText = "Ctrl + Alt + H";

            highlightFlyout.Items.Add(highlightCustomNodes);
            //remove highlight
            var removeHighlightCustomNodes = new MenuItem { Header = Properties.Resources.HighlightDisableMenuItemHeader };
            removeHighlightCustomNodes.Click += (sender, args) =>
            {
                m.ResetCustomNodeHighlights();
            };
            removeHighlightCustomNodes.InputGestureText = "Ctrl + Alt + R";

            highlightFlyout.Items.Add(removeHighlightCustomNodes);

            //mega input identifier
            var inputIdentifier = new MenuItem { Header = Properties.Resources.MegaInputIdentifierMenuItem };
            inputIdentifier.Click += (sender, args) =>
            {
                m.RevealInputs();
            };
            inputIdentifier.InputGestureText = "Ctrl + Alt + I";

            highlightFlyout.Items.Add(inputIdentifier);
        }

        public static void RegisterKeyboardShortcuts(ViewLoadedParams p, PackageUsageModel m)
        {
            var view = p.DynamoWindow as DynamoView;


            try
            {
                //add package usage shortie
                var packageUsage = new CommandBinding(new RoutedUICommand("PackageUsage", "PackageUsageCommand",
                    typeof(ResourceNames.MainWindow), new InputGestureCollection
                    {
                        new KeyGesture(Key.P, ModifierKeys.Control | ModifierKeys.Shift)
                    }));
                packageUsage.Executed += (sender, args) =>
                {
                    var vm = new PackageUsageViewModel(m);

                    var boringWindow = new PackageUsageView
                    {
                        // Set the data context for the main grid in the window.
                        MainGrid = { DataContext = vm },
                        // Set the owner of the window to the Dynamo window.
                        Owner = p.DynamoWindow,
                        Top = p.DynamoWindow.Top + 200,
                        Left = p.DynamoWindow.Left + 200,
                    };

                    boringWindow.Show();

                    //unsubscribe from events
                    boringWindow.Closing += (o, eventArgs) => vm.Dispose();
                };
                view.CommandBindings.Add(packageUsage);

                //add the highlight shortie
                var highlight = new CommandBinding(new RoutedUICommand("Highlight", "HighlightCustomNodesCommand",
                    typeof(ResourceNames.MainWindow), new InputGestureCollection
                    {
                        new KeyGesture(Key.H, ModifierKeys.Control | ModifierKeys.Alt)
                    }));
                highlight.Executed += (sender, args) =>
                {
                    m.HighlightCustomNodes();
                };
                view.CommandBindings.Add(highlight);

                //add the reset highlight shortie
                var resetHighlight = new CommandBinding(new RoutedUICommand("ResetHighlight", "ResetHighlightCustomNodesCommand",
                    typeof(ResourceNames.MainWindow), new InputGestureCollection
                    {
                        new KeyGesture(Key.R, ModifierKeys.Control | ModifierKeys.Alt)
                    }));
                resetHighlight.Executed += (sender, args) =>
                {
                    m.ResetCustomNodeHighlights();
                };
                view.CommandBindings.Add(resetHighlight);
                //add the input identifier shortie
                var identifyInput = new CommandBinding(new RoutedUICommand("IdentifyInput", "IdentifyInputCommand",
                    typeof(ResourceNames.MainWindow), new InputGestureCollection
                    {
                        new KeyGesture(Key.I, ModifierKeys.Control | ModifierKeys.Alt)
                    }));
                identifyInput.Executed += (sender, args) =>
                {
                    m.RevealInputs();
                };
                view.CommandBindings.Add(identifyInput);
            }
            catch (Exception e)
            {
                m.DynamoViewModel.Model.Logger.LogWarning($"Monocle- {e.Message}", WarningLevel.Mild);
            }

            InputGestureCollection gestures = new InputGestureCollection
            {
                new KeyGesture(Key.P, ModifierKeys.Control | ModifierKeys.Shift)
            };

            RoutedUICommand uiCommand = new RoutedUICommand("PackageUsage", "PackageUsageCommand", typeof(ResourceNames.MainWindow), gestures);
            var binding = new CommandBinding(uiCommand);
            binding.Executed += (sender, args) =>
            {
                //var dvm = p.DynamoWindow.DataContext as DynamoViewModel;
                //var m = new PackageUsageModel(dvm, p);
                var vm = new PackageUsageViewModel(m);

                var boringWindow = new PackageUsageView
                {
                    // Set the data context for the main grid in the window.
                    MainGrid = { DataContext = vm },
                    // Set the owner of the window to the Dynamo window.
                    Owner = p.DynamoWindow,
                    Top = p.DynamoWindow.Top + 200,
                    Left = p.DynamoWindow.Left + 200,
                };

                boringWindow.Show();

                //unsubscribe from events
                boringWindow.Closing += (o, eventArgs) => vm.Dispose();
            };


            view.CommandBindings.Add(binding);
        }
    }
}
