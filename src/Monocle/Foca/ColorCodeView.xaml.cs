using System.Windows;
using System.Windows.Controls;

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
