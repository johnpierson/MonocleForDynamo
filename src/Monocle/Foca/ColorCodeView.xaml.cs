using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using MonocleViewExtension.Utilities;

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

        private void PickColorClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || !(button.DataContext is Settings.GroupSetting setting)) return;

            var current = Colors.White;
            try
            {
                current = (Color)ColorConverter.ConvertFromString(setting.GroupColor);
            }
            catch (Exception)
            {
                // keep white for an unparsable stored color
            }

            using (var dialog = new System.Windows.Forms.ColorDialog
                   {
                       AllowFullOpen = true,
                       FullOpen = true,
                       AnyColor = true,
                       Color = System.Drawing.Color.FromArgb(current.A, current.R, current.G, current.B)
                   })
            {
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

                var picked = dialog.Color;
                setting.GroupColor = Color.FromArgb(255, picked.R, picked.G, picked.B).ToString();
            }

            // GroupSetting doesn't raise PropertyChanged; refresh this row's swatch directly.
            if (button.Content is Border swatch)
            {
                BindingOperations.GetBindingExpression(swatch, Border.BackgroundProperty)?.UpdateTarget();
            }
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
