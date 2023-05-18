using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.DesignScript.Geometry;
using Dynamo.Controls;
using Dynamo.Graph;
using Dynamo.Graph.Annotations;
using Dynamo.Models;
using Dynamo.UI.Commands;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;

namespace MonocleViewExtension.NodeSwapper
{
    internal class NodeSwapperModel
    {
        public DynamoView dynamoView { get; }
        public DynamoViewModel dynamoViewModel { get; }
        public ViewLoadedParams LoadedParams { get; }
        public Dictionary<string, Autodesk.DesignScript.Geometry.Point> OriginalLocations { get; set; }
        public NodeSwapperModel(DynamoViewModel dvm, ViewLoadedParams loadedParams)
        {
            dynamoView = loadedParams.DynamoWindow as DynamoView;
            dynamoViewModel = dvm;
            LoadedParams = loadedParams;
        }

        public void SetRunStatus()
        {
            dynamoViewModel.HomeSpace.RunSettings.RunType = RunType.Manual;
        }

        public NodeViewModel Selection(string mode = "")
        {
            if (!dynamoViewModel.CurrentSpaceViewModel.HasSelection) return null;
            
            return dynamoViewModel.CurrentSpaceViewModel.Nodes.First(n => n.NodeModel.IsSelected);
        }
       
    }
}
