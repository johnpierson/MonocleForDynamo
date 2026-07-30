using System;
using System.Diagnostics;
using Dynamo.UI.Commands;
using Dynamo.ViewModels;
using MonocleViewExtension.Core;

namespace MonocleViewExtension.GraphResizerer
{
    internal class GraphResizererViewModel : ViewModelBase
    {
        public GraphResizererModel Model { get; set; }
        public DelegateCommand ResizeGraph { get; set; }
        public DelegateCommand Link { get; set; }
        public DelegateCommand Close { get; set; }

        private double _xScaleFactor;
        public double XScaleFactor
        {
            get { return _xScaleFactor; }
            set { _xScaleFactor = value; RaisePropertyChanged(nameof(XScaleFactor)); }
        }
        private double _yScaleFactor;
        public double YScaleFactor
        {
            get { return _yScaleFactor; }
            set { _yScaleFactor = value; RaisePropertyChanged(nameof(YScaleFactor)); }
        }
        private string _results;
        public string Results
        {
            get { return _results; }
            set { _results = value; RaisePropertyChanged(nameof(Results)); }
        }
        private bool _resultsVisibility;
        public bool ResultsVisibility
        {
            get { return _resultsVisibility; }
            set { _resultsVisibility = value; RaisePropertyChanged(nameof(ResultsVisibility)); }
        }
        private int _runCount;
        public int RunCount
        {
            get { return _runCount; }
            set { _runCount = value; RaisePropertyChanged(nameof(RunCount)); }
        }
        public GraphResizererViewModel(GraphResizererModel m)
        {
            Model = m;

            XScaleFactor = 1.5;
            YScaleFactor = 2.25;

            RunCount = 0;

            ResizeGraph = new DelegateCommand(OnResizeGraph);
            Link = new DelegateCommand(OnLink);
            Close = new DelegateCommand(OnClose);

            ResultsVisibility = false;

            Model.SetRunStatus();
        }
        private void OnResizeGraph(object o)
        {
            // Fires on every tick of a slider drag, so a failure must not throw into the
            // dispatcher and take Dynamo down with it.
            try
            {
                //if it is the first run, then store the original locations
                if (RunCount == 0)
                {
                    Model.GetNodes();
                }
                var changeCount = Model.ResizeGraph(XScaleFactor, YScaleFactor);
                RunCount++;

                Results = $"{changeCount} nodes and notes changed, please review your results before saving.";
                ResultsVisibility = true;
            }
            catch (Exception e)
            {
                new MonocleLog(Model.dynamoViewModel).Error("Could not resize the graph.", e);
                Results = "Could not resize the graph. See the Dynamo log for details.";
                ResultsVisibility = true;
            }
        }

        private void OnLink(object o)
        {
            Process.Start("https://forum.dynamobim.com/t/graph-resizer-for-dynamo-2-13/75612");
        }
        private void OnClose(object o)
        {
            ResultsVisibility = false;
            RunCount = 0;
            GraphResizererView win = o as GraphResizererView;
           win.Close();
        }
    }
}
