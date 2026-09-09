using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Dynamo.Graph.Nodes.CustomNodes;
using Dynamo.Graph.Nodes.ZeroTouch;
using Dynamo.UI.Commands;
using Dynamo.ViewModels;


namespace MonocleViewExtension.NodeDocumentation
{
    internal class NodeDocumentationViewModel : ViewModelBase
    {
        public NodeDocumentationModel Model { get; set; }
        public DelegateCommand GetNodeCommand { get; set; }
        public DelegateCommand PickPathCommand { get; set; }
        public DelegateCommand CreateDocumentation { get; set; }

        private string _path;
        public string Path
        {
            get => _path;
            set { _path = value; RaisePropertyChanged(nameof(Path)); }
        }
        private string _nodeName;
        public string NodeName
        {
            get => _nodeName;
            set { _nodeName = value; RaisePropertyChanged(nameof(NodeName)); }
        }
        private string _fullNodeName;
        public string FullNodeName
        {
            get => _fullNodeName;
            set { _fullNodeName = value; RaisePropertyChanged(nameof(FullNodeName)); }
        }
        private string _description;
        public string Description
        {
            get => _description;
            set { _description = value; RaisePropertyChanged(nameof(Description)); }
        }
        private string _extendedDescription;
        public string ExtendedDescription
        {
            get => _extendedDescription;
            set { _extendedDescription = value; RaisePropertyChanged(nameof(ExtendedDescription)); }
        }

        private bool _fileExists;
        public bool FileExists
        {
            get => _fileExists;
            set { _fileExists = value; RaisePropertyChanged(nameof(FileExists)); }
        }
        private string _notificationMessage;
        public string NotificationMessage
        {
            get => _notificationMessage;
            set { _notificationMessage = value; RaisePropertyChanged(nameof(NotificationMessage)); }
        }

        private bool[] _imgModeArray = new bool[] { true, false, false };
        public bool[] ImgModeArray
        {
            get { return _imgModeArray; }
        }
        public int SelectedImgMode
        {
            get { return Array.IndexOf(_imgModeArray, true); }
        }

        private bool _canGetNode;
        public bool CanGetNode
        {
            get => _canGetNode;
            set { _canGetNode = value; RaisePropertyChanged(nameof(CanGetNode)); }
        }
        private bool _canDocumentNode;
        public bool CanDocumentNode
        {
            get => _canDocumentNode;
            set { _canDocumentNode = value; RaisePropertyChanged(nameof(CanDocumentNode)); }
        }

        private NodeDocumentation _nodeDocumentation;

        public NodeDocumentationViewModel(NodeDocumentationModel m)
        {
            Model = m;

            FileExists = false;
            GetNodeCommand = new DelegateCommand(OnGetNode);
            CreateDocumentation = new DelegateCommand(OnCreateDocumentation);
            PickPathCommand = new DelegateCommand(OnPickPath);

            CanGetNode = false;
            CanDocumentNode = false;
        }
        private void OnGetNode(object o)
        {
            try
            {
                var selectedNode = Model.DynamoViewModel.CurrentSpace.CurrentSelection.First();
                
                string nodeFullName;

                
                switch (selectedNode)
                {
                    case DSFunction dsFunction:
                        string fullSignature = dsFunction.FunctionSignature;
                        nodeFullName = fullSignature.Split('@')[0];
                        break;
                    case Function function:
                        string fullFunctionSignature = function.FunctionSignature.ToString();
                        nodeFullName = fullFunctionSignature.Split('@')[0];
                        break;
                    default:
                        nodeFullName = selectedNode.GetType().ToString();
                        break;
                }


                _nodeDocumentation =
                    new NodeDocumentation(Path, nodeFullName, selectedNode.Name)
                    {
                        Description = selectedNode.Description
                    };
                
                NodeName = _nodeDocumentation.NodeName;
                FullNodeName = _nodeDocumentation.FullNodeName;
                Description = _nodeDocumentation.Description;

                CheckIfDocsExist();

                CanDocumentNode = true;
            }
            catch (Exception)
            {
                //suppress for now TODO: Add some kind of alert here
            }

        }
        private void OnCreateDocumentation(object o)
        {
            if(!CanDocumentNode) return;

            if (!TrySynchronizeDocumentation()) return;

            try
            {
                //first save dyn
                Model.SaveDyn(_nodeDocumentation.SampleGraph);
                if (!File.Exists(_nodeDocumentation.SampleGraph))
                {
                    throw new IOException($"Dynamo did not export the sample graph to '{_nodeDocumentation.SampleGraph}'.");
                }

                //now save image
                Model.ExportImage(SelectedImgMode, _nodeDocumentation.SampleGraphImagePath);
                if (!File.Exists(_nodeDocumentation.SampleGraphImagePath))
                {
                    throw new IOException($"Dynamo did not export the sample image to '{_nodeDocumentation.SampleGraphImagePath}'.");
                }

                //then save md if an extended description exists
                if (!string.IsNullOrWhiteSpace(ExtendedDescription))
                {
                    Model.ExportMd(NodeName, _nodeDocumentation.SampleGraphImage, _nodeDocumentation.MarkdownPath, ExtendedDescription);
                }
                else
                {
                    Model.ExportMdForSampleOnly(NodeName, _nodeDocumentation.SampleGraphImage, _nodeDocumentation.MarkdownPath);
                }

                FileExists = false;
                NotificationMessage = "Documentation created successfully.";
            }
            catch (Exception exception)
            {
                FileExists = true;
                NotificationMessage = $"Documentation export failed: {exception.Message}";
            }
        }

        private void OnPickPath(object o)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.ShowNewFolderButton = true;

            fbd.ShowDialog();

            if (!string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                Path = fbd.SelectedPath;
                CanGetNode = true;
            }

            CheckIfDocsExist();
        }

        private void CheckIfDocsExist()
        {
            if (string.IsNullOrWhiteSpace(Path) || string.IsNullOrWhiteSpace(FullNodeName)) return;

            NodeDocumentation documentation;
            try
            {
                documentation = CreateDocumentationFromCurrentValues();
            }
            catch (ArgumentException)
            {
                return;
            }

            //check if the file exists to alert user
            var dynPath = documentation.SampleGraph;
            var mdPath = documentation.MarkdownPath;
            var dynExists = File.Exists(dynPath);
            var mdExists = File.Exists(mdPath);

            FileExists = dynExists || mdExists;

            if (dynExists && mdExists)
            {
                NotificationMessage = "documentation already exists at given location. 🥺";

                try
                {
                    documentation.ReadMarkdown();
                    ExtendedDescription = documentation.FullDescription;
                }
                catch (Exception exception)
                {
                    NotificationMessage = $"Documentation exists, but its markdown could not be read: {exception.Message}";
                }
            }
            else if (dynExists)
            {
                NotificationMessage = "A sample graph exists, but its markdown file is missing.";
            }
            else if (mdExists)
            {
                NotificationMessage = "A markdown file exists, but its sample graph is missing.";
            }
        }

        private NodeDocumentation CreateDocumentationFromCurrentValues()
        {
            return new NodeDocumentation(Path, FullNodeName, NodeName)
            {
                Description = Description,
                FullDescription = ExtendedDescription
            };
        }

        private bool TrySynchronizeDocumentation()
        {
            try
            {
                var documentation = CreateDocumentationFromCurrentValues();
                documentation.Validate();
                _nodeDocumentation = documentation;
                return true;
            }
            catch (Exception exception)
            {
                FileExists = true;
                NotificationMessage = $"Documentation export cannot start: {exception.Message}";
                return false;
            }
        }

    }


}
