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

        public StandardViewsViewModel(StandardViewsModel model)
        {
            Model = model;
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
                ViewControlPanel = Model.dynamoView.FindVisualChild<StackPanel>(s => s.Name == "viewControlPanel");
                ViewControlPanel?.Children.Insert(1, View);
            }
            catch (Exception e)
            {
                Model.DynamoViewModel.Model.Logger.LogWarning($"Monocle- {e.Message}", WarningLevel.Mild);
            }
        }


        public void OnSetCamera(object view)
        {
            var threeDeeView = Model.dynamoView.FindVisualChild<FrameworkElement>(CameraController.IsHelixViewport);
            if (threeDeeView == null) return;

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
            catch (Exception)
            {
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
