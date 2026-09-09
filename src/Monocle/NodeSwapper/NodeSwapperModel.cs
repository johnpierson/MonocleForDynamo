using System.Collections.Generic;
using System.Linq;
using Dynamo.Controls;
using Dynamo.Models;
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

            // A workspace selection can contain notes or groups without containing a node.
            // Treat that as no node so callers can cancel cleanly instead of throwing.
            return dynamoViewModel.CurrentSpaceViewModel.Nodes.FirstOrDefault(n => n.NodeModel.IsSelected);
        }
       
    }
}
