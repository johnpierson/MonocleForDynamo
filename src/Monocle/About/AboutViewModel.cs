using System;
using Dynamo.Core;
using Dynamo.Extensions;

namespace MonocleViewExtension.About
{
    public class AboutViewModel : NotificationObject, IDisposable
    {
        private ReadyParams readyParams;

        public string MonocleVersion => $"v.{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";

        public AboutViewModel(ReadyParams p)
        {
            readyParams = p;
        }

        public void Dispose()
        {

        }
    }
}
