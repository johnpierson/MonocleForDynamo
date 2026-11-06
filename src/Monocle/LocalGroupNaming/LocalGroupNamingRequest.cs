using System;
using System.Collections.Generic;
using System.Linq;

namespace MonocleViewExtension.LocalGroupNaming
{
    internal sealed class LocalGroupNodeSnapshot
    {
        public Guid Id { get; }
        public string Name { get; }

        public LocalGroupNodeSnapshot(Guid id, string name)
        {
            Id = id;
            Name = name ?? string.Empty;
        }
    }

    internal sealed class LocalGroupNamingRequest
    {
        public Guid GroupId { get; }
        public string OriginalTitle { get; }
        public IReadOnlyList<LocalGroupNodeSnapshot> Nodes { get; }
        public IReadOnlyList<string> NodeNames { get; }
        public long SessionGeneration { get; }

        /// <summary>
        /// The workspace instance the group was created in, compared by reference. Group and
        /// node GUIDs survive saving and reopening a graph, so a reopened or copied workspace
        /// passes every GUID check; only the instance identity tells the two apart.
        /// </summary>
        public object Workspace { get; }

        public LocalGroupNamingRequest(
            Guid groupId,
            string originalTitle,
            IEnumerable<LocalGroupNodeSnapshot> nodes,
            long sessionGeneration,
            object workspace = null)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));

            var snapshots = nodes
                .Select(node => node ?? throw new ArgumentException("A node snapshot is required.", nameof(nodes)))
                .ToList();

            GroupId = groupId;
            OriginalTitle = originalTitle;
            Nodes = snapshots.AsReadOnly();
            NodeNames = snapshots.Select(node => node.Name).ToList().AsReadOnly();
            SessionGeneration = sessionGeneration;
            Workspace = workspace;
        }
    }

    internal static class LocalGroupNamingState
    {
        public static bool ShouldApplySuggestion(
            LocalGroupNamingRequest request,
            bool sessionIsCurrent,
            Guid currentGroupId,
            string currentTitle,
            IEnumerable<LocalGroupNodeSnapshot> currentNodes,
            object currentWorkspace = null)
        {
            if (request == null || !sessionIsCurrent || request.GroupId != currentGroupId)
            {
                return false;
            }

            if (request.Workspace != null && !ReferenceEquals(request.Workspace, currentWorkspace))
            {
                return false;
            }

            if (!string.Equals(request.OriginalTitle, currentTitle, StringComparison.Ordinal))
            {
                return false;
            }

            if (currentNodes == null) return false;

            var expectedNodes = request.Nodes.OrderBy(node => node.Id).ToList();
            var actualNodes = currentNodes.OrderBy(node => node.Id).ToList();
            if (expectedNodes.Count != actualNodes.Count) return false;

            for (var index = 0; index < expectedNodes.Count; index++)
            {
                if (expectedNodes[index].Id != actualNodes[index].Id ||
                    !string.Equals(expectedNodes[index].Name, actualNodes[index].Name, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
