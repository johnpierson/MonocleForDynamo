using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dynamo.Controls;
using Dynamo.Graph;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;

namespace MonocleViewExtension.NodeDocumentation
{

    public class NodeDocumentationModel
    {
        public DynamoView DynamoView { get; }
        public DynamoViewModel DynamoViewModel { get; }
        public ViewLoadedParams LoadedParams { get; }

        public NodeDocumentationModel(DynamoViewModel dvm, ViewLoadedParams loadedParams)
        {
            DynamoView = loadedParams.DynamoWindow as DynamoView;
            DynamoViewModel = dvm;
            LoadedParams = loadedParams;
        }

        public void SaveDyn(string path)
        {
            //dynamoViewModel.DoGraphAutoLayout("");
            DynamoViewModel.SaveAs(path,SaveContext.Save,false);

            try
            {
                DynamoViewModel.CurrentSpace.CurrentSelection.First().Deselect();
            }
            catch (Exception)
            {
                //suppress it all
            }
        }

        public void ExportImage(string path)
        {
            DynamoViewModel.SaveImage(path);
        }

        public void ExportMd(string nodeName, string imageName, string path, string content)
        {
            string documentation = $"## In Depth\n{content}\n___\n## Example File\n\n![{nodeName}](./{imageName})";

            File.WriteAllText(path,documentation);
        }
    }
}
