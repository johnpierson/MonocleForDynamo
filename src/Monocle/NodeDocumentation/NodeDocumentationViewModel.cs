using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dynamo.Extensions;
using Dynamo.UI.Commands;
using Dynamo.ViewModels;

namespace MonocleViewExtension.NodeDocumentation
{
    internal class NodeDocumentationViewModel : ViewModelBase
    {
        public NodeDocumentationModel Model { get; set; }
        private ReadyParams _readyParams;
        public DelegateCommand CreateDocumentation { get; set; }

        private string _path;
        public string Path
        {
            get { return _path; }
            set { _path = value; RaisePropertyChanged(nameof(Path)); }
        }
        private string _nodeName;
        public string NodeName
        {
            get { return _nodeName; }
            set { _nodeName = value; RaisePropertyChanged(nameof(NodeName)); }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set { _description = value; RaisePropertyChanged(nameof(Description)); }
        }

        public NodeDocumentationViewModel(NodeDocumentationModel m)
        {
            Model = m;

            _readyParams = m.LoadedParams;
            try
            {
                NodeName = m.dynamoViewModel.CurrentSpace.CurrentSelection.First().Name;
                string basePath = @"D:\Autodesk\Generative BIM - Dynamo Dictionary\";
                Path = $"{basePath}{NodeName}";
            }
            catch (Exception)
            {
                //
            }


            Description = "description";

            CreateDocumentation = new DelegateCommand(OnCreateDocumentation);
        }

        private void OnCreateDocumentation(object o)
        {
            string dynPath = System.IO.Path.Combine(Path, $"{NodeName}.dyn");
            string imgPath = System.IO.Path.Combine(Path, $"{NodeName}.png");
            string mdPath = System.IO.Path.Combine(Path, $"{NodeName}.md");

            //first save dyn
            Model.SaveDyn(dynPath);
            //now save image
            Model.ExportImage(imgPath);
            //then save md
            Model.ExportMd(NodeName, mdPath,Description);
        }
    }
}
