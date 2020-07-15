using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Workspaces;
using Dynamo.Models;
using Dynamo.Search.SearchElements;
using Dynamo.Wpf.Extensions;
using MonocleViewExtension.PackageUsageDoge;
using Utilities = MonocleViewExtension.PackageUsageDoge.Utilities;

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
            PackList();
            //_searchTimer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(1)};
            //_searchTimer.Tick += SearchTimerOnTick;
        }

        private void SearchTimerOnTick(object sender, EventArgs e)
        {
            _searchTimer.Stop();
            this.ResultsList.ItemsSource = SearchAndDisplayResults(this.SearchTextBox.Text.SimplifyString());
        }

        private void PackList()
        {
            NodesToSearch.Clear();
            NodesToSearch.AddRange(MonocleViewExtension.dynView.Model.SearchModel.SearchEntries.Where(n => n.IsVisibleInSearch));

            //generate the non custom list
            NodesToSearchOOTB.Clear();
            foreach (var n in NodesToSearch)
            {
                string elemType = n.ElementType.ToString();
                bool addToList = elemType.Contains("Packaged") || elemType.Contains("CustomNode");
                if (!addToList)
                {
                    NodesToSearchOOTB.Add(n);
                }
            }
            
            //this.ResultsList.ItemsSource = NodesToSearch;
        }

        
        /// <summary>
        /// Dispose function for WorkspaceDependencyView
        /// </summary>
        public void Dispose()
        {
            //loadedParams.CurrentWorkspaceChanged -= OnWorkspaceChanged;
            //loadedParams.CurrentWorkspaceCleared -= OnWorkspaceCleared;

            //DynamoView.CloseExtension -= this.OnExtensionTabClosedHandler;
            //HomeWorkspaceModel.WorkspaceClosed -= this.CloseExtensionTab;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextSearchBoxPlaceholder.Visibility = Visibility.Hidden;
            //_searchTimer.Stop();
            if (string.IsNullOrWhiteSpace(this.SearchTextBox.Text))
            {
                return;
            }
            //_searchTimer.Start();
            this.ResultsList.ItemsSource = SearchAndDisplayResults(this.SearchTextBox.Text.SimplifyString());
            //CollectionViewSource.GetDefaultView(this.ResultsList.ItemsSource).Filter = UserFilter;
            //CollectionViewSource.GetDefaultView(this.ResultsList.ItemsSource).DeferRefresh();
        }

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

        private bool UserFilter(object item)
        {
            if (String.IsNullOrEmpty(SearchTextBox.Text.SimplifyString()))
                return false;
            string textToSearch = SearchTextBox.Text.SimplifyString();
            var nse = (NodeSearchElement)item;

            return SetSearchName(nse).Contains(textToSearch);
        }


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
    }
}
