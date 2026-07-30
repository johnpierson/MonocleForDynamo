using System;
using System.Windows.Controls;

namespace MonocleViewExtension.Core
{
    /// <summary>
    /// One Monocle tool. Registering builds its menu entries, shortcuts and event hooks;
    /// disposing must undo all of them, since Dynamo keeps running after we are unloaded.
    /// </summary>
    internal interface IMonocleFeature : IDisposable
    {
        string Name { get; }

        void Register(MonocleContext ctx, MenuItem monocleMenu);
    }

    /// <summary>
    /// Adapts a feature that still registers itself through a static entry point. Lets features
    /// move to a real <see cref="IMonocleFeature"/> one at a time instead of all at once.
    /// </summary>
    internal sealed class DelegateFeature : IMonocleFeature
    {
        private readonly Action<MonocleContext, MenuItem> _register;
        private readonly Action _dispose;

        public DelegateFeature(string name, Action<MonocleContext, MenuItem> register, Action dispose = null)
        {
            Name = name;
            _register = register;
            _dispose = dispose;
        }

        public string Name { get; }

        public void Register(MonocleContext ctx, MenuItem monocleMenu) => _register(ctx, monocleMenu);

        public void Dispose() => _dispose?.Invoke();
    }
}
