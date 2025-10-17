using System.Windows;
using System.Windows.Controls;

namespace ScreeningLogicServiceApp.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        public ComboBox NamesCombo => NamesToProcessComboBox;

        public event RoutedEventHandler? StartClicked;
        public event RoutedEventHandler? StopClicked;

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StartClicked?.Invoke(this, e);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopClicked?.Invoke(this, e);
        }
    }
}
