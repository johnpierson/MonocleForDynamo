using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace MonocleViewExtension.PackageUsage
{
    /// <summary>
    /// Interaction logic for PackageUsageView.xaml
    /// </summary>
    public partial class PackageUsageView : Window
    {
        public PackageUsageView()
        {
            InitializeComponent();
        }
        public void ShowLabel()
        {
            var myControl = this.ResultGrid;

            myControl.Visibility = System.Windows.Visibility.Visible;

            var a = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                FillBehavior = FillBehavior.Stop,
                BeginTime = TimeSpan.FromSeconds(1),
                Duration = new Duration(TimeSpan.FromSeconds(0.5))
            };
            var storyboard = new Storyboard();

            storyboard.Children.Add(a);
            Storyboard.SetTarget(a, myControl);
            Storyboard.SetTargetProperty(a, new PropertyPath(OpacityProperty));
            storyboard.Completed += delegate { myControl.Visibility = System.Windows.Visibility.Hidden; };
            storyboard.Begin();
        }

        private void ResultTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            ShowLabel();
        }
    }
}
