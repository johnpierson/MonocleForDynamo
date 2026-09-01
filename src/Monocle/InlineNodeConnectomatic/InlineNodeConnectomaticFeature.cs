using System.Windows.Controls;
using Dynamo.ViewModels;
using MonocleViewExtension.Core;

namespace MonocleViewExtension.InlineNodeConnectomatic
{
    /// <summary>
    /// Drop a node on the middle of a wire while holding left Alt to splice it into that connection.
    /// </summary>
    internal sealed class InlineNodeConnectomaticFeature : IMonocleFeature
    {
        private InlineNodeConnectomaticModel _model;
        private MonocleContext _ctx;

        public string Name => "Inline Node Connect-o-matic";

        public void Register(MonocleContext ctx, MenuItem monocleMenu)
        {
            _ctx = ctx;
            _model = new InlineNodeConnectomaticModel(ctx.DynamoViewModel, ctx.LoadedParams);

            var menuItem = new MenuItem
            {
                IsCheckable = true,
                Header = "inline node connect-o-matic",
                ToolTip = "this tool allows you to drag a node over the center of a wire to connect it.",
                IsChecked = ctx.Settings.Current.IsConnectoEnabled
            };

            menuItem.Checked += (sender, args) => SetEnabled(true);
            menuItem.Unchecked += (sender, args) => SetEnabled(false);

            monocleMenu.Items.Add(menuItem);

            // The checkbox reflects the setting, but Checked only fires on a change, so apply the
            // stored state here too.
            if (menuItem.IsChecked) _model.Attach();
        }

        private void SetEnabled(bool enabled)
        {
            _ctx.Settings.Current.IsConnectoEnabled = enabled;

            if (enabled)
            {
                _model.Attach();
            }
            else
            {
                _model.Detach();
            }
        }

        public void Dispose() => _model?.Detach();
    }
}
