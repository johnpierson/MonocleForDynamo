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
        public DynamoView dynamoView { get; }
        public DynamoViewModel dynamoViewModel { get; }
        public ViewLoadedParams LoadedParams { get; }

        public NodeDocumentationModel(DynamoViewModel dvm, ViewLoadedParams loadedParams)
        {
            dynamoView = loadedParams.DynamoWindow as DynamoView;
            dynamoViewModel = dvm;
            LoadedParams = loadedParams;
        }

        public void SaveDyn(string path)
        {
            //dynamoViewModel.DoGraphAutoLayout("");
            dynamoViewModel.SaveAs(path,SaveContext.Save,false);

            try
            {
                dynamoViewModel.CurrentSpace.CurrentSelection.First().Deselect();
            }
            catch (Exception e)
            {
            }
        }

        public void ExportImage(string path)
        {
            dynamoViewModel.SaveImage(path);
        }

        public void ExportMd(string nodeName, string path, string content)
        {
            string documentation = $"## In Depth\n{content}\n___\n## Example File\n\n![{nodeName}](./{nodeName}.png)";

            File.WriteAllText(path,documentation);
        }
    }
}
