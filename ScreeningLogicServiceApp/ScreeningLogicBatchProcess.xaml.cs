using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ScreeningLogicServiceApp.Views;

namespace ScreeningLogicServiceApp
{
    /// <summary>
    /// Interaction logic for ScreeningLogicBatchProcess.xaml
    /// </summary>
    public partial class ScreeningLogicBatchProcess : Window
    {
        public ScreeningLogicBatchProcess()
        {
            InitializeComponent();

            var dashboard = FindChild<DashboardView>(MainTabs);
            if (dashboard?.NamesCombo != null)
            {
                dashboard.NamesCombo.SelectedIndex = 2; // 0:1, 1:2, 2:5, 3:10, 4:25, 5:50
                dashboard.StartClicked += StartButton_Click;
                dashboard.StopClicked += StopButton_Click;
            }
        }

        private async void StartButton_Click(object? sender, RoutedEventArgs e)
        {
            AppCloseButton.IsEnabled = false;
            try
            {
                await Task.Run(async () => { await Task.Delay(3000); });
            }
            finally
            {
                AppCloseButton.IsEnabled = true;
            }
        }

        private void StopButton_Click(object? sender, RoutedEventArgs e)
        {
            MessageBox.Show("Stopped.");
        }

        private void AppCloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match) return match;
                var result = FindChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}
