using System.Windows.Controls;
using System.Windows.Input;
using MonocleViewExtension.Core;

namespace MonocleViewExtension.FancyPaste
{
    /// <summary>
    /// Extra paste options, in Dynamo's own Edit menu rather than the monocle menu.
    /// </summary>
    internal sealed class FancyPasteFeature : IMonocleFeature
    {
        private ShortcutRegistrar _shortcuts;

        public string Name => "Fancy Paste";

        public void Register(MonocleContext ctx, MenuItem monocleMenu)
        {
            var model = new FancyPasteModel(ctx.DynamoViewModel, ctx.LoadedParams);

            var editMenu = MenuBuilder.FindDynamoMenu(ctx, "editMenu");
            if (editMenu == null)
            {
                ctx.Log.Warn("Dynamo's Edit menu was not found, so Fancy Paste has no menu entry. The keyboard shortcut still works.");
            }
            else
            {
                var flyout = MenuBuilder.AddFlyout(editMenu, "Fancy Paste", insertAt: 5);
                flyout.ToolTip = "More paste options. Inspired by Grasshopper v2's \"Paste Exotic\". Brought to you by monocle™️.";

                MenuBuilder.AddItem(flyout, "Paste Without Wires",
                    () => model.FancyPaste("PasteWithoutWires"), ctx, inputGestureText: "Ctrl + Shift + V");
            }

            _shortcuts = new ShortcutRegistrar(ctx);
            _shortcuts.Add("PasteWithoutWiresCommand", "Paste Without Wires", Key.V,
                ModifierKeys.Control | ModifierKeys.Shift, () => model.FancyPaste("PasteWithoutWires"));
        }

        public void Dispose() => _shortcuts?.Dispose();
    }
}
