using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Dynamo.Graph.Nodes;
using Dynamo.Models;
using Dynamo.Search.SearchElements;
using Dynamo.ViewModels;
using MonocleViewExtension.Utilities;

namespace MonocleViewExtension.SimpleSearch
{
    /// <summary>
    /// Interaction logic for SimpleSearchView.xaml
    /// </summary>
    public partial class SimpleSearchView : UserControl
    {
        public SimpleSearchView(DynamoViewModel dvm)
        {
             //Compatibility.FixThemesForDevExpress(this);


            InitializeComponent();
            this.Loaded+= OnLoaded;

            this.DataContext = new SimpleSearchViewModel(dvm);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            RefreshSelection();
        }
        public void RefreshSelection()
        {
            this.Filter.Clear();
            this.Filter.Focus();
        }
        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var svm = this.DataContext as SimpleSearchViewModel;

            try
            {
                var nse = this.Nodes.SelectedItems[0] as NodeSearchElement;

                PlaceNode(svm.dynamoViewModel,nse);
            }
            catch (Exception)
            {
               //
            }
        }

        private void Filter_OnKeyDown(object sender, KeyEventArgs e)
        {
            var svm = this.DataContext as SimpleSearchViewModel;
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                if (svm.SelectedNode != null)
                {
                    PlaceNode(svm.dynamoViewModel, svm.SelectedNode);
                }
                else
                {
                    svm.Nodes.MoveCurrentToFirst();
                    var nse = svm.Nodes.CurrentItem as NodeSearchElement;
                    PlaceNode(svm.dynamoViewModel, nse);
                }

                this.Filter.Focus();
            }
        }

        private void PlaceNode(DynamoViewModel dvm, NodeSearchElement nse)
        {
            var dynMethod = nse.GetType().GetMethod("ConstructNewNodeModel",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var obj = dynMethod.Invoke(nse, new object[] { });
            var nM = obj as NodeModel;
            dvm.ExecuteCommand(new DynamoModel.CreateNodeCommand(nM, 0, 0, true, false));

            if (SimpleSearchCommand.SimpleSearchPopup != null)
            {
                SimpleSearchCommand.SimpleSearchPopup.IsOpen = false;
            }
        }

        private void Filter_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                this.Nodes.SelectedIndex++;
                //svm.Nodes.MoveCurrentToNext();
            }

            if (e.Key == Key.Up)
            {
                if (this.Nodes.SelectedIndex > 0)
                {
                    this.Nodes.SelectedIndex--;
                }
                
            }
        }

        private void Nodes_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var svm = this.DataContext as SimpleSearchViewModel;
                svm.SelectedNode = e.AddedItems as NodeSearchElement;
            }
            catch (Exception)
            {
                // suppress for now
            }
        }

       
    }
}
