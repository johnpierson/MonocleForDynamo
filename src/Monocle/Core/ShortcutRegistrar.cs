using System;
using System.Collections.Generic;
using System.Windows.Input;
using Dynamo.Controls;
using Dynamo.Wpf.Interfaces;

namespace MonocleViewExtension.Core
{
    /// <summary>
    /// Adds keyboard shortcuts to the Dynamo window and takes them all back on dispose.
    /// Replaces the RoutedUICommand/CommandBinding boilerplate every feature used to repeat.
    /// </summary>
    internal sealed class ShortcutRegistrar : IDisposable
    {
        private readonly DynamoView _view;
        private readonly IMonocleLogger _log;
        private readonly List<CommandBinding> _bindings = new List<CommandBinding>();

        public ShortcutRegistrar(MonocleContext ctx)
        {
            _view = ctx.DynamoView;
            _log = ctx.Log;
        }

        /// <param name="commandName">Internal command name, used only for diagnostics.</param>
        /// <param name="displayText">Text Dynamo shows for the gesture.</param>
        public void Add(string commandName, string displayText, Key key, ModifierKeys modifiers, Action execute)
        {
            if (_view == null) return;

            try
            {
                var binding = new CommandBinding(new RoutedUICommand(displayText, commandName,
                    typeof(ResourceNames.MainWindow), new InputGestureCollection
                    {
                        new KeyGesture(key, modifiers)
                    }));

                binding.Executed += (sender, args) =>
                {
                    try
                    {
                        execute();
                    }
                    catch (Exception e)
                    {
                        _log.Error($"Shortcut '{commandName}' failed.", e);
                    }
                };

                _view.CommandBindings.Add(binding);
                _bindings.Add(binding);
            }
            catch (Exception e)
            {
                _log.Warn($"Could not register the shortcut for '{commandName}'.", e);
            }
        }

        public void Dispose()
        {
            if (_view == null) return;

            foreach (var binding in _bindings)
            {
                _view.CommandBindings.Remove(binding);
            }
            _bindings.Clear();
        }
    }
}
