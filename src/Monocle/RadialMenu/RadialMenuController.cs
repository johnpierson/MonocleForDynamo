using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Dynamo.Controls;
using Dynamo.UI.Controls;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;
using MonocleViewExtension.Utilities;

namespace MonocleViewExtension.RadialMenu
{
    internal class RadialMenuController : IDisposable
    {
        private readonly DynamoView dynamoView;
        private readonly DynamoViewModel dynamoViewModel;
        private readonly NodeAutocompleteAdapter autocomplete;
        private bool isEnabled = true;
        private bool suppressNextRightButtonUp;
        private Point searchPosition;
        private FrameworkElement currentWorkspaceView;

        public RadialMenuController(ViewLoadedParams p, MenuItem monocleMenuItem)
        {
            dynamoView = p.DynamoWindow as DynamoView;
            dynamoViewModel = p.DynamoWindow.DataContext as DynamoViewModel;
            autocomplete = new NodeAutocompleteAdapter(dynamoViewModel, p.DynamoWindow.Dispatcher);

            var toggle = new MenuItem
            {
                Header = "Predict next node search",
                ToolTip = "Open a Dynamo search prefilled from the last node placed.",
                InputGestureText = "Shift + Right Mouse",
                IsCheckable = true,
                IsChecked = true
            };
            toggle.Checked += (sender, args) => isEnabled = true;
            toggle.Unchecked += (sender, args) =>
            {
                isEnabled = false;
                autocomplete.Cancel();
            };
            monocleMenuItem.Items.Add(toggle);

            if (dynamoView != null)
            {
                dynamoView.PreviewMouseRightButtonDown += OnPreviewMouseRightButtonDown;
                dynamoView.PreviewMouseRightButtonUp += OnPreviewMouseRightButtonUp;
                dynamoView.Deactivated += OnWindowDeactivated;
            }
        }

        public void Dispose()
        {
            autocomplete.Cancel();
            if (dynamoView == null) return;

            dynamoView.PreviewMouseRightButtonDown -= OnPreviewMouseRightButtonDown;
            dynamoView.PreviewMouseRightButtonUp -= OnPreviewMouseRightButtonUp;
            dynamoView.Deactivated -= OnWindowDeactivated;
        }

        private void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isEnabled || (Keyboard.Modifiers & ModifierKeys.Shift) == 0 || IsEditingText()) return;

            var workspaceView = FindWorkspaceView(e.OriginalSource as DependencyObject);
            if (workspaceView == null) return;

            var workspaceElements = workspaceView.FindName("WorkspaceElements") as IInputElement;
            searchPosition = Mouse.GetPosition(workspaceElements ?? workspaceView);

            var port = FindLastPlacedNodePort();
            if (port == null) return;

            currentWorkspaceView = workspaceView;
            suppressNextRightButtonUp = true;
            e.Handled = true;
            autocomplete.Request(port, OpenSearch, LogFailure);
        }

        private void OnPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!suppressNextRightButtonUp) return;

            suppressNextRightButtonUp = false;
            e.Handled = true;
        }

        private void OpenSearch(string searchName)
        {
            var workspace = dynamoViewModel?.CurrentSpaceViewModel;
            var search = workspace?.InCanvasSearchViewModel;
            if (search == null || string.IsNullOrWhiteSpace(searchName)) return;

            if (currentWorkspaceView?.FindName("ContextMenuPopup") is Popup contextMenu)
            {
                contextMenu.IsOpen = false;
            }
            workspace.ShowInCanvasSearchCommand.Execute(ShowHideFlags.Show);
            search.InCanvasSearchPosition = searchPosition;
            search.SearchText = searchName;
        }

        private static FrameworkElement FindWorkspaceView(DependencyObject source)
        {
            var current = source;
            while (current != null)
            {
                if (current is NodeView) return null;
                if (current is FrameworkElement element && current.GetType().Name == "WorkspaceView")
                {
                    return element;
                }

                current = GetParent(current);
            }
            return null;
        }

        private PortViewModel FindLastPlacedNodePort()
        {
            var nodeModel = dynamoViewModel?.CurrentSpaceViewModel?.Nodes.LastOrDefault()?.NodeModel;
            if (nodeModel == null) return null;

            var portModel = nodeModel.OutPorts.FirstOrDefault(port => !port.Connectors.Any())
                            ?? nodeModel.OutPorts.FirstOrDefault()
                            ?? nodeModel.InPorts.FirstOrDefault(port => !port.Connectors.Any());
            if (portModel == null) return null;

            return MiscUtils.FindVisualChildren<FrameworkElement>(dynamoView)
                .Select(element => element.DataContext)
                .OfType<PortViewModel>()
                .FirstOrDefault(port => ReferenceEquals(port.PortModel, portModel));
        }

        private static DependencyObject GetParent(DependencyObject child)
        {
            if (child is Visual || child is Visual3D)
            {
                return VisualTreeHelper.GetParent(child);
            }
            return LogicalTreeHelper.GetParent(child);
        }

        private bool IsEditingText()
        {
            if (Keyboard.FocusedElement is TextBoxBase) return true;

            var focused = Keyboard.FocusedElement as DependencyObject;
            while (focused != null)
            {
                if (focused is CodeBlockEditor) return true;
                focused = GetParent(focused);
            }
            return false;
        }

        private void LogFailure(string message)
        {
            dynamoViewModel?.Model?.Logger?.Log($"Monocle predictive search - {message}");
        }

        private void OnWindowDeactivated(object sender, EventArgs e)
        {
            suppressNextRightButtonUp = false;
            autocomplete.Cancel();
        }
    }
}
