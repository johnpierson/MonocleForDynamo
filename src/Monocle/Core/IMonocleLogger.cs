using System;

namespace MonocleViewExtension.Core
{
    /// <summary>
    /// Logging surface that carries no Dynamo types, so the code behind it stays testable.
    /// </summary>
    public interface IMonocleLogger
    {
        void Info(string message);
        void Warn(string message, Exception exception = null);
        void Error(string message, Exception exception = null);
    }

    /// <summary>
    /// Drops everything. Used by tests and as a fallback before the real logger exists.
    /// </summary>
    public sealed class NullMonocleLogger : IMonocleLogger
    {
        public static readonly NullMonocleLogger Instance = new NullMonocleLogger();
        public void Info(string message) { }
        public void Warn(string message, Exception exception = null) { }
        public void Error(string message, Exception exception = null) { }
    }
}
