using Microsoft.Extensions.DependencyInjection;
using ScreeningLogicServiceApp.Repository;
using System.Configuration;
using System.Diagnostics;
using System.IO;
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
        private readonly IConfigurationRepository _configurationRepo;
        private readonly IScreeningLogicScrappingRepository _scrappingRepo;
        private bool _stopping = false;

        public ScreeningLogicBatchProcess()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            _configurationRepo = App.Services.GetRequiredService<IConfigurationRepository>();
            _scrappingRepo = App.Services.GetRequiredService<IScreeningLogicScrappingRepository>();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            var dashboard = DashboardViewControl; // named element from XAML
            if (dashboard?.NamesCombo != null)
            {
                dashboard.NamesCombo.SelectedIndex = 6; // 0:1, 1:2, 2:5, 3:10, 4:25, 5:50, 6:All(50)
                dashboard.StartClicked -= StartButton_Click; // avoid duplicate
                dashboard.StopClicked -= StopButton_Click;
                dashboard.StartClicked += StartButton_Click;
                dashboard.StopClicked += StopButton_Click;
            }

            int inProcessCount = await _scrappingRepo.GetScreeningLogicScrappingInProgressInJusticeExchangeAsync();
            if (inProcessCount > 1)
            {
                DashboardViewControl.ShowInfoMessage($"There are {inProcessCount} records awaiting to be processed in JusticeExchange. Click on start to continue processing.");
            }
            else if (inProcessCount == 1)
            {
                DashboardViewControl.ShowInfoMessage("There is 1 record awaiting to be processed in JusticeExchange. Click on start to continue processing.");
            }
        }

        private async Task ExecuteScreeningProcess()
        {
            DashboardViewControl.ClearInfoMessage();
            DashboardViewControl.SetStopEnabled(true);
            AppCloseButton.IsEnabled = false;
            await _configurationRepo.UndoStop();
            var dashboard = DashboardViewControl;
            try
            {
                // Check if JE password change is required; if yes, show message and skip processing
                var changePwdRequired = await _configurationRepo.GetConfigurationValueAsync("ChangePasswordRequiredInJusticeExchange");
                if (string.Equals(changePwdRequired, "Yes", StringComparison.OrdinalIgnoreCase))
                {
                    DashboardViewControl.ShowInfoMessage("Previous login attempt failed in Justice Exchange due to Invalid Password or Password Expired. Please update password.");
                    return; // Skip rest; finally block will execute because this is inside async void
                }

                // Determine parameter from UI (selected count) or set your own value
                var selected = dashboard?.NamesCombo?.SelectedItem as ComboBoxItem;
                int countToProcess = 50; // default fallback
                if (selected != null)
                {
                    // Prefer Tag if provided (e.g., "All" item carries Tag="50")
                    if (selected.Tag is string tagStr && int.TryParse(tagStr, out var tagVal))
                    {
                        countToProcess = tagVal;
                    }
                    else if (selected.Tag is int tagInt)
                    {
                        countToProcess = tagInt;
                    }
                    else if (selected.Content is string contentStr && int.TryParse(contentStr, out var contentVal))
                    {
                        countToProcess = contentVal;
                    }
                }

                await _configurationRepo.UpdateMaxRecordsToProcessAsync(countToProcess);


                // *********** Start of Screening Logic WinForms app process ***********
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

                using (var process = Process.Start(psi))
                {
                    if (process == null)
                        throw new InvalidOperationException("Failed to start external process.");

                    // Await process exit; WinForms app calls this.Close() when done
                    await process.WaitForExitAsync();
                }
                // *********** End of Screening Logic WinForms app process ***********

                var processStartStop = await _configurationRepo.GetProcessStartAndStopAsync();
                if (!processStartStop.Stop)
                {
                    // *********** Start of Justice Exchange WinForms app process ***********
                    // Highlight JusticeExchangeCard while running JE process
                    dashboard?.HighlightJusticeExchangeProcessing();

                    string? jeExePath = ConfigurationManager.AppSettings["JusticeExchangeWinFormsPath"];
                    if (string.IsNullOrWhiteSpace(jeExePath))
                        throw new InvalidOperationException("Missing appSettings key 'JusticeExchangeWinFormsPath' in App.config.");

                    jeExePath = jeExePath.Trim();
                    if (!File.Exists(jeExePath))
                        throw new FileNotFoundException($"WinForms app not found at configured path: {jeExePath}");

                    var jePsi = new ProcessStartInfo
                    {
                        FileName = jeExePath,
                        Arguments = "--hidden",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = Path.GetDirectoryName(jeExePath) ?? AppDomain.CurrentDomain.BaseDirectory,
                    };

                    using (var jeProcess = Process.Start(jePsi))
                    {
                        if (jeProcess == null)
                            throw new InvalidOperationException("Failed to start Justice Exchange external process.");

                        await jeProcess.WaitForExitAsync();
                    }
                    // *********** End of Justice Exchange WinForms app process ***********
                }
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
                DashboardViewControl.SetStopEnabled(false);
                DashboardViewControl.ClearInfoMessage();

                // Delete all records from all tables except Configuration table and ProcessStartAndStop table
                await DeleteAllRecords();
            }
        }

        private async void StartButton_Click(object? sender, RoutedEventArgs e)
        {
            await ExecuteScreeningProcess();
        }

        private async Task DeleteAllRecords() 
        { 
            // Delegate to repository method to perform FK-safe bulk deletes
            await _scrappingRepo.DeleteAllExceptConfigurationAndProcessAsync();
        }

        private async void StopButton_Click(object? sender, RoutedEventArgs e)
        {
            _stopping = true;
            DashboardViewControl.ShowWarningMessage("Attempting to stop process. Please wait...");
            await _configurationRepo.StopProcess();
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
