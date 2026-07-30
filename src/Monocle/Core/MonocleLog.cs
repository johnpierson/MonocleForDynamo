using System;
using Dynamo.Logging;
using Dynamo.ViewModels;

namespace MonocleViewExtension.Core
{
    /// <summary>
    /// Writes to Dynamo's log with a monocle prefix. Always logs the full exception, not just
    /// its message — a field bug report needs the stack trace to be worth anything.
    /// </summary>
    public sealed class MonocleLog : IMonocleLogger
    {
        private const string Prefix = "[monocle]";
        private readonly DynamoViewModel _dynamoViewModel;

        public MonocleLog(DynamoViewModel dynamoViewModel)
        {
            _dynamoViewModel = dynamoViewModel;
        }

        public void Info(string message) => Write($"{Prefix} {message}", WarningLevel.Mild, isWarning: false);

        public void Warn(string message, Exception exception = null) =>
            Write(Compose(message, exception), WarningLevel.Mild, isWarning: true);

        public void Error(string message, Exception exception = null) =>
            Write(Compose(message, exception), WarningLevel.Moderate, isWarning: true);

        private static string Compose(string message, Exception exception) =>
            exception == null
                ? $"{Prefix} {message}"
                : $"{Prefix} {message}{Environment.NewLine}{exception}";

        private void Write(string text, WarningLevel level, bool isWarning)
        {
            try
            {
                var logger = _dynamoViewModel?.Model?.Logger;
                if (logger == null)
                {
                    // Very early in startup, or in a host that gave us no view model.
                    LogMessage.Warning(text, level);
                    return;
                }

                if (isWarning)
                {
                    logger.LogWarning(text, level);
                }
                else
                {
                    logger.Log(text);
                }
            }
            catch
            {
                // Logging must never be the thing that takes Dynamo down.
            }
        }
    }
}
