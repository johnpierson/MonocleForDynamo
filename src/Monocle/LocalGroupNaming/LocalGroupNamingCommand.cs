using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Dynamo.Graph.Annotations;
using Dynamo.Graph.Nodes;
using Dynamo.Models;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;

namespace MonocleViewExtension.LocalGroupNaming
{
    internal static class LocalGroupNamingCommand
    {
        public static void AddModelToggle(
            MenuItem parentMenuItem,
            ViewLoadedParams viewLoadedParams,
            LocalLlamaServerClient client,
            LocalModelProvisioner provisioner)
        {
            var toggle = new MenuItem
            {
                Header = "local group naming (local AI)",
                IsCheckable = true,
                IsChecked = false,
                ToolTip = "Set up and load the local model, then automatically name groups created from the Monocle flyout."
            };

            toggle.Checked += async (sender, args) =>
            {
                toggle.IsEnabled = false;
                LocalModelSetupWindow setupWindow = null;
                using (var cancellation = new CancellationTokenSource())
                {
                    try
                    {
                        if (provisioner.RequiresAgreement)
                        {
                            var agreementWindow = new LocalModelAgreementWindow(viewLoadedParams.DynamoWindow);
                            if (agreementWindow.ShowDialog() != true)
                            {
                                toggle.IsChecked = false;
                                return;
                            }

                            provisioner.RecordAgreementAcceptance();
                        }

                        setupWindow = new LocalModelSetupWindow(viewLoadedParams.DynamoWindow);
                        setupWindow.CancelRequested += (cancelSender, cancelArgs) => cancellation.Cancel();
                        var progress = new Progress<LocalModelDownloadProgress>(setupWindow.UpdateDownload);
                        setupWindow.Show();

                        await provisioner.EnsureInstalledAsync(progress, cancellation.Token);
                        setupWindow.ShowLoadingModel();
                        await client.EnableAsync(cancellation.Token);
                        toggle.ToolTip = "The local model is loaded. Uncheck this item to stop it.";
                    }
                    catch (OperationCanceledException)
                    {
                        toggle.IsChecked = false;
                    }
                    catch (Exception exception)
                    {
                        toggle.IsChecked = false;
                        MessageBox.Show(
                            viewLoadedParams.DynamoWindow,
                            exception.Message,
                            "Monocle local group naming",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                    finally
                    {
                        setupWindow?.CompleteAndClose();
                        toggle.IsEnabled = true;
                    }
                }
            };

            toggle.Unchecked += (sender, args) =>
            {
                client.Disable();
                toggle.ToolTip = "Set up and load the local model, then automatically name groups created from the Monocle flyout.";
            };

            parentMenuItem.Items.Add(toggle);
        }

        public static async Task SuggestAndRenameAsync(
            Window owner,
            DynamoViewModel dynamoViewModel,
            AnnotationModel group,
            LocalLlamaServerClient client)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (dynamoViewModel == null) throw new ArgumentNullException(nameof(dynamoViewModel));
            if (group == null) throw new ArgumentNullException(nameof(group));
            if (client == null) throw new ArgumentNullException(nameof(client));

            var nodes = group.Nodes
                .OfType<NodeModel>()
                .Select(node => new GroupNodeSummary(node.Name))
                .ToList();

            // The create-groups flyout also supports empty groups. Keep its configured
            // title when there are no node names from which to infer a purpose.
            if (nodes.Count == 0) return;

            try
            {
                var nodeNames = nodes.Select(node => node.Name).ToList();
                var prompt = GroupNamingPromptBuilder.Build(nodes);

                var response = await client
                    .SuggestNameAsync(prompt, CancellationToken.None)
                    .ConfigureAwait(false);

                var hasValidSuggestion = GroupNameValidator.TryNormalize(
                    response,
                    out var suggestion,
                    out var validationError);
                if (!hasValidSuggestion ||
                    GroupNameValidator.IsApiStyleIdentifier(suggestion) ||
                    GroupNameValidator.MatchesNodeName(suggestion, nodeNames))
                {
                    var retryPrompt = GroupNamingPromptBuilder.BuildRetry(prompt, response);
                    response = await client
                        .SuggestNameAsync(retryPrompt, CancellationToken.None)
                        .ConfigureAwait(false);

                    hasValidSuggestion = GroupNameValidator.TryNormalize(
                        response,
                        out suggestion,
                        out validationError);
                    if (!hasValidSuggestion ||
                        GroupNameValidator.IsApiStyleIdentifier(suggestion) ||
                        GroupNameValidator.MatchesNodeName(suggestion, nodeNames))
                    {
                        throw new InvalidOperationException(validationError ??
                            "The local model copied a node name instead of naming the complete group.");
                    }
                }

                owner.Dispatcher.Invoke(() =>
                {
                    group.AnnotationText = suggestion;
                    var updateCommand = new DynamoModel.UpdateModelValueCommand(
                        group.GUID,
                        "TextBlockText",
                        suggestion);
                    dynamoViewModel.Model.ExecuteCommand(updateCommand);
                });
            }
            catch (Exception exception)
            {
                if (!owner.Dispatcher.HasShutdownStarted)
                {
                    owner.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            owner,
                            exception.Message,
                            "Monocle local group naming",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    });
                }
            }
        }
    }
}
