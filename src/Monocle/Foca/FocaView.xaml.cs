using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace MonocleViewExtension.Foca
{
    /// <summary>
    /// Interaction logic for MonocleWidget.xaml
    /// </summary>
    public partial class FocaView : UserControl
    {
        public FocaView()
        {
            InitializeComponent();
            
        }

       

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Image img = sender as Image;
            var vm = this.MainGrid.DataContext as FocaViewModel;
            vm.AlignClick.Execute(img.Tag);

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
            this.combinifier.Visibility = Visibility.Hidden;
            this.powList.Visibility = Visibility.Hidden;
            this.dropdownConverter.Visibility = Visibility.Hidden;

        }

        private void ToolboxGrid_OnMouseEnter(object sender, MouseEventArgs e)
        {
            this.combinifier.Visibility = Convert.ToInt32(this.combinifier.Tag) == 1 ? Visibility.Visible : Visibility.Hidden;
            this.powList.Visibility = Convert.ToInt32(this.powList.Tag) == 1 ? Visibility.Visible : Visibility.Hidden;
            this.fundleBundle.Visibility = Convert.ToInt32(this.fundleBundle.Tag) == 1 ? Visibility.Visible : Visibility.Hidden;
            this.dropdownConverter.Visibility = Convert.ToInt32(this.dropdownConverter.Tag) == 1 ? Visibility.Visible : Visibility.Hidden;
            this.nodeSwapper.Visibility = Convert.ToInt32(this.nodeSwapper.Tag) == 1 ? Visibility.Visible : Visibility.Hidden;

        }

        private void ToolboxGrid_OnUnloaded(object sender, RoutedEventArgs e)
        {
            this.combinifier.Visibility = Visibility.Hidden;
            this.powList.Visibility = Visibility.Hidden;
            this.dropdownConverter.Visibility = Visibility.Hidden;
            this.nodeSwapper.Visibility = Visibility.Hidden;
        }

      
    }
    
}
