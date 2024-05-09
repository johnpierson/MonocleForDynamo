using Dynamo.Controls;
using Dynamo.Graph.Nodes;
using Dynamo.Models;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using MonocleViewExtension.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using Path = System.Windows.Shapes.Path;

namespace MonocleViewExtension.InlineNodeConnectomatic
{
    internal class InlineNodeConnectomaticModel
    {
        public DynamoView dynamoView { get; }
        public DynamoViewModel dynamoViewModel { get; }
        public ViewLoadedParams LoadedParams { get; }

        public InlineNodeConnectomaticModel(DynamoViewModel dvm, ViewLoadedParams loadedParams)
        {
            dynamoView = loadedParams.DynamoWindow as DynamoView;
            dynamoViewModel = dvm;
            LoadedParams = loadedParams;

            dynamoView.MouseLeftButtonUp += DgOnMouseLeftButtonUp;
        }

        //This section would not be made possible without Konrad Sobon's awesome example here:https://github.com/ksobon/archilab/blob/master/archilabViewExtension/ArchilabViewExtension.cs
        private void DgOnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!Globals.IsConnectoEnabled) return;

            try
            {
                var pt = e.GetPosition((UIElement)sender);
                HitResultsList.Clear();

                var nodeViews = MiscUtils.FindVisualChildren<NodeView>(dynamoView);

                var selectedNodes = nodeViews.Where(n => n.ViewModel.IsSelected).ToList();

                if (!selectedNodes.Any()) return;

                var node = selectedNodes.FirstOrDefault();

                var selectedNode = dynamoViewModel.CurrentSpaceViewModel.Nodes.First(n => n.IsSelected);

                var rect = NodeUtils.GetBoundingRectangle(selectedNodes);

                var l1 = new EllipseGeometry(pt, rect.Width, rect.Height);

                GeometryHitTestParameters geoHit = new GeometryHitTestParameters(l1);

                VisualTreeHelper.HitTest(LoadedParams.DynamoWindow, null, MyHitTestResult, geoHit);
                if (HitResultsList.Count <= 0) return;

                // Check if Wire/Connector was hit. Kudos to Konrad for this code
                var paths = HitResultsList.Where(x => x is Path p && p.DataContext is ConnectorViewModel).ToList();

                if (paths == null) return;

                Path connPath = null;

                //check if any of the paths are ones already connected to the dragged node. if so, skip those.
                foreach (Path p in paths)
                {
                    var cvm = p.DataContext as ConnectorViewModel;

                    if (cvm.ConnectorModel.Start.Owner.GUID.Equals(selectedNode.NodeModel.GUID) || cvm.ConnectorModel.End.Owner.GUID.Equals(selectedNode.NodeModel.GUID))
                    {
                        continue;
                    }

                    connPath = p; break;
                }

                if(connPath == null) return;

                var dataContext = (ConnectorViewModel)connPath.DataContext;

                //check if the center of the connector is within the node's rectangle. We only want to do this if people are really trying to connect this thing
                var startNodeCenter = dataContext.ConnectorModel.Start.Center;
                var endNodeCenter = dataContext.ConnectorModel.End.Center;
                var lineCenter = new Point((startNodeCenter.X + endNodeCenter.X) / 2,
                    (startNodeCenter.Y + endNodeCenter.Y) / 2);

                if (!rect.Contains(lineCenter)) return;

                var start = dataContext.ConnectorModel.Start;
                var end = dataContext.ConnectorModel.End;

                // (John) connect the input of the node
                dynamoViewModel?.ExecuteCommand(
                    new DynamoModel.MakeConnectionCommand(start.Owner.GUID, start.Index, PortType.Output,
                        DynamoModel.MakeConnectionCommand.Mode.Begin));
                dynamoViewModel?.ExecuteCommand(
                    new DynamoModel.MakeConnectionCommand(selectedNode.NodeModel.GUID, 0, PortType.Input, DynamoModel.MakeConnectionCommand.Mode.End));

                // (John) connect the output of the node
                dynamoViewModel?.ExecuteCommand(
                    new DynamoModel.MakeConnectionCommand(selectedNode.NodeModel.GUID, 0, PortType.Output, DynamoModel.MakeConnectionCommand.Mode.Begin));
                dynamoViewModel?.ExecuteCommand(
                    new DynamoModel.MakeConnectionCommand(end.Owner.GUID, end.Index, PortType.Input, DynamoModel.MakeConnectionCommand.Mode.End));
            }
            catch (System.Exception)
            {
                // do nothin
            }
            
        }

        internal List<DependencyObject> HitResultsList = new List<DependencyObject>();
        public HitTestResultBehavior MyHitTestResult(HitTestResult result)
        {
            HitResultsList.Add(result.VisualHit);
            return HitTestResultBehavior.Continue;
        }

        
    }
}
