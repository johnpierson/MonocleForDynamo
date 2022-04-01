using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dynamo.Controls;
using Dynamo.Graph.Connectors;
using Dynamo.Utilities;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;

namespace MonocleViewExtension.PasteExtravagant
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
