using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dynamo.Controls;
using Dynamo.Graph;
using Dynamo.Graph.Connectors;
using Dynamo.Utilities;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using MonocleViewExtension.Utilities;

namespace MonocleViewExtension.BetterSave
{
    internal class BetterSaveModel
    {
        public DynamoView dynamoView { get; }
        public DynamoViewModel dynamoViewModel { get; }
        public ViewLoadedParams LoadedParams { get; }

        public BetterSaveModel(DynamoViewModel dvm, ViewLoadedParams loadedParams)
        {
            dynamoView = loadedParams.DynamoWindow as DynamoView;
            dynamoViewModel = dvm;
            LoadedParams = loadedParams;
        }
        public void BetterSave(string command)
        {
            if(string.IsNullOrWhiteSpace(dynamoViewModel.CurrentSpace.FileName)) return;

            var originalName = dynamoViewModel.CurrentSpace.FileName;

            var timestamp = $"{DateTime.Now.ToString(Globals.QuickSaveDateFormat)}.dyn";

            var nameWithTimestamp = originalName.Replace(".dyn", timestamp);

            switch (command)
            {
                case "QuickSave":
                    dynamoViewModel.SaveAs(nameWithTimestamp, SaveContext.Copy,true);
                    break;
            }
        }
    }
}
