using System;
using Dynamo.Core;
using Dynamo.Extensions;
using Dynamo.Wpf.Extensions;

namespace MonocleViewExtension.About
{
    public class AboutViewModel : NotificationObject, IDisposable
    {
        public ViewLoadedParams vlp;

        public string MonocleVersion => $"v.{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";

        public AboutViewModel(ViewLoadedParams p)
        {
            vlp = p;
        }

        public void Dispose()
        {

        }
    }
}
