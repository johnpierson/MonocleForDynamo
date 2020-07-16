using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Threading;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Workspaces;
using Dynamo.Models;
using Dynamo.Search.SearchElements;
using Dynamo.Wpf.Extensions;
using MonocleViewExtension.PackageUsage;
using MonocleViewExtension.Utilities;

namespace MonocleViewExtension.SimpleSearch
{
    /// <summary>
    /// Interaction logic for SimpleSearch.xaml
    /// </summary>
    public partial class SimpleSearch : UserControl, IDisposable
    {
        public static TextBox _searchTextBox;
        private static int _searchCriteriaFlag = 0;
        private WorkspaceModel currentWorkspace;
        private static bool _excludeCustomNodes = false;
        private static List<NodeSearchElement> NodesToSearch = new List<NodeSearchElement>();
        private static List<NodeSearchElement> NodesToSearchOOTB = new List<NodeSearchElement>();
        HashSet<string> hs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private ViewLoadedParams loadedParams;
        private DispatcherTimer _searchTimer;

        public SimpleSearch(ViewLoadedParams p)
        {
            InitializeComponent();
            this.DataContext = this;
            currentWorkspace = p.CurrentWorkspaceModel as WorkspaceModel;
            loadedParams = p;
            _searchTextBox = SearchTextBox;
            PackLists();
        }
        /// <summary>
        /// This builds our search list upfront so I don't have to do it later.
        /// </summary>
        private void PackLists()
        {
            //generate the custom list to search
            NodesToSearch.Clear();
            NodesToSearch.AddRange(MonocleViewExtension.dynView.Model.SearchModel.SearchEntries.Where(n => n.IsVisibleInSearch));
            //generate the non custom list to search
            NodesToSearchOOTB.Clear();
            NodesToSearchOOTB.AddRange(NodesToSearch.Where(n => !n.ElementType.ToString().Contains("Packaged") && !n.ElementType.ToString().Contains("CustomNode")));       
        }

        
        /// <summary>
        /// Dispose function for Simple Search
        /// </summary>
        public void Dispose()
        {
            //loadedParams.CurrentWorkspaceChanged -= OnWorkspaceChanged;
            //loadedParams.CurrentWorkspaceCleared -= OnWorkspaceCleared;

            //DynamoView.CloseExtension -= this.OnExtensionTabClosedHandler;
            //HomeWorkspaceModel.WorkspaceClosed -= this.CloseExtensionTab;
        }
        /// <summary>
        /// Refresh the list on textbox changed
        /// </summary>
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextSearchBoxPlaceholder.Visibility = Visibility.Hidden;
            if (string.IsNullOrWhiteSpace(this.SearchTextBox.Text))
            {
                return;
            }
            this.ResultsList.ItemsSource = SearchAndDisplayResults(this.SearchTextBox.Text.SimplifyString());
        }
        /// <summary>
        /// Do the search stuff n thangs
        /// </summary>
        private static List<NodeSearchElement> SearchAndDisplayResults(string searchString)
        {
            if (_excludeCustomNodes)
            {
                return NodesToSearchOOTB.Where(n => SetSearchName(n).Contains(searchString))
                    .OrderBy(n => StringComparisonUtilities.Compute(searchString, SetSearchName(n))).ToList();
            }
            //we reach this for all other searches
            return NodesToSearch.Where(n => SetSearchName(n).Contains(searchString))
                .OrderBy(n => StringComparisonUtilities.Compute(searchString, SetSearchName(n))).ToList();

        }
        /// <summary>
        /// This builds the string to search by based on the user selections
        /// </summary>
        private static string SetSearchName(NodeSearchElement nsm)
        {
            string returnString = (nsm.Name).SimplifyString();

            switch (_searchCriteriaFlag)
            {
                case 1:
                    returnString = (nsm.Name).SimplifyString();
                    break;
                case 2:
                    returnString = nsm.Description.SimplifyString();
                    break;
                case 3:
                    returnString = (nsm.Name + nsm.Description).SimplifyString();
                    break;
                case 4:
                    returnString = (nsm.FullName).SimplifyString();
                    break;
                case 5:
                    returnString = (nsm.Name + nsm.FullName).SimplifyString();
                    break;
                case 6:
                    returnString = (nsm.FullName + nsm.Description).SimplifyString();
                    break;
                case 7:
                    returnString = (nsm.Name + nsm.FullName + nsm.Description).SimplifyString();
                    break;
            }

            return returnString;
        }
        /// <summary>
        /// Try to place a node because why else are we searching
        /// </summary>
        private void PlaceNode(object sender, MouseButtonEventArgs e)
        {
            if (!(ResultsList.SelectedItem is NodeSearchElement simpleNode))
            {
                return;
            }

            try
            {
                var dynMethod = simpleNode.GetType().GetMethod("ConstructNewNodeModel",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var obj = dynMethod.Invoke(simpleNode, new object[] { });
                var nM = obj as NodeModel;

                MonocleViewExtension.dynView.ExecuteCommand(new DynamoModel.CreateNodeCommand(nM, 0, 0, true, false));
            }
            catch (Exception)
            {
                //do nothing
            }
        }

        private void OnChecked(object sender, RoutedEventArgs e)
        {
            CheckBox cBox = sender as CheckBox;
            switch (cBox.Name)
            {
                case "NodeNameCheckBox":
                    _searchCriteriaFlag = _searchCriteriaFlag + 1;
                    break;
                case "NodeDescriptionCheckBox":
                    _searchCriteriaFlag = _searchCriteriaFlag + 2;
                    break;
                case "NodeCategoryCheckBox":
                    _searchCriteriaFlag = _searchCriteriaFlag + 4;
                    break;
                case "ExcludeCustomPackagesCheckBox":
                    _excludeCustomNodes = true;
                    break;
            }

            RefreshSearch();
        }

        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cBox = sender as CheckBox;
            switch (cBox.Name)
            {
                case "NodeNameCheckBox":
                    _searchCriteriaFlag = _searchCriteriaFlag - 1;
                    break;
                case "NodeDescriptionCheckBox":
                    _searchCriteriaFlag = _searchCriteriaFlag - 2;
                    break;
                case "NodeCategoryCheckBox":
                    _searchCriteriaFlag = _searchCriteriaFlag - 4;
                    break;
                case "ExcludeCustomPackagesCheckBox":
                    _excludeCustomNodes = false;
                    break;
            }

            RefreshSearch();
        }
        /// <summary>
        /// Adds the enter command to search.
        /// </summary>
        private void SearchBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!(ResultsList.Items[0] is NodeSearchElement simpleNode))
                {
                    return;
                }
                try
                {
                    var dynMethod = simpleNode.GetType().GetMethod("ConstructNewNodeModel",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    var obj = dynMethod.Invoke(simpleNode, new object[] { });
                    var nM = obj as NodeModel;
                    MonocleViewExtension.dynView.ExecuteCommand(new DynamoModel.CreateNodeCommand(nM, 0, 0, true, false));
                }
                catch (Exception)
                {
                    //do nothing
                }
            }
        }
        /// <summary>
        /// Janky way to refresh the search
        /// </summary>
        private void RefreshSearch()
        {
            try
            {
                if (TextSearchBoxPlaceholder.Visibility != Visibility.Visible)
                {
                    this.SearchTextBox.Text = " " + this.SearchTextBox.Text;
                    this.SearchTextBox.Text = this.SearchTextBox.Text.Remove(0, 1);
                }
            }
            catch (Exception)
            {
                //suppress
            }
        }
        //this section is for sorting stuff
        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;
        private void ResultsList_OnClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader headerClicked)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    ListSortDirection direction;
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    Sort("Name", direction);
                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }
        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView = ResultsList.Items;
            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();

        }

        private void EventSetter_OnHandler(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader columnHeader = sender as GridViewColumnHeader;
            Border HeaderBorder = columnHeader.Template.FindName("HeaderBorder", columnHeader) as Border;
            if (HeaderBorder != null)
            {
                HeaderBorder.Background = HeaderBorder.Background;
            }
            Border HeaderHoverBorder = columnHeader.Template.FindName("HeaderHoverBorder", columnHeader) as Border;
            if (HeaderHoverBorder != null)
            {
                HeaderHoverBorder.BorderBrush = HeaderHoverBorder.BorderBrush;
            }
            Rectangle UpperHighlight = columnHeader.Template.FindName("UpperHighlight", columnHeader) as Rectangle;
            if (UpperHighlight != null)
            {
                UpperHighlight.Visibility = UpperHighlight.Visibility;
            }
            Thumb PART_HeaderGripper = columnHeader.Template.FindName("PART_HeaderGripper", columnHeader) as Thumb;
            if (PART_HeaderGripper != null)
            {
                PART_HeaderGripper.Background = PART_HeaderGripper.Background;
            }
        }
    }
}
