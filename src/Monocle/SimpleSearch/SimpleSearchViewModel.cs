using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using Dynamo.Search.SearchElements;
using Dynamo.ViewModels;

namespace MonocleViewExtension.SimpleSearch
{
    internal class SimpleSearchViewModel
    {
        public DynamoViewModel dynamoViewModel;

        private readonly ICollectionView nodes;
        private NodeSearchElement selectedNode;

        public SimpleSearchViewModel(DynamoViewModel dvm)
        {
            dynamoViewModel = dvm;
#if net8
            var myNodes = dvm.Model.SearchModel.Entries.Where(s => s.IsVisibleInSearch).OrderBy(n => n.Name);
#endif

#if !net8
              var myNodes = dvm.Model.SearchModel.SearchEntries.Where(s => s.IsVisibleInSearch).OrderBy(n => n.Name);
#endif
            
            this.nodes = CollectionViewSource.GetDefaultView(myNodes);

            //add the filter
            this.nodes.Filter = ContainsFilter;
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
                    this.Nodes.Refresh();
                }
            }
        }

        private bool ContainsFilter(object item)
        {
            if (!(item is NodeSearchElement node))
            {
                return false;
            }

            if (string.IsNullOrEmpty(this.Filter))
            {
                return false;
            }

            if (node.FullName.ToUpperInvariant().Replace(".","").Replace(" ","").Contains(this.Filter.ToUpperInvariant().Replace(".", "").Replace(" ", "")))
            {
                return true;
            }

            return false;
        }
    }
}
