using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Dynamo.Controls;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Notes;
using Dynamo.Logging;
using Dynamo.Models;
using Dynamo.Search.SearchElements;
using Dynamo.Utilities;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using MonocleViewExtension.Utilities;
using Border = System.Windows.Controls.Border;
using Thickness = System.Windows.Thickness;

namespace MonocleViewExtension.PackageUsage
{
    public class PackageUsageModel
    {
        public DynamoView DynamoView { get; }
        public DynamoViewModel DynamoViewModel { get; }
        public ViewLoadedParams LoadedParams { get; }

        public PackageUsageModel(DynamoViewModel dvm, ViewLoadedParams loadedParams)
        {
            DynamoView = loadedParams.DynamoWindow as DynamoView;
            DynamoViewModel = dvm;
            LoadedParams = loadedParams;
        }

        public ObservableCollection<PackageUsageWrapper> GetCustomNodeInfos()
        {
            var resolver = CreateResolver();

            var packageList = LoadedParams.CurrentWorkspaceModel.Nodes
                .Where(n => resolver.IsCustomCategory(n.Category))
                .GroupBy(n => resolver.GetPackageName(n.Category))
                .Select(group => new PackageUsageWrapper
                {
                    PackageName = group.Key,
                    PackageVersion = resolver.GetPackageVersion(group.Key),
                    NodeCount = group.Count()
                })
                .ToList();

            return new ObservableCollection<PackageUsageWrapper>(packageList);
        }

        public int ClearNotes()
        {
            int count = 0;
            foreach (NoteModel note in DynamoViewModel.Model.CurrentWorkspace.Notes)
            {
                if (note.Text.StartsWith("**") || note.Text.StartsWith("Custom Node:") || note.Text.StartsWith(Globals.CustomNodeNotePrefix))
                {
                    try
                    {
                        var pinnedNodeProp = note.GetType().GetProperty("PinnedNode");
                        if (pinnedNodeProp != null)
                        {
                            var pinnedNode = pinnedNodeProp.GetValue(note);
                            if (pinnedNode != null)
                            {
                                // Find NoteViewModel and invoke unpin action
                                var noteViewModel = DynamoViewModel.CurrentSpaceViewModel.Notes.FirstOrDefault(n => n.Model.GUID == note.GUID);
                                if (noteViewModel != null)
                                {
                                    var unpinMethod = noteViewModel.GetType().GetMethod("UnpinFromNode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                                   ?? noteViewModel.GetType().GetMethod("UnpinNode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                                   ?? noteViewModel.GetType().GetMethod("Unpin", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                                    if (unpinMethod != null)
                                    {
                                        if (unpinMethod.GetParameters().Length == 0)
                                            unpinMethod.Invoke(noteViewModel, null);
                                        else if (unpinMethod.GetParameters().Length == 1)
                                            unpinMethod.Invoke(noteViewModel, new object[] { pinnedNode });
                                    }
                                }
                                
                                // Also clear the model property as fallback
                                pinnedNodeProp.SetValue(note, null);
                            }
                        }
                    }
                    catch { }

                    DynamoViewModel.Model.ExecuteCommand(new DynamoModel.DeleteModelCommand(note.GUID));

                    count++;
                }
            }

            return count;
        }

        public int AddPackageNote()
        {
            int count = 0;

            //clear old notes first
            ClearNotes();

            var resolver = CreateResolver();

            foreach (var node in DynamoViewModel.CurrentSpaceViewModel.Nodes)
            {
                if (resolver.IsCustomCategory(node.NodeModel.Category) && !IsExcludedUtilityNode(node.NodeModel))
                {
                    var noteGuid = Guid.NewGuid();
                    var packageName = resolver.GetPackageName(node.NodeModel.Category);
                    DynamoModel.RecordableCommand cmd = new DynamoModel.CreateNoteCommand(noteGuid,
                        Globals.CustomNodeNotePrefix + packageName + resolver.GetPackageVersion(packageName), node.X, node.Y - 35, false);

                    DynamoViewModel.Model.ExecuteCommand(cmd);

                    //try and fail if user is in older dynamo
                    try
                    {
                        var newNote = DynamoViewModel.CurrentSpaceViewModel.Notes.First(note => note.Model.GUID.Equals(noteGuid));

                        DynamoModel.SelectModelCommand select = new DynamoModel.SelectModelCommand(node.NodeModel.GUID, ModifierKeys.None);
                        DynamoViewModel.Model.ExecuteCommand(select);

                        var annotation = DynamoViewModel.CurrentSpaceViewModel.Annotations.FirstOrDefault(a =>
                            a.Nodes.Any(n => n.GUID.ToString().Equals(node.NodeModel.GUID.ToString())));

                        if (annotation != null)
                        {
                            annotation.AnnotationModel.Select();
                            DynamoViewModel.Model.ExecuteCommand(new DynamoModel.AddModelToGroupCommand(newNote.Model.GUID.ToString()));
                            annotation.AnnotationModel.Deselect();
                        }

                        //Try to pin note to node (fails if user is on version without this option)
                        MethodInfo pinToNode = typeof(NoteViewModel).GetMethod("PinToNode",
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        pinToNode.Invoke(newNote, new object[] { node.NodeModel });
                    }
                    catch (Exception e)
                    {
                        DynamoViewModel.Model.Logger.LogWarning($"Monocle- {e.Message}", WarningLevel.Mild);
                    }
                    count++;
                }

            }
            return count++;
        }

        private Border GetNodeBorder(NodeView nv)
        {
            var border = nv.FindName("nodeBorder") as Border 
                         ?? nv.FindName("NodeBorder") as Border 
                         ?? nv.FindName("nodeBackground") as Border 
                         ?? nv.FindName("NodeBackground") as Border;
            if (border != null) return border;

            var allBorders = nv.FindVisualChildren<Border>().ToList();
            return allBorders
                .Where(b => string.IsNullOrEmpty(b.Name) || !b.Name.Equals("selectionBorder", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(b => b.ActualWidth * b.ActualHeight)
                .FirstOrDefault();
        }

        private System.Windows.Shapes.Rectangle GetNodeRectangle(NodeView nv)
        {
            var rect = nv.FindName("nodeBorder") as System.Windows.Shapes.Rectangle 
                       ?? nv.FindName("NodeBorder") as System.Windows.Shapes.Rectangle 
                       ?? nv.FindName("nodeBackground") as System.Windows.Shapes.Rectangle 
                       ?? nv.FindName("NodeBackground") as System.Windows.Shapes.Rectangle;
            if (rect != null) return rect;

            var allRects = nv.FindVisualChildren<System.Windows.Shapes.Rectangle>().ToList();
            return allRects
                .Where(r => string.IsNullOrEmpty(r.Name) || !r.Name.Equals("selectionBorder", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.ActualWidth * r.ActualHeight)
                .FirstOrDefault();
        }

        public void HighlightCustomNodes()
        {
            var nodeViews = NodeUtils.GetNodeViews(DynamoView);

            var resolver = CreateResolver();

            foreach (var nv in nodeViews)
            {
                var nvm = nv.ViewModel;

                if (resolver.IsCustomCategory(nvm.NodeModel.Category) && !IsExcludedUtilityNode(nvm.NodeModel))
                {
                    //try and fail if user is in older dynamo
                    try
                    {
                        if (Globals.DynamoVersion.CompareTo(Globals.NewUiVersion) >= 0)
                        {
                            var border = GetNodeBorder(nv);
                            if (border != null)
                            {
                                VisualBrush vb = new VisualBrush();
                                Rectangle rec = new Rectangle
                                {
                                    Width = border.ActualWidth,
                                    Height = border.ActualHeight,
                                    StrokeDashArray = new DoubleCollection { 6, 2 },
                                    Stroke = new SolidColorBrush(Globals.CustomNodeIdentificationColor),
                                    Margin = new Thickness(-Globals.CustomNodeBorderThickness),
                                    RadiusX = 8,
                                    RadiusY = 8,
                                    StrokeThickness = Globals.CustomNodeBorderThickness,
                                };
                                vb.Visual = rec;
                                border.BorderBrush = vb;
                                border.BorderThickness = new Thickness(Globals.CustomNodeBorderThickness);
                                border.Margin = new Thickness(-Globals.CustomNodeBorderThickness);
                            }
                        }
                        else
                        {
                            var rect = GetNodeRectangle(nv);
                            if (rect != null)
                            {
                                rect.Stroke = new SolidColorBrush(Globals.CustomNodeIdentificationColor);
                                rect.StrokeThickness = Globals.CustomNodeBorderThickness;
                                rect.Margin = new Thickness(-Globals.CustomNodeBorderThickness);
                                rect.RadiusX = 4;
                                rect.RadiusY = 4;
                                rect.StrokeDashArray = new DoubleCollection { 6, 2 };
                            }
                        }
                        //TODO: Enable this for 2.15+
                        //nvm.ImgGlyphOneSource = "/MonocleViewExtension;component/Foca/Resources/customnode-64.png";
                        var nodeBorder = nv.FindName("nodeColorOverlayZoomOut") as Border;
                        if (nodeBorder != null)
                        {
                            nodeBorder.Background = new SolidColorBrush(Globals.CustomNodeIdentificationColor);
                        }
                    }
                    catch (Exception e)
                    {
                        DynamoViewModel.Model.Logger.LogWarning($"Monocle- {e.Message}", WarningLevel.Mild);
                    }
                }
            }
        }

        public void ResetCustomNodeHighlights()
        {
            var nodeViews = NodeUtils.GetNodeViews(DynamoView);
            var resolver = CreateResolver();

            foreach (var nv in nodeViews)
            {
                var nvm = nv.ViewModel;
                if (resolver.IsCustomCategory(nvm.NodeModel.Category) && !IsExcludedUtilityNode(nvm.NodeModel))
                {
                    //try and fail if user is in older dynamo
                    try
                    {
                        ResetNodeColor(nv);
                    }
                    catch (Exception e)
                    {
                        DynamoViewModel.Model.Logger.LogWarning($"Monocle- {e.Message}", WarningLevel.Mild);
                    }
                }
            }
        }

        public void ResetNodeColor(NodeView nv)
        {
            if (Globals.DynamoVersion.CompareTo(Globals.NewUiVersion) >= 0)
            {
                var border = GetNodeBorder(nv);
                if (border != null)
                {
                    border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF9F9F9"));
                    border.BorderThickness = new Thickness(1);
                    border.CornerRadius = new CornerRadius(8, 8, 0, 0);
                    border.Margin = new Thickness(-1);
                }
            }
            else
            {
                var rect = GetNodeRectangle(nv);
                if (rect != null)
                {
                    rect.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5E5C5A"));
                    rect.StrokeThickness = 1;
                    rect.Margin = new Thickness(-1);
                    rect.RadiusX = 0;
                    rect.RadiusY = 0;
                    rect.StrokeDashArray.Clear();
                }
            }
        }

        public void RevealInputs()
        {
            var nodeViews = NodeUtils.GetNodeViews(DynamoView);

            var inputNodeViews = nodeViews.Where(n => n.ViewModel.IsInput).ToList();

            if (!inputNodeViews.Any()) return;

            foreach (var inv in inputNodeViews)
            {
                ResetNodeColor(inv);
            }


            var markedAsinputNodeViews = inputNodeViews.Where(n => n.ViewModel.IsSetAsInput).ToList();

            foreach (var nv in markedAsinputNodeViews)
            {
                try
                {
                    Storyboard storyboard = new Storyboard { FillBehavior = FillBehavior.Stop };

                    DoubleAnimation doubleAnimation = new DoubleAnimation
                    {
                        From = 45,
                        Duration = new Duration(TimeSpan.FromSeconds(10)),
                        RepeatBehavior = new RepeatBehavior(1),
                        EasingFunction = new ElasticEase() { EasingMode = EasingMode.EaseOut, Oscillations = 24, Springiness = 8 }
                    };
                    storyboard.Children.Add(doubleAnimation);

                    nv.RenderTransform = new RotateTransform(0, nv.RenderSize.Width, (nv.RenderSize.Height / 2) + 14);
                    Storyboard.SetTarget(doubleAnimation, nv);
                    Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("RenderTransform.Angle"));

                    var border = GetNodeBorder(nv);
                    var rect = border == null ? GetNodeRectangle(nv) : null;

                    if (border != null || rect != null)
                    {
                        ColorAnimation colorAnimation = new ColorAnimation(Colors.Transparent, Colors.Aquamarine, TimeSpan.FromSeconds(5), FillBehavior.Stop)
                        {
                            RepeatBehavior = new RepeatBehavior(1)
                        };
                        storyboard.Children.Add(colorAnimation);

                        if (border != null)
                        {
                            border.BorderBrush = new SolidColorBrush(Colors.Aquamarine);
                            border.BorderThickness = new Thickness(Globals.CustomNodeBorderThickness + 2);
                            border.CornerRadius = new CornerRadius(8, 8, 0, 0);
                            border.Margin = new Thickness(-1);
                            
                            Storyboard.SetTarget(colorAnimation, border);
                            Storyboard.SetTargetProperty(colorAnimation, new PropertyPath("(Border.BorderBrush).(SolidColorBrush.Color)"));
                        }
                        else if (rect != null)
                        {
                            rect.Stroke = new SolidColorBrush(Colors.Aquamarine);
                            rect.StrokeThickness = Globals.CustomNodeBorderThickness + 2;
                            rect.Margin = new Thickness(-Globals.CustomNodeBorderThickness + 2);
                            rect.RadiusX = 4;
                            rect.RadiusY = 4;
                            
                            Storyboard.SetTarget(colorAnimation, rect);
                            Storyboard.SetTargetProperty(colorAnimation, new PropertyPath("(Shape.Stroke).(SolidColorBrush.Color)"));
                        }
                    }

                    storyboard.Begin();
                }
                catch (Exception e)
                {
                    DynamoViewModel.Model.Logger.LogWarning($"Monocle- {e.Message}", WarningLevel.Mild);
                }
            }
            
        }

       

        /// <summary>
        /// Build a package-name resolver from one pass over the search library (and installed packages on 3.0+).
        /// Callers create one per operation instead of rescanning the library for every node.
        /// </summary>
        internal PackageNameResolver CreateResolver()
        {
#if net8 || net10
            var libraries = DynamoViewModel.Model.SearchModel.Entries;
#else
            var libraries = DynamoViewModel.Model.SearchModel.SearchEntries;
#endif
            var roots = new List<string>();
            foreach (var element in libraries)
            {
                // Only include packages and custom nodes
                if (!element.ElementType.HasFlag(ElementTypes.Packaged) && !element.ElementType.HasFlag(ElementTypes.CustomNode)) continue;

                var top = element.Categories.FirstOrDefault();
                if (top != null && !top.StartsWith("Core") && !top.StartsWith("Revit"))
                {
                    roots.Add(top);
                }
            }

            List<KeyValuePair<string, string>> localPackages = null;
            try
            {
#if D30_OR_GREATER
                if (Globals.PmExtension?.PackageLoader != null)
                {
                    localPackages = Globals.PmExtension.PackageLoader.LocalPackages
                        .Select(p => new KeyValuePair<string, string>(p.Name, p.VersionName))
                        .ToList();
                }
#endif
            }
            catch (Exception e)
            {
                DynamoViewModel.Model.Logger.LogWarning($"Monocle- reading installed packages failed: {e.Message}", WarningLevel.Mild);
            }

            return new PackageNameResolver(roots, localPackages);
        }

        public List<string> GetCustomPackageList() => CreateResolver().Roots.ToList();

        public bool IsCustomNode(NodeModel node) => CreateResolver().IsCustomCategory(node.Category);

        public string GetPackageName(NodeModel node) => CreateResolver().GetPackageName(node.Category);

        public string GetPackageVersion(NodeModel node)
        {
            var resolver = CreateResolver();
            return resolver.GetPackageVersion(resolver.GetPackageName(node.Category));
        }

        private static bool IsExcludedUtilityNode(NodeModel node)
        {
            var name = node.Name?.ToLower() ?? string.Empty;
            return name.Contains("relay") || name.Contains("remember") || name.Contains("gate");
        }
    }
}
