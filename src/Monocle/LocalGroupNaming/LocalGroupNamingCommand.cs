using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Dynamo.Graph.Annotations;
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
                            var agreementWindow = new LocalModelAgreementWindow(viewLoadedParams.DynamoWindow, provisioner.Manifest);
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

            // The server process died while enabled (Task Manager, antivirus, a GGML abort):
            // the client has already shut down; reflect that in the menu and say so once.
            client.ServerFaulted += (sender, message) =>
            {
                var dispatcher = viewLoadedParams.DynamoWindow.Dispatcher;
                if (dispatcher.HasShutdownStarted) return;

                dispatcher.BeginInvoke(new Action(() =>
                {
                    if (toggle.IsChecked == true) toggle.IsChecked = false;
                    if (!viewLoadedParams.DynamoWindow.IsLoaded) return;

                    MessageBox.Show(
                        viewLoadedParams.DynamoWindow,
                        message,
                        "Monocle local group naming",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }));
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

            var request = CaptureRequest(group, dynamoViewModel.CurrentSpace, sessionGeneration);
            await SuggestAndRenameAsync(
                owner,
                dynamoViewModel,
                request,
                client,
                CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Snapshots the group on the UI thread: ids plus one descriptive line per member
        /// (node names, with code, string values, drop-down selections and notes where they
        /// exist), and the workspace instance it lives in. The snapshot is both the prompt
        /// input and the proof, after inference, that the same group in the same open
        /// workspace is still unchanged (a reopened graph reuses every GUID).
        /// </summary>
        internal static LocalGroupNamingRequest CaptureRequest(AnnotationModel group, object workspace, long sessionGeneration)
        {
            if (group == null) throw new ArgumentNullException(nameof(group));

            return new LocalGroupNamingRequest(
                group.GUID,
                group.AnnotationText,
                CaptureNodeSnapshots(group),
                sessionGeneration,
                workspace);
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

                var suggestion = await GroupNameSuggester
                    .SuggestAsync(client, request.NodeNames, cancellationToken)
                    .ConfigureAwait(false);

                owner.Dispatcher.Invoke(() =>
                {
                    if (!owner.IsLoaded || !client.IsSessionCurrent(request.SessionGeneration)) return;

                    // The user may have undone, deleted, or switched away from the
                    // group, edited its title, changed its nodes, or closed and
                    // reopened the graph while the model was thinking; skip the rename then.
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
                            CaptureNodeSnapshots(currentGroup),
                            dynamoViewModel.CurrentSpace)) return;

                    var updateCommand = new DynamoModel.UpdateModelValueCommand(
                        request.GroupId,
                        "TextBlockText",
                        suggestion.Title);
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
                    CaptureNodeSnapshots(currentGroup),
                    dynamoViewModel.CurrentSpace);
            });

            return isCurrent;
        }

        private static List<LocalGroupNodeSnapshot> CaptureNodeSnapshots(AnnotationModel group)
        {
            var snapshots = new List<LocalGroupNodeSnapshot>();
            foreach (var model in group.Nodes)
            {
                if (model == null) continue;

                var description = GroupNodeDescriber.Describe(model);
                if (string.IsNullOrWhiteSpace(description)) continue;

                snapshots.Add(new LocalGroupNodeSnapshot(model.GUID, description));
            }

            return snapshots;
        }
    }
}
