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

        public LocalGroupNamingRequest(
            Guid groupId,
            string originalTitle,
            IEnumerable<LocalGroupNodeSnapshot> nodes,
            long sessionGeneration)
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
        }
    }

    internal static class LocalGroupNamingState
    {
        public static bool ShouldApplySuggestion(
            LocalGroupNamingRequest request,
            bool sessionIsCurrent,
            Guid currentGroupId,
            string currentTitle,
            IEnumerable<LocalGroupNodeSnapshot> currentNodes)
        {
            if (request == null || !sessionIsCurrent || request.GroupId != currentGroupId)
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
