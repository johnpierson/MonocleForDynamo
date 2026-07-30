using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using Dynamo.Graph.Workspaces;
using MonocleViewExtension.Core;
using MenuItem = System.Windows.Controls.MenuItem;

namespace MonocleViewExtension.BetterSave
{
    /// <summary>
    /// Quick Save, Sloppy Save and the graph thumbnail maker, in Dynamo's own File menu.
    /// </summary>
    internal sealed class BetterSaveFeature : IMonocleFeature
    {
        private MonocleContext _ctx;
        private BetterSaveModel _model;
        private ShortcutRegistrar _shortcuts;

        public string Name => "Better Save";

        public void Register(MonocleContext ctx, MenuItem monocleMenu)
        {
            _ctx = ctx;
            _model = new BetterSaveModel(ctx.DynamoViewModel, ctx.LoadedParams);

            var fileMenu = MenuBuilder.FindDynamoMenu(ctx, "fileMenu");
            if (fileMenu == null)
            {
                ctx.Log.Warn("Dynamo's File menu was not found, so Better Save has no menu entries. The Quick Save shortcut still works.");
            }
            else
            {
                var flyout = MenuBuilder.AddFlyout(fileMenu, Properties.Resources.BetterSaveFlyoutHeader, insertAt: 7);
                flyout.ToolTip = Properties.Resources.BetterSaveFlyoutTooltip;

                MenuBuilder.AddItem(flyout, Properties.Resources.QuickSaveMenuItemHeader,
                    () => _model.BetterSave("QuickSave"), ctx,
                    Properties.Resources.QuickSaveMenuItemTooltip,
                    Properties.Resources.QuickSaveMenuItemKeyboardShortcut);

                MenuBuilder.AddItem(flyout, Properties.Resources.SloppySaveMenuItemHeader,
                    () => _model.BetterSave("SloppySave"), ctx,
                    Properties.Resources.SloppySaveMenuItemTooltip);

                MenuBuilder.AddItem(fileMenu, "Quick Graph Thumbnail (ᵇʳᵒᵘᵍʰᵗ ᵗᵒ ʸᵒᵘ ᵇʸ ᵐᵒⁿᵒᶜˡᵉ™️)",
                    () => _model.CreateGraphThumbnail(), ctx);
                // AddItem appends; move it to sit directly under the flyout.
                var thumbnail = fileMenu.Items[fileMenu.Items.Count - 1];
                fileMenu.Items.Remove(thumbnail);
                fileMenu.Items.Insert(8, thumbnail);
            }

            _shortcuts = new ShortcutRegistrar(ctx);
            _shortcuts.Add("QuickSaveCommand", "QuickSave", Key.S,
                ModifierKeys.Alt | ModifierKeys.Control, () => _model.BetterSave("QuickSave"));

            ctx.LoadedParams.CurrentWorkspaceClearingStarted += OnWorkspaceClearingStarted;
        }

        /// <summary>
        /// Offers a Sloppy Save before a workspace with work in it is cleared. This was behind
        /// #if DEBUG, so no released build ever offered it.
        /// </summary>
        private void OnWorkspaceClearingStarted(IWorkspaceModel workspace)
        {
            if (workspace == null || !workspace.Nodes.Any()) return;

            var result = MessageBox.Show(Properties.Resources.SloppySaveMessageBoxTitle,
                Properties.Resources.SloppySaveMessageBoxCaption, MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes)
            {
                _model.BetterSave("SloppySave");
            }
        }

        public void Dispose()
        {
            if (_ctx != null)
            {
                _ctx.LoadedParams.CurrentWorkspaceClearingStarted -= OnWorkspaceClearingStarted;
            }
            _shortcuts?.Dispose();
        }
    }
}
