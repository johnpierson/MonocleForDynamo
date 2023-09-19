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
        public NodeDocumentationViewModel(NodeDocumentationModel m)
        {
            Model = m;

            OnGetNode(null);

            FileExists = false;
            Description = "description";
            GetNodeCommand = new DelegateCommand(OnGetNode);
            CreateDocumentation = new DelegateCommand(OnCreateDocumentation);
            PickPathCommand = new DelegateCommand(OnPickPath);
        }
        private void OnGetNode(object o)
        {
            try
            {
                var selectedNode = Model.DynamoViewModel.CurrentSpace.CurrentSelection.First();
                FullNodeName = selectedNode.GetType().ToString();
                NodeName = selectedNode.Name;
                string basePath = @"D:\repos_john\DynamoRevit-NodeSamples\src\Documentation";
                Path = $"{basePath}";
                Description = selectedNode.Description;

                CheckIfDocsExist();
            }
            catch (Exception)
            {
               //suppress for now TODO: Add some kind of alert here
            }
            
        }
        private void OnCreateDocumentation(object o)
        {
            string dynPath = System.IO.Path.Combine(Path, $"{FullNodeName}.dyn");
            string imageName = $"{FullNodeName}_img.jpg";
            string imgPath = System.IO.Path.Combine(Path, imageName);
            string mdPath = System.IO.Path.Combine(Path, $"{FullNodeName}.md");


            //first save dyn
            Model.SaveDyn(dynPath);
            //now save image
            Model.ExportImage(imgPath);
            //then save md
            Model.ExportMd(NodeName, imageName, mdPath, ExtendedDescription);
        }

        private void OnPickPath(object o)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.ShowNewFolderButton = true;

            fbd.ShowDialog();

            if (!string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                Path = fbd.SelectedPath;
            }

            CheckIfDocsExist();
        }

        private void CheckIfDocsExist()
        {
            if (string.IsNullOrWhiteSpace(Path)) return;

            //check if the file exists to alert user
            var dynPath = System.IO.Path.Combine(Path, $"{FullNodeName}.dyn");
            FileExists = File.Exists(dynPath);

            if (FileExists)
            {
                NotificationMessage = "documentation already exists at given location. 🥺";
            }
        }

    }

   
}
