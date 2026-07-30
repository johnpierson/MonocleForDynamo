using System.Windows.Controls;
using System.Windows.Input;
using MonocleViewExtension.Core;

namespace MonocleViewExtension.Foca
{
    /// <summary>
    /// FOCA: the in-canvas widget for aligning, distributing and colour-coding a selection.
    /// </summary>
    internal sealed class FocaFeature : IMonocleFeature
    {
        private FocaViewModel _viewModel;
        private ShortcutRegistrar _shortcuts;

        public string Name => "FOCA";

        public void Register(MonocleContext ctx, MenuItem monocleMenu)
        {
            var model = new FocaModel(ctx.LoadedParams);
            _viewModel = new FocaViewModel(model);
            _viewModel.View = new FocaView { MainGrid = { DataContext = _viewModel } };

            monocleMenu.Items.Add(new Separator());

            var focaMenuItem = new MenuItem
            {
                IsCheckable = true,
                Header = $"{Properties.Resources.FocaMenuItemHeader} | ᶘ ᵒᴥᵒᶅ",
                ToolTip = Properties.Resources.FocaMenuItemTooltip,
                IsChecked = ctx.Settings.Current.IsFocaEnabled
            };
            focaMenuItem.Checked += (sender, args) => ctx.Settings.Current.IsFocaEnabled = true;
            focaMenuItem.Unchecked += (sender, args) => ctx.Settings.Current.IsFocaEnabled = false;
            monocleMenu.Items.Add(focaMenuItem);

            _shortcuts = new ShortcutRegistrar(ctx);
            _shortcuts.Add("AlignLeftCommand", "AlignLeft", Key.Left, ModifierKeys.Alt, () => model.AlignSelected("HorizontalLeft"));
            _shortcuts.Add("AlignRightCommand", "AlignRight", Key.Right, ModifierKeys.Alt, () => model.AlignSelected("HorizontalRight"));
            _shortcuts.Add("AlignTopCommand", "AlignTop", Key.Up, ModifierKeys.Alt, () => model.AlignSelected("VerticalTop"));
            _shortcuts.Add("AlignBottomCommand", "AlignBottom", Key.Down, ModifierKeys.Alt, () => model.AlignSelected("VerticalBottom"));

            MenuBuilder.AddItem(monocleMenu, Properties.Resources.FocaStandardGroupMenuItemHeader, () =>
            {
                new ColorCodeView
                {
                    // Set the data context for the main grid in the window.
                    MainGrid = { DataContext = _viewModel },
                    // Set the owner of the window to the Dynamo window.
                    Owner = ctx.DynamoWindow
                }.Show();
            }, ctx);

            monocleMenu.Items.Add(new Separator());
        }

        public void Dispose()
        {
            _shortcuts?.Dispose();
            _viewModel?.Dispose();
        }
    }
}
