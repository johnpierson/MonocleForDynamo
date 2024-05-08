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

            dynamoView.MouseLeftButtonUp += DgOnMouseLeftButtonUp;
        }


        #region StuffToMoveToOwnTool
        //This section would not be made possible without Konrad Sobon's awesome example here:https://github.com/ksobon/archilab/blob/master/archilabViewExtension/ArchilabViewExtension.cs
        private void DgOnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!Keyboard.IsKeyDown(Key.LeftAlt)) return;
            
            var pt = e.GetPosition((UIElement)sender);
            HitResultsList.Clear();

            var nodeViews = MiscUtils.FindVisualChildren<NodeView>(dynamoView);

            var selectedNodes = nodeViews.Where(n => n.ViewModel.IsSelected).ToList();

            if (!selectedNodes.Any()) return;
            
            var node = selectedNodes.FirstOrDefault();
            
            var selectedNode = dynamoViewModel.CurrentSpaceViewModel.Nodes.First(n => n.IsSelected);

            var rect = GetBoundingRectangle(selectedNodes);

            var l1 = new EllipseGeometry(pt,rect.Width, rect.Height);
           
            GeometryHitTestParameters geoHit = new GeometryHitTestParameters(l1);
            
            VisualTreeHelper.HitTest(LoadedParams.DynamoWindow, null, MyHitTestResult, geoHit);
            if (HitResultsList.Count <= 0) return;

            // Check if Wire/Connector was hit. Kudos to Konrad for this code
            var path = HitResultsList.FirstOrDefault(x => x is Path p && p.DataContext is ConnectorViewModel);
            if (path == null) return;

            var connPath = (Path)path;
           

            var dataContext = (ConnectorViewModel)connPath.DataContext;

            //check if the center of the connector is within the node's rectangle. We only want to do this if people are really trying to connect this thing
            var startNodeCenter = dataContext.ConnectorModel.Start.Center;
            var endNodeCenter = dataContext.ConnectorModel.End.Center;
            var lineCenter = new Point((startNodeCenter.X + endNodeCenter.X) / 2,
                (startNodeCenter.Y + endNodeCenter.Y) / 2);

            if (!rect.Contains(lineCenter)) return;


            var start = dataContext.ConnectorModel.Start;
            var end = dataContext.ConnectorModel.End;

            // (John) connect the input of the relay
            dynamoViewModel?.ExecuteCommand(
                new DynamoModel.MakeConnectionCommand(start.Owner.GUID, start.Index, PortType.Output,
                    DynamoModel.MakeConnectionCommand.Mode.Begin));
            dynamoViewModel?.ExecuteCommand(
                new DynamoModel.MakeConnectionCommand(selectedNode.NodeModel.GUID, 0, PortType.Input, DynamoModel.MakeConnectionCommand.Mode.End));

            // (John) connect the output of the relay
            dynamoViewModel?.ExecuteCommand(
                new DynamoModel.MakeConnectionCommand(selectedNode.NodeModel.GUID, 0, PortType.Output, DynamoModel.MakeConnectionCommand.Mode.Begin));
            dynamoViewModel?.ExecuteCommand(
                new DynamoModel.MakeConnectionCommand(end.Owner.GUID, end.Index, PortType.Input, DynamoModel.MakeConnectionCommand.Mode.End));


        }

        internal List<DependencyObject> HitResultsList = new List<DependencyObject>();
        public HitTestResultBehavior MyHitTestResult(HitTestResult result)
        {
            HitResultsList.Add(result.VisualHit);
            return HitTestResultBehavior.Continue;
        }

        internal Rect GetBoundingRectangle(List<NodeView> nodeViews)
        {
            List<double> yAxis = new List<double>();
            List<double> xAxis = new List<double>();

            foreach (var nv in nodeViews)
            {
                NodeViewModel nvm = nv.DataContext as NodeViewModel;

                if (Globals.DynamoVersion.CompareTo(Globals.NewUiVersion) >= 0)
                {
                    var border = (Border)nv.FindName("selectionBorder");
                    var geo = border.RenderSize;

                    xAxis.Add(nvm.Left);
                    xAxis.Add(nvm.Left + geo.Width);
                    yAxis.Add(nvm.Top);
                    yAxis.Add(nvm.Top + geo.Height);
                }
                else
                {
                    var border = (System.Windows.Shapes.Rectangle)nv.FindName("selectionBorder");
                    var geo = border.RenderedGeometry.Bounds;

                    xAxis.Add(nvm.Left);
                    xAxis.Add(nvm.Left + geo.Width);
                    yAxis.Add(nvm.Top);
                    yAxis.Add(nvm.Top + geo.Height);
                }
            }

            double xMin = xAxis.Min() - 14;
            double yMin = yAxis.Min() - 14;
            double xMax = xAxis.Max() + 14;
            double yMax = yAxis.Max() + 14;
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

#endregion
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
