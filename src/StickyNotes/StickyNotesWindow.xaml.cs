using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace MonocleViewExtension.StickyNotes
{
    /// <summary>
    /// Interaction logic for StickyNotesWindow.xaml
    /// </summary>
    public partial class StickyNotesWindow : Window
    {
        public StickyNotesWindow()
        {
            InitializeComponent();

            RotateTransform rt = new RotateTransform(GetRandomNumber(-6,6));
            this.Note1.RenderTransform = rt;
            this.Close.MouseLeftButtonDown += rectangle_MouseLeftButtonDown;
            this.Close.MouseRightButtonDown += rectangle_MouseRightButtonDown;
            this.Doge.MouseLeftButtonDown += rectangle_MouseLeftButtonDown;
            this.MouseLeftButtonDown += Window_MouseDown;
            this.Note1.MouseLeftButtonDown += FocusWindow;
        }


        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        private void FocusWindow(object sender, MouseButtonEventArgs e)
        {
            this.Focus();
        }
        public double GetRandomNumber(double minimum, double maximum)
        {
            Random random = new Random();
            return random.NextDouble() * (maximum - minimum) + minimum;
        }
        void rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
        void rectangle_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Doge.Visibility = Visibility.Visible;
            this.Close.Visibility = Visibility.Hidden;
        }
        

    }
}
