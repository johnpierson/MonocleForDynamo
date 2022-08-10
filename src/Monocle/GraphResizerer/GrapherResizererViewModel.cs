using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Dynamo.Extensions;
using Dynamo.UI.Commands;
using Dynamo.ViewModels;

namespace MonocleViewExtension.GraphResizerer
{
    internal class GrapherResizererViewModel : ViewModelBase
    {
        public GrapherResizererModel Model { get; set; }
        private ReadyParams _readyParams;
        public DelegateCommand ResizeGraph { get; set; }
        public DelegateCommand Link { get; set; }
        public DelegateCommand Close { get; set; }

        private double _xScaleFactor;
        public double XScaleFactor
        {
            get { return _xScaleFactor; }
            set { _xScaleFactor = value; RaisePropertyChanged(() => XScaleFactor); }
        }
        private double _yScaleFactor;
        public double YScaleFactor
        {
            get { return _yScaleFactor; }
            set { _yScaleFactor = value; RaisePropertyChanged(() => YScaleFactor); }
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
        public GrapherResizererViewModel(GrapherResizererModel m)
        {
            Model = m;
            _readyParams = m.LoadedParams;

            XScaleFactor = 1.5;
            YScaleFactor = 2.25;

            ResizeGraph = new DelegateCommand(OnResizeGraph);
            Link = new DelegateCommand(OnLink);
            Close = new DelegateCommand(OnClose);

            ResultsVisibility = false;
        }
        private void OnResizeGraph(object o)
        {
            var changeCount = Model.ResizeGraph(XScaleFactor,YScaleFactor);

            Results = $"{changeCount} nodes and notes changed, please review your results before saving.";
            ResultsVisibility = true;
        }

        private void OnLink(object o)
        {
            Process.Start("https://forum.dynamobim.com/t/graph-resizer-for-dynamo-2-13/75612");
        }
        private void OnClose(object o)
        {
           GraphResizererView win = o as GraphResizererView;
           win.Close();
        }
    }
}
