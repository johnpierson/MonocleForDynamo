using System;
using System.Windows.Controls;
using System.Windows.Input;
using Dynamo.Controls;
using Dynamo.Logging;
using Dynamo.Wpf.Extensions;
using Dynamo.Wpf.Interfaces;
using MonocleViewExtension.Utilities;

namespace MonocleViewExtension.Foca
{
    #region WhyIsThisCalledFoca
    /*Why is this called Foca?
    Well, this tool helps clean your dynamo graphs.
    Foca is a laundry detergent,
    and also means "Seal" in spanish
    ⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⢀⣀⣀⣀⣀⡀⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄
    ⠄⠄⠄⠄⠄⠄⠄⢀⣤⣶⠶⠛⠋⠉⠉⠉⠉⠉⠉⠙⠛⠳⠶⣤⣀⠄⠄⠄⠄⠄⠄⠄
    ⠄⠄⠄⠄⠄⣠⣾⠟⠁⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠉⠛⣦⠄⠄⠄⠄⠄
    ⠄⠄⠄⢀⣼⠟⠄⠄⠄⠄⠄⠄⠄⠄⣀⣀⡀⠄⠄⠄⠄⠄⠄⠄⣀⣤⣈⡳⡄⠄⠄⠄
    ⠄⠄⢠⡾⠃⠄⠄⠄⠄⠄⠄⠠⣴⡾⠛⠉⠉⠂⠄⠄⠄⠄⠄⠄⠄⠄⠙⢀⡹⣆⠄⠄
    ⠄⠄⡿⠃⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠈⠉⢿⡿⠄⠄⠈⢻⡀⠄
    ⠄⢸⠇⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⡀⠄⢠⠄⠄⠄⠄⠄⠄⡇⠄⠄⠰⢎⡇⠄
    ⠄⣼⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⡈⠉⠄⡄⠄⠄⢀⣀⣤⣾⣧⣤⣄⣥⡾⣿⠄
    ⠄⢸⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠈⠙⠶⠶⠶⠿⠟⠉⠁⠄⠄⠈⡹⠁⠄⣿⠄
    ⠄⠈⢇⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⢠⠇⠄
    ⠄⠄⠈⠑⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⢀⡴⠏⠄⠄
    ⠄⠄⠄⠄⠄⠄⠄⡀⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⣀⣤⠟⠄⠄⠄⠄
    ⠄⠄⠄⠄⠄⠄⠄⠄⠁⠒⠄⠤⠄⢀⣀⣀⣀⣀⣀⣤⣤⡤⠶⠞⠛⠉⠄⠄⠄⠄⠄⠄
    ⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠀⠀⠀⠀⠀⠀
     */
    #endregion

    public class FocaCommand
    {
        /// <summary>
        /// Enable FOCA - the cleaner of your dynamo graphs
        /// </summary>
        /// <param name="menuItem">monocle menu item</param>
        /// <param name="p">our view loaded parameters for dynamo</param>
        public static void EnableFoca(ViewLoadedParams p, MenuItem menuItem)
        {
            var m = new FocaModel(p);
            var vm = new FocaViewModel(m);

            var focaWidget = new FocaView {MainGrid = {DataContext = vm}};
            vm.View = focaWidget;

            menuItem.Items.Add(new Separator());

            //foca menu item for enable / disable
            var focaMenuItem = new MenuItem
            {
                IsCheckable = true,
                Header = "enable FOCA   | ᶘ ᵒᴥᵒᶅ",
                ToolTip = "FOCA, formerly known as the \"in-canvas align widget\", is the cleaner of your dynamo graphs. this feature includes alignment tools, grouping tools and more.",
                IsChecked = Globals.IsFocaEnabled
            };

            focaMenuItem.Checked += (sender, args) =>
            {
                Globals.IsFocaEnabled = true;
            };
            focaMenuItem.Unchecked += (sender, args) =>
            {
                Globals.IsFocaEnabled = false;
            };

            // add the about menu and a separator
            menuItem.Items.Add(focaMenuItem);

            RegisterKeyboardShortcuts(p, m);

            var colorCodeMenuItem = new MenuItem { Header = "standard group creation" };

            colorCodeMenuItem.Click += (sender, args) =>
            {
                var window = new ColorCodeView()
                {
                    // Set the data context for the main grid in the window.
                    MainGrid = { DataContext = vm },
                    // Set the owner of the window to the Dynamo window.
                    Owner = p.DynamoWindow
                };

                window.Show();
            };

            //add the about menu and a separator
            menuItem.Items.Add(colorCodeMenuItem);
            menuItem.Items.Add(new Separator());
        }

        public static void RegisterKeyboardShortcuts(ViewLoadedParams p, FocaModel m)
        {
            var view = p.DynamoWindow as DynamoView;
            try
            {
                //AlignLeft
                var bindingLeft = new CommandBinding(new RoutedUICommand("AlignLeft", "AlignLeftCommand",
                    typeof(ResourceNames.MainWindow), new InputGestureCollection
                    {
                        new KeyGesture(Key.Left, ModifierKeys.Alt)
                    }));
                bindingLeft.Executed += (sender, args) =>
                {
                    m.AlignSelected("HorizontalLeft");
                };
                view.CommandBindings.Add(bindingLeft);
                //AlignRight
                var bindingRight = new CommandBinding(new RoutedUICommand("AlignRight", "AlignRightCommand",
                    typeof(ResourceNames.MainWindow), new InputGestureCollection
                    {
                        new KeyGesture(Key.Right, ModifierKeys.Alt)
                    }));
                bindingRight.Executed += (sender, args) =>
                {
                    m.AlignSelected("HorizontalRight");
                };
                view.CommandBindings.Add(bindingRight);
                //AlignTop
                var bindingTop = new CommandBinding(new RoutedUICommand("AlignTop", "AlignTopCommand",
                    typeof(ResourceNames.MainWindow), new InputGestureCollection
                    {
                        new KeyGesture(Key.Up, ModifierKeys.Alt)
                    }));
                bindingTop.Executed += (sender, args) =>
                {
                    m.AlignSelected("VerticalTop");
                };
                view.CommandBindings.Add(bindingTop);
                //AlignBottom
                var bindingBottom = new CommandBinding(new RoutedUICommand("AlignBottom", "AlignBottomCommand",
                    typeof(ResourceNames.MainWindow), new InputGestureCollection
                    {
                        new KeyGesture(Key.Down, ModifierKeys.Alt)
                    }));
                bindingBottom.Executed += (sender, args) =>
                {
                    m.AlignSelected("VerticalBottom");
                };
                view.CommandBindings.Add(bindingBottom);

               
            }
            catch (Exception e)
            {
                m.DynamoViewModel.Model.Logger.LogWarning($"Monocle- {e.Message}", WarningLevel.Mild);
            }
           
        }

        

    }
}
