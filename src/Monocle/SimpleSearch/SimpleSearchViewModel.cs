using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using Dynamo.Search.SearchElements;
using Dynamo.ViewModels;

namespace MonocleViewExtension.SimpleSearch
{
    internal class SimpleSearchViewModel : INotifyPropertyChanged
    {
        public DynamoViewModel dynamoViewModel;

        private ICollectionView nodes;
        private NodeSearchElement selectedNode;

        // Normalized search key per entry, computed once instead of on every keystroke for every entry.
        private Dictionary<NodeSearchElement, string> keys = new Dictionary<NodeSearchElement, string>();
        private int sourceCount = -1;
        private string normalizedFilter = string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        public SimpleSearchViewModel(DynamoViewModel dvm)
        {
            dynamoViewModel = dvm;
            EnsureFresh();
        }

        private IEnumerable<NodeSearchElement> SourceEntries
        {
            get
            {
#if net8 || net10
                return dynamoViewModel.Model.SearchModel.Entries;
#else
                return dynamoViewModel.Model.SearchModel.SearchEntries;
#endif
            }
        }

        /// <summary>
        /// Rebuild the (sorted, keyed) entry list only when the library changed size, e.g. a package was
        /// installed or loaded after Monocle started. Called when the search UI is shown.
        /// </summary>
        public void EnsureFresh()
        {
            var entries = SourceEntries.ToList();
            if (nodes != null && entries.Count == sourceCount) return;

            sourceCount = entries.Count;
            var visible = entries.Where(s => s.IsVisibleInSearch).OrderBy(n => n.Name).ToList();
            var newKeys = new Dictionary<NodeSearchElement, string>(visible.Count);
            foreach (var entry in visible)
            {
                if (!newKeys.ContainsKey(entry)) newKeys.Add(entry, SearchKey.Normalize(entry.FullName));
            }

            keys = newKeys;
            var view = new ListCollectionView(visible) { Filter = ContainsFilter };
            nodes = view;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Nodes)));
        }

        public ICollectionView Nodes
        {
            get
            {
                return this.nodes;
            }
        }

        public NodeSearchElement SelectedNode
        {
            get
            {
                return this.selectedNode;
            }
            set
            {
                if (this.selectedNode != value)
                {
                    this.selectedNode = value;
                }
            }
        }

        private string filter = "";

        public string Filter
        {
            get
            {
                return this.filter;
            }
            set
            {
                if (this.filter != value)
                {
                    this.filter = value;
                    this.normalizedFilter = SearchKey.Normalize(value);
                    this.Nodes.Refresh();
                }
            }
        }

        private bool ContainsFilter(object item)
        {
            return item is NodeSearchElement node &&
                   keys.TryGetValue(node, out var key) &&
                   SearchKey.Matches(key, normalizedFilter);
        }
    }
}
