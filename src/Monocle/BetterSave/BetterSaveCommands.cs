using System;
using System.Windows.Controls;
using System.Windows.Input;
using Dynamo.Controls;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using Dynamo.Wpf.Interfaces;

namespace MonocleViewExtension.BetterSave
{
    internal class BetterSaveCommand
    {
        public static void AddMenuItem(ViewLoadedParams p)
        {
            var dvm = p.DynamoWindow.DataContext as DynamoViewModel;
            var m = new BetterSaveModel(dvm, p);

            var flyout = new MenuItem
            {
                Header = "Better Save",
                ToolTip = "Better save options. Because you deserve it. Brought to you by monocle™️"
            };
            var quickSave = new MenuItem
            {
                Header = "Quick Save",
                InputGestureText = "Ctrl + Alt + S",
                ToolTip = "Provides a quick date/time stamped \"snapshot\" of the current file."
            };

            quickSave.Click += (sender, args) =>
            {
                m.BetterSave("QuickSave");
            };

            flyout.Items.Add(quickSave);

            foreach (MenuItem menu in p.dynamoMenu.Items)
            {
                if (menu.Name.Equals("fileMenu"))
                {
                    menu.Items.Insert(7, flyout);
                }
            }

            RegisterKeyboardShortcuts(p, m);
        }

        private static void RegisterKeyboardShortcuts(ViewLoadedParams p, BetterSaveModel m)
        {
            var view = p.DynamoWindow as DynamoView;

            try
            {
                //Paste without wires
                var pasteWithoutWires = new CommandBinding(new RoutedUICommand("QuickSave", "QuickSaveCommand",
                    typeof(ResourceNames.MainWindow), new InputGestureCollection
                    {
                        new KeyGesture(Key.S, ModifierKeys.Alt | ModifierKeys.Control)
                    }));
                pasteWithoutWires.Executed += (sender, args) =>
                {
                    m.BetterSave("QuickSave");
                };
                view.CommandBindings.Add(pasteWithoutWires);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
    }
}
