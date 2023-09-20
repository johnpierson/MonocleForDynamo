using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

        public void ExportImage(int mode, string path)
        {
            if (mode == 0)
            {
                DynamoViewModel.SaveImage(path);
            }

            //export out only the background, this uses a method that is available as far back as Dynamo 2.0.0
            if (mode == 1)
            {
                DynamoViewModel.BackgroundPreviewViewModel.ZoomToFitCommand.Execute(null);

                ImageSaveEventArgs backgroundImageArgs = new ImageSaveEventArgs(path);
                DynamoViewModel.OnRequestSave3DImage("dyn", backgroundImageArgs);
            }

            if (mode == 2)
            {
                DynamoViewModel.BackgroundPreviewViewModel.ZoomToFitCommand.Execute(null);

                //rename to indicate background
                string backgroundPath = path.Replace("img", "b");
                //export background first
                ImageSaveEventArgs backgroundImageArgs = new ImageSaveEventArgs(backgroundPath);
                DynamoViewModel.OnRequestSave3DImage("dyn", backgroundImageArgs);

                //now export graph view
                string graphViewPath = path.Replace("img", "f");
                DynamoViewModel.SaveImage(graphViewPath);

                var combined = OverlayImages(backgroundPath, graphViewPath,1.5);

                //save the combined as the original filename
                SaveBitmapToJpg(combined,path);

                //delete the other files
                try
                {
                    File.Delete(backgroundPath);
                }
                catch (Exception)
                {
                    //
                }

                try
                {
                    File.Delete(graphViewPath);
                }
                catch (Exception)
                {
                    //
                }
                

            }
        }

        public void ExportMd(string nodeName, string imageName, string path, string content)
        {
            string documentation = $"## In Depth\n{content}\n___\n## Example File\n\n![{nodeName}](./{imageName})";

            File.WriteAllText(path,documentation);
        }

        #region ExporterUtils
        //thanks to https://github.com/DynamoDS/ExportSampleImagesViewExtension

        /// <summary>
        /// Combines 2 bitmap images with transparent background
        /// </summary>
        /// <param name="background"></param>
        /// <param name="foreground"></param>
        /// <returns></returns>
        public static Bitmap OverlayImages(string background, string foreground, double scale = 1.0)
        {
            Bitmap finalImage;

            GetCurrentDPI(out var dpiX, out var dpiY);

            using (var baseImage = (Bitmap)Image.FromFile(background))
            {
                using (var overlayImage = (Bitmap)Image.FromFile(foreground))
                {
                    overlayImage.SetResolution(dpiX, dpiY);
                    var resizedImage = Resize(overlayImage, baseImage, scale);  // Resize the 3D background 

                    finalImage = new Bitmap(resizedImage.Width, resizedImage.Height, PixelFormat.Format32bppArgb);


                    finalImage.SetResolution(dpiX, dpiY);
                    try
                    {
                        var graphics = Graphics.FromImage(finalImage);

                        graphics.CompositingMode = CompositingMode.SourceOver;
                        graphics.Clear(Color.FromArgb(249, 249, 249)); // Set a Dynamo-white background
                        graphics.DrawImage(resizedImage, 0,
                            resizedImage.Height * (float)0.15); // Move the 3D Background a bit down
                        graphics.DrawImage(overlayImage,
                            Convert.ToInt32((resizedImage.Width - overlayImage.Width) * (float)0.5),
                            Convert.ToInt32((resizedImage.Height - overlayImage.Height) *
                                            (float)0.25)); // Offset the overlaid image in the upper center part 

                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
            }

            return finalImage;
        }
        /// <summary>
        ///     Resize logic for bitmap images.
        ///     Match target to source Image 
        /// </summary>
        /// <param name="sourceImg">The image to match the size to</param>
        /// <param name="targetImg">The image to be resized</param>
        /// <returns></returns>
        public static Bitmap Resize(Bitmap sourceImg, Bitmap targetImg, double scale = 1.0)
        {
            var scaleFactor = Math.Max(sourceImg.Width / (float)targetImg.Width,
                sourceImg.Height / (float)targetImg.Height);   // Take the smaller of the two ratios
            var newWidth = (int)(targetImg.Width * scaleFactor * scale);
            var newHeight = (int)(targetImg.Height * scaleFactor * scale);
            var newImage = new Bitmap(newWidth, newHeight);

            using (var graphics = Graphics.FromImage(newImage))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(targetImg, new Rectangle(0, 0, newWidth, newHeight));
                return newImage;
            }
        }
        /// <summary>
        ///     Retrieve the current system DPI settings
        ///     Uses reflection, does not need a Control
        /// </summary>
        private static void GetCurrentDPI(out int dpiX, out int dpiY)
        {
            var dpiXProperty =
                typeof(SystemParameters).GetProperty("DpiX", BindingFlags.NonPublic | BindingFlags.Static);
            var dpiYProperty =
                typeof(SystemParameters).GetProperty("Dpi", BindingFlags.NonPublic | BindingFlags.Static);

            dpiX = (int)dpiXProperty.GetValue(null, null);
            dpiY = (int)dpiYProperty.GetValue(null, null);
        }
        /// <summary>
        /// Saves a bitmap image to jpg
        /// </summary>
        /// <param name="image"></param>
        /// <param name="path"></param>
        public static void SaveBitmapToJpg(Bitmap image, string path)
        {
            if (image == null) return;
            image.Save(path, ImageFormat.Jpeg);
        }
        #endregion
    }
}
