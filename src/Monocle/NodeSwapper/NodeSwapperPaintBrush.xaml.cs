using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MonocleViewExtension.NodeSwapper
{
    /// <summary>
    /// Interaction logic for NodeSwapperPaintBrush.xaml
    /// </summary>
    public partial class NodeSwapperPaintBrush : Window
    {
        public NodeSwapperPaintBrush()
        {
            InitializeComponent();

            this.Loaded += OnContentRendered;
        }

        public void UpdateLocation()
        {
            MoveBottomRightEdgeOfWindowToMousePosition();
        }
        private void OnContentRendered(object sender, EventArgs e)
        {
            base.OnContentRendered(e);
            MoveBottomRightEdgeOfWindowToMousePosition();
        }
        
        private void MoveBottomRightEdgeOfWindowToMousePosition()
        {
            var transform = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;
            var mouse = transform.Transform(GetMousePosition());
            Left = mouse.X + 8;
            Top = mouse.Y + 8;
        }

        public System.Windows.Point GetMousePosition()
        {
            System.Drawing.Point point = System.Windows.Forms.Control.MousePosition;
            return new System.Windows.Point(point.X, point.Y);
        }
    }
}
