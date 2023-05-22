using System;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using Dynamo.Controls;
using Dynamo.Graph.Workspaces;
using Dynamo.Nodes.Prompts;
using Dynamo.UI.Prompts;
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
                Header = "Better Save",
                ToolTip = "Better save options. Because you deserve it. Brought to you by monocle™️"
            };
            //quick save with timestamp
            var quickSave = new MenuItem
            {
                Header = "Quick Save",
                InputGestureText = "Ctrl + Alt + S",
                ToolTip = "Provides a quick date/time stamped \"snapshot\" of the current file."
            };

            quickSave.Click += (sender, args) =>
            {
                betterSaveModel.BetterSave("QuickSave");
            };

            flyout.Items.Add(quickSave);

            //save with new guids
            var saveWithNewGuids = new MenuItem
            {
                Header = "Save With New Guids",
                ToolTip = "You may not know this, but Dynamo (before 2.18) saves workspace with duplicate GUIDs which sucks. This fixes that."
            };

            saveWithNewGuids.Click += (sender, args) =>
            {
                betterSaveModel.BetterSave("SaveWithNewGuids");
            };

            flyout.Items.Add(saveWithNewGuids);

            //sloppy save with random words
            var sloppySave = new MenuItem
            {
                Header = "Sloppy Save",
                ToolTip = "Quickly save your file to your desktop, because you knew you were going to anyway."
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

            var result = MessageBox.Show("sloppy save file?", "sloppy save to desktop", MessageBoxButtons.YesNo);

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
