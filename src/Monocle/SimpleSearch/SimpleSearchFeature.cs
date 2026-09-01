using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Dynamo.UI.Controls;
using MonocleViewExtension.Core;
using Xceed.Wpf.AvalonDock.Controls;

namespace MonocleViewExtension.SimpleSearch
{
    /// <summary>
    /// An alternate node search: docked in the sidebar (Ctrl+F) or as an in-canvas popup
    /// (Shift+Space).
    /// </summary>
    internal sealed class SimpleSearchFeature : IMonocleFeature
    {
        private static readonly string Header = Properties.Resources.SimpleSearchMenuItemHeader;

        private MonocleContext _ctx;
        private MonocleViewExtension _extension;
        private MenuItem _menuItem;
        private SimpleSearchView _sidebarView;
        private ShortcutRegistrar _shortcuts;

        public SimpleSearchFeature(MonocleViewExtension extension)
        {
            _extension = extension;
        }

        public string Name => "Simple Search";

        /// <summary>
        /// The in-canvas popup, which FOCA closes when the selection changes. Null until the
        /// popup has been built, so callers must null-check.
        /// </summary>
        internal static Popup SimpleSearchPopup { get; private set; }

        public void Register(MonocleContext ctx, MenuItem monocleMenu)
        {
            _ctx = ctx;

            _menuItem = new MenuItem { Header = $"{Header} 🔍", IsCheckable = true };
            _menuItem.Checked += (sender, args) => OpenSidebar();
            _menuItem.Unchecked += (sender, args) => CloseSidebar();
            monocleMenu.Items.Add(_menuItem);

            _shortcuts = new ShortcutRegistrar(ctx);
            _shortcuts.Add("SimpleSearchCommand", Header, Key.F, ModifierKeys.Control, () =>
            {
                _menuItem.IsChecked = true;
                _sidebarView?.RefreshSelection();
            });

            if (ctx.Settings.Current.InCanvasSearchEnabled)
            {
                BuildPopup(ctx);
            }
        }

        private void OpenSidebar()
        {
            _sidebarView = new SimpleSearchView(_ctx.DynamoViewModel);
            _ctx.LoadedParams.AddToExtensionsSideBar(_extension, _sidebarView);
        }

        private void CloseSidebar()
        {
            _ctx.LoadedParams.CloseExtensioninInSideBar(_extension);
            _sidebarView = null;
        }

        private void BuildPopup(MonocleContext ctx)
        {
            SimpleSearchPopup = new Popup
            {
                Child = new SimpleSearchView(ctx.DynamoViewModel),
                Placement = PlacementMode.MousePoint,
                IsOpen = false,
                StaysOpen = false,
                MaxWidth = 250,
                MaxHeight = 400,
                MinWidth = 250,
                MinHeight = 400
            };

            _shortcuts.Add("SimpleSearchCanvasCommand", Header, Key.Space, ModifierKeys.Shift, ShowPopup);
        }

        private void ShowPopup()
        {
            //don't fire off command if the user is editing a code block
            var codeBlockEdits = _ctx.DynamoView.FindVisualChildren<CodeBlockEditor>().ToList();
            if (codeBlockEdits.Any(c => c.IsKeyboardFocusWithin)) return;

            //you at least need one node placed for this to work (for now)
            if (!_ctx.DynamoViewModel.CurrentSpaceViewModel.Nodes.Any()) return;

            _ctx.DynamoWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
            {
                SimpleSearchPopup.Child.Visibility = Visibility.Visible;
                SimpleSearchPopup.Child.UpdateLayout();
                SimpleSearchPopup.IsOpen = true;
                SimpleSearchPopup.CustomPopupPlacementCallback = null;
            }));
        }

        public void Dispose()
        {
            _shortcuts?.Dispose();

            if (SimpleSearchPopup != null)
            {
                SimpleSearchPopup.IsOpen = false;
                SimpleSearchPopup = null;
            }

            if (_menuItem != null && _menuItem.IsChecked)
            {
                CloseSidebar();
            }

            _extension = null;
        }
    }
}
