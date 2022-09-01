using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Dynamo.Graph.Workspaces;
using Dynamo.Models;
using Dynamo.ViewModels;

namespace MonocleViewExtension.Photocopier
{
    internal class PhotocopierViewModel : ViewModelBase
    {
        public PhotocopierModel Model { get; set; }
        private bool _available;
        public bool Available
        {
            get { return _available; }
            set { _available = value; RaisePropertyChanged(() => Available); }
        }
        private string _dynName;
        public string DynName
        {
            get { return _dynName; }
            set { _dynName = value; RaisePropertyChanged(() => DynName); }
        }
        private int _sequence;
        public int Sequence
        {
            get { return _sequence; }
            set { _sequence = value; RaisePropertyChanged(() => Sequence); }
        }
        public PhotocopierViewModel(PhotocopierModel model)
        {
            Model = model;
            DynName = model.dynamoViewModel.CurrentSpace.FileName;

            if (string.IsNullOrWhiteSpace(DynName))
            {
                Available = false;
            }

            Sequence = 0;
            Model.dynamoViewModel.Model.EvaluationCompleted += EvaluationCompleted;
        }

        private void EvaluationCompleted(object sender, EventArgs e)
        {
            Thread.Sleep(1000);
            Model.ExportImage(DynName, Sequence);
            Sequence++;
        }

       
      
    }
}
