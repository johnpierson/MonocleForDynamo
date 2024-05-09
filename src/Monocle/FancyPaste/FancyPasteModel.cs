using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Dynamo.Controls;
using Dynamo.Graph.Connectors;
using Dynamo.Graph.Nodes;
using Dynamo.Models;
using Dynamo.Utilities;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using MonocleViewExtension.Foca;
using MonocleViewExtension.Utilities;
using Xceed.Wpf.AvalonDock.Controls;
using DragEventArgs = System.Windows.DragEventArgs;
using ModifierKeys = System.Windows.Input.ModifierKeys;

namespace MonocleViewExtension.FancyPaste
{
    internal class FancyPasteModel
    {
        public DynamoView dynamoView { get; }
        public DynamoViewModel dynamoViewModel { get; }
        public ViewLoadedParams LoadedParams { get; }

        public FancyPasteModel(DynamoViewModel dvm, ViewLoadedParams loadedParams)
        {
            dynamoView = loadedParams.DynamoWindow as DynamoView;
            dynamoViewModel = dvm;
            LoadedParams = loadedParams;
        }


    
        public void FancyPaste(string command)
        {
            var clipBoard = dynamoViewModel.Model.ClipBoard;

            if (!clipBoard.Any())
            {
                return;
            }
            switch (command)
            {
                case "PasteWithoutWires":
                    clipBoard.RemoveAll(n => n is ConnectorModel);
                    dynamoViewModel.Model.Paste();
                    break;
            }
        }
    }
}
