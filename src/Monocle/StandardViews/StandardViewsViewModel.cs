using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Dynamo.Logging;
using Dynamo.UI.Commands;
using Dynamo.ViewModels;
using HelixToolkit.Wpf.SharpDX;
using MonocleViewExtension.Core;
using MonocleViewExtension.Utilities;

namespace MonocleViewExtension.StandardViews
{
    internal class StandardViewsViewModel : ViewModelBase
    {
        public StandardViewsModel Model { get; set; }

        public StandardViews View;

        public DelegateCommand SetCameraCommand { get; set; }

        private StackPanel _viewControlPanel;
        public StackPanel ViewControlPanel
        {
            get => _viewControlPanel;
            set { _viewControlPanel = value; RaisePropertyChanged(nameof(ViewControlPanel)); }
        }

        private readonly IMonocleLogger _log;

        public StandardViewsViewModel(StandardViewsModel model)
        {
            Model = model;
            _log = new MonocleLog(model.DynamoViewModel);
            model.LoadedParams.SelectionCollectionChanged += LoadedParamsOnSelectionCollectionChanged;
            //commands
            SetCameraCommand = new DelegateCommand(OnSetCamera, CanSetCamera);

        }

        private void LoadedParamsOnSelectionCollectionChanged(NotifyCollectionChangedEventArgs obj)
        {
            //TODO: Make this a bit cleaner for moving around
            if (View.IsLoaded) return;
            try
            {
                //remove old
                ViewControlPanel?.Children.Remove(View);
                ViewControlPanel = MiscUtils.FindVisualChildren<StackPanel>(Model.dynamoView).First(s => s.Name == "viewControlPanel");
                ViewControlPanel?.Children.Insert(1, View);
            }
            catch (Exception e)
            {
                _log.Warn("Could not re-attach the standard view buttons to the status bar.", e);
            }
        }


        public void OnSetCamera(object view)
        {
            Viewport3DX threeDeeView = MiscUtils.FindVisualChildren<Viewport3DX>(Model.dynamoView).First();

            switch (view)
            {
                case "Front":
                    CameraController.SetCameraView(threeDeeView, CameraController.eCameraViews.Front, 2000);
                    break;
                case "Back":
                    CameraController.SetCameraView(threeDeeView, CameraController.eCameraViews.Back, 2000);
                    break;
                case "Left":
                    CameraController.SetCameraView(threeDeeView, CameraController.eCameraViews.Left, 2000);
                    break;
                case "Right":
                    CameraController.SetCameraView(threeDeeView, CameraController.eCameraViews.Right, 2000);
                    break;
                case "Top":
                    CameraController.SetCameraView(threeDeeView, CameraController.eCameraViews.Top, 2000);
                    break;
                case "Bottom":
                    CameraController.SetCameraView(threeDeeView, CameraController.eCameraViews.Bottom, 2000);
                    break;
            }
        }
        public bool CanSetCamera(object parameter)
        {
            try
            {
                return Model.DynamoViewModel.BackgroundPreviewActive;
            }
            catch (Exception e)
            {
                // Runs constantly as WPF re-evaluates the buttons, so keep it quiet.
                _log.Info($"Could not determine whether the 3D preview is active: {e.Message}");
                return false;
            }
        }

        public override void Dispose()
        {
            Model.LoadedParams.SelectionCollectionChanged -= LoadedParamsOnSelectionCollectionChanged;
            ViewControlPanel?.Children.Remove(View);
            base.Dispose();
        }
    }

}
