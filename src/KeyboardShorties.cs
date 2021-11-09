using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using CoreNodeModels;
using Dynamo.Controls;
using Dynamo.Graph.Connectors;
using Dynamo.Graph.Nodes;
using Dynamo.Models;
using Dynamo.Wpf.Extensions;
using Dynamo.Wpf.Interfaces;
using MonocleViewExtension.PackageUsage;

namespace MonocleViewExtension
{
    public class KeyboardShorties
    {
        public static void Add(DynamoView view, ViewLoadedParams p)
        {
            try
            {
                InputGestureCollection gestures = new InputGestureCollection
                {
                    new KeyGesture(Key.P, ModifierKeys.Control | ModifierKeys.Shift)
                };
                RoutedUICommand uiCommand = new RoutedUICommand("PackageUsage", "PackageUsageCommand", typeof(ResourceNames.MainWindow), gestures);
                var binding = new CommandBinding(uiCommand);
                binding.Executed += (sender, args) =>
                {
                    var viewModel = new PackageUsageDogeViewmodel(p);
                    var window = new PackageUsageWindow()
                    {
                        // Set the data context for the main grid in the window.
                        MainGrid = { DataContext = viewModel },
                        // Set the owner of the window to the Dynamo window.
                        Owner = p.DynamoWindow
                    };

                    window.Left = window.Owner.Left + 400;
                    window.Top = window.Owner.Top + 200;

                    // Show a modeless window.
                    window.Show();
                };
                view.CommandBindings.Add(binding);
            }
            catch (Exception)
            {
                //suppress
            }
        }
        public static void AddAlignment(DynamoView view, ViewLoadedParams p)
        {
            try
            {
                //AlignLeft
                var bindingLeft = new CommandBinding(new RoutedUICommand("AlignLeft", "AlignLeftCommand",
                    typeof(ResourceNames.MainWindow), new InputGestureCollection
                    {
                        new KeyGesture(Key.Left, ModifierKeys.Alt)
                    }));
                bindingLeft.Executed += (sender, args) =>
                {
                    MonocleViewExtension.AlignFtwCommand.Execute("HorizontalLeft");
                    InCanvasAlignWidget.DropWidget();
                };
                view.CommandBindings.Add(bindingLeft);
                //AlignRight
                var bindingRight = new CommandBinding(new RoutedUICommand("AlignRight", "AlignRightCommand",
                    typeof(ResourceNames.MainWindow), new InputGestureCollection
                    {
                        new KeyGesture(Key.Right, ModifierKeys.Alt)
                    }));
                bindingRight.Executed += (sender, args) =>
                {
                    MonocleViewExtension.AlignFtwCommand.Execute("HorizontalRight");
                    InCanvasAlignWidget.DropWidget();
                };
                view.CommandBindings.Add(bindingRight);
                //AlignTop
                var bindingTop = new CommandBinding(new RoutedUICommand("AlignTop", "AlignTopCommand",
                    typeof(ResourceNames.MainWindow), new InputGestureCollection
                    {
                        new KeyGesture(Key.Up, ModifierKeys.Alt)
                    }));
                bindingTop.Executed += (sender, args) =>
                {
                    MonocleViewExtension.AlignFtwCommand.Execute("VerticalTop");
                    InCanvasAlignWidget.DropWidget();
                };
                view.CommandBindings.Add(bindingTop);
                //AlignBottom
                var bindingBottom = new CommandBinding(new RoutedUICommand("AlignBottom", "AlignBottomCommand",
                    typeof(ResourceNames.MainWindow), new InputGestureCollection
                    {
                        new KeyGesture(Key.Down, ModifierKeys.Alt)
                    }));
                bindingBottom.Executed += (sender, args) =>
                {
                    MonocleViewExtension.AlignFtwCommand.Execute("VerticalBottom");
                    InCanvasAlignWidget.DropWidget();
                };
                view.CommandBindings.Add(bindingBottom);
            }
            catch (Exception)
            {
                //suppress
            }
        }

        public static void ReplaceDropdown(NodeModel nodeModel)
        {
            if (nodeModel.NodeType != "ExtensionNode")
            {
                return;
            }
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

            if (nodeModel.GetType().ToString().Contains("Revit") && !nodeModel.Name.Equals("Categories") && !nodeModel.Name.Equals("Element Types") && !nodeModel.Name.Equals("Family Types"))
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

                    codeBlock = new CodeBlockNodeModel($"//{nodeModel.CachedValue.StringData};Revit.Elements.ElementSelector.ByElementId({elementId});", 0, 0, MonocleViewExtension.DynView.Model.LibraryServices, MonocleViewExtension.DynView.Model.CurrentWorkspace.ElementResolver);
                }
                catch (Exception)
                {
                    return;
                }
            }

            else
            {
                string creationName = nodeModel.Name;
                var stringData = nodeModel.CachedValue.StringData;
                if (stringData.Equals("null"))
                {
                    return;
                }

                switch (creationName)
                {
                    case "Family Types":
                        codeBlock = new CodeBlockNodeModel($"Revit.Elements.FamilyType.ByName(\"{stringData.Split(':').Last().TrimStart(' ')}\")", 0, 0, MonocleViewExtension.DynView.Model.LibraryServices, MonocleViewExtension.DynView.Model.CurrentWorkspace.ElementResolver);
                        break;
                    case "Element Types":
                        codeBlock = new CodeBlockNodeModel($"DSCore.Types.FindTypeByNameInAssembly(\"{stringData.Split('.').Last()}\", \"RevitAPI\")", 0, 0, MonocleViewExtension.DynView.Model.LibraryServices, MonocleViewExtension.DynView.Model.CurrentWorkspace.ElementResolver);
                        break;
                    case "Categories":
                        codeBlock = new CodeBlockNodeModel($"Category.ByName(\"{stringData}\")", 0, 0, MonocleViewExtension.DynView.Model.LibraryServices, MonocleViewExtension.DynView.Model.CurrentWorkspace.ElementResolver);
                        break;
                    case "Unit Types":
                        codeBlock = new CodeBlockNodeModel($"DSCore.Types.FindTypeByNameInAssembly(\"{stringData.Split('.').Last()}\", \"DynamoUnits\")", 0, 0, MonocleViewExtension.DynView.Model.LibraryServices, MonocleViewExtension.DynView.Model.CurrentWorkspace.ElementResolver);
                        break;
                    default:
                        return;
                }
            }

            try
            {
                MonocleViewExtension.DynView.Model.ExecuteCommand(
                    new DynamoModel.CreateNodeCommand(codeBlock, nodeModel.CenterX, nodeModel.CenterY, false, false));

                if (outports != null)
                {
                    foreach (var outport in outports)
                    {
                        
                        //connect it
                        MonocleViewExtension.DynView.ExecuteCommand(
                            new DynamoModel.MakeConnectionCommand(codeBlock.GUID, outport.Start.Index, PortType.Output, DynamoModel.MakeConnectionCommand.Mode.Begin));
                        MonocleViewExtension.DynView.ExecuteCommand(
                            new DynamoModel.MakeConnectionCommand(outport.End.Owner.GUID, outport.End.Index, PortType.Input, DynamoModel.MakeConnectionCommand.Mode.End));
                    }
                }

                //delete the original node
                MonocleViewExtension.DynView.ExecuteCommand(new DynamoModel.DeleteModelCommand(nodeModel.GUID));
                codeBlock.Name = "ᶜᵒⁿᵛᵉʳᵗᵉᵈ ᶠʳᵒᵐ ᴰʳᵒᵖᵈᵒʷⁿ ʷᶦᵗʰ ᴹᵒⁿᵒᶜˡᵉ™";
            }
            catch (Exception)
            {
                //
            }

        }

        public static void Combinifier(List<NodeModel> nodeModel)
        {
            if (!nodeModel.Any()) return;

            var listCreateSize = nodeModel.Select(n => n.OutPorts.Count).ToList().Sum();

            if (listCreateSize.Equals(0)) return;

            //create the list.create node
            string newGuid = Guid.NewGuid().ToString();
            var command = new DynamoModel.CreateNodeCommand(newGuid, "List.Create", nodeModel.Max(n => n.CenterX) + 120, nodeModel.Min(n => n.CenterY - n.Height / 2), false, false);
            MonocleViewExtension.DynView.Model.ExecuteCommand(command);

            //iterate through and add the input ports
            for (int i = 1; i < listCreateSize; i++)
            {
                MonocleViewExtension.DynView.ExecuteCommand(new DynamoModel.ModelEventCommand(newGuid, "AddInPort"));
            }

            int overallListInput = 0;

            foreach (var node in nodeModel)
            {
                
                for (int i = 0; i < node.OutPorts.Count; i++)
                {
                    //begin connection
                    MonocleViewExtension.DynView.ExecuteCommand(
                        new DynamoModel.MakeConnectionCommand(node.GUID, i, PortType.Output, DynamoModel.MakeConnectionCommand.Mode.Begin));

                    MonocleViewExtension.DynView.ExecuteCommand(
                        new DynamoModel.MakeConnectionCommand(newGuid, overallListInput, PortType.Input, DynamoModel.MakeConnectionCommand.Mode.End));

                    overallListInput++;
                }
            }
        }

        public static void PowList(List<NodeModel> nodeModel)
        {
            foreach (var node in nodeModel)
            {
                if (node.OutPorts.Count > 1 || node.CachedValue is null)
                {
                    return;
                }

                var values = node.CachedValue.GetElements().ToList();
                var count = values.Count > 20 ? 20 : values.Count;

                string codeBlockString = string.Empty;

                for (int i = 0; i < count; i++)
                {
                    codeBlockString = codeBlockString + $"value[{i}];";
                }

                var codeBlock = new CodeBlockNodeModel(codeBlockString, 0, 0, MonocleViewExtension.DynView.Model.LibraryServices, MonocleViewExtension.DynView.Model.CurrentWorkspace.ElementResolver);

                MonocleViewExtension.DynView.Model.ExecuteCommand(
                    new DynamoModel.CreateNodeCommand(codeBlock, node.CenterX + 50, node.CenterY, false, false));

                //connect it
                MonocleViewExtension.DynView.ExecuteCommand(
                    new DynamoModel.MakeConnectionCommand(node.GUID, 0, PortType.Output, DynamoModel.MakeConnectionCommand.Mode.Begin));
                MonocleViewExtension.DynView.ExecuteCommand(
                    new DynamoModel.MakeConnectionCommand(codeBlock.GUID, 0, PortType.Input, DynamoModel.MakeConnectionCommand.Mode.End));


                //rename code block
                codeBlock.Name = "💣";
            }
        }
    }
}
