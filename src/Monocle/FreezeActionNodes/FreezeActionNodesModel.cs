using System;
using System.Linq;
using System.Reflection;
using CoreNodeModels;
using Dynamo.Controls;
using Dynamo.Graph.Nodes;
using Dynamo.Logging;
using Dynamo.Models;
using Dynamo.ViewModels;
using Dynamo.Wpf.Extensions;

namespace MonocleViewExtension.FreezeActionNodes
{
    internal class FreezeActionNodesModel
    {
        public DynamoView DynamoView { get; }
        public DynamoViewModel DynamoViewModel { get; }
        public ViewLoadedParams LoadedParams { get; }

        public FreezeActionNodesModel(DynamoViewModel dvm, ViewLoadedParams loadedParams)
        {
            DynamoView = loadedParams.DynamoWindow as DynamoView;
            DynamoViewModel = dvm;
            LoadedParams = loadedParams;
        }

        /// <summary>
        /// Toggles the frozen state of all action nodes in the graph (freezes if unfrozen, unfreezes if frozen)
        /// </summary>
        /// <returns>Number of nodes toggled</returns>
        public int FreezeActionNodes()
        {
            int toggledCount = 0;

            // Get all nodes in the current workspace
            var allNodes = DynamoViewModel.CurrentSpaceViewModel.Nodes.ToList();

            foreach (var nodeViewModel in allNodes)
            {
                var nodeModel = nodeViewModel.NodeModel;

                // Check if this node is an action node
                if (IsActionNode(nodeModel))
                {
                    try
                    {
                        // Toggle the frozen state of the node
                        if (ToggleNodeFreeze(nodeModel))
                        {
                            toggledCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log warning but continue processing other nodes
                        DynamoViewModel.Model.Logger.LogWarning(
                            $"Monocle- Failed to toggle freeze state for node {nodeModel.Name}: {ex.Message}",
                            WarningLevel.Mild);
                    }
                }
            }

            return toggledCount;
        }

        /// <summary>
        /// Determines if a node is an action node (performs side effects)
        /// </summary>
        /// <param name="nodeModel">The node to check</param>
        /// <returns>True if the node is an action node</returns>
        private bool IsActionNode(NodeModel nodeModel)
        {
            // Check if node has no output ports (common for action nodes)
            if (nodeModel.OutPorts.Count == 0)
            {
                // Skip input nodes and code blocks
                if (nodeModel.IsInputNode || nodeModel is CodeBlockNodeModel)
                {
                    return false;
                }
                return true;
            }

            // Check for nodes with side effects using reflection
            // Many action nodes have properties or attributes indicating side effects
            try
            {
                var nodeType = nodeModel.GetType();
                
                // Check for IsActionNode property or similar
                var isActionProperty = nodeType.GetProperty("IsActionNode", BindingFlags.Public | BindingFlags.Instance);
                if (isActionProperty != null && (bool)isActionProperty.GetValue(nodeModel))
                {
                    return true;
                }

                // Check for HasSideEffects property
                var hasSideEffectsProperty = nodeType.GetProperty("HasSideEffects", BindingFlags.Public | BindingFlags.Instance);
                if (hasSideEffectsProperty != null && (bool)hasSideEffectsProperty.GetValue(nodeModel))
                {
                    return true;
                }

                // Check node name for common action node patterns
                var nodeName = nodeModel.Name.ToLower();
                var actionKeywords = new[] { "write", "save", "export", "delete", "modify", "update", "create", "set", "add", "remove" };
                if (actionKeywords.Any(keyword => nodeName.Contains(keyword)))
                {
                    // Additional check: make sure it's not just a query node
                    if (!nodeName.Contains("query") && !nodeName.Contains("get") && !nodeName.Contains("read"))
                    {
                        return true;
                    }
                }

                // Check category for action-related categories
                var category = nodeModel.Category?.ToLower() ?? "";
                if (category.Contains("action") || category.Contains("modify") || category.Contains("write"))
                {
                    return true;
                }
            }
            catch (Exception)
            {
                // If reflection fails, continue with other checks
            }

            return false;
        }

        /// <summary>
        /// Toggles the frozen state of a node by setting its IsFrozen property
        /// </summary>
        /// <param name="nodeModel">The node to toggle</param>
        /// <returns>True if the node's frozen state was successfully toggled</returns>
        private bool ToggleNodeFreeze(NodeModel nodeModel)
        {
            try
            {
                // Try to toggle IsFrozen property directly on the node model
                var nodeType = nodeModel.GetType();
                var isFrozenProperty = nodeType.GetProperty("IsFrozen", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                
                if (isFrozenProperty != null && isFrozenProperty.CanWrite)
                {
                    // Get current frozen state
                    var currentValue = isFrozenProperty.GetValue(nodeModel);
                    if (currentValue is bool isFrozen)
                    {
                        // Toggle the frozen state
                        isFrozenProperty.SetValue(nodeModel, !isFrozen);
                        return true;
                    }
                }

                // If direct property access doesn't work, try setting through ViewModel
                var nodeViewModel = DynamoViewModel.CurrentSpaceViewModel.Nodes
                    .FirstOrDefault(nvm => nvm.NodeModel.GUID == nodeModel.GUID);
                
                if (nodeViewModel != null)
                {
                    var viewModelType = nodeViewModel.GetType();
                    var vmIsFrozenProperty = viewModelType.GetProperty("IsFrozen", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                    
                    if (vmIsFrozenProperty != null && vmIsFrozenProperty.CanWrite)
                    {
                        var currentValue = vmIsFrozenProperty.GetValue(nodeViewModel);
                        if (currentValue is bool isFrozen)
                        {
                            // Toggle the frozen state
                            vmIsFrozenProperty.SetValue(nodeViewModel, !isFrozen);
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

