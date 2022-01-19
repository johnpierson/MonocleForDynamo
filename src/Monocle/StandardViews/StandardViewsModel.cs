using Dynamo.Controls;
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
