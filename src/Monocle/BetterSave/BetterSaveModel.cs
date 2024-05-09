using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dynamo.Controls;
using Dynamo.Core;
using Dynamo.Graph;
using Dynamo.Models;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using MonocleViewExtension.Utilities;

namespace MonocleViewExtension.BetterSave
{
    internal class BetterSaveModel : NotificationObject
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
            if (string.IsNullOrWhiteSpace(dynamoViewModel.CurrentSpace.FileName))
            {
                SloppySave();
                return;
            };

            var originalName = dynamoViewModel.CurrentSpace.FileName;

            var timestamp = $"{DateTime.Now.ToString(Globals.QuickSaveDateFormat)}.dyn";

            var nameWithTimestamp = originalName.Replace(".dyn", timestamp);

            switch (command)
            {
                case "QuickSave":
                    dynamoViewModel.SaveAs(nameWithTimestamp, SaveContext.Copy,true);
                    break;
                case "SaveWithNewGuids":
                    MakeWorkspaceUnique(originalName);
                    break;
                case "SloppySave":
                    SloppySave();
                    break;
            }
        }

        private void MakeWorkspaceUnique(string path)
        {
            //first save it as is
            dynamoViewModel.SaveAs(path, SaveContext.Save, false);

            //close it
            dynamoViewModel.CloseHomeWorkspaceCommand.Execute(null);


            string jsonData = File.ReadAllText(path);

            string pattern = @"([a-z0-9]{32})";
            string updatedJsonData = jsonData;

            // The unique collection of Guids
            var mc = Regex.Matches(jsonData, pattern)
                .Cast<Match>()
                .Select(m => m.Value)
                .Distinct();

            foreach (var match in mc)
            {
                updatedJsonData = updatedJsonData.Replace(match, Guid.NewGuid().ToString("N"));
            }

            //file uuid match
            string filePattern = @"([a-z0-9]{8}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{12})";
            var fileMc = Regex.Matches(updatedJsonData, filePattern).Cast<Match>().Select(m => m.Value).Distinct();
            foreach (var match in fileMc)
            {
                updatedJsonData = updatedJsonData.Replace(match, Guid.NewGuid().ToString());
            }

            File.WriteAllText(path,updatedJsonData);


            //reopen it
            dynamoViewModel.OpenCommand.Execute(path);
        }

        private void SloppySave()
        {
            var dogeWords = new List<string>()
            {
                "very",
                "wow",
                "much",
                "heckin",
                "many",
                "so"
            };
            var dogeWords2 = new List<string>()
            {
                "neat",
                "plz",
                "cool",
                "what"
            };
            var graphDescriptors = new List<string>()
            {
                "graph",
                "script",
                "workflow",
                "dee why in",
                "thingy",
                "scripty"
            };

            var random = new Random();

            string sloppyFileName =
                $"{dogeWords[random.Next(dogeWords.Count)]} {dogeWords2[random.Next(dogeWords2.Count)]} {graphDescriptors[random.Next(graphDescriptors.Count)]} {random.Next(100)}.dyn";

            string userDesktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            dynamoViewModel.SaveAs(Path.Combine(userDesktop,sloppyFileName), SaveContext.SaveAs,true);
        }

        internal void CreateGraphThumbnail()
        {
            if(string.IsNullOrWhiteSpace(dynamoViewModel.CurrentSpaceViewModel.FileName))return;

            string dynPath = dynamoViewModel.CurrentSpaceViewModel.FileName;

            dynamoViewModel.CurrentSpaceViewModel.RunSettingsViewModel.Model.RunType = RunType.Manual;

            dynamoViewModel.ZoomOutCommand.Execute(null);

            string imagePath = Path.Combine(Globals.TempPath, dynamoViewModel.CurrentSpaceViewModel.Name);

            dynamoViewModel.SaveImageCommand.Execute(imagePath);

            //if the export worked, convert to base64 and set it to the thumbnail
            if (File.Exists(imagePath))
            {
                byte[] imageArray = System.IO.File.ReadAllBytes(imagePath);
                string base64ImageRepresentation = Convert.ToBase64String(imageArray);

                
                dynamoViewModel.HomeSpace.Thumbnail = base64ImageRepresentation;

                dynamoViewModel.CurrentSpaceViewModel.HasUnsavedChanges = true;
                dynamoViewModel.SaveCommand.Execute(dynPath);
                dynamoViewModel.CloseHomeWorkspaceCommand.Execute(null);
                dynamoViewModel.OpenCommand.Execute(dynPath);
            }


           
        }

    }
}
