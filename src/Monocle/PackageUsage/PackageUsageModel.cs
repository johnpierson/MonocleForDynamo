using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
using Path = System.IO.Path;
using Thickness = System.Windows.Thickness;

namespace MonocleViewExtension.PackageUsage
{
    public class PackageUsageModel
    {
        public DynamoView dynamoView { get; }
        public DynamoViewModel dynamoViewModel { get; }
        public ViewLoadedParams LoadedParams { get; }

        public PackageUsageModel(DynamoViewModel dvm, ViewLoadedParams loadedParams)
        {
            dynamoView = loadedParams.DynamoWindow as DynamoView;
            dynamoViewModel = dvm;
            LoadedParams = loadedParams;
        }

        public ObservableCollection<PackageUsageWrapper> GetCustomNodeInfos()
        {
            List<PackageUsageWrapper> packages = new List<PackageUsageWrapper>();
            List<PackageUsageWrapper> packageList = new List<PackageUsageWrapper>();

            foreach (var n in LoadedParams.CurrentWorkspaceModel.Nodes)
            {
                if (IsCustomNode(n))
                {
                    packages.Add(new PackageUsageWrapper
                    {
                        PackageName = GetPackageName(n),
                        PackageVersion = GetPackageVersion(n)
                    });
                }

            }
            var grouping = packages.GroupBy(p => p.PackageName)
                .Select(group => group.OrderBy(p => p.PackageName));

            foreach (var group in grouping)
            {
                var first = group.First();
                first.NodeCount = group.Count();
                packageList.Add(first);
            }

            return new ObservableCollection<PackageUsageWrapper>(packageList);
        }

        public int ClearNotes()
        {
            int count = 0;
            foreach (NoteModel note in dynamoViewModel.Model.CurrentWorkspace.Notes)
            {
                if (note.Text.StartsWith("**") || note.Text.StartsWith("Custom Node:") || note.Text.StartsWith(Globals.CustomNodeNotePrefix))
                {
                    //note.PinnedNode = null;
                    dynamoViewModel.Model.ExecuteCommand(new DynamoModel.DeleteModelCommand(note.GUID));

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

            foreach (var node in dynamoViewModel.CurrentSpaceViewModel.Nodes)
            {
                if (IsCustomNode(node.NodeModel) && !node.NodeModel.Name.ToLower().Contains("relay") && !node.NodeModel.Name.ToLower().Contains("remember") && !node.NodeModel.Name.ToLower().Contains("gate"))
                {
                    var noteGuid = Guid.NewGuid();
                    DynamoModel.RecordableCommand cmd = new DynamoModel.CreateNoteCommand(noteGuid,
                        Globals.CustomNodeNotePrefix + GetPackageName(node.NodeModel) + GetPackageVersion(node.NodeModel), node.X, node.Y - 35, false);

                    dynamoViewModel.Model.ExecuteCommand(cmd);

                    //try and fail if user is in older dynamo
                    try
                    {
                        var newNote = dynamoViewModel.CurrentSpaceViewModel.Notes.First(note => note.Model.GUID.Equals(noteGuid));

                        DynamoModel.SelectModelCommand select = new DynamoModel.SelectModelCommand(node.NodeModel.GUID, ModifierKeys.None);
                        dynamoViewModel.Model.ExecuteCommand(select);

                        //newNote.PinToNodeCommand.Execute(null);

                    }
                    catch (Exception e)
                    {
                        dynamoViewModel.Model.Logger.LogWarning($"Monocle- {e.Message}", WarningLevel.Mild);
                    }
                    count++;
                }

            }
            return count++;
        }

        public void HighlightCustomNodes()
        {
            var nodeViews = MiscUtils.FindVisualChildren<NodeView>(dynamoView);

            foreach (var nv in nodeViews)
            {
                var nvm = nv.ViewModel;
                if (IsCustomNode(nvm.NodeModel) && !nvm.NodeModel.Name.ToLower().Contains("relay") && !nvm.NodeModel.Name.ToLower().Contains("remember") && !nvm.NodeModel.Name.ToLower().Contains("gate"))
                {
                    //try and fail if user is in older dynamo
                    try
                    {
                        if (Globals.DynamoVersion.CompareTo(Globals.NewUiVersion) >= 0)
                        {
                            var border = (Border)nv.FindName("nodeBorder");

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
                            border.BorderThickness = new Thickness(Globals.CustomNodeBorderThickness) ;
                            border.Margin = new Thickness(-Globals.CustomNodeBorderThickness);
                        }
                        else
                        {
                            var rect = nv.FindName("nodeBorder") as System.Windows.Shapes.Rectangle;
                            rect.Stroke = new SolidColorBrush(Globals.CustomNodeIdentificationColor);
                            rect.StrokeThickness = Globals.CustomNodeBorderThickness;
                            rect.Margin = new Thickness(-Globals.CustomNodeBorderThickness);
                            rect.RadiusX = 4;
                            rect.RadiusY = 4;
                            rect.StrokeDashArray = new DoubleCollection { 6, 2 };
                        }
                    }
                    catch (Exception e)
                    {
                        dynamoViewModel.Model.Logger.LogWarning($"Monocle- {e.Message}", WarningLevel.Mild);
                    }
                }
            }
        }

        public void ResetCustomNodeHighlights()
        {
            var nodeViews = MiscUtils.FindVisualChildren<NodeView>(dynamoView);

            foreach (var nv in nodeViews)
            {
                var nvm = nv.ViewModel;
                if (IsCustomNode(nvm.NodeModel) && !nvm.NodeModel.Name.ToLower().Contains("relay") && !nvm.NodeModel.Name.ToLower().Contains("remember") && !nvm.NodeModel.Name.ToLower().Contains("gate"))
                {
                    //try and fail if user is in older dynamo
                    try
                    {
                        if (Globals.DynamoVersion.CompareTo(Globals.NewUiVersion) >= 0)
                        {
                            var border = (Border)nv.FindName("nodeBorder");

                            border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF9F9F9"));
                            border.BorderThickness = new Thickness(1);
                            border.CornerRadius = new CornerRadius(8, 8, 0, 0);
                            border.Margin = new Thickness(-1);
                        }
                        else
                        {
                            var rect = nv.FindName("nodeBorder") as System.Windows.Shapes.Rectangle;
                            rect.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5E5C5A"));
                            rect.StrokeThickness = 1;
                            rect.Margin = new Thickness(-1);
                            rect.RadiusX = 0;
                            rect.RadiusY = 0;
                            rect.StrokeDashArray.Clear();
                        }
                    }
                    catch (Exception e)
                    {
                        dynamoViewModel.Model.Logger.LogWarning($"Monocle- {e.Message}", WarningLevel.Mild);
                    }
                }
            }
        }

        public List<string> GetCustomPackageList()
        {
            List<NodeSearchElement> libraries = dynamoViewModel.Model.SearchModel.SearchEntries.ToList();
            List<string> addOns = new List<string>();
            foreach (var element in libraries)
            {
                // Only include packages and custom nodes
                if (element.ElementType.HasFlag(ElementTypes.Packaged) || element.ElementType.HasFlag(ElementTypes.CustomNode))
                {
                    // Ordered list of all categories for the search element including all nested categories
                    var allAddOns = element.Categories.ToList();
                    // Construct all categories levels for the element starting at the top level
                    for (int i = 0; i < allAddOns.Count; i++)
                    {
                        if (i == 0 && !allAddOns[i].StartsWith("Core") && !allAddOns[i].StartsWith("Revit"))
                        {
                            addOns.Add(allAddOns[i]);
                        }
                    }
                }
            }
            return addOns.Distinct().ToList();
        }
        public string AllCustomNodes()
        {
            List<NodeSearchElement> libraries = dynamoViewModel.Model.SearchModel.SearchEntries.ToList();
            List<string> addOns = new List<string>();
            foreach (var element in libraries)
            {
                // Only include packages and custom nodes
                if (element.ElementType.HasFlag(ElementTypes.Packaged) || element.ElementType.HasFlag(ElementTypes.CustomNode))
                {
                    // Ordered list of all categories for the search element including all nested categories
                    var allAddOns = element.Categories.ToList();
                    // Construct all categories levels for the element starting at the top level
                    for (int i = 0; i < allAddOns.Count; i++)
                    {
                        if (i == 0 && !allAddOns[i].StartsWith("Core"))
                        {
                            addOns.Add("{" + "\"" + element.CreationName.Split('@').First() + "\"" + "," + "\"" + allAddOns[i] + "\"" + "},");
                        }
                    }
                }
            }
            return string.Join("\n", addOns.Distinct().ToList());
        }
        public bool IsCustomNode(NodeModel node)
        {
            bool result = GetCustomPackageList().Any(x => node.Category.StartsWith(x, StringComparison.OrdinalIgnoreCase));

            return result;
        }

        public string GetPackageName(NodeModel node)
        {
            string result = null;
            List<int> values = new List<int>();

            foreach (var c in GetCustomPackageList())
            {
                string nodeCat = node.Category.Split('.').First();
                values.Add(StringUtils.Compute(nodeCat, c));
            }
            int minIndex = values.IndexOf(values.Min());
            result = GetCustomPackageList()[minIndex];

            string fixedResult = String.Empty;
            switch (result.ToLower())
            {
                case "springs":
                    fixedResult = "spring nodes";
                    break;
                case "archilab_bumblebee":
                    fixedResult = "BumbleBee";
                    break;
                default:
                    fixedResult = result;
                    break;
            }


            return fixedResult;
        }
        public string GetPackageVersion(NodeModel node)
        {
            string jsonPath = null;
            string version = null;
            DirectoryInfo packageFolder = null;

            //event handlers for when changes are made
            try
            {
                string packagePath = dynamoViewModel.Model.PathManager.PackagesDirectories.First(p => p.ToLower().Contains("appdata"));
                DirectoryInfo packageDirectory = new DirectoryInfo(packagePath);
                packageFolder = packageDirectory
                    .GetDirectories().First(d => d.Name.CleanupString().Contains(GetPackageName(node).CleanupString()));
                jsonPath = Path.Combine(packageFolder.FullName, "pkg.json");
                // read the json for the file version
                StreamReader sr = new StreamReader(jsonPath);
                string[] splitDoc = sr.ReadToEnd().Split(Char.Parse(","));
                string clean = splitDoc.First(s => s.Contains("version")).Replace("\"", "").Trim();
                version = clean.Replace("version:", " v.");
            }
            catch (Exception)
            {
                version = "";
            }
            return version;
        }
    }
}
