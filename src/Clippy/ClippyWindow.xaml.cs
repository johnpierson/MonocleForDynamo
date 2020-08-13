using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using Clippy.Configurations;
using Visibility = System.Windows.Visibility;

namespace MonocleViewExtension.Clippy
{
    /// <summary>
    /// Interaction logic for ClippyWindow.xaml
    /// </summary>
    public partial class ClippyWindow : Window
    {
        //private int _animation = 0;
        private static readonly List<ClippyAnimations> _clippyAnimations = new List<ClippyAnimations>();
        public static global::MonocleViewExtension.Clippy.Clippy clippyItem;
        private static Border _bubbleBody;
        //private static Polygon _bubbleBorder;
        private static Polygon _bubbleCorner;
        private static TextBlock _bubbleText;
        System.Windows.Forms.Timer aTimer = new System.Windows.Forms.Timer();
        private static Button _bubbleButton;


        public ClippyWindow()
        {
            InitializeComponent();
            HideBubble();
            //Widen scope
            _bubbleBody = this.BubbleBody;
            _bubbleCorner = this.BubbleCorner;
            _bubbleText = this.BubbleText;
            _bubbleButton = BubbleButton;

            //event handlers for drag window and close window
            this.MouseLeftButtonDown += OnOnMouseLeftButtonDown;
            this.MouseRightButtonDown += OnOnMouseRightButtonDown;

            //generate a new clippy element
            clippyItem = new global::MonocleViewExtension.Clippy.Clippy(this.ClippyCanvas);
            clippyItem.StartAnimation(ClippyAnimations.Greeting);
            GetAnimations();

        }
        private void OnOnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            clippyItem.StartAnimation(ClippyAnimations.GoodBye);
            //start a timer to delay the closing.
            aTimer.Interval = 4000;
            aTimer.Tick += OnTimerOnElapsed;
            aTimer.Start();
        }

        private void OnTimerOnElapsed(object sender, EventArgs e)
        {
            HideBubble();
            aTimer.Stop();
            this.Close();
        }


        private void OnOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void ClippyCanvas_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Random rnd = new Random();
            int animationInt = rnd.Next(0, 38);

            ClippyAnimations animation = _clippyAnimations[animationInt];
            clippyItem.StartAnimation(animation);
        }

        private void GetAnimations()
        {
            var values = Enum.GetValues(typeof(ClippyAnimations));
            foreach (ClippyAnimations v in values)
            {
                if (v != ClippyAnimations.Save && v != ClippyAnimations.Print && v != ClippyAnimations.GoodBye &&
                    v != ClippyAnimations.Searching && v != ClippyAnimations.Greeting)
                {
                    _clippyAnimations.Add(v);
                }
            }
        }
        public static void ShowBubble()
        {
            _bubbleCorner.Visibility = Visibility.Visible;
            _bubbleBody.Visibility = Visibility.Visible;
            _bubbleText.Visibility = Visibility.Visible;
            _bubbleButton.Visibility = Visibility.Visible;
        }

        private void HideBubble()
        {
            BubbleCorner.Visibility = Visibility.Hidden;
            BubbleBody.Visibility = Visibility.Hidden;
            BubbleText.Visibility = Visibility.Hidden;
            BubbleButton.Visibility = Visibility.Hidden;
        }

        private void BubbleButton_Click(object sender, RoutedEventArgs e)
        {
            HideBubble();
        }
    }

}
