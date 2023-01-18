using Dynamo.Extensions;
using MonocleViewExtension.GraphResizerer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.LinkLabel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using System.Windows.Controls.Primitives;

namespace MonocleViewExtension.Snippets
{
    internal class SnippetsViewModel
    {
        public SnippetsModel Model { get; set; }
        private ReadyParams _readyParams;
        public SnippetsViewModel(SnippetsModel m)
        {
            Model = m;
            _readyParams = m.LoadedParams;
        }
    }
}
