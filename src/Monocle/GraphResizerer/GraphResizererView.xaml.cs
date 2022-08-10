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
            try
            {
                var vm = this.MainGrid.DataContext as GraphResizererViewModel;
                vm.ResizeGraph.Execute(this);
            }
            catch (Exception)
            {
                // suppress warnings for now
            }
           

        }

        private void AutoRunCheckbox_OnChecked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            try
            {
                var vm = this.MainGrid.DataContext as GraphResizererViewModel;
                vm.ResizeGraph.Execute(this);
            }
            catch (Exception)
            {
                // suppress warnings for now
            }

        }
    }
}
