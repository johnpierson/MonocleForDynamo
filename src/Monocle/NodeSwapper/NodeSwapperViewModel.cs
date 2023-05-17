using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Dynamo.Extensions;
using Dynamo.Graph.Nodes;
using Dynamo.Models;
using Dynamo.UI.Commands;
using Dynamo.ViewModels;

namespace MonocleViewExtension.NodeSwapper
{
    internal class NodeSwapperViewModel : ViewModelBase
    {
        public NodeSwapperModel Model { get; set; }
        private ReadyParams _readyParams;
        public DelegateCommand SwapNode { get; set; }
        public DelegateCommand SelectNodeToSwap { get; set; }
        public DelegateCommand SelectNodeToSwapTo { get; set; }
        public DelegateCommand Link { get; set; }
        public DelegateCommand Close { get; set; }

        private bool _canRun;
        public bool CanRun
        {
            get { return _canRun; }
            set { _canRun = value; RaisePropertyChanged(() => CanRun); }
        }


        private NodeViewModel _nodeToSwap;
        public NodeViewModel NodeToSwap
        {
            get { return _nodeToSwap; }
            set { _nodeToSwap = value; RaisePropertyChanged(() => NodeToSwap); }
        }

        private string _nodeToSwapName;
        public string NodeToSwapName
        {
            get { return _nodeToSwapName; }
            set { _nodeToSwapName = value; RaisePropertyChanged(() => NodeToSwapName); }
        }

        private NodeViewModel _nodeToSwapTo;
        public NodeViewModel NodeToSwapTo
        {
            get { return _nodeToSwapTo; }
            set { _nodeToSwapTo = value; RaisePropertyChanged(() => NodeToSwap); }
        }
        private string _nodeToSwapToName;
        public string NodeToSwapToName
        {
            get { return _nodeToSwapToName; }
            set { _nodeToSwapToName = value; RaisePropertyChanged(() => NodeToSwapToName); }
        }

        private string _results;
        public string Results
        {
            get { return _results; }
            set { _results = value; RaisePropertyChanged(() => Results); }
        }
        private bool _resultsVisibility;
        public bool ResultsVisibility
        {
            get { return _resultsVisibility; }
            set { _resultsVisibility = value; RaisePropertyChanged(() => ResultsVisibility); }
        }
        private int _runCount;
        public int RunCount
        {
            get { return _runCount; }
            set { _runCount = value; RaisePropertyChanged(() => RunCount); }
        }
        public NodeSwapperViewModel(NodeSwapperModel m)
        {
            Model = m;
            _readyParams = m.LoadedParams;

            CanRun = false;
            NodeToSwapName = "pending";
            NodeToSwapToName = "pending";

            RunCount = 0;

            SelectNodeToSwap = new DelegateCommand(OnSelectToSwap);
            SelectNodeToSwapTo = new DelegateCommand(OnSelectToSwapTo);

            SwapNode = new DelegateCommand(OnSwapNode);
            Link = new DelegateCommand(OnLink);
            Close = new DelegateCommand(OnClose);

            ResultsVisibility = false;

            Model.SetRunStatus();
        }

        private void OnSelectToSwap(object command)
        {
            NodeToSwap = Model.Selection();
            NodeToSwapName = NodeToSwap.Name;

            if (NodeToSwap != null && NodeToSwapTo != null)
            {
                CanRun = true;
            }
        }
        private void OnSelectToSwapTo(object command)
        {
            NodeToSwapTo = Model.Selection();
            NodeToSwapToName = NodeToSwapTo.Name;

            if (NodeToSwap != null && NodeToSwapTo != null)
            {
                CanRun = true;
            }
        }
        private void OnSwapNode(object o)
        {
            //create a duplicate first
            DynamoModel.CreateNodeCommand createRefactoredIf =
                new DynamoModel.CreateNodeCommand(Guid.NewGuid().ToString(), NodeToSwapTo.Name, NodeToSwap.X, NodeToSwap.Y, false, false);

            Model.dynamoViewModel.ExecuteCommand(createRefactoredIf);

            NodeViewModel replacementNode = Model.dynamoViewModel.CurrentSpaceViewModel.Nodes.LastOrDefault();


            List<DynamoModel.MakeConnectionCommand> connectionCommands = new List<DynamoModel.MakeConnectionCommand>();

            
            var inports = NodeToSwap.NodeModel.InPorts.ToList();
            var outports = NodeToSwap.NodeModel.OutPorts.ToList();

            //connect inports
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
            //connect outports
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

            foreach (var connectionCommand in connectionCommands)
            {
                Model.dynamoViewModel.ExecuteCommand(connectionCommand);
            }

            
            DynamoModel.DeleteModelCommand delete = new DynamoModel.DeleteModelCommand(NodeToSwap.NodeModel.GUID);

            
            Model.dynamoViewModel.ExecuteCommand(delete);

            NodeToSwap = null;
            NodeToSwapName = "pending";
            CanRun = false;
            
        }

        private void OnLink(object o)
        {
            Process.Start("https://forum.dynamobim.com/t/graph-resizer-for-dynamo-2-13/75612");
        }
        private void OnClose(object o)
        {
            ResultsVisibility = false;
            RunCount = 0;
            NodeSwapperView win = o as NodeSwapperView;
           win.Close();
        }
    }
}
