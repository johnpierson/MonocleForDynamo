using System;
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
            if (!(this.DataContext is SimpleSearchViewModel svm)) return;
            if (this.Nodes.SelectedItems.Count == 0) return;
            if (!(this.Nodes.SelectedItems[0] is NodeSearchElement nse)) return;

            PlaceNode(svm.dynamoViewModel, nse);
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
            var nM = NodeModelFactory.Construct(nse);
            dvm.ExecuteCommand(new DynamoModel.CreateNodeCommand(nM, 0, 0, true, false));

            if (SimpleSearchFeature.SimpleSearchPopup != null)
            {
                SimpleSearchFeature.SimpleSearchPopup.IsOpen = false;
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
            if (!(this.DataContext is SimpleSearchViewModel svm)) return;

            /* AddedItems is a list. Casting the list itself to NodeSearchElement always produced
               null, so SelectedNode was never set and pressing Enter always placed the first
               result rather than the one the arrow keys had highlighted. */
            svm.SelectedNode = e.AddedItems.Count > 0 ? e.AddedItems[0] as NodeSearchElement : null;
        }

       
    }
}
