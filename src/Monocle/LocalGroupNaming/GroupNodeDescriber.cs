using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Dynamo.Graph;
using Dynamo.Graph.Annotations;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Notes;

namespace MonocleViewExtension.LocalGroupNaming
{
    /// <summary>
    /// Describes each member of a group for the naming prompt. Plain nodes contribute
    /// their name; code blocks, Python nodes, string and file inputs, drop-downs, notes
    /// and nested groups add the detail that actually says what the group is for
    /// ("Categories: Doors", "String: "Mark"", "Note: renumber the sheets").
    /// Reflection is used for node types that live in assemblies Monocle does not
    /// reference directly, and every failure falls back to the plain node name.
    /// </summary>
    internal static class GroupNodeDescriber
    {
        private const string PythonNodeNamespace = "PythonNodeModels";
        private const string InputNodeNamespace = "CoreNodeModels.Input";
        private const string DropDownBaseTypeName = "DSDropDownBase";

        public static string Describe(ModelBase model)
        {
            if (model == null) return null;

            try
            {
                var note = model as NoteModel;
                if (note != null) return GroupNodeDescription.Compose("Note", note.Text);

                var nestedGroup = model as AnnotationModel;
                if (nestedGroup != null) return GroupNodeDescription.Compose("Group", nestedGroup.AnnotationText);

                var codeBlock = model as CodeBlockNodeModel;
                if (codeBlock != null)
                {
                    return GroupNodeDescription.Compose(codeBlock.Name, GroupNodeDescription.FirstMeaningfulLine(codeBlock.Code));
                }

                var node = model as NodeModel;
                if (node != null) return GroupNodeDescription.Compose(node.Name, DescribeDetail(node));
            }
            catch (Exception)
            {
                // A half-initialised node or a reflection surprise must never block group creation.
            }

            var fallback = model as NodeModel;
            return fallback == null ? null : GroupNodeDescription.Compose(fallback.Name, null);
        }

        private static string DescribeDetail(NodeModel node)
        {
            var type = node.GetType();
            var typeNamespace = type.Namespace ?? string.Empty;

            if (typeNamespace == PythonNodeNamespace)
            {
                return GroupNodeDescription.FirstMeaningfulLine(ReadStringProperty(node, "Script"));
            }

            if (typeNamespace == InputNodeNamespace)
            {
                var value = ReadStringProperty(node, "Value");
                switch (type.Name)
                {
                    case "StringInput":
                        return GroupNodeDescription.Quote(value);
                    case "Filename":
                    case "Directory":
                        return string.IsNullOrWhiteSpace(value) ? null : SafeFileName(value);
                    default:
                        return null;
                }
            }

            return IsDropDown(type) ? ReadSelectedDropDownItem(node) : null;
        }

        private static bool IsDropDown(Type type)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                if (current.Name == DropDownBaseTypeName) return true;
            }

            return false;
        }

        private static string ReadSelectedDropDownItem(NodeModel node)
        {
            var items = ReadProperty(node, "Items") as IList;
            var selectedIndex = ReadProperty(node, "SelectedIndex") as int?;
            if (items == null || !selectedIndex.HasValue || selectedIndex.Value < 0 || selectedIndex.Value >= items.Count)
            {
                return null;
            }

            var item = items[selectedIndex.Value];
            return item == null ? null : ReadProperty(item, "Name") as string;
        }

        private static string ReadStringProperty(object target, string propertyName)
        {
            return ReadProperty(target, propertyName) as string;
        }

        private static object ReadProperty(object target, string propertyName)
        {
            var property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            return property == null || !property.CanRead ? null : property.GetValue(target);
        }

        private static string SafeFileName(string path)
        {
            try
            {
                var fileName = Path.GetFileName(path.Trim());
                return string.IsNullOrWhiteSpace(fileName) ? path.Trim() : fileName;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
