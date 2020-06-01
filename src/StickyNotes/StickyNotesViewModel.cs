using System;
using Dynamo.Core;
using Dynamo.Extensions;
using Dynamo.Graph.Nodes;

namespace MonocleViewExtension.StickyNotes
{
    public class StickyNotesViewModel : NotificationObject, IDisposable
    {
        
        private ReadyParams readyParams;


        public StickyNotesViewModel(ReadyParams p)
        {
            readyParams = p;
            //p.CurrentWorkspaceModel.NodeAdded += CurrentWorkspaceModel_NodesChanged;
            //p.CurrentWorkspaceModel.NodeRemoved += CurrentWorkspaceModel_NodesChanged;
        }

        private void CurrentWorkspaceModel_NodesChanged(NodeModel obj)
        {
            RaisePropertyChanged();
        }

        public void Dispose()
        {
        }
    }
}
