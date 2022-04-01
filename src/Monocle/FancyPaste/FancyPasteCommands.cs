using System;
using System.Windows.Controls;
using System.Windows.Input;
using Dynamo.Controls;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using Dynamo.Wpf.Interfaces;
using MonocleViewExtension.PasteExtravagant;

namespace MonocleViewExtension.FancyPaste
{
    internal class FancyPasteCommand
    {
        public static void AddMenuItem(ViewLoadedParams p)
        {
            var dvm = p.DynamoWindow.DataContext as DynamoViewModel;
            var m = new FancyPasteModel(dvm, p);

            var flyout = new MenuItem
            {
                Header = "Fancy Paste",
                ToolTip = "More paste options. Inspired by Grasshopper v2's \"Paste Exotic\". Brought to you by Monocle."
            };
            var pasteWithoutWires = new MenuItem
            {
                Header = "Paste Without Wires",
                InputGestureText = "Ctrl + Shift + V"
            };

            pasteWithoutWires.Click += (sender, args) =>
            {
                m.FancyPaste("PasteWithoutWires");
            };

            flyout.Items.Add(pasteWithoutWires);

            foreach (MenuItem menu in p.dynamoMenu.Items)
            {
                if (menu.Name.Equals("editMenu"))
                {
                    menu.Items.Insert(5, flyout);
                }
            }

            RegisterKeyboardShortcuts(p, m);
        }

        private static void RegisterKeyboardShortcuts(ViewLoadedParams p, FancyPasteModel m)
        {
            var view = p.DynamoWindow as DynamoView;

            try
            {
                //Paste without wires
                var pasteWithoutWires = new CommandBinding(new RoutedUICommand("AlignBottom", "AlignBottomCommand",
                    typeof(ResourceNames.MainWindow), new InputGestureCollection
                    {
                        new KeyGesture(Key.V, ModifierKeys.Control | ModifierKeys.Shift)
                    }));
                pasteWithoutWires.Executed += (sender, args) =>
                {
                    m.FancyPaste("PasteWithoutWires");
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
