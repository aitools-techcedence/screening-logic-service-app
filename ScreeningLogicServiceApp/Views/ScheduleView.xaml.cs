using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScreeningLogicServiceApp.Views
{
    public partial class ScheduleView : UserControl
    {
        private sealed class DayRow
        {
            public required string Name { get; init; }
            public required CheckBox TurnOff { get; init; }
            public required ComboBox Start { get; init; }
            public required ComboBox Stop { get; init; }
            public required ComboBox MaintenanceStart { get; init; }
            public required ComboBox MaintenanceStop { get; init; }

            public IEnumerable<ComboBox> TimeControls
            {
                get
                {
                    yield return Start;
                    yield return Stop;
                    yield return MaintenanceStart;
                    yield return MaintenanceStop;
                }
            }
        }

        private DayRow[] _rows = [];

        public ScheduleView()
        {
            InitializeComponent();
            CacheRows();
            PopulateTimeDropdowns();
            ApplyTurnOffState();
        }

        private void CacheRows()
        {
            _rows =
            [
                new DayRow { Name = "Sunday", TurnOff = TurnOffSunday, Start = StartSunday, Stop = StopSunday, MaintenanceStart = MaintenanceStartSunday, MaintenanceStop = MaintenanceStopSunday },
                new DayRow { Name = "Monday", TurnOff = TurnOffMonday, Start = StartMonday, Stop = StopMonday, MaintenanceStart = MaintenanceStartMonday, MaintenanceStop = MaintenanceStopMonday },
                new DayRow { Name = "Tuesday", TurnOff = TurnOffTuesday, Start = StartTuesday, Stop = StopTuesday, MaintenanceStart = MaintenanceStartTuesday, MaintenanceStop = MaintenanceStopTuesday },
                new DayRow { Name = "Wednesday", TurnOff = TurnOffWednesday, Start = StartWednesday, Stop = StopWednesday, MaintenanceStart = MaintenanceStartWednesday, MaintenanceStop = MaintenanceStopWednesday },
                new DayRow { Name = "Thursday", TurnOff = TurnOffThursday, Start = StartThursday, Stop = StopThursday, MaintenanceStart = MaintenanceStartThursday, MaintenanceStop = MaintenanceStopThursday },
                new DayRow { Name = "Friday", TurnOff = TurnOffFriday, Start = StartFriday, Stop = StopFriday, MaintenanceStart = MaintenanceStartFriday, MaintenanceStop = MaintenanceStopFriday },
                new DayRow { Name = "Saturday", TurnOff = TurnOffSaturday, Start = StartSaturday, Stop = StopSaturday, MaintenanceStart = MaintenanceStartSaturday, MaintenanceStop = MaintenanceStopSaturday }
            ];
        }

        private void PopulateTimeDropdowns()
        {
            var startItems = new List<string> { string.Empty };
            for (var hour = 0; hour < 24; hour++)
            {
                startItems.Add($"{hour:00}:00");
            }

            var stopItems = new List<string>(startItems)
            {
                "23:59"
            };

            foreach (var row in _rows)
            {
                row.Start.ItemsSource = startItems;
                row.MaintenanceStart.ItemsSource = startItems;
                row.Stop.ItemsSource = stopItems;
                row.MaintenanceStop.ItemsSource = stopItems;

                row.Start.SelectedIndex = 0;
                row.MaintenanceStart.SelectedIndex = 0;
                row.Stop.SelectedIndex = 0;
                row.MaintenanceStop.SelectedIndex = 0;
            }
        }

        private void ApplyTurnOffState()
        {
            foreach (var row in _rows)
            {
                var isTurnedOff = row.TurnOff.IsChecked == true;
                foreach (var combo in row.TimeControls)
                {
                    combo.IsEnabled = !isTurnedOff;
                    if (isTurnedOff)
                    {
                        combo.SelectedIndex = 0;
                        ClearError(combo);
                    }
                }
            }
        }

        private void TurnOff_CheckedChanged(object sender, RoutedEventArgs e)
        {
            ApplyTurnOffState();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var allValid = true;
            var missingStartStop = false;
            var invalidStartStopRange = false;
            var incompleteMaintenance = false;
            var invalidMaintenanceRange = false;

            ValidationMessageTextBlock.Text = string.Empty;

            foreach (var row in _rows)
            {
                if (row.TurnOff.IsChecked == true)
                {
                    continue;
                }

                var hasStart = !string.IsNullOrWhiteSpace(row.Start.SelectedItem as string);
                var hasStop = !string.IsNullOrWhiteSpace(row.Stop.SelectedItem as string);

                allValid &= MarkRequired(row.Start, hasStart);
                allValid &= MarkRequired(row.Stop, hasStop);
                if (!hasStart || !hasStop)
                {
                    missingStartStop = true;
                }

                if (hasStart && hasStop)
                {
                    var start = ParseTime((string)row.Start.SelectedItem!);
                    var stop = ParseTime((string)row.Stop.SelectedItem!);
                    if (start >= stop)
                    {
                        MarkError(row.Start);
                        MarkError(row.Stop);
                        allValid = false;
                        invalidStartStopRange = true;
                    }
                }

                var maintenanceStartText = row.MaintenanceStart.SelectedItem as string;
                var maintenanceStopText = row.MaintenanceStop.SelectedItem as string;
                var hasMaintenanceStart = !string.IsNullOrWhiteSpace(maintenanceStartText);
                var hasMaintenanceStop = !string.IsNullOrWhiteSpace(maintenanceStopText);

                if (hasMaintenanceStart ^ hasMaintenanceStop)
                {
                    allValid &= MarkRequired(row.MaintenanceStart, hasMaintenanceStart);
                    allValid &= MarkRequired(row.MaintenanceStop, hasMaintenanceStop);
                    incompleteMaintenance = true;
                }
                else
                {
                    if (hasMaintenanceStart && hasMaintenanceStop)
                    {
                        var maintenanceStart = ParseTime(maintenanceStartText!);
                        var maintenanceStop = ParseTime(maintenanceStopText!);
                        if (maintenanceStart >= maintenanceStop)
                        {
                            MarkError(row.MaintenanceStart);
                            MarkError(row.MaintenanceStop);
                            allValid = false;
                            invalidMaintenanceRange = true;
                        }
                    }
                    else
                    {
                        ClearError(row.MaintenanceStart);
                        ClearError(row.MaintenanceStop);
                    }
                }
            }

            if (!allValid)
            {
                var messageParts = new List<string>();
                if (missingStartStop)
                {
                    messageParts.Add("Select Start and Stop time for active days.");
                }
                if (invalidStartStopRange)
                {
                    messageParts.Add("Start time must be before Stop time.");
                }
                if (incompleteMaintenance)
                {
                    messageParts.Add("Select both Maintenance times.");
                }
                if (invalidMaintenanceRange)
                {
                    messageParts.Add("Maintenance Start must be before Maintenance Stop.");
                }

                ValidationMessageTextBlock.Text = string.Join(" ", messageParts);
                return;
            }

            ValidationMessageTextBlock.Text = string.Empty;

            // TODO: persist schedule settings
        }

        private static TimeSpan ParseTime(string timeText)
        {
            return TimeSpan.ParseExact(timeText, "hh\\:mm", CultureInfo.InvariantCulture);
        }

        private static bool MarkRequired(Control control, bool isValid)
        {
            if (isValid)
            {
                ClearError(control);
                return true;
            }

            MarkError(control);
            return false;
        }

        private static void MarkError(Control control)
        {
            control.BorderBrush = Brushes.IndianRed;
            control.BorderThickness = new Thickness(2);
        }

        private static void ClearError(Control control)
        {
            control.ClearValue(Border.BorderBrushProperty);
            control.ClearValue(Border.BorderThicknessProperty);
        }
    }
}
