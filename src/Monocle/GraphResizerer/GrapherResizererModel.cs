using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dynamo.Controls;
using Dynamo.Graph;
using Dynamo.Graph.Annotations;
using Dynamo.Models;
using Dynamo.UI.Commands;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;

namespace MonocleViewExtension.GraphResizerer
{
    internal class GrapherResizererModel
    {
        public DynamoView dynamoView { get; }
        public DynamoViewModel dynamoViewModel { get; }
        public ViewLoadedParams LoadedParams { get; }

        public GrapherResizererModel(DynamoViewModel dvm, ViewLoadedParams loadedParams)
        {
            dynamoView = loadedParams.DynamoWindow as DynamoView;
            dynamoViewModel = dvm;
            LoadedParams = loadedParams;
        }

        public int ResizeGraph(double xScaleFactor, double yScaleFactor)
        {
            int changeCount = 0;
            foreach (var nvm in dynamoViewModel.CurrentSpaceViewModel.Nodes)
            {
                nvm.X *= xScaleFactor;
                nvm.Y *= yScaleFactor;
                nvm.NodeModel.ReportPosition();
                changeCount++;
            }

            foreach (var noteViewModel in dynamoViewModel.CurrentSpaceViewModel.Notes)
            {
                noteViewModel.Left *= xScaleFactor;
                noteViewModel.Top *= yScaleFactor;
                changeCount++;
            }

            return changeCount;
        }
    }
}
