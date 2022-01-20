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
using Dynamo.Controls;
using Dynamo.Graph.Workspaces;
using Dynamo.ViewModels;
using Dynamo.Views;
using MonocleViewExtension.About;
using MonocleViewExtension.Utilities;

namespace MonocleViewExtension.MiniMap_Future_
{
    /// <summary>
    /// Interaction logic for MiniMapView.xaml
    /// </summary>
    public partial class MiniMapView : Window
    {
        public MiniMapView()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            BindRect();
        }

        private CompositeCollection workspaceElements;
        private void BindRect()
        {
            var vm = this.MainGrid.DataContext as AboutViewModel;

            var dynamoView = vm.vlp.DynamoWindow as DynamoView;

            var workspaceView = MiscUtils.FindVisualChildren<WorkspaceView>(dynamoView).First();


            var wvm = workspaceView.DataContext as WorkspaceViewModel;

            WorkspaceView wv = new WorkspaceView();

            WorkspaceViewModel newVm = new WorkspaceViewModel(wvm.Model, wvm.DynamoViewModel);

            wv.DataContext = newVm;

            wv.ViewModel.Zoom = Double.MaxValue;

            this.MainGrid.Children.Add(wv);

            return;

            workspaceElements = wvm.WorkspaceElements;

            var grid = MiscUtils.FindVisualChildren<ZoomBorder>(dynamoView).First();

            VisualBrush visualBrush = new VisualBrush();

            visualBrush.Visual = grid;

            
            this.MiniMapRectangle.Fill = visualBrush;
            this.MiniMapRectangle.Width = grid.ActualWidth;
            this.MiniMapRectangle.Height = grid.ActualHeight;

            tryThis(dynamoView, this.MiniMapRectangle,grid);

        }
        private void tryThis(DynamoView v, Rectangle rct, ZoomBorder g)
        {
            Rect bounds = new Rect();

            var initialized = false;

            double minX = 0.0, minY = 0.0;
            var dragCanvas = MiscUtils.FindVisualChildren<DragCanvas>(v).First();
            var childrenCount = VisualTreeHelper.GetChildrenCount(dragCanvas);
            for (int index = 0; index < childrenCount; ++index)
            {
                ContentPresenter contentPresenter = VisualTreeHelper.GetChild(dragCanvas, index) as ContentPresenter;

                var firstChild = VisualTreeHelper.GetChild(contentPresenter, 0);

                switch (firstChild.GetType().Name)
                {
                    case "NodeView":
                    case "NoteView":
                    case "AnnotationView":
                        break;

                    // Until we completely removed InfoBubbleView (or fixed its broken 
                    // size calculation), we will not be including it in our size 
                    // calculation here. This means that the info bubble, if any, will 
                    // still go beyond the boundaries of the final PNG file. I would 
                    // prefer not to add this hack here as it introduces multiple issues 
                    // (including NaN for Grid inside the view and the fix would be too 
                    // ugly to type in). Suffice to say that InfoBubbleView is not 
                    // included in the size calculation for screen capture (work-around 
                    // should be obvious).
                    // 
                    // case "InfoBubbleView":
                    //     child = WpfUtilities.ChildOfType<Grid>(child);
                    //     break;

                    // We do not take anything other than those above 
                    // into consideration when the canvas size is measured.
                    default:
                        continue;
                }

                // Determine the smallest corner of all given visual elements on the 
                // graph. This smallest top-left corner value will be useful in making 
                // the offset later on.
                // 
                var childBounds = VisualTreeHelper.GetDescendantBounds(contentPresenter as Visual);
                minX = childBounds.X < minX ? childBounds.X : minX;
                minY = childBounds.Y < minY ? childBounds.Y : minY;
                childBounds.X = (double)(contentPresenter as Visual).GetValue(Canvas.LeftProperty);
                childBounds.Y = (double)(contentPresenter as Visual).GetValue(Canvas.TopProperty);

                if (initialized)
                {
                    bounds.Union(childBounds);
                }
                else
                {
                    initialized = true;
                    bounds = childBounds;
                }
            }

            // Nothing found in the canvas, bail out.
            if (!initialized) return;

            // Add padding to the edge and make them multiples of two (pad 10px on each side).
            bounds.Width = 20 + ((((int)Math.Ceiling(bounds.Width)) + 1) & ~0x01);
            bounds.Height = 20 + ((((int)Math.Ceiling(bounds.Height)) + 1) & ~0x01);


            rct.Width = bounds.Width;
            rct.Height = bounds.Height;


            var currentTransformGroup = g.RenderTransform as TransformGroup;
            this.ZoomViewbox.RenderTransform = new TranslateTransform(10.0 - bounds.X - minX, 10.0 - bounds.Y - minY);
            this.ZoomViewbox.UpdateLayout();

            //var rtb = new RenderTargetBitmap(((int)bounds.Width),
            //    ((int)bounds.Height), 96, 96, PixelFormats.Default);

            //rtb.Render(WorkspaceElements);
            //WorkspaceElements.RenderTransform = currentTransformGroup;
        }
    }
}
