using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MonocleViewExtension.Foca
{
    /// <summary>
    /// Interaction logic for MonocleWidget.xaml
    /// </summary>
    public partial class FocaView : UserControl
    {
        private const double MinimumHitTargetPixels = 28.0;
        private static readonly TimeSpan ToolboxCloseDelay = TimeSpan.FromMilliseconds(200);
        private readonly DispatcherTimer _toolboxCloseTimer;

        public FocaView()
        {
            InitializeComponent();

            _toolboxCloseTimer = new DispatcherTimer(DispatcherPriority.Input, Dispatcher)
            {
                Interval = ToolboxCloseDelay
            };
            _toolboxCloseTimer.Tick += ToolboxCloseTimer_OnTick;
            LayoutUpdated += FocaView_OnLayoutUpdated;
        }

       

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var target = sender as FrameworkElement;
            var vm = this.MainGrid.DataContext as FocaViewModel;
            e.Handled = true;
            vm?.AlignClick.Execute(target?.Tag);
        }

        private void GroupMouseDown(object sender, MouseButtonEventArgs e)
        {
            Path path = sender as Path;

            var vm = this.MainGrid.DataContext as FocaViewModel;
            vm.CreateGroup.Execute(path.Name);
        }

        private void ColorWheel_OnMouseEnter(object sender, MouseEventArgs e)
        {
            var vm = this.MainGrid.DataContext as FocaViewModel;
            vm.MouseEnter.Execute("");
        }

       

        private void ToolboxItems_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var vm = this.MainGrid.DataContext as FocaViewModel;
            if (sender is Path path)
            {
                vm.ToolboxClick.Execute(path.Name);
            }

            if (sender is Image image)
            {
                vm.ToolboxClick.Execute(image.Name);
            }
        }

        private void ToolboxGrid_OnMouseLeave(object sender, MouseEventArgs e)
        {
            _toolboxCloseTimer.Stop();
            _toolboxCloseTimer.Start();
        }

        private void ToolboxCloseTimer_OnTick(object sender, EventArgs e)
        {
            if (ToolboxGrid.IsMouseOver)
            {
                _toolboxCloseTimer.Stop();
                return;
            }

            HideToolboxItems();
            _toolboxCloseTimer.Stop();
        }

        private void HideToolboxItems()
        {
            this.combinifier.Visibility = Visibility.Hidden;
            this.powList.Visibility = Visibility.Hidden;
            this.fundleBundle.Visibility = Visibility.Hidden;
            this.dropdownConverter.Visibility = Visibility.Hidden;
            this.nodeSwapper.Visibility = Visibility.Hidden;
        }

        private void ToolboxGrid_OnMouseEnter(object sender, MouseEventArgs e)
        {
            _toolboxCloseTimer.Stop();
            this.combinifier.Visibility = Convert.ToInt32(this.combinifier.Tag) == 1 ? Visibility.Visible : Visibility.Hidden;
            this.powList.Visibility = Convert.ToInt32(this.powList.Tag) == 1 ? Visibility.Visible : Visibility.Hidden;
            this.fundleBundle.Visibility = Convert.ToInt32(this.fundleBundle.Tag) == 1 ? Visibility.Visible : Visibility.Hidden;
            this.dropdownConverter.Visibility = Convert.ToInt32(this.dropdownConverter.Tag) == 1 ? Visibility.Visible : Visibility.Hidden;
            this.nodeSwapper.Visibility = Convert.ToInt32(this.nodeSwapper.Tag) == 1 ? Visibility.Visible : Visibility.Hidden;
        }

        private void ToolboxGrid_OnUnloaded(object sender, RoutedEventArgs e)
        {
            _toolboxCloseTimer.Stop();
            HideToolboxItems();
        }

        private void FocaView_OnLayoutUpdated(object sender, EventArgs e)
        {
            if (!IsLoaded || !(MainGrid.DataContext is FocaViewModel vm) || !vm.FocaVisible)
            {
                return;
            }

            try
            {
                var dynamoView = vm.Model.DynamoView;
                var frameOrigin = AlignmentGrid.TranslatePoint(new Point(0, 0), dynamoView);
                var frameBottomRight = AlignmentGrid.TranslatePoint(
                    new Point(AlignmentGrid.ActualWidth, AlignmentGrid.ActualHeight), dynamoView);
                var topClamped = frameOrigin.Y < MinimumHitTargetPixels;
                var leftClamped = frameOrigin.X < MinimumHitTargetPixels;
                var bottomClamped = frameBottomRight.Y > dynamoView.ActualHeight - MinimumHitTargetPixels;
                var rightClamped = frameBottomRight.X > dynamoView.ActualWidth - MinimumHitTargetPixels;

                UpdateTargets(topClamped, TopAlignTarget, TopDistributeTarget, TopCenterTarget);
                UpdateTargets(bottomClamped, BottomAlignTarget, BottomDistributeTarget, BottomCenterTarget);
                UpdateTargets(leftClamped, LeftAlignTarget, LeftDistributeTarget, LeftCenterTarget);
                UpdateTargets(rightClamped, RightAlignTarget, RightDistributeTarget, RightCenterTarget);
            }
            catch (InvalidOperationException)
            {
                // The overlay can be between visual-tree hosts while the selection changes.
            }
        }

        private static void UpdateTargets(bool clampInside, params Border[] targets)
        {
            foreach (var target in targets)
            {
                if (!(target.Child is FrameworkElement glyph))
                {
                    continue;
                }

                var targetSize = Math.Max(MinimumHitTargetPixels, Math.Max(glyph.Width, glyph.Height));
                SetIfChanged(target, targetSize);

                var margin = target.Margin;
                if (target.VerticalAlignment == VerticalAlignment.Top)
                {
                    margin.Top = clampInside ? 0 : GetOutsideMargin(targetSize, glyph.Height);
                }
                else if (target.VerticalAlignment == VerticalAlignment.Bottom)
                {
                    margin.Bottom = clampInside ? 0 : GetOutsideMargin(targetSize, glyph.Height);
                }
                else if (target.HorizontalAlignment == HorizontalAlignment.Left)
                {
                    margin.Left = clampInside ? 0 : GetOutsideMargin(targetSize, glyph.Width);
                }
                else if (target.HorizontalAlignment == HorizontalAlignment.Right)
                {
                    margin.Right = clampInside ? 0 : GetOutsideMargin(targetSize, glyph.Width);
                }

                if (target.Margin != margin)
                {
                    target.Margin = margin;
                }
            }
        }

        private static double GetOutsideMargin(double targetSize, double glyphSize)
        {
            var originalCenterOffset = 24.0 - glyphSize / 2.0;
            return -originalCenterOffset - targetSize / 2.0;
        }

        private static void SetIfChanged(Border target, double size)
        {
            if (Math.Abs(target.Width - size) < 0.1 && Math.Abs(target.Height - size) < 0.1)
            {
                return;
            }

            target.Width = size;
            target.Height = size;
        }

      
    }
    
}
