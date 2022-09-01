using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Dynamo.Controls;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;

namespace MonocleViewExtension.Photocopier
{
    internal class PhotocopierModel
    {
        public DynamoView dynamoView { get; }
        public DynamoViewModel dynamoViewModel { get; }
        public ViewLoadedParams LoadedParams { get; }

        public PhotocopierModel(DynamoViewModel dvm, ViewLoadedParams loadedParams)
        {
            dynamoView = loadedParams.DynamoWindow as DynamoView;
            dynamoViewModel = dvm;
            LoadedParams = loadedParams;
        }

        public void ExportImage(string dynName, int sequence)
        {
            FileInfo fInfo = new FileInfo(dynName);
           
            string canvasImage = $"{fInfo.DirectoryName}//{fInfo.Name}_Canvas-{sequence}.png";
            string geometryImage = $"{fInfo.DirectoryName}//{fInfo.Name}_Geometry-{sequence}.png";
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                dynamoViewModel.OnRequestSaveImage("dyn", new ImageSaveEventArgs(geometryImage));
                dynamoViewModel.OnRequestSave3DImage("dyn", new ImageSaveEventArgs(geometryImage));
            }));
        }
    }
}
