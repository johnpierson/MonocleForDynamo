using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Dynamo.Extensions;
using Dynamo.Graph.Workspaces;
using Dynamo.Models;
using Dynamo.UI.Commands;
using Dynamo.ViewModels;
using MonocleViewExtension.GraphResizerer;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace MonocleViewExtension.Snippets
{
    internal class SnippetsViewModel : ViewModelBase
    {
        public SnippetsModel Model { get; set; }
        private ReadyParams _readyParams;
        public DelegateCommand LoadSnippets { get; set; }

        private string _directoryPath;
        public string DirectoryPath
        {
            get { return _directoryPath; }
            set { _directoryPath = value; RaisePropertyChanged(() => DirectoryPath); }
        }

        private List<WorkspaceModel> _workspaceSnippets { get; set; }
        public List<WorkspaceModel> WorkspaceSnippets
        {
            get { return _workspaceSnippets; }
            set { _workspaceSnippets = value; RaisePropertyChanged(() => WorkspaceSnippets);
            }
        }
        public SnippetsViewModel(SnippetsModel m)
        {
            Model = m;
            _readyParams = m.LoadedParams;

            LoadSnippets = new DelegateCommand(OnLoadSnippets);
        }
        private void OnLoadSnippets(object o)
        {
            if (string.IsNullOrWhiteSpace(DirectoryPath))
            {
                //TODO: Implement search for DYNs in folder
                using (var fbd = new FolderBrowserDialog())
                {
                    DialogResult result = fbd.ShowDialog();

                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    {
                        //set the path to what the user selected
                        DirectoryPath = fbd.SelectedPath;

                        string[] files = Directory.GetFiles(fbd.SelectedPath,"*.dyn");

                        //clear the existing snips
                        //WorkspaceSnippets.Clear();
                        List<WorkspaceModel> newSnippets = new List<WorkspaceModel>();

                        foreach (string filePath in files)
                        {
                            string fileContents;
                            if (DynamoUtilities.PathHelper.isValidJson(filePath, out fileContents, out Exception ex))
                            {
                                var wm = WorkspaceModel.FromJson(fileContents, null, Model.dynamoViewModel.EngineController,
                                    Model.dynamoViewModel.Model.Scheduler, Model.dynamoViewModel.Model.NodeFactory,
                                    false, false, Model.dynamoViewModel.Model.CustomNodeManager,
                                    Model.dynamoViewModel.Model.LinterManager);
                                newSnippets.Add(wm);
                            }
                        }
                        WorkspaceSnippets = newSnippets;
                        
                    }
                }
            }
        }
    }
}
