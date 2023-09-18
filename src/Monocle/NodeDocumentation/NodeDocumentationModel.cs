using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dynamo.Controls;
using Dynamo.Graph;
using Dynamo.Models;
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

            //if (File.Exists(path))
            //{
            //    System.Drawing.Bitmap bitmap = new Bitmap(path);

            //    var filled = Transparent2Color(bitmap, Color.White);

            //    filled.Save(path);
            //}
        }

        public void ExportMd(string nodeName, string imageName, string path, string content)
        {
            string documentation = $"## In Depth\n{content}\n___\n## Example File\n\n![{nodeName}](./{imageName})";

            File.WriteAllText(path,documentation);
        }

        Bitmap Transparent2Color(Bitmap bmp1, Color target)
        {
            Bitmap bmp2 = new Bitmap(bmp1.Width, bmp1.Height);
            Rectangle rect = new Rectangle(Point.Empty, bmp1.Size);
            using (Graphics G = Graphics.FromImage(bmp2))
            {
                G.Clear(target);
                G.DrawImageUnscaledAndClipped(bmp1, rect);
            }
            return bmp2;
        }
    }
}
