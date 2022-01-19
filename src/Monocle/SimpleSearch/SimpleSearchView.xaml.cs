﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Dynamo.Graph.Nodes;
using Dynamo.Models;
using Dynamo.Search.SearchElements;
using Dynamo.Utilities;
using Dynamo.ViewModels;

namespace MonocleViewExtension.SimpleSearch
{
    /// <summary>
    /// Interaction logic for SimpleSearchView.xaml
    /// </summary>
    public partial class SimpleSearchView : UserControl
    {
        public SimpleSearchView(DynamoViewModel dvm)
        {
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
                var nse = svm.SelectedNode;

                PlaceNode(svm.dynamoViewModel,nse);
            }
            catch (Exception exception)
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
    }
}