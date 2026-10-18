using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace MonocleViewExtension.Utilities
{
    /// <summary>
    /// Monocle's own visual-tree helpers. These replace the Xceed.Wpf.AvalonDock extension methods so
    /// Monocle no longer needs (or embeds) any third-party UI assembly.
    /// </summary>
    internal static class VisualTreeExtensions
    {
        /// <summary>
        /// Depth-first search for descendants of type <typeparamref name="T"/>. Iterative (no per-level iterator
        /// allocations). When <paramref name="descendIntoMatches"/> is false, the search does not walk inside a match,
        /// which is much cheaper for things like NodeView whose internals are never what we are looking for.
        /// </summary>
        internal static IEnumerable<T> FindVisualChildren<T>(this DependencyObject root, bool descendIntoMatches = true) where T : DependencyObject
        {
            if (root == null) yield break;

            var stack = new Stack<DependencyObject>();
            PushChildren(stack, root);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current is T match)
                {
                    yield return match;
                    if (!descendIntoMatches) continue;
                }

                PushChildren(stack, current);
            }
        }

        internal static T FindVisualChild<T>(this DependencyObject root, Func<T, bool> predicate = null) where T : DependencyObject
        {
            foreach (var child in root.FindVisualChildren<T>())
            {
                if (predicate == null || predicate(child)) return child;
            }

            return null;
        }

        internal static T FindVisualAncestor<T>(this DependencyObject element) where T : DependencyObject
        {
            var current = element;
            while (current != null)
            {
                if (current is T match) return match;
                current = current is Visual || current is System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(current)
                    : LogicalTreeHelper.GetParent(current);
            }

            return null;
        }

        private static void PushChildren(Stack<DependencyObject> stack, DependencyObject parent)
        {
            if (!(parent is Visual) && !(parent is System.Windows.Media.Media3D.Visual3D)) return;

            var count = VisualTreeHelper.GetChildrenCount(parent);
            // push in reverse so children are visited in their natural order
            for (var i = count - 1; i >= 0; i--)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child != null) stack.Push(child);
            }
        }
    }
}
