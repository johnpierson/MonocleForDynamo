using System;
using System.Collections.Generic;
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
                ToolTip = "Set up and load the local model for this Dynamo session. It stops when unchecked or when Dynamo closes."
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
                        toggle.ToolTip = "The local model is loaded for this Dynamo session. Uncheck this item to stop it.";
                    }
                    catch (OperationCanceledException)
                    {
                        toggle.IsChecked = false;
                    }
                    catch (Exception exception)
                    {
                        toggle.IsChecked = false;
                        setupWindow?.CompleteAndClose();
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
                toggle.ToolTip = "Set up and load the local model for this Dynamo session. It stops when unchecked or when Dynamo closes.";
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

            if (!client.TryCaptureSessionGeneration(out var sessionGeneration)) return;

            var request = CaptureRequest(group, sessionGeneration);
            await SuggestAndRenameAsync(
                owner,
                dynamoViewModel,
                request,
                client,
                CancellationToken.None).ConfigureAwait(false);
        }

        internal static LocalGroupNamingRequest CaptureRequest(AnnotationModel group, long sessionGeneration)
        {
            if (group == null) throw new ArgumentNullException(nameof(group));

            var nodes = group.Nodes
                .OfType<NodeModel>()
                .Select(node => new LocalGroupNodeSnapshot(node.GUID, node.Name))
                .ToList();

            return new LocalGroupNamingRequest(
                group.GUID,
                group.AnnotationText,
                nodes,
                sessionGeneration);
        }

        internal static async Task SuggestAndRenameAsync(
            Window owner,
            DynamoViewModel dynamoViewModel,
            LocalGroupNamingRequest request,
            LocalLlamaServerClient client,
            CancellationToken cancellationToken)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (dynamoViewModel == null) throw new ArgumentNullException(nameof(dynamoViewModel));
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (client == null) throw new ArgumentNullException(nameof(client));

            // The create-groups flyout also supports empty groups. Keep its configured
            // title when there are no node names from which to infer a purpose.
            if (request.NodeNames.Count == 0) return;

            LocalGroupNamingIndicator indicator = null;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!client.IsSessionCurrent(request.SessionGeneration)) return;
                if (!IsCurrentRequestState(owner, dynamoViewModel, request, client)) return;

                owner.Dispatcher.Invoke(() =>
                {
                    if (!owner.IsLoaded || !client.IsSessionCurrent(request.SessionGeneration)) return;

                    indicator = new LocalGroupNamingIndicator(owner);
                    indicator.Show();
                });
                if (indicator == null) return;

                var prompt = GroupNamingPromptBuilder.Build(request.NodeNames);
                var suggestion = await RequestValidNameAsync(
                    client,
                    prompt,
                    request.NodeNames,
                    cancellationToken).ConfigureAwait(false);

                owner.Dispatcher.Invoke(() =>
                {
                    if (!owner.IsLoaded || !client.IsSessionCurrent(request.SessionGeneration)) return;

                    // The user may have undone, deleted, or switched away from the
                    // group, edited its title, or changed its nodes while the model
                    // was thinking; skip the rename then.
                    var annotations = dynamoViewModel.CurrentSpaceViewModel?.Annotations;
                    var currentGroup = annotations?
                        .FirstOrDefault(a => a.AnnotationModel.GUID == request.GroupId)?
                        .AnnotationModel;
                    if (currentGroup == null) return;
                    if (!LocalGroupNamingState.ShouldApplySuggestion(
                            request,
                            client.IsSessionCurrent(request.SessionGeneration),
                            currentGroup.GUID,
                            currentGroup.AnnotationText,
                            CaptureNodeSnapshots(currentGroup))) return;

                    var updateCommand = new DynamoModel.UpdateModelValueCommand(
                        request.GroupId,
                        "TextBlockText",
                        suggestion);
                    dynamoViewModel.Model.ExecuteCommand(updateCommand);
                });
            }
            catch (OperationCanceledException)
            {
                // Naming was turned off or cancelled while the request was in flight.
            }
            catch (Exception exception)
            {
                if (client.IsSessionCurrent(request.SessionGeneration) && !owner.Dispatcher.HasShutdownStarted)
                {
                    owner.Dispatcher.Invoke(() =>
                    {
                        indicator?.Dismiss();
                        if (!owner.IsLoaded || !client.IsSessionCurrent(request.SessionGeneration)) return;

                        MessageBox.Show(
                            owner,
                            exception.Message,
                            "Monocle local group naming",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    });
                }
            }
            finally
            {
                if (indicator != null && !owner.Dispatcher.HasShutdownStarted)
                {
                    owner.Dispatcher.Invoke(indicator.Dismiss);
                }
            }
        }

        private static bool IsCurrentRequestState(
            Window owner,
            DynamoViewModel dynamoViewModel,
            LocalGroupNamingRequest request,
            LocalLlamaServerClient client)
        {
            if (!client.IsSessionCurrent(request.SessionGeneration) || owner.Dispatcher.HasShutdownStarted)
            {
                return false;
            }

            var isCurrent = false;
            owner.Dispatcher.Invoke(() =>
            {
                if (!owner.IsLoaded || !client.IsSessionCurrent(request.SessionGeneration)) return;

                var currentGroup = dynamoViewModel.CurrentSpaceViewModel?.Annotations?
                    .FirstOrDefault(a => a.AnnotationModel.GUID == request.GroupId)?
                    .AnnotationModel;
                if (currentGroup == null) return;

                isCurrent = LocalGroupNamingState.ShouldApplySuggestion(
                    request,
                    client.IsSessionCurrent(request.SessionGeneration),
                    currentGroup.GUID,
                    currentGroup.AnnotationText,
                    CaptureNodeSnapshots(currentGroup));
            });

            return isCurrent;
        }

        private static List<LocalGroupNodeSnapshot> CaptureNodeSnapshots(AnnotationModel group)
        {
            return group.Nodes
                .OfType<NodeModel>()
                .Select(node => new LocalGroupNodeSnapshot(node.GUID, node.Name))
                .ToList();
        }

        private static async Task<string> RequestValidNameAsync(
            LocalLlamaServerClient client,
            string prompt,
            IReadOnlyList<string> nodeNames,
            CancellationToken cancellationToken)
        {
            var response = await client
                .SuggestNameAsync(prompt, cancellationToken)
                .ConfigureAwait(false);
            if (TryGetValidName(response, nodeNames, out var suggestion, out var validationError))
            {
                return suggestion;
            }

            var retryPrompt = GroupNamingPromptBuilder.BuildRetry(prompt, response);
            response = await client
                .SuggestNameAsync(retryPrompt, cancellationToken)
                .ConfigureAwait(false);
            if (TryGetValidName(response, nodeNames, out suggestion, out validationError))
            {
                return suggestion;
            }

            throw new InvalidOperationException(validationError ??
                "The local model copied a node name instead of naming the complete group.");
        }

        private static bool TryGetValidName(
            string response,
            IReadOnlyList<string> nodeNames,
            out string suggestion,
            out string error)
        {
            if (!GroupNameValidator.TryNormalize(response, out suggestion, out error)) return false;

            if (GroupNameValidator.IsApiStyleIdentifier(suggestion) ||
                GroupNameValidator.MatchesNodeName(suggestion, nodeNames))
            {
                suggestion = null;
                return false;
            }

            return true;
        }
    }
}
