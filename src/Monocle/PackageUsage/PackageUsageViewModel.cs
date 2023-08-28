using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Dynamo.Extensions;
using Dynamo.Graph.Nodes;
using Dynamo.UI.Commands;
using Dynamo.ViewModels;
using MonocleViewExtension.Utilities;

//using GalaSoft.MvvmLight.Command;

namespace MonocleViewExtension.PackageUsage
{
    public class PackageUsageViewModel : ViewModelBase
    {
        public PackageUsageModel Model { get; set; }
        private readonly ReadyParams _readyParams;

        public DelegateCommand AnnotateNodes { get; set; }
        public DelegateCommand ClearNotes { get; set; }
        public DelegateCommand SendToClipboard { get; set; }

        public DelegateCommand HighlightCustomNodes { get; set; }
        public DelegateCommand ResetHighlightCustomNodes { get; set; }

        private string _result;
        public string Result
        {
            get { return _result; }
            set { _result = value; RaisePropertyChanged(nameof(Result)); }
        }

        private string _customNodePrefix;
        public string CustomNodePrefix
        {
            get { return _customNodePrefix; }
            set { _customNodePrefix = value; RaisePropertyChanged(nameof(CustomNodePrefix)); }
        }

        private ObservableCollection<PackageUsageWrapper> _activeCustomNodes;
        public ObservableCollection<PackageUsageWrapper> ActiveCustomNodes
        {
            get { return _activeCustomNodes; }
            set { _activeCustomNodes = value; RaisePropertyChanged(nameof(ActiveCustomNodes)); }
        }

        private string _packagesInUse;
        public string PackagesInUse
        {
            get
            {
                return _packagesInUse;
            }
            set { _packagesInUse = value; RaisePropertyChanged(nameof(PackagesInUse)); }
        }

        public string DisplayImage
        {
            get
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    return "/MonocleViewExtension;component/PackageUsage/DogeImages/cheems_heaven.jpg";
                }


                int switcher = DateTime.Now.Month;

                if (DateTime.Now.Month == 1 && DateTime.Now.Day <=7)
                {
                    switcher = 2;
                }

                int day = DateTime.Now.Day;

                switch (switcher)
                {
                    case 2:
                        return "/MonocleViewExtension;component/PackageUsage/DogeImages/doge_newyear.png";
                    case 10:
                        var randomHalloween = new Random();
                        List<string> possibleHalloween = new List<string>
                        {
                            "/MonocleViewExtension;component/PackageUsage/DogeImages/dogePreview.png",
                            "/MonocleViewExtension;component/PackageUsage/DogeImages/dogePreview.png",
                            "/MonocleViewExtension;component/PackageUsage/DogeImages/dogePreview.png",
                            "/MonocleViewExtension;component/PackageUsage/DogeImages/doge_halloween.gif",
                        };
                        var selector = randomHalloween.Next(3);
                        return day == 1 ? "/MonocleViewExtension;component/PackageUsage/DogeImages/dwight-pumpkin.gif" : possibleHalloween[selector];
                      
                    case 12:                       
                        return "/MonocleViewExtension;component/PackageUsage/DogeImages/doge_christmas.jpg";
                    case 7:                       
                        return "/MonocleViewExtension;component/PackageUsage/DogeImages/doge_summer.jpg";
                    default:
                        var rand = new Random();
                        List<string> possibleOthers = new List<string>
                        {
                            "/MonocleViewExtension;component/PackageUsage/DogeImages/dogePreview.png",
                            "/MonocleViewExtension;component/PackageUsage/DogeImages/dogePreview.png",
                            "/MonocleViewExtension;component/PackageUsage/DogeImages/doge_dapper.jpg",
                            "/MonocleViewExtension;component/PackageUsage/DogeImages/doge_business.jpg",
                            "/MonocleViewExtension;component/PackageUsage/DogeImages/doge-comfort.png",
                            "/MonocleViewExtension;component/PackageUsage/DogeImages/doge-comfort.png",
                            "/MonocleViewExtension;component/PackageUsage/DogeImages/doge_heaven.jpg",
                            "/MonocleViewExtension;component/PackageUsage/DogeImages/cheems_heaven.jpg"
                        };
                        return possibleOthers[rand.Next(7)];
                }
            }
        }

        public PackageUsageViewModel(PackageUsageModel m)
        {
            Model = m;

            _readyParams = m.LoadedParams;

            ActiveCustomNodes = Model.GetCustomNodeInfos();
            PackagesInUse = string.Join("\n", ActiveCustomNodes.Select(c => c.PackageName).Distinct());

            _readyParams.CurrentWorkspaceModel.NodeAdded += CurrentWorkspaceModel_NodesChanged;
            _readyParams.CurrentWorkspaceModel.NodeRemoved += CurrentWorkspaceModel_NodesChanged;

            _customNodePrefix = Globals.CustomNodeNotePrefix;

            AnnotateNodes = new DelegateCommand(OnAnnotateNodes);
            ClearNotes = new DelegateCommand(OnClearNotes);
            SendToClipboard = new DelegateCommand(OnSendToClipboard);
            HighlightCustomNodes = new DelegateCommand(OnHighlightCustomNodes);
            ResetHighlightCustomNodes = new DelegateCommand(OnResetHighlightCustomNodes);
        }

        private void OnHighlightCustomNodes(object o)
        {
            Model.HighlightCustomNodes();
        }
        private void OnResetHighlightCustomNodes(object o)
        {
            Model.ResetCustomNodeHighlights();
        }
        private void OnAnnotateNodes(object o)
        {
            var count = Model.AddPackageNote();

            Result = $"{count} notes added/updated.";
        }

        private void OnClearNotes(object o)
        {
            var count = Model.ClearNotes();

            Result = $"{count} notes cleared.";
        }

        private void OnSendToClipboard(object o)
        {
            if (ActiveCustomNodes.Any())
            {
                Clipboard.SetText(string.Join("\n", ActiveCustomNodes.Select(c => $"{c.PackageName} | {c.PackageVersion}")));
            }
        }

        private void CurrentWorkspaceModel_NodesChanged(NodeModel obj)
        {
            ActiveCustomNodes = Model.GetCustomNodeInfos();
            PackagesInUse = string.Join("\n", ActiveCustomNodes.Select(c => c.PackageName).Distinct());
        }

        public override void Dispose()
        {
            _readyParams.CurrentWorkspaceModel.NodeAdded -= CurrentWorkspaceModel_NodesChanged;
            _readyParams.CurrentWorkspaceModel.NodeRemoved -= CurrentWorkspaceModel_NodesChanged;
        }
    }
}
