using System.Windows.Input;
using MonocleViewExtension.Core;
using MenuItem = System.Windows.Controls.MenuItem;

namespace MonocleViewExtension.BetterSave
{
    /// <summary>
    /// Quick Save, Sloppy Save and the graph thumbnail maker, in Dynamo's own File menu.
    /// </summary>
    internal sealed class BetterSaveFeature : IMonocleFeature
    {
        private BetterSaveModel _model;
        private ShortcutRegistrar _shortcuts;

        public string Name => "Better Save";

        public void Register(MonocleContext ctx, MenuItem monocleMenu)
        {
            _model = new BetterSaveModel(ctx.DynamoViewModel, ctx.LoadedParams);

            /* Shortcuts first, deliberately. Injecting into Dynamo's own File menu depends on that
               menu's layout, which we do not control; registering here means a surprise there
               cannot cost the user Ctrl+Alt+S as well. */
            _shortcuts = new ShortcutRegistrar(ctx);
            _shortcuts.Add("QuickSaveCommand", "QuickSave", Key.S,
                ModifierKeys.Alt | ModifierKeys.Control, () => _model.BetterSave("QuickSave"));

            var fileMenu = MenuBuilder.FindDynamoMenu(ctx, "fileMenu");
            if (fileMenu == null)
            {
                ctx.Log.Warn("Dynamo's File menu was not found, so Better Save has no menu entries. Ctrl+Alt+S still works.");
                return;
            }

            var flyout = MenuBuilder.AddFlyout(fileMenu, Properties.Resources.BetterSaveFlyoutHeader, insertAt: FileMenuPosition);
            flyout.ToolTip = Properties.Resources.BetterSaveFlyoutTooltip;

            MenuBuilder.AddItem(flyout, Properties.Resources.QuickSaveMenuItemHeader,
                () => _model.BetterSave("QuickSave"), ctx,
                Properties.Resources.QuickSaveMenuItemTooltip,
                Properties.Resources.QuickSaveMenuItemKeyboardShortcut);

            MenuBuilder.AddItem(flyout, Properties.Resources.SloppySaveMenuItemHeader,
                () => _model.BetterSave("SloppySave"), ctx,
                Properties.Resources.SloppySaveMenuItemTooltip);

            MenuBuilder.AddItem(fileMenu, "Quick Graph Thumbnail (ᵇʳᵒᵘᵍʰᵗ ᵗᵒ ʸᵒᵘ ᵇʸ ᵐᵒⁿᵒᶜˡᵉ™️)",
                () => _model.CreateGraphThumbnail(), ctx,
                insertAt: fileMenu.Items.IndexOf(flyout) + 1);

            ctx.Log.Info($"Better Save attached to Dynamo's File menu at position {fileMenu.Items.IndexOf(flyout)} of {fileMenu.Items.Count}.");
        }

        /// <summary>
        /// Where the Better Save flyout sits in Dynamo's File menu: just below Save As. If a
        /// future Dynamo has a shorter File menu, the flyout is appended instead.
        /// </summary>
        private const int FileMenuPosition = 7;

        public void Dispose() => _shortcuts?.Dispose();
    }
}
