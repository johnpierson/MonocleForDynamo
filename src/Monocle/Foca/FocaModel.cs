using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Dynamo.Controls;
using Dynamo.Graph;
using Dynamo.Graph.Connectors;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Notes;
using Dynamo.Models;
using Dynamo.Search.SearchElements;
using Dynamo.Utilities;
using Dynamo.ViewModels;
using Dynamo.Views;
using Dynamo.Wpf.Extensions;
using MonocleViewExtension.Utilities;
using ProtoCore.AST.ImperativeAST;
using Thickness = System.Windows.Thickness;

namespace MonocleViewExtension.Foca
{
    public class FocaModel
    {
        public DynamoView dynamoView { get; }
        public ViewLoadedParams LoadedParams { get; }
        public DynamoViewModel DynamoViewModel { get; }
        public FocaModel(ViewLoadedParams p)
        {
            dynamoView = p.DynamoWindow as DynamoView;
            LoadedParams = p;
            DynamoViewModel = p.DynamoWindow.DataContext as DynamoViewModel;
        }

        
       
        #region ToolboxCommands

        public void ToolBoxCommand(string command)
        {
            if (!DynamoViewModel.CurrentSpace.CurrentSelection.Any()) return;

            var nodes = DynamoViewModel.CurrentSpace.CurrentSelection.ToList();

            switch (command)
            {
                case "combinifier":
                    Combinify(nodes);
                    break;
                case "dropdownConverter":
                    foreach (var n in nodes)
                    {
                        try
                        {
                            ReplaceDropdown(n);
                        }
                        catch (Exception)
                        {
                            //this error is silenced
                        }

                    }

                    break;
                case "powList":
                    try
                    {
                        PowList(nodes);
                    }
                    catch (Exception)
                    {
                        //this error is silenced
                    }

                    break;
            }
        }
        public void ReplaceDropdown(NodeModel nodeModel)
        {
            if (nodeModel.NodeType != "ExtensionNode")
            {
                return;
            }

            var nodeViews = MiscUtils.FindVisualChildren<NodeView>(dynamoView);

            var nodeView = nodeViews.FirstOrDefault(n => n.ViewModel.Id.Equals(nodeModel.GUID));

            if (nodeView == null)
            {
                return;
            }

            string creationName = nodeView.ViewModel.OriginalName;

            //to build our code block and connect it
            CodeBlockNodeModel codeBlock = null;
            List<ConnectorModel> outports;
            try
            {
                outports = nodeModel.AllConnectors.ToList();
            }
            catch (Exception)
            {
                outports = null;
            }

            if (nodeModel.GetType().ToString().Contains("Revit") && !creationName.Equals("Categories") && !creationName.Equals("Element Types") && !creationName.Equals("Family Types"))
            {
                var data = nodeModel.CachedValue.Data;

                //return if it is not a Revit.Elements.Element
                if (data is null || !data.GetType().ToString().Contains("Revit.Elements")) return;
                try
                {
                    //find the assembly
                    Assembly sourceAssembly = Assembly.GetAssembly(data.GetType());

                    Type type = sourceAssembly.GetType("Revit.Elements.Element");
                    PropertyInfo[] props = type.GetProperties();
                    int elementId = 0;
                    foreach (var p in props)
                    {
                        if (p.Name.Equals("Id"))
                        {
                            elementId = Convert.ToInt32(p.GetValue(data));
                        }
                    }

                    codeBlock = new CodeBlockNodeModel($"//{nodeModel.CachedValue.StringData};Revit.Elements.ElementSelector.ByElementId({elementId});", 0, 0, DynamoViewModel.Model.LibraryServices, DynamoViewModel.Model.CurrentWorkspace.ElementResolver);
                }
                catch (Exception)
                {
                    return;
                }
            }

            else
            {

                var stringData = nodeModel.CachedValue.StringData;
                if (stringData.Equals("null"))
                {
                    return;
                }

                switch (creationName)
                {
                    case "Family Types":
                        codeBlock = new CodeBlockNodeModel($"Revit.Elements.FamilyType.ByName(\"{stringData.Split(':').Last().TrimStart(' ')}\")", 0, 0, DynamoViewModel.Model.LibraryServices, DynamoViewModel.Model.CurrentWorkspace.ElementResolver);
                        break;
                    case "Element Types":
                        codeBlock = new CodeBlockNodeModel($"DSCore.Types.FindTypeByNameInAssembly(\"{stringData.Split('.').Last()}\", \"RevitAPI\")", 0, 0, DynamoViewModel.Model.LibraryServices, DynamoViewModel.Model.CurrentWorkspace.ElementResolver);
                        break;
                    case "Element Classes":
                        codeBlock = new CodeBlockNodeModel($"DSCore.Types.FindTypeByNameInAssembly(\"{stringData.Split('.').Last()}\", \"RevitAPI\")", 0, 0, DynamoViewModel.Model.LibraryServices, DynamoViewModel.Model.CurrentWorkspace.ElementResolver);
                        break;
                    case "Categories":
                        dynamic thingy = nodeModel.CachedValue.Data;
                        var id = thingy.Id;
                        codeBlock = new CodeBlockNodeModel($"\"{stringData}\";\nRevit.Elements.Category.ById({id})", 0, 0, DynamoViewModel.Model.LibraryServices, DynamoViewModel.Model.CurrentWorkspace.ElementResolver);
                        break;
                    case "Unit Types":
                        codeBlock = new CodeBlockNodeModel($"DSCore.Types.FindTypeByNameInAssembly(\"{stringData.Split('.').Last()}\", \"DynamoUnits\")", 0, 0, DynamoViewModel.Model.LibraryServices, DynamoViewModel.Model.CurrentWorkspace.ElementResolver);
                        break;
                    default:
                        return;
                }
            }

            try
            {
                DynamoViewModel.Model.ExecuteCommand(
                    new DynamoModel.CreateNodeCommand(codeBlock, nodeModel.CenterX, nodeModel.CenterY, false, false));

                if (outports != null)
                {
                    foreach (var outport in outports)
                    {

                        //connect it
                        DynamoViewModel.ExecuteCommand(
                            new DynamoModel.MakeConnectionCommand(codeBlock.GUID, outport.Start.Index, PortType.Output, DynamoModel.MakeConnectionCommand.Mode.Begin));
                        DynamoViewModel.ExecuteCommand(
                            new DynamoModel.MakeConnectionCommand(outport.End.Owner.GUID, outport.End.Index, PortType.Input, DynamoModel.MakeConnectionCommand.Mode.End));
                    }
                }

                //delete the original node
                //DynamoViewModel.ExecuteCommand(new DynamoModel.DeleteModelCommand(nodeModel.GUID));
                codeBlock.Name = $"{nodeModel.Name} ⁽ᶜᵒⁿᵛᵉʳᵗᵉᵈ ᵈʳᵒᵖᵈᵒʷⁿ⁾";
            }
            catch (Exception)
            {
                //
            }

        }
        public void Combinify(List<NodeModel> nodeModel)
        {


            var listCreateSize = nodeModel.Select(n => n.OutPorts.Count).ToList().Sum();

            if (listCreateSize.Equals(0)) return;

            //create the list.create node
            string newGuid = Guid.NewGuid().ToString();
            var command = new DynamoModel.CreateNodeCommand(newGuid, "List.Create", nodeModel.Max(n => n.CenterX) + 120, nodeModel.Min(n => n.CenterY - n.Height / 2), false, false);
            DynamoViewModel.Model.ExecuteCommand(command);

            //iterate through and add the input ports
            for (int i = 1; i < listCreateSize; i++)
            {
                DynamoViewModel.Model.ExecuteCommand(new DynamoModel.ModelEventCommand(newGuid, "AddInPort"));
            }
            int overallListInput = 0;
            foreach (var node in nodeModel.OrderBy(n => n.CenterY))
            {

                for (int i = 0; i < node.OutPorts.Count; i++)
                {
                    //begin connection
                    DynamoViewModel.Model.ExecuteCommand(
                        new DynamoModel.MakeConnectionCommand(node.GUID, i, PortType.Output, DynamoModel.MakeConnectionCommand.Mode.Begin));

                    DynamoViewModel.Model.ExecuteCommand(
                        new DynamoModel.MakeConnectionCommand(newGuid, overallListInput, PortType.Input, DynamoModel.MakeConnectionCommand.Mode.End));

                    overallListInput++;
                }
            }
        }

        public void PowList(List<NodeModel> nodeModel)
        {
            foreach (var node in nodeModel)
            {
                if (node.OutPorts.Count > 1 || node.CachedValue is null)
                {
                    return;
                }

                var cachedValue = node.CachedValue;

                if (!cachedValue.IsCollection) return;

                var values = cachedValue.GetElements().ToList();
                var count = values.Count > 20 ? 20 : values.Count;

                string codeBlockString = string.Empty;

                for (int i = 0; i < count; i++)
                {
                    codeBlockString = codeBlockString + $"value[{i}];";
                }

                var codeBlock = new CodeBlockNodeModel(codeBlockString, 0, 0, DynamoViewModel.Model.LibraryServices, DynamoViewModel.Model.CurrentWorkspace.ElementResolver);

                DynamoViewModel.Model.ExecuteCommand(
                    new DynamoModel.CreateNodeCommand(codeBlock, node.CenterX + 50, node.CenterY, false, false));

                //connect it
                DynamoViewModel.Model.ExecuteCommand(
                    new DynamoModel.MakeConnectionCommand(node.GUID, 0, PortType.Output, DynamoModel.MakeConnectionCommand.Mode.Begin));
                DynamoViewModel.Model.ExecuteCommand(
                    new DynamoModel.MakeConnectionCommand(codeBlock.GUID, 0, PortType.Input, DynamoModel.MakeConnectionCommand.Mode.End));


                //rename code block
                codeBlock.Name = "💣";
            }
        }
        #endregion

        public bool ShowWidget()
        {
            int selectedGroupCount;
            try
            {
                selectedGroupCount = DynamoViewModel.CurrentSpaceViewModel.Annotations.Count(a => a.PreviewState.Equals(PreviewState.Selection));
            }
            catch (Exception)
            {
                selectedGroupCount = 0;
            }
            if (!DynamoViewModel.CurrentSpace.CurrentSelection.Any() || selectedGroupCount > 0)
            {
                return false;
            }
            return true;
        }

        public Rect WrapNodes()
        {
            var nodeViews = Globals.DynamoVersion.CompareTo(Globals.NewUiVersion) >= 0 ? FindVisualChildren<NodeView>(dynamoView).Where(nv => ((Border)nv.FindName("selectionBorder")).IsVisible).ToList() : FindVisualChildren<NodeView>(dynamoView).Where(nv => ((System.Windows.Shapes.Rectangle)nv.FindName("selectionBorder")).IsVisible).ToList();

            return GetBoundingRectangle(nodeViews);
        }

        public Canvas GetExpansionBay()
        {
            var nodeViews = Globals.DynamoVersion.CompareTo(Globals.NewUiVersion) >= 0 ? FindVisualChildren<NodeView>(dynamoView).Where(nv => ((Border)nv.FindName("selectionBorder")).IsVisible).ToList() : FindVisualChildren<NodeView>(dynamoView).Where(nv => ((System.Windows.Shapes.Rectangle)nv.FindName("selectionBorder")).IsVisible).ToList();


            var leftMostNode = nodeViews.OrderBy(nv => nv.ViewModel.Left).ThenBy(nv => nv.ViewModel.Top).First();
            var topMostNode = nodeViews.OrderBy(nv => nv.ViewModel.Top).First();

            //host it in the left most node's expansion bay so it can move and live it's life
            return FindVisualChildren<Canvas>(leftMostNode)
                .First(c => c.Name == "expansionBay");
        }

        public Thickness GetThickness()
        {
            var nodeViews = Globals.DynamoVersion.CompareTo(Globals.NewUiVersion) >= 0 ? FindVisualChildren<NodeView>(dynamoView).Where(nv => ((Border)nv.FindName("selectionBorder")).IsVisible).ToList() : FindVisualChildren<NodeView>(dynamoView).Where(nv => ((System.Windows.Shapes.Rectangle)nv.FindName("selectionBorder")).IsVisible).ToList();

            var leftMostNode = nodeViews.OrderBy(nv => nv.ViewModel.Left).ThenBy(nv => nv.ViewModel.Top).First();
            var topMostNode = nodeViews.OrderBy(nv => nv.ViewModel.Top).First();
            return new Thickness(-14,
                ((topMostNode.ViewModel.Top - leftMostNode.ViewModel.Top) - leftMostNode.ActualHeight - 6), 14,
                14);
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

        public void CreateGroup(string groupName)
        {
            Globals.MonocleGroupSettings.TryGetValue(groupName, out Settings.GroupSetting groupSetting);

            var colorToUse = (Color)ColorConverter.ConvertFromString(groupSetting.GroupColor);

            string groupText = groupSetting.GroupText;

            int fontSize = groupSetting.FontSize;

            if (DynamoViewModel.CurrentSpace.CurrentSelection.Any())
            {
                DynamoModel.CreateAnnotationCommand annotationCommand;
                var currentSelection = DynamoViewModel.CurrentSpace.CurrentSelection.First();
                string caseSwitch = currentSelection.GetType().Name;
                switch (caseSwitch)
                {
                    case "AnnotationModel":
                        AnnotationViewModel am = DynamoViewModel.CurrentSpaceViewModel.Annotations.First(a => a.AnnotationModel.GUID == currentSelection.GUID);
                        am.Background = colorToUse;
                        am.FontSize = fontSize;
                        break;
                    case "NodeModel":
                        annotationCommand =
                            new DynamoModel.CreateAnnotationCommand(Guid.NewGuid(), groupText,
                                currentSelection.CenterX, currentSelection.CenterY, false);
                        DynamoViewModel.Model.ExecuteCommand(annotationCommand);
                        DynamoViewModel.CurrentSpaceViewModel.Annotations.Last().FontSize = fontSize;
                        DynamoViewModel.CurrentSpaceViewModel.Annotations.Last().Background = colorToUse;
                        break;
                    default:
                        try
                        {
                            annotationCommand =
                                new DynamoModel.CreateAnnotationCommand(Guid.NewGuid(), groupText,
                                    currentSelection.CenterX, currentSelection.CenterY, false);
                            DynamoViewModel.Model.ExecuteCommand(annotationCommand);
                            DynamoViewModel.CurrentSpaceViewModel.Annotations.Last().FontSize = fontSize;
                            DynamoViewModel.CurrentSpaceViewModel.Annotations.Last().Background = colorToUse;
                        }
                        catch (Exception)
                        {
                            //silent fail
                        }
                        break;
                }
            }
            else //for notes
            {
                try
                {
                    var annotationCommand = new DynamoModel.CreateAnnotationCommand(Guid.NewGuid(), groupText,
                        0, 0, false);
                    DynamoViewModel.Model.ExecuteCommand(annotationCommand);
                    DynamoViewModel.CurrentSpaceViewModel.Annotations.Last().FontSize = 24;
                    DynamoViewModel.CurrentSpaceViewModel.Annotations.Last().Background = colorToUse;
                }
                catch (Exception)
                {
                    //silent fail
                }
            }

        }

        public void AlignSelected(string alignment)
        {
            var currentSelection = DynamoViewModel.CurrentSpace.CurrentSelection.Select(n => n.GUID.ToString()).ToList();
            currentSelection.AddRange(DynamoViewModel.CurrentSpace.Notes.Where(n => n.IsSelected).Select(n => n.GUID.ToString()).ToList());

            if (currentSelection.ToList().Count <= 1) return;

            List<SuperNode> superNodes = CurrentSelection();
            switch (alignment)
            {
                case "HorizontalCenter":
                    {
                        if (superNodes.Any())
                        {
                            var xAll = GetSelectionAverageX();

                            superNodes.ForEach((x) => { x.CenterX = xAll; });
                        }
                        else
                        {
                            DynamoViewModel.AlignSelected("HorizontalCenter");
                        }
                    }
                    break;
                case "HorizontalLeft":
                    {
                        if (superNodes.Any(s => s.ObjectType.ToString().Contains("Annotation") || s.ObjectType.ToString().Contains("Note")))
                        {
                            var xAll = GetSelectionMinX();

                            superNodes.ForEach((x) => { x.X = xAll; });
                        }
                        else
                        {
                            DynamoViewModel.AlignSelected("HorizontalLeft");
                        }

                    }
                    break;
                case "HorizontalRight":
                    {
                        if (superNodes.Any(s => s.ObjectType.ToString().Contains("Annotation") || s.ObjectType.ToString().Contains("Note")))
                        {
                            var xAll = GetSelectionMaxX();

                            var width = superNodes.OrderBy(s => s.X).Last().Width;

                            foreach (var s in superNodes)
                            {
                                if (s.X.Equals(xAll) && s.Width.Equals(width)) continue;

                                if (s.Width.Equals(width))
                                {
                                    s.X = xAll;
                                }
                                else
                                {
                                    s.X = xAll + (width - s.Width);
                                }
                            }


                            //uniqueSuperNodes.ForEach((x) => { x.X = xAll - x.Width; });
                        }
                        else
                        {
                            DynamoViewModel.AlignSelected("HorizontalRight");
                        }
                    }
                    break;
                case "VerticalCenter":
                    {
                        if (superNodes.Any())
                        {
                            var yAll = GetSelectionAverageY();

                            superNodes.ForEach((x) => { x.CenterY = yAll; });
                        }
                        else
                        {
                            DynamoViewModel.AlignSelected("VerticalCenter");
                        }
                    }
                    break;
                case "VerticalTop":
                    {
                        if (superNodes.Any(s => s.ObjectType.ToString().Contains("Annotation") || s.ObjectType.ToString().Contains("Note")))
                        {
                            var yAll = GetSelectionMinY();
                            superNodes.ForEach((x) => { x.Y = yAll; });
                        }
                        else
                        {
                            DynamoViewModel.AlignSelected("VerticalTop");
                        }
                    }
                    break;
                case "VerticalBottom":
                    {
                        if (superNodes.Any(s => s.ObjectType.ToString().Contains("Annotation") || s.ObjectType.ToString().Contains("Note")))
                        {
                            var yAll = GetSelectionMaxY();

                            var height = superNodes.OrderBy(s => s.Y).Last().Height;

                            foreach (var s in superNodes)
                            {
                                if (s.Y.Equals(yAll) && s.Height.Equals(height)) continue;

                                if (s.Height.Equals(height))
                                {
                                    s.Y = yAll;
                                }
                                else
                                {
                                    s.Y = yAll + (height - s.Height);
                                }
                            }

                            //uniqueSuperNodes.ForEach((x) => { x.Y = yAll - x.Height; });
                        }
                        else
                        {
                            DynamoViewModel.AlignSelected("VerticalBottom");
                        }
                    }
                    break;
                case "VerticalDistribute":
                    {
                        if (superNodes.Any())
                        {
                            var yMin = GetSelectionMinY();
                            var yMax = GetSelectionMaxY();
                            var spacing = 20.0;
                            var span = yMax - yMin;

                            var nodeHeightSum = GetSelectionHeight();
                            if (span > nodeHeightSum)
                            {
                                spacing = (span - nodeHeightSum)
                                          / (CurrentSelection().Count - 1);
                            }
                            var cursor = yMin;

                            List<SuperNode> sortedSuperNodes = superNodes.OrderBy(s => s.Y).ToList();

                            foreach (var superNode in sortedSuperNodes)
                            {
                                superNode.Y = cursor;
                                cursor += (superNode.Height) + spacing;
                            }
                        }
                        else
                        {
                            DynamoViewModel.AlignSelected("VerticalDistribute");
                        }
                    }
                    break;
                case "HorizontalDistribute":
                    {
                        if (superNodes.Any())
                        {
                            var xMin = GetSelectionMinX();
                            var xMax = GetSelectionMaxX();
                            var spacing = 20.0;
                            var span = xMax - xMin;


                            var nodeWidthSum = GetSelectionWidth();
                            if (span > nodeWidthSum)
                            {
                                spacing = (span - nodeWidthSum)
                                          / (CurrentSelection().Count - 1);
                            }
                            var cursor = xMin;

                            List<SuperNode> sortedSuperNodes = superNodes.OrderBy(s => s.X).ToList();

                            foreach (var superNode in sortedSuperNodes)
                            {
                                superNode.X = cursor;
                                cursor += (superNode.Width) + spacing;
                            }
                        }
                        else
                        {
                            DynamoViewModel.AlignSelected("HorizontalDistribute");
                        }
                    }
                    break;
            }
            //this updates the wire representation. ¯\_(ツ)_/¯
            try
            {
                if (superNodes.Any())
                {
                    DynamoViewModel.AlignSelected("HorizontalRight");
                    var undoCommand = new DynamoModel.UndoRedoCommand(DynamoModel.UndoRedoCommand.Operation.Undo);
                    DynamoViewModel.Model.ExecuteCommand(undoCommand);
                }

            }
            catch (Exception)
            {
                //do nothing
            }

            DynamoModel.SelectInRegionCommand cmd = new DynamoModel.SelectInRegionCommand(new Rect2D(), false);
            DynamoViewModel.ExecuteCommand(cmd);

        }

        public List<SuperNode> CurrentSelection()
        {
            List<SuperNode> wrappedNodes = new List<SuperNode>();

            var currentSelection = DynamoViewModel.CurrentSpace.CurrentSelection.Select(n => n.GUID.ToString()).ToList();
            currentSelection.AddRange(DynamoViewModel.CurrentSpace.Notes.Where(n => n.IsSelected).Select(n => n.GUID.ToString()).ToList());
            List<string> objectsInGroups = new List<string>();
            //iterate through groups to add to alignment command and add notes / nodes to the filter list
            foreach (var anno in DynamoViewModel.CurrentSpaceViewModel.Annotations)
            {
                if (anno.PreviewState.Equals(PreviewState.Selection))
                {
                    //add the group
                    wrappedNodes.Add(new SuperNode() { Object = anno, GUID = anno.AnnotationModel.GUID.ToString(), CenterX = anno.AnnotationModel.CenterX, CenterY = anno.AnnotationModel.CenterY, X = anno.AnnotationModel.X, Y = anno.AnnotationModel.Y });

                    foreach (var node in anno.Nodes)
                    {
                        if (currentSelection.Contains(node.GUID.ToString()))
                        {
                            objectsInGroups.Add(node.GUID.ToString());
                        }
                    }

                    //add notes in groups
                    foreach (var note in DynamoViewModel.CurrentSpaceViewModel.Notes)
                    {
                        if (anno.AnnotationModel.Rect.Contains(note.Model.Rect.TopLeft) || anno.AnnotationModel.Rect.Contains(note.Model.Rect.BottomLeft)
                            || anno.AnnotationModel.Rect.Contains(note.Model.Rect.BottomRight))
                        {
                            objectsInGroups.Add(note.Model.GUID.ToString());
                        }
                    }
                }
            }
            //add nodes and notes to list that are not in groups
            foreach (var nodeModel in DynamoViewModel.CurrentSpace.CurrentSelection)
            {
                if (!nodeModel.IsSelected) continue;
                if (!objectsInGroups.Contains(nodeModel.GUID.ToString()))
                {
                    wrappedNodes.Add(new SuperNode() { Object = nodeModel, GUID = nodeModel.GUID.ToString(), CenterX = nodeModel.CenterX, CenterY = nodeModel.CenterY, X = nodeModel.X, Y = nodeModel.Y });
                }
            }

            foreach (var noteModel in DynamoViewModel.CurrentSpace.Notes)
            {
                if (!noteModel.IsSelected) continue;
                if (!objectsInGroups.Contains(noteModel.GUID.ToString()))
                {
                    wrappedNodes.Add(new SuperNode() { Object = noteModel, GUID = noteModel.GUID.ToString(), CenterX = noteModel.CenterX, CenterY = noteModel.CenterY, X = noteModel.X, Y = noteModel.Y });
                }
            }

            //ensure unique selection
            List<SuperNode> uniqueSuperNodes = wrappedNodes.GroupBy(s => s.GUID.ToString())
                .Select(group => group.First()).ToList();

            return uniqueSuperNodes;
        }
        #region SelectionAverages
        public double GetSelectionAverageX()
        {
            List<double> values = CurrentSelection().Select(s => s.CenterX).ToList();

            return values.Average();
        }

        private void NodeOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public double GetSelectionAverageY()
        {
            List<double> values = CurrentSelection().Select(s => s.CenterY).ToList();

            return values.Average();
        }

        public double GetSelectionMinX()
        {
            List<double> values = CurrentSelection().Select(s => s.X).ToList();

            return values.Min();
        }

        public double GetSelectionMinY()
        {
            List<double> values = CurrentSelection().Select(s => s.Y).ToList();

            return values.Min();
        }

        public double GetSelectionMaxX()
        {
            List<double> values = CurrentSelection().Select(s => s.X).ToList();

            return values.Max();
        }

        public double GetSelectionMaxLeftX()
        {
            return DynamoViewModel.CurrentSpace.CurrentSelection.Where((x) => x is ILocatable)
                           .Cast<ILocatable>()
                           .Select((x) => x.X)
                           .Max();
        }

        public double GetSelectionMaxY()
        {
            List<double> values = CurrentSelection().Select(s => s.Y).ToList();

            return values.Max();
        }

        public double GetSelectionMaxTopY()
        {
            return DynamoViewModel.CurrentSpace.CurrentSelection.Where((x) => x is ILocatable)
                           .Cast<ILocatable>()
                           .Select((x) => x.Y)
                           .Max();
        }
        public double GetSelectionHeight()
        {
            List<double> values = CurrentSelection().Select(s => s.Height).ToList();

            return values.Sum();
        }
        public double GetSelectionWidth()
        {
            List<double> values = CurrentSelection().Select(s => s.Width).ToList();

            return values.Sum();
        }

        #endregion

        #region Helpers
        public IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
        #endregion


    }
}
