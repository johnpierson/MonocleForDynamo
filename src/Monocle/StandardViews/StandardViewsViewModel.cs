using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Dynamo.Graph.Workspaces;
using Dynamo.Logging;
using Dynamo.UI.Commands;
using Dynamo.ViewModels;
using HelixToolkit.Wpf.SharpDX;

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
            set { _viewControlPanel = value; RaisePropertyChanged(() => ViewControlPanel); }
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
                ViewControlPanel = FindVisualChildren<StackPanel>(Model.dynamoView).First(s => s.Name == "viewControlPanel");
                ViewControlPanel?.Children.Insert(1, View);
            }
            catch (Exception e)
            {
                Model.DynamoViewModel.Model.Logger.LogWarning($"Monocle- {e.Message}", WarningLevel.Mild);
            }
        }


        public void OnSetCamera(object view)
        {
            Viewport3DX threeDeeView =  FindVisualChildren<Viewport3DX>(Model.dynamoView).First();

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


        #region Helpers
        public IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
        #endregion
    }

}
