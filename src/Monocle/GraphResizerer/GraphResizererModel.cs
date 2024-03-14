﻿using System.Collections.Generic;
using Autodesk.DesignScript.Geometry;
using Dynamo.Controls;
using Dynamo.Models;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;

namespace MonocleViewExtension.GraphResizerer
{
    internal class GraphResizererModel
    {
        public DynamoView dynamoView { get; }
        public DynamoViewModel dynamoViewModel { get; }
        public ViewLoadedParams LoadedParams { get; }
        public Dictionary<string, Autodesk.DesignScript.Geometry.Point> OriginalLocations { get; set; }
        public GraphResizererModel(DynamoViewModel dvm, ViewLoadedParams loadedParams)
        {
            dynamoView = loadedParams.DynamoWindow as DynamoView;
            dynamoViewModel = dvm;
            LoadedParams = loadedParams;
        }

        public void SetRunStatus()
        {
            dynamoViewModel.HomeSpace.RunSettings.RunType = RunType.Manual;
        }
        public void GetNodes()
        {
            OriginalLocations = new Dictionary<string, Autodesk.DesignScript.Geometry.Point>();
            foreach (var nvm in dynamoViewModel.CurrentSpaceViewModel.Nodes)
            {
                OriginalLocations.Add(nvm.Id.ToString(), Point.ByCoordinates(nvm.X,nvm.Y));
            }
            foreach (var noteViewModel in dynamoViewModel.CurrentSpaceViewModel.Notes)
            {
                OriginalLocations.Add(noteViewModel.Model.GUID.ToString(), Point.ByCoordinates(noteViewModel.Left, noteViewModel.Top));
            }
        }
        public int ResizeGraph(double xScaleFactor, double yScaleFactor)
        {
            int changeCount = 0;
            foreach (var nvm in dynamoViewModel.CurrentSpaceViewModel.Nodes)
            {
                OriginalLocations.TryGetValue(nvm.Id.ToString(), out Point originalValue);
                if (originalValue != null)
                {
                    nvm.X = originalValue.X * xScaleFactor;
                    nvm.Y = originalValue.Y * yScaleFactor;
                }
                else
                {
                    nvm.X *= xScaleFactor;
                    nvm.Y *= yScaleFactor;
                }
                
                nvm.NodeModel.ReportPosition();
                changeCount++;
            }

            foreach (var noteViewModel in dynamoViewModel.CurrentSpaceViewModel.Notes)
            {
                OriginalLocations.TryGetValue(noteViewModel.Model.GUID.ToString(), out Point originalValue);
                if (originalValue != null)
                {
                    noteViewModel.Left = originalValue.X * xScaleFactor;
                    noteViewModel.Top = originalValue.Y * yScaleFactor;
                }
                else
                {
                    noteViewModel.Left *= xScaleFactor;
                    noteViewModel.Top *= yScaleFactor;
                }

                changeCount++;
            }

            return changeCount;
        }
       
    }
}
