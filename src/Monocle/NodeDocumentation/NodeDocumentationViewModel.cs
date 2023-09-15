using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dynamo.UI.Commands;
using Dynamo.ViewModels;

namespace MonocleViewExtension.NodeDocumentation
{
    internal class NodeDocumentationViewModel : ViewModelBase
    {
        public NodeDocumentationModel Model { get; set; }
        public DelegateCommand GetNodeCommand { get; set; }
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
        public NodeDocumentationViewModel(NodeDocumentationModel m)
        {
            Model = m;

            try
            {
               OnGetNode(null);
            }
            catch (Exception e)
            {
                //
            }

            Description = "description";
            GetNodeCommand = new DelegateCommand(OnGetNode);
            CreateDocumentation = new DelegateCommand(OnCreateDocumentation);
        }
        private void OnGetNode(object o)
        {
            var selectedNode = Model.DynamoViewModel.CurrentSpace.CurrentSelection.First();
            FullNodeName = selectedNode.GetType().ToString();
            NodeName = selectedNode.Name;
            string basePath = @"D:\repos_john\DynamoRevit-NodeSamples\src\Documentation";
            Path = $"{basePath}";
            Description = selectedNode.Description;
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
            Model.ExportMd(NodeName, imageName, mdPath, Description);
        }
    }
}
