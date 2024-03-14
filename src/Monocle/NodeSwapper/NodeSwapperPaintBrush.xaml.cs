using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

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
    public class BooleanConverter<T> : IValueConverter
    {
        public BooleanConverter(T trueValue, T falseValue)
        {
            True = trueValue;
            False = falseValue;
        }

        public T True { get; set; }
        public T False { get; set; }

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool && ((bool)value) ? True : False;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is T && EqualityComparer<T>.Default.Equals((T)value, True);
        }
    }
    public sealed class BooleanToVisibilityConverter : BooleanConverter<Visibility>
    {
        public BooleanToVisibilityConverter() :
            base(Visibility.Visible, Visibility.Collapsed)
        { }
    }
}
