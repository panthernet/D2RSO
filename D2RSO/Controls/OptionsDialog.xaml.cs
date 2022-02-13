using System.Windows;
using System.Windows.Controls;

namespace D2RSO.Controls
{
    /// <summary>
    /// Interaction logic for OptionsDialog.xaml
    /// </summary>
    public partial class OptionsDialog : UserControl
    {
        public OptionsDialog()
        {
            InitializeComponent();

            TrackerSlider.Value = App.Settings.FormScaleX * 10;
        }

        private void Scaler_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            App.Settings.FormScaleX = e.NewValue / 10;
            App.Settings.FormScaleY = e.NewValue / 10;
        }
    }
}
