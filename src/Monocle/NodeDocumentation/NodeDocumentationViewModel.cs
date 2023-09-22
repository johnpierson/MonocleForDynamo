using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
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
                
                _nodeDocumentation =
                    new NodeDocumentation(Path, selectedNode.GetType().ToString(), selectedNode.Name)
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
            _nodeDocumentation.FullDescription = ExtendedDescription;
            //first save dyn
            Model.SaveDyn(_nodeDocumentation.SampleGraph);

            //now save image
            Model.ExportImage(SelectedImgMode,_nodeDocumentation.SampleGraphImagePath);
            
            //then save md
            Model.ExportMd(NodeName, _nodeDocumentation.SampleGraphImage, _nodeDocumentation.MarkdownPath, ExtendedDescription);
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
            if (string.IsNullOrWhiteSpace(Path)) return;

            //check if the file exists to alert user
            var dynPath = System.IO.Path.Combine(Path, $"{FullNodeName}.dyn");
            var mdPath = System.IO.Path.Combine(Path, $"{FullNodeName}.md");

            FileExists = File.Exists(dynPath);

            if (FileExists)
            {
                NotificationMessage = "documentation already exists at given location. 🥺";

                _nodeDocumentation.ReadMarkdown();

                ExtendedDescription = _nodeDocumentation.FullDescription;
            }
        }

    }


}
