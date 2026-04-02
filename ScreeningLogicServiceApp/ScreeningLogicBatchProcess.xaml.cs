using Microsoft.Extensions.DependencyInjection;
using ScreeningLogicServiceApp.Models;
using ScreeningLogicServiceApp.Repository;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading;

namespace ScreeningLogicServiceApp
{
    /// <summary>
    /// Interaction logic for ScreeningLogicBatchProcess.xaml
    /// </summary>
    public partial class ScreeningLogicBatchProcess : Window
    {
        private readonly IConfigurationRepository _configurationRepo;
        private readonly IScreeningLogicScrappingRepository _scrappingRepo;
        private readonly IIncomingOrderSearchRepository _incomingOrderSearchRepository;
        private bool _stopping = false;
        private CancellationTokenSource? _cts;
        private Task? _continuousTask;
        private bool _isContinuousRunning = false;
        private bool _passwordChangeDetected = false; // track to preserve message and stop loop
        private bool _errorResponseDetected = false; // track if error response from JE detected

        public ScreeningLogicBatchProcess()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            _configurationRepo = App.Services.GetRequiredService<IConfigurationRepository>();
            _scrappingRepo = App.Services.GetRequiredService<IScreeningLogicScrappingRepository>();
            _incomingOrderSearchRepository = App.Services.GetRequiredService<IIncomingOrderSearchRepository>();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            var dashboard = DashboardViewControl; // named element from XAML
            if (dashboard?.NamesCombo != null)
            {
                dashboard.NamesCombo.SelectedIndex = 6; // 0:1, 1:2, 2:5, 3:10, 4:25, 5:50, 6:All(100)
                dashboard.StartClicked -= StartButton_Click; // avoid duplicate
                dashboard.StopClicked -= StopButton_Click;
                dashboard.StartClicked += StartButton_Click;
                dashboard.StopClicked += StopButton_Click;
            }
                       
            // On application load, delete all records from all tables except Configuration table and ProcessStartAndStop table
            await DeleteAllRecords();
            await RefreshDashboardMetricsAsync();

            //int inProcessCount = await _scrappingRepo.GetScreeningLogicScrappingInProgressInJusticeExchangeAsync();
            //if (inProcessCount > 1)
            //{
            //    DashboardViewControl.ShowInfoMessage($"There are {inProcessCount} records awaiting to be processed in JusticeExchange. Click on start to continue processing.");
            //}
            //else if (inProcessCount == 1)
            //{
            //    DashboardViewControl.ShowInfoMessage("There is 1 record awaiting to be processed in JusticeExchange. Click on start to continue processing.");
            //}
        }

        private async Task ExecuteScreeningProcess()
        {
            DashboardViewControl.ClearInfoMessage();
            DashboardViewControl.HighlightJusticeExchangeProcessing();
            DashboardViewControl.SetStopEnabled(true);
            AppCloseButton.IsEnabled = false;
            await _configurationRepo.UndoStop();
            var dashboard = DashboardViewControl;
            bool passwordStop = false; // local flag for this execution
            bool errorResponseStop = false; // local flag for this execution
            try
            {
                // Check if JE password change is required; if yes, show message and stop continuous processing
                var changePwdRequired = await _configurationRepo.GetConfigurationValueAsync("ChangePasswordRequiredInJusticeExchange");
                if (string.Equals(changePwdRequired, "Yes", StringComparison.OrdinalIgnoreCase))
                {
                    _passwordChangeDetected = true;
                    passwordStop = true;
                    DashboardViewControl.ShowInfoMessage("Previous login attempt failed in Justice Exchange due to Invalid Password or Password Expired. Please update password. Scheduled processing stopped.");
                    _cts?.Cancel(); // cancel continuous loop
                    _isContinuousRunning = false;
                    dashboard?.SetStartEnabled(true);
                    DashboardViewControl.SetStopEnabled(false);
                    return; // skip remaining processing
                }

                // Check if error response occurred in JE; if yes, show message and stop continuous processing
                //var errorResponse = await _configurationRepo.GetConfigurationValueAsync("ErrorResponseOccurred");
                //if (string.Equals(errorResponse, "Yes", StringComparison.OrdinalIgnoreCase))
                //{
                //    _errorResponseDetected = true;
                //    errorResponseStop = true; // reuse flag to preserve message
                //    DashboardViewControl.ShowInfoMessage("An error response was received from last processing attempt. Scheduled processing stopped.");
                //    _cts?.Cancel(); // cancel continuous loop
                //    _isContinuousRunning = false;
                //    dashboard?.SetStartEnabled(true);
                //    DashboardViewControl.SetStopEnabled(false);
                //    return; // skip remaining processing
                //}


                // Determine parameter from UI (selected count) or set your own value
                //var selected = dashboard?.NamesCombo?.SelectedItem as ComboBoxItem;
                //int countToProcess = 100; // default fallback
                //if (selected != null)
                //{
                //    // Prefer Tag if provided (e.g., "All" item carries Tag="100")
                //    if (selected.Tag is string tagStr && int.TryParse(tagStr, out var tagVal))
                //    {
                //        countToProcess = tagVal;
                //    }
                //    else if (selected.Tag is int tagInt)
                //    {
                //        countToProcess = tagInt;
                //    }
                //    else if (selected.Content is string contentStr && int.TryParse(contentStr, out var contentVal))
                //    {
                //        countToProcess = contentVal;
                //    }
                //}

                //await _configurationRepo.UpdateMaxRecordsToProcessAsync(100);

                var processStartStop = await _configurationRepo.GetProcessStartAndStopAsync();
                if (!processStartStop.Stop && _isContinuousRunning == true)
                {
                    // *********** Start of Justice Exchange WinForms app process ***********
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
                // After completion, return highlight to Stopped and conditionally re-enable Start button
                dashboard?.HighlightStopped();
                if (_isContinuousRunning)
                {
                    DashboardViewControl.SetStopEnabled(true);
                }
                else
                {
                    dashboard?.SetStartEnabled(true);
                }
                
                
                if (!passwordStop && !errorResponseStop) // only show completion message if not stopped for these reasons
                {
                    DashboardViewControl.ShowInfoMessage("Screening process completed for current cycle. Next cycle will start shortly.");
                }
                // Delete all records from all tables except Configuration table and ProcessStartAndStop table
                await DeleteAllRecords();
                await RefreshDashboardMetricsAsync();
            }
        }

        private async Task RefreshDashboardMetricsAsync()
        {
            try
            {
                var metrics = await _incomingOrderSearchRepository.GetDashboardMetricsAsync();
                DashboardViewControl.SetDashboardMetrics(metrics);
            }
            catch
            {
                DashboardViewControl.ShowWarningMessage("Unable to load dashboard metrics.");
            }
        }

        // Continuous scheduling logic
        private async Task RunContinuousProcessingAsync(CancellationToken token)
        {
            DashboardViewControl.SetStopEnabled(true);

            while (!token.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var scheduleMap = await GetProcessingScheduleMapAsync();

                if (!HasAnyActiveSchedule(scheduleMap))
                {                   
                    DashboardViewControl.ShowInfoMessage("No active schedule configured.");
                }
                else if (ShouldRunNow(now, scheduleMap) && WillEnterMaintenanceWithin(now, TimeSpan.FromMinutes(45), scheduleMap))
                {
                    DashboardViewControl.ShowInfoMessage("The scheduled process will continue running after the Maintenance Window.");

                    DateTime maintenanceEnd = GetMaintenanceEndForDay(now, scheduleMap);
                    TimeSpan maintenanceDelay = maintenanceEnd - DateTime.Now;
                    if (maintenanceDelay < TimeSpan.Zero)
                        maintenanceDelay = TimeSpan.Zero;

                    try
                    {
                        await Task.Delay(maintenanceDelay, token);
                        await RefreshDashboardMetricsAsync();
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }

                    continue;
                }
                else if (ShouldRunNow(now, scheduleMap))
                {
                    await ExecuteScreeningProcess();
                }
                else
                {                    
                    // Show waiting message based on configured schedule
                    if (IsInConfiguredMaintenanceWindow(now, scheduleMap))
                    {
                        DashboardViewControl.ShowInfoMessage("The scheduled process will continue running after the Maintenance Window.");
                    }
                    else
                    {
                        DashboardViewControl.ShowInfoMessage("Outside configured schedule window.");
                    }
                }

                // Compute next allowed start (1 minute after completion or current time if we skipped)
                DateTime earliest = DateTime.Now.AddMinutes(1);
                DateTime nextStart = GetNextAllowedStart(earliest, scheduleMap);
                TimeSpan delay = nextStart - DateTime.Now;
                if (delay < TimeSpan.Zero)
                    delay = TimeSpan.Zero;

                try
                {
                    await Task.Delay(delay, token);
                    await RefreshDashboardMetricsAsync();
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
            // After loop finishes ensure Start button enabled
            _isContinuousRunning = false;
            if (!_passwordChangeDetected && !_errorResponseDetected)
            {
                DashboardViewControl.ClearInfoMessage();
            }
            DashboardViewControl.SetStartEnabled(true);            
        }

        private async Task<Dictionary<DayOfWeek, ProcessingSchedule>> GetProcessingScheduleMapAsync()
        {
            var schedules = await _configurationRepo.GetProcessingScheduleAsync();
            var map = new Dictionary<DayOfWeek, ProcessingSchedule>();

            foreach (var schedule in schedules)
            {
                var day = (DayOfWeek)schedule.DayOfWeek;
                map[day] = schedule;
            }

            return map;
        }

        private static bool HasAnyActiveSchedule(IReadOnlyDictionary<DayOfWeek, ProcessingSchedule> scheduleMap)
        {
            foreach (var schedule in scheduleMap.Values)
            {
                if (!schedule.IsTurnedOff && schedule.StartTime.HasValue && schedule.StopTime.HasValue && schedule.StartTime.Value < schedule.StopTime.Value)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ShouldRunNow(DateTime now, IReadOnlyDictionary<DayOfWeek, ProcessingSchedule> scheduleMap)
        {
            if (!scheduleMap.TryGetValue(now.DayOfWeek, out var schedule))
            {
                return false;
            }

            if (schedule.IsTurnedOff || !schedule.StartTime.HasValue || !schedule.StopTime.HasValue)
            {
                return false;
            }

            var time = now.TimeOfDay;
            var withinRunWindow = time >= schedule.StartTime.Value && time < schedule.StopTime.Value;
            if (!withinRunWindow)
            {
                return false;
            }

            if (schedule.MaintenanceStartTime.HasValue && schedule.MaintenanceStopTime.HasValue)
            {
                var inMaintenance = time >= schedule.MaintenanceStartTime.Value && time < schedule.MaintenanceStopTime.Value;
                if (inMaintenance)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsInConfiguredMaintenanceWindow(DateTime now, IReadOnlyDictionary<DayOfWeek, ProcessingSchedule> scheduleMap)
        {
            if (!scheduleMap.TryGetValue(now.DayOfWeek, out var schedule))
            {
                return false;
            }

            if (!schedule.MaintenanceStartTime.HasValue || !schedule.MaintenanceStopTime.HasValue)
            {
                return false;
            }

            var time = now.TimeOfDay;
            return time >= schedule.MaintenanceStartTime.Value && time < schedule.MaintenanceStopTime.Value;
        }

        private static bool WillEnterMaintenanceWithin(DateTime now, TimeSpan lookAhead, IReadOnlyDictionary<DayOfWeek, ProcessingSchedule> scheduleMap)
        {
            if (!scheduleMap.TryGetValue(now.DayOfWeek, out var schedule))
            {
                return false;
            }

            if (!schedule.MaintenanceStartTime.HasValue || !schedule.MaintenanceStopTime.HasValue)
            {
                return false;
            }

            var current = now.TimeOfDay;
            var maintenanceStart = schedule.MaintenanceStartTime.Value;

            return current < maintenanceStart && maintenanceStart <= current.Add(lookAhead);
        }
                
        private DateTime GetNextAllowedStart(DateTime earliest, IReadOnlyDictionary<DayOfWeek, ProcessingSchedule> scheduleMap)
        {
            if (!HasAnyActiveSchedule(scheduleMap))
            {
                return earliest;
            }

            DateTime dt = earliest;
            // Search up to 8 days ahead for next runnable minute based on configured schedule
            for (int i = 0; i < 60 * 24 * 8; i++)
            {
                if (ShouldRunNow(dt, scheduleMap))
                    return dt;

                dt = dt.AddMinutes(1);
            }

            return earliest;
        }

        private DateTime GetMaintenanceEndForDay(DateTime now, IReadOnlyDictionary<DayOfWeek, ProcessingSchedule> scheduleMap)
        {
            if (!scheduleMap.TryGetValue(now.DayOfWeek, out var schedule))
            {
                return now;
            }

            if (!schedule.MaintenanceStopTime.HasValue)
            {
                return now;
            }

            return now.Date.Add(schedule.MaintenanceStopTime.Value);
        }

        private async void StartButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_isContinuousRunning)
                return; // Already running
            _stopping = false;
            _isContinuousRunning = true;
            _passwordChangeDetected = false; // reset flag
            _errorResponseDetected = false; // reset flag
            DashboardViewControl.SetStartEnabled(false);
            DashboardViewControl.ShowInfoMessage("Scheduled processing started.");
            _cts = new CancellationTokenSource();
            _continuousTask = RunContinuousProcessingAsync(_cts.Token); // fire & forget

            ResetErrorResponseOccurredToFalse(); // reset error response flag in configuration at the start of processing
        }

        private async void ResetErrorResponseOccurredToFalse()
        {
            try
            {
                await _configurationRepo.SetConfigurationValueAsync("ErrorResponseOccurred", "No");
            }
            catch
            {
                // Intentionally ignore: failure to reset flag should not block starting processing.
            }
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
            _cts?.Cancel();
            _isContinuousRunning = false;
            await _configurationRepo.StopProcess();
            DashboardViewControl.SetStopEnabled(false);
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
