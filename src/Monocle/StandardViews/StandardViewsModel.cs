using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Dynamo.Controls;
using Dynamo.Graph.Workspaces;
using Dynamo.Models;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;

namespace MonocleViewExtension.StandardViews
{
    internal class StandardViewsModel
    {

        public DynamoView dynamoView { get; }
        public ViewLoadedParams LoadedParams { get; }
        public DynamoViewModel DynamoViewModel { get; }
        public StandardViewsModel(ViewLoadedParams p)
        {
            dynamoView = p.DynamoWindow as DynamoView;
            
            LoadedParams = p;
            DynamoViewModel = p.DynamoWindow.DataContext as DynamoViewModel;
        }

        
    }
}
