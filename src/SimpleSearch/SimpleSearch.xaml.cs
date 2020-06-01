using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Workspaces;
using Dynamo.Models;
using Dynamo.Search.SearchElements;
using Dynamo.Wpf.Extensions;
using MonocleViewExtension.PackageUsageDoge;

namespace MonocleViewExtension.SimpleSearch
{
    /// <summary>
    /// Interaction logic for SimpleSearch.xaml
    /// </summary>
    public partial class SimpleSearch : UserControl, IDisposable
    {
        public static TextBox _searchTextBox;
        private static int searchCriteriaFlag = 0;
        private WorkspaceModel currentWorkspace;


        private ViewLoadedParams loadedParams;
        //private SimpleSearch dependencyViewExtension;

        public SimpleSearch(ViewLoadedParams p)
        {
            InitializeComponent();
            this.DataContext = this;
            currentWorkspace = p.CurrentWorkspaceModel as WorkspaceModel;
            loadedParams = p;
            //grid view to add the dynamo info to the list
            GridView grid = new GridView();
            //column to contain simple node names
            GridViewColumn col0 = new GridViewColumn();
            col0.Width = 20;
            col0.DisplayMemberBinding = new System.Windows.Data.Binding("Weight");
            grid.Columns.Add(col0);
            GridViewColumn col1 = new GridViewColumn();
            col1.Width = 200;
            col1.Header = "Node Name";
            col1.DisplayMemberBinding = new System.Windows.Data.Binding("Name");
            grid.Columns.Add(col1);
            //column to contain full node names
            GridViewColumn col2 = new GridViewColumn();
            col2.Width = 200;
            col2.Header = "Node Full Name";
            col2.DisplayMemberBinding = new System.Windows.Data.Binding("FullCategoryName");
            grid.Columns.Add(col2);
            //bind the list view to the grid
            this.ResultsList.View = grid;
            _searchTextBox = SearchTextBox;
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
            string toSearch = this.SearchTextBox.Text;
            this.ResultsList.ItemsSource = SearchAndDisplayResults(toSearch);
        }

        private static List<NodeSearchElement> SearchAndDisplayResults(string searchString)
        {
            List<NodeSearchElement> results = new List<NodeSearchElement>();
            List<int> scores = new List<int>();
            foreach (NodeSearchElement nsm in MonocleViewExtension.dynView.Model.SearchModel.SearchEntries)
            {
                if (!nsm.IsVisibleInSearch)
                {
                    continue;
                }

                string searchableText = string.Empty;

                switch (searchCriteriaFlag)
                {
                    case 1:
                        searchableText = nsm.Name;
                        break;
                    case 2:
                        searchableText = nsm.Description;
                        break;
                    case 3:
                        searchableText = nsm.Name + nsm.Description;
                        break;
                    case 4:
                        searchableText = nsm.FullName;
                        break;
                    case 5:
                        searchableText = nsm.Name + nsm.FullName;
                        break;
                    case 6:
                        searchableText = nsm.FullName + nsm.Description;
                        break;
                    case 7:
                        searchableText = nsm.Name + nsm.FullName + nsm.Description;
                        break;
                }


                if (searchableText.SimplifyString().Contains(searchString.SimplifyString()))
                {
                    results.Add(nsm); 
                    nsm.Weight = StringComparisonUtilities.Compute(searchString.SimplifyString(),nsm.Name);
                   
                }
            }
            
            return results.OrderBy(r => r.Weight).ToList();
        }


        private void ResultsList_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            NodeSearchElement simpleNode = ResultsList.SelectedItem as NodeSearchElement;
            if (simpleNode == null)
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
                    searchCriteriaFlag = searchCriteriaFlag + 1;              
                    break;
                case "NodeDescriptionCheckBox":
                    searchCriteriaFlag = searchCriteriaFlag + 2;
                    break;
                case "NodeCategoryCheckBox":
                    searchCriteriaFlag = searchCriteriaFlag + 4;               
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
                    searchCriteriaFlag = searchCriteriaFlag - 1;
                    break;
                case "NodeDescriptionCheckBox":
                    searchCriteriaFlag = searchCriteriaFlag - 2;
                    break;
                case "NodeCategoryCheckBox":
                    searchCriteriaFlag = searchCriteriaFlag - 4;
                    break;
            }

            RefreshSearch();
        }
        private void SearchBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NodeSearchElement simpleNode = ResultsList.Items[0] as NodeSearchElement;
                if (simpleNode == null)
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
        

    }
}
