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
            MonocleContext ctx, string toolTip = null, string inputGestureText = null)
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

            parent.Items.Add(item);
            return item;
        }

        public static MenuItem AddFlyout(ItemsControl parent, object header, int? insertAt = null)
        {
            var flyout = new MenuItem { Header = header };

            if (insertAt.HasValue)
            {
                parent.Items.Insert(insertAt.Value, flyout);
            }
            else
            {
                parent.Items.Add(flyout);
            }

            return flyout;
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
