using System;
using System.Linq;
using System.Windows.Controls;

namespace MonocleViewExtension.Core
{
    /// <summary>
    /// Small helpers for building Monocle's menu entries and for finding the Dynamo menus we
    /// inject into.
    /// </summary>
    internal static class MenuBuilder
    {
        /// <summary>
        /// Adds a clickable item. The click handler is wrapped so a failing tool logs and stays
        /// out of Dynamo's way instead of throwing into the UI dispatcher.
        /// </summary>
        public static MenuItem AddItem(ItemsControl parent, object header, Action onClick,
            MonocleContext ctx, string toolTip = null, string inputGestureText = null,
            int? insertAt = null)
        {
            var item = new MenuItem { Header = header };

            if (toolTip != null) item.ToolTip = toolTip;
            if (inputGestureText != null) item.InputGestureText = inputGestureText;

            item.Click += (sender, args) =>
            {
                try
                {
                    onClick();
                }
                catch (Exception e)
                {
                    ctx.Log.Error($"'{header}' failed.", e);
                }
            };

            InsertOrAppend(parent, item, insertAt);
            return item;
        }

        public static MenuItem AddFlyout(ItemsControl parent, object header, int? insertAt = null)
        {
            var flyout = new MenuItem { Header = header };
            InsertOrAppend(parent, flyout, insertAt);
            return flyout;
        }

        /// <summary>
        /// Places an item at a position in one of Dynamo's menus, appending instead if that
        /// position is past the end.
        ///
        /// The positions we inject at are chosen to sit next to related Dynamo commands, but they
        /// are guesses about a menu we do not own: Dynamo can and does add and remove entries
        /// between versions. An out-of-range insert used to throw, and since registration is
        /// wrapped per feature, that made the whole tool disappear rather than merely land in the
        /// wrong place.
        /// </summary>
        public static void InsertOrAppend(ItemsControl parent, object item, int? insertAt)
        {
            if (insertAt.HasValue && insertAt.Value <= parent.Items.Count)
            {
                parent.Items.Insert(insertAt.Value, item);
            }
            else
            {
                parent.Items.Add(item);
            }
        }

        /// <summary>
        /// Finds one of Dynamo's own top-level menus by name (e.g. "fileMenu", "editMenu",
        /// "HelpMenu"). Returns null if this Dynamo version renamed or removed it.
        /// </summary>
        public static MenuItem FindDynamoMenu(MonocleContext ctx, string menuName) =>
            ctx.LoadedParams.dynamoMenu.Items.OfType<MenuItem>()
                .FirstOrDefault(m => menuName.Equals(m.Name, StringComparison.OrdinalIgnoreCase));
    }
}
