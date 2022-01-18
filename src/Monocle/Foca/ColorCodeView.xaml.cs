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

namespace MonocleViewExtension.Foca
{
    /// <summary>
    /// Interaction logic for ColorCodeView.xaml
    /// </summary>
    public partial class ColorCodeView : Window
    {
        public ColorCodeView()
        {
            InitializeComponent();
        }

        private void ColorButtonClick(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            string groupName = $"Group{button.Tag}";

            var vm = this.MainGrid.DataContext as FocaViewModel;
            vm.CreateGroup.Execute(groupName);
        }
    }
}
