using System;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using Dynamo.Controls;
using Dynamo.Graph.Workspaces;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using Dynamo.Wpf.Interfaces;
using MenuItem = System.Windows.Controls.MenuItem;

namespace MonocleViewExtension.BetterSave
{
    internal class BetterSaveCommand
    {
        private static BetterSaveModel betterSaveModel;
        public static void AddMenuItem(ViewLoadedParams p)
        {
            var dvm = p.DynamoWindow.DataContext as DynamoViewModel;
            betterSaveModel = new BetterSaveModel(dvm, p);

            var flyout = new MenuItem
            {
                Header = Properties.Resources.ResourceManager.GetString("BetterSaveFlyoutHeader"),
                ToolTip = Properties.Resources.ResourceManager.GetString("BetterSaveFlyoutTooltip")
            };
            //quick save with timestamp
            var quickSave = new MenuItem
            {
                Header = Properties.Resources.ResourceManager.GetString("QuickSaveMenuItemHeader"),
                InputGestureText = Properties.Resources.ResourceManager.GetString("QuickSaveMenuItemKeyboardShortcut"),
                ToolTip = Properties.Resources.ResourceManager.GetString("QuickSaveMenuItemTooltip")
            };

            quickSave.Click += (sender, args) =>
            {
                betterSaveModel.BetterSave("QuickSave");
            };

            flyout.Items.Add(quickSave);


            //sloppy save with random words
            var sloppySave = new MenuItem
            {
                Header = Properties.Resources.ResourceManager.GetString("SloppySaveMenuItemHeader"),
                ToolTip = Properties.Resources.ResourceManager.GetString("SloppySaveMenuItemTooltip")
            };

            sloppySave.Click += (sender, args) =>
            {
                betterSaveModel.BetterSave("SloppySave");
            };

            flyout.Items.Add(sloppySave);


            foreach (MenuItem menu in p.dynamoMenu.Items)
            {
                if (menu.Name.Equals("fileMenu"))
                {
                    menu.Items.Insert(7, flyout);
                }
            }

            RegisterKeyboardShortcuts(p, betterSaveModel);

#if DEBUG
            p.CurrentWorkspaceClearingStarted += POnCurrentWorkspaceClearingStarted;
#endif
        }

        private static void POnCurrentWorkspaceClearingStarted(IWorkspaceModel obj)
        {
            if(!obj.Nodes.Any()) return;
            
            //Dynamo.UI.Prompts.GenericTaskDialog dialog = new GenericTaskDialog();
            //dialog.ShowDialog(); tODO: Make this dialog dynamo-ey

            var result = MessageBox.Show(Properties.Resources.ResourceManager.GetString("SloppySaveMessageBoxTitle"), Properties.Resources.ResourceManager.GetString("SloppySaveMessageBoxCaption"), MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes)
            {
                betterSaveModel.BetterSave("SloppySave");
            }
           
        }

      

        private static void RegisterKeyboardShortcuts(ViewLoadedParams p, BetterSaveModel m)
        {
            var view = p.DynamoWindow as DynamoView;

            try
            {
                var quickSave = new CommandBinding(new RoutedUICommand("QuickSave", "QuickSaveCommand",
                    typeof(ResourceNames.MainWindow), new InputGestureCollection
                    {
                        new KeyGesture(Key.S, ModifierKeys.Alt | ModifierKeys.Control)
                    }));
                quickSave.Executed += (sender, args) =>
                {
                    m.BetterSave("QuickSave");
                };
                view.CommandBindings.Add(quickSave);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
    }
}
