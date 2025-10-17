using Microsoft.Extensions.DependencyInjection;
using ScreeningLogicServiceApp.Repository;
using ScreeningLogicServiceApp.Views;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScreeningLogicServiceApp
{
    /// <summary>
    /// Interaction logic for ScreeningLogicBatchProcess.xaml
    /// </summary>
    public partial class ScreeningLogicBatchProcess : Window
    {
        private readonly IConfigurationRepository _repo;

        public ScreeningLogicBatchProcess()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            _repo = App.Services.GetRequiredService<IConfigurationRepository>();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var dashboard = DashboardViewControl; // named element from XAML
            if (dashboard?.NamesCombo != null)
            {
                dashboard.NamesCombo.SelectedIndex = 2; // 0:1, 1:2, 2:5, 3:10, 4:25, 5:50
                dashboard.StartClicked -= StartButton_Click; // avoid duplicate
                dashboard.StopClicked -= StopButton_Click;
                dashboard.StartClicked += StartButton_Click;
                dashboard.StopClicked += StopButton_Click;
            }
        }

        private async void StartButton_Click(object? sender, RoutedEventArgs e)
        {
            AppCloseButton.IsEnabled = false;
            var dashboard = DashboardViewControl;
            try
            {
                // Determine parameter from UI (selected count) or set your own value
                string param = "";
                var selected = dashboard?.NamesCombo?.SelectedItem as ComboBoxItem;
                if (selected?.Content is string s && !string.IsNullOrWhiteSpace(s))
                    param = s; // e.g., "5"

                await _repo.UpdateMaxRecordsToProcessAsync(int.Parse(param));

                // Read full path to WinForms EXE from configuration
                string? exePath = ConfigurationManager.AppSettings["ScreeningLogicWinFormsPath"];
                if (string.IsNullOrWhiteSpace(exePath))
                    throw new InvalidOperationException("Missing appSettings key 'ScreeningLogicWinFormsPath' in App.config.");

                exePath = exePath.Trim();
                if (!File.Exists(exePath))
                    throw new FileNotFoundException($"WinForms app not found at configured path: {exePath}");

                // Build arguments: WinForms expects "--hidden" as argument.
                string args = "--hidden";

                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(exePath) ?? AppDomain.CurrentDomain.BaseDirectory,
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start external process.");
                }

                // Await process exit; WinForms app calls this.Close() when done
                await process.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                // Optional: log or notify; keeping simple with a message box for now
                MessageBox.Show($"Failed to run external process: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                AppCloseButton.IsEnabled = true;
                // After completion, return highlight to Stopped and re-enable Start button
                dashboard?.HighlightStopped();
                dashboard?.SetStartEnabled(true);
            }
        }

        private void StopButton_Click(object? sender, RoutedEventArgs e)
        {
            // Dashboard handles its own UI state
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
