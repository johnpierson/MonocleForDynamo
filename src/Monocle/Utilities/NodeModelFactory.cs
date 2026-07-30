using System;
using System.Reflection;
using Dynamo.Graph.Nodes;
using Dynamo.Search.SearchElements;

namespace MonocleViewExtension.Utilities
{
    internal static class NodeModelFactory
    {
        /// <summary>
        /// Turns a search result into a placeable node.
        ///
        /// Dynamo keeps ConstructNewNodeModel non-public, so this reaches for it by name. That
        /// makes it the first thing to break when Dynamo renames it, which is exactly why it
        /// lives in one place and returns null (rather than throwing a NullReferenceException
        /// from inside a click handler) when the method is gone.
        /// </summary>
        public static NodeModel Construct(NodeSearchElement searchElement)
        {
            if (searchElement == null) return null;

            var constructor = searchElement.GetType().GetMethod("ConstructNewNodeModel",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (constructor == null)
            {
                throw new MissingMethodException(searchElement.GetType().FullName, "ConstructNewNodeModel");
            }

            return constructor.Invoke(searchElement, Array.Empty<object>()) as NodeModel;
        }
    }
}
