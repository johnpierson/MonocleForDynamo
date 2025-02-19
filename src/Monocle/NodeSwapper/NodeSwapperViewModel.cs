using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Dynamo.Graph.Nodes;
using Dynamo.Logging;
using Dynamo.Models;
using Dynamo.ViewModels;
using Dynamo.Views;
using Xceed.Wpf.AvalonDock.Controls;
using ModifierKeys = Dynamo.Utilities.ModifierKeys;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace MonocleViewExtension.NodeSwapper
{
    internal class NodeSwapperViewModel : ViewModelBase
    {
        public NodeSwapperModel Model { get; set; }


        private bool _imageMode;
        public bool ImageMode
        {
            get { return _imageMode; }
            set { _imageMode = value; RaisePropertyChanged(nameof(ImageMode)); }
        }


        private NodeViewModel _nodeToSwap;
        public NodeViewModel NodeToSwap
        {
            get { return _nodeToSwap; }
            set { _nodeToSwap = value; RaisePropertyChanged(nameof(NodeToSwap)); }
        }


        private NodeViewModel _nodeToSwapTo;
        public NodeViewModel NodeToSwapTo
        {
            get { return _nodeToSwapTo; }
            set { _nodeToSwapTo = value; RaisePropertyChanged(nameof(NodeToSwap)); }
        }
        
        private string _results;
        public string Results
        {
            get { return _results; }
            set { _results = value; RaisePropertyChanged(nameof(Results)); }
        }

        private string _paintStatusMessage;
        public string PaintStatusMessage
        {
            get { return _paintStatusMessage; }
            set { _paintStatusMessage = value; RaisePropertyChanged(nameof(PaintStatusMessage)); }
        }

        private int _currentStep = 0;
        private NodeSwapperPaintBrush _paintBrush;

        private WorkspaceView _workspaceView;
        public NodeSwapperViewModel(NodeSwapperModel m, NodeModel node = null)
        {
            Model = m;
            _workspaceView = Model.dynamoView.FindVisualChildren<WorkspaceView>().First();

            //set paint brush settings
            PaintStatusMessage =  Properties.Resources.NodeSwapperStatusMessageSelectReplacement;

            //set to manual run mode
            Model.SetRunStatus();

            ImageMode = true;

            //in canvas
            if (node != null)
            {
                InCanvas(node);
            }
            else
            {
                _paintBrush = new NodeSwapperPaintBrush()
                {
                    // Set the data context for the main grid in the window.
                    MainGrid = { DataContext = this },
                    // Set the owner of the window to the Dynamo window.
                    Owner = m.LoadedParams.DynamoWindow
                };
                _paintBrush.Show();

                _workspaceView.MouseLeftButtonUp += WsViewOnMouseUp;
                _workspaceView.MouseMove += WsViewOnMouseMove;
            }
        }

        private void InCanvas(NodeModel node)
        {
   
            DynamoModel.SelectModelCommand select = new DynamoModel.SelectModelCommand(node.GUID, ModifierKeys.None);
            Model.dynamoViewModel.Model.ExecuteCommand(select);

            NodeToSwapTo = Model.Selection();

            _currentStep++;
            ImageMode = false;

            //set the message
            PaintStatusMessage = Properties.Resources.NodeSwapperStatusMessageSelectToReplace;


            _paintBrush = new NodeSwapperPaintBrush()
            {
                // Set the data context for the main grid in the window.
                MainGrid = { DataContext = this },
                // Set the owner of the window to the Dynamo window.
                Owner = Model.LoadedParams.DynamoWindow
            };

 
            _paintBrush.Show();

            _workspaceView.MouseLeftButtonUp += WsViewOnMouseUp;
            _currentStep++;
            
            _workspaceView.MouseMove += WsViewOnMouseMove;
        }

        private void Wipeout()
        {
            _workspaceView.MouseLeftButtonUp -= WsViewOnMouseUp;
            _workspaceView.MouseMove -= WsViewOnMouseMove;
            _workspaceView = null;

            Model = null;
            _paintBrush.Close();
            _paintBrush = null;
        }

        private void WsViewOnMouseMove(object sender, MouseEventArgs e)
        {
            _paintBrush.UpdateLocation();
        }

      
        private void OnSwapNode(object o)
        {
            // this is reserved for future use
            var theObject = o.ToString();
            Model.dynamoViewModel.Model.Logger.LogNotification($"Monocle","NodeSwapper",$"{theObject}",$"Node swapped.");

            if (NodeToSwapTo.IsCustomFunction)
            {
                DynamoModel.CreateNodeCommand replacementCommand =
                    new DynamoModel.CreateNodeCommand(Guid.NewGuid().ToString(), NodeToSwapTo.NodeModel.CreationName, NodeToSwap.X, NodeToSwap.Y, false, false);
                Model.dynamoViewModel.ExecuteCommand(replacementCommand);
            }

            else
            {
                //create code block if that is the node.
                if (NodeToSwapTo.NodeModel.NodeType.Equals("CodeBlockNode"))
                {
                    CodeBlockNodeModel existingCodeBlock = NodeToSwapTo.NodeModel as CodeBlockNodeModel;
                    var codeBlock = new CodeBlockNodeModel(existingCodeBlock.Code, 0, 0, Model.dynamoViewModel.Model.LibraryServices, Model.dynamoViewModel.Model.CurrentWorkspace.ElementResolver);

                    Model.dynamoViewModel.ExecuteCommand(new DynamoModel.CreateNodeCommand(codeBlock, NodeToSwap.X, NodeToSwap.Y, false, false));
                }
                else
                {
                    DynamoModel.CreateNodeCommand replacementCommand =
                        new DynamoModel.CreateNodeCommand(Guid.NewGuid().ToString(), NodeToSwapTo.NodeModel.CreationName, NodeToSwap.X, NodeToSwap.Y, false, false);
                    Model.dynamoViewModel.ExecuteCommand(replacementCommand);
                }
            }

            NodeViewModel replacementNode = Model.dynamoViewModel.CurrentSpaceViewModel.Nodes.LastOrDefault();


            List<DynamoModel.MakeConnectionCommand> connectionCommands = new List<DynamoModel.MakeConnectionCommand>();


            var inports = NodeToSwap.NodeModel.InPorts.ToList();
            var outports = NodeToSwap.NodeModel.OutPorts.ToList();

            //connect inports
            if (replacementNode.InPorts.Any())
            {
                foreach (var inport in inports)
                {
                    foreach (var connector in inport.Connectors)
                    {
                        var guid = connector.Start.Owner.GUID;

                        var portIndex = connector.Start.Index;

                        //begin the connection
                        connectionCommands.Add(new DynamoModel.MakeConnectionCommand(guid, portIndex, PortType.Output,
                            DynamoModel.MakeConnectionCommand.Mode.Begin));

                        //end the connection
                        connectionCommands.Add(
                            new DynamoModel.MakeConnectionCommand(replacementNode.NodeModel.GUID, connector.End.Index, PortType.Input,
                                DynamoModel.MakeConnectionCommand.Mode.End));
                    }
                }
            }
            //connect outports
            if (replacementNode.OutPorts.Any())
            {
                foreach (var outport in outports)
                {
                    foreach (var connector in outport.Connectors)
                    {
                        var guid = connector.End.Owner.GUID;

                        var portIndex = connector.Start.Index;

                        //begin the connection
                        connectionCommands.Add(
                            new DynamoModel.MakeConnectionCommand(replacementNode.NodeModel.GUID, portIndex,
                                PortType.Output,
                                DynamoModel.MakeConnectionCommand.Mode.Begin));

                        //end the connection
                        connectionCommands.Add(new DynamoModel.MakeConnectionCommand(guid, connector.End.Index,
                            PortType.Input,
                            DynamoModel.MakeConnectionCommand.Mode.End));
                    }
                }
            }
            foreach (var connectionCommand in connectionCommands)
            {
                try
                {
                    Model.dynamoViewModel.ExecuteCommand(connectionCommand);
                }
                catch (Exception)
                {
                    //suppress, this happens when you are replacing a node with one with more outputs
                }
             
            }

            //if the original node was in a group, put the new node in it now
            var groups = Model.dynamoViewModel.CurrentSpaceViewModel.Annotations.ToList();

            if (groups.Any())
            {
                var originalNodeGuid = NodeToSwap.NodeModel.GUID;

                foreach (var group in groups)
                {
                    if (group.Nodes.Any(n => n.GUID.Equals(originalNodeGuid)))
                    {
                        DynamoModel.AddModelToGroupCommand addToGroup =
                            new DynamoModel.AddModelToGroupCommand(replacementNode.NodeModel.GUID);


                        DynamoModel.SelectModelCommand selectModel =
                            new DynamoModel.SelectModelCommand(group.AnnotationModel.GUID, ModifierKeys.None);

                        Model.dynamoViewModel.ExecuteCommand(selectModel);

                        Model.dynamoViewModel.ExecuteCommand(addToGroup);
                    }
                }
            }
           
            DynamoModel.DeleteModelCommand delete = new DynamoModel.DeleteModelCommand(NodeToSwap.NodeModel.GUID);


            Model.dynamoViewModel.ExecuteCommand(delete);

            NodeToSwap = null;

            if (Keyboard.IsKeyDown(Key.LeftAlt))
            {
                DynamoModel.DeleteModelCommand delete2 = new DynamoModel.DeleteModelCommand(NodeToSwapTo.NodeModel.GUID);


                Model.dynamoViewModel.ExecuteCommand(delete2);
            }

        }

        private void WsViewOnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_currentStep == 0)
            {
                NodeToSwapTo = Model.Selection();

                if (NodeToSwapTo != null)
                {
                    _currentStep++;
                    ImageMode = false;

                    //set the message
                    PaintStatusMessage = Properties.Resources.NodeSwapperStatusMessageSelectToReplace;
                }
                else
                {
                    Wipeout();
                }
            }

            if (_currentStep == 1)
            {
                NodeToSwap = Model.Selection();
                if (NodeToSwap != null)
                {
                    try
                    {
                        OnSwapNode(new object());
                    }
                    catch (Exception exception)
                    {
                        Model.dynamoViewModel.Model.Logger.LogWarning($"Monocle- {exception.Message}", WarningLevel.Mild);
                    }
                 
                }
                else
                {
                    Wipeout();
                }
            }

            if (_currentStep == 2)
            {
                _currentStep = 1;
            }
        }
    }
}
