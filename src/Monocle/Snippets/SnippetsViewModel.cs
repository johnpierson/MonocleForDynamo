using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Dynamo.Extensions;
using Dynamo.Graph;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Workspaces;
using Dynamo.Models;
using Dynamo.Search.SearchElements;
using Dynamo.UI.Commands;
using Dynamo.Utilities;
using Dynamo.ViewModels;
using MonocleViewExtension.GraphResizerer;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using Button = System.Windows.Controls.Button;
using Image = System.Drawing.Image;
using Thickness = System.Windows.Thickness;

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
            set { _directoryPath = value; RaisePropertyChanged(nameof(DirectoryPath));}
        }

        private List<WorkspaceModel> _workspaceSnippets { get; set; }
        public List<WorkspaceModel> WorkspaceSnippets
        {
            get { return _workspaceSnippets; }
            set { _workspaceSnippets = value; RaisePropertyChanged(nameof(WorkspaceSnippets));
            }
        }

        private  List<Button> _workspaceButtons { get; set; }
        public List<Button> WorkspaceButtons
        {
            get { return _workspaceButtons; }
            set
            {
                _workspaceButtons = value;
                RaisePropertyChanged(nameof(WorkspaceButtons));
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
                        List<Button> newButtons = new List<Button>();

                        foreach (string filePath in files)
                        {
                            string fileContents;
                            if (DynamoUtilities.PathHelper.isValidJson(filePath, out fileContents, out Exception ex))
                            {
                                HomeWorkspaceModel wm = WorkspaceModel.FromJson(fileContents, null, Model.dynamoViewModel.EngineController,
                                    Model.dynamoViewModel.Model.Scheduler, Model.dynamoViewModel.Model.NodeFactory,
                                    false, true, Model.dynamoViewModel.Model.CustomNodeManager,
                                    Model.dynamoViewModel.Model.LinterManager) as HomeWorkspaceModel;
                                newSnippets.Add(wm);
                                
                                //make the image brush
                                ImageBrush imageBrush = new ImageBrush(Convert(wm.Thumbnail))
                                {
                                    Stretch = Stretch.Uniform
                                };
                                //make the textbox too
                                Label buttonLabel = new Label();
                                buttonLabel.Text = wm.Name;
                                buttonLabel.TextAlign = ContentAlignment.BottomCenter;
                                System.Windows.Controls.Button btn = new Button
                                {
                                    Background = imageBrush,
                                    Width = 200,
                                    Height = 200,
                                    Margin = new Thickness(12),
                                    Content = wm.Name,
                                    VerticalContentAlignment = VerticalAlignment.Bottom,
                                    Foreground = new SolidColorBrush(Colors.White),
                                };
                                btn.Click += BtnOnClick;
                                newButtons.Add(btn);
                            }
                        }
                        WorkspaceSnippets = newSnippets;
                        WorkspaceButtons = newButtons;
                    }
                }
            }
        }

        private void BtnOnClick(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            var wmName = btn.Content.ToString();

            WorkspaceModel wm = WorkspaceSnippets.First(w => w.Name.Equals(wmName));

            var currentWm = Model.dynamoViewModel.Model.CurrentWorkspace;
            
            var dynMethod = typeof(NodeSearchElement).GetMethod("ConstructNewNodeModel",
                BindingFlags.NonPublic | BindingFlags.Instance);


       
            ObservableCollection<ModelBase> models = new ObservableCollection<ModelBase>();
            //models.AddRange(wm.Annotations);
  
            foreach (var node in wm.Nodes)
            {
                models.Add(node);
            }
            
            foreach (var connector in wm.Connectors)
            {
                models.Add(connector);
            }

            WorkspaceViewModel vm = new WorkspaceViewModel(wm, Model.dynamoViewModel);
            vm.SelectAllCommand.Execute(null);
            
            Model.dynamoViewModel.Model.Copy(); 

            Model.dynamoViewModel.Model.Paste();
        }

        public BitmapImage Convert(string value)
        {
            string s = value as string;

            if (string.IsNullOrWhiteSpace(s))
            {
                BitmapImage a = new BitmapImage();

                a.BeginInit();
                a.StreamSource = new MemoryStream(System.Convert.FromBase64String(@"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"));
                a.EndInit();

                return a;
            }


            BitmapImage bi = new BitmapImage();

            bi.BeginInit();
            bi.StreamSource = new MemoryStream(System.Convert.FromBase64String(s));
            bi.EndInit();

            return bi;
        }
    }
}
