using System;
using System.Windows;

namespace MonocleViewExtension.GraphResizerer
{
    /// <summary>
    /// Interaction logic for GraphResizererView.xaml
    /// </summary>
    public partial class GraphResizererView : Window
    {
        public GraphResizererView()
        {
            InitializeComponent();
        }

        private void RangeBase_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(!IsLoaded) return;

            if (AutoRunCheckbox.IsChecked != null && !AutoRunCheckbox.IsChecked.Value) return;
            if (this.MainGrid.DataContext is GraphResizererViewModel vm)
            {
                vm.ResizeGraph.Execute(this);
            }
           

        }

        private void AutoRunCheckbox_OnChecked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            if (this.MainGrid.DataContext is GraphResizererViewModel vm)
            {
                vm.ResizeGraph.Execute(this);
            }

        }
    }
}
