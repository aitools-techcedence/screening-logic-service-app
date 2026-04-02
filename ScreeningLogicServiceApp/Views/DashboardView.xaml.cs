using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ScreeningLogicServiceApp.Models;

namespace ScreeningLogicServiceApp.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            HighlightStopped();
        }

        public ComboBox NamesCombo => NamesToProcessComboBox;

        public event RoutedEventHandler? StartClicked;
        public event RoutedEventHandler? StopClicked;

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            SetStartEnabled(false);
            ClearInfoMessage();
            StartClicked?.Invoke(this, e);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {            
            ClearInfoMessage();
            StopClicked?.Invoke(this, e);
        }

        public void HighlightStopped()
        {
            SetActive(StoppedCard);
            SetInactive(JusticeExchangeCard);
            //SetStartEnabled(true);
            SetStopEnabled(false);
        }

        public void HighlightJusticeExchangeProcessing()
        {
            SetInactive(StoppedCard);
            SetActive(JusticeExchangeCard);
        }

        private static void SetActive(Border card)
        {
            card.BorderBrush = new SolidColorBrush(Color.FromRgb(27, 91, 106)); // #1B5B6A
            card.BorderThickness = new Thickness(2);
            card.Background = new SolidColorBrush(Color.FromRgb(210, 238, 247)); // #D2EEF7
        }

        private static void SetInactive(Border card)
        {
            card.BorderBrush = new SolidColorBrush(Color.FromRgb(157, 185, 194)); // #9DB9C2
            card.BorderThickness = new Thickness(1);
            card.Background = new SolidColorBrush(Color.FromRgb(246, 252, 255)); // #F6FCFF
        }

        public void SetStartEnabled(bool enabled)
        {
            StartButton.IsEnabled = enabled;
        }

        public void SetStopEnabled(bool enabled)
        {
            StopButton.IsEnabled = enabled;
        }

        public void ShowInfoMessage(string message)
        {
            InfoTextBlock.Text = message;
            InfoTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(27, 91, 106)); // info color
            InfoTextBlock.Visibility = string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;
        }

        public void ShowWarningMessage(string message)
        {
            InfoTextBlock.Text = message;
            InfoTextBlock.Foreground = Brushes.DarkOrange;
            InfoTextBlock.FontWeight = FontWeights.SemiBold;
            InfoTextBlock.Visibility = string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;
        }

        public void ClearInfoMessage()
        {
            InfoTextBlock.Text = string.Empty;
            InfoTextBlock.Visibility = Visibility.Collapsed;
        }

        public void SetDashboardMetrics(IncomingOrderDashboardMetrics metrics)
        {
            ApplicationProcessedValueText.Text = metrics.ApplicationProcessedCount.ToString();
            SinceValueText.Text = metrics.Since?.ToString("MM/dd/yyyy hh:mm tt") ?? "-";
            HitRatioValueText.Text = $"{metrics.HitRatioPercent:0.##}%";
            AverageProcessTimeValueText.Text = FormatDuration(metrics.AverageProcessTime);
            AverageTurnaroundValueText.Text = FormatDuration(metrics.AverageTurnaround);
        }

        private static string FormatDuration(TimeSpan? duration)
        {
            if (!duration.HasValue)
            {
                return "-";
            }

            var value = duration.Value;
            return value.TotalHours >= 1
                ? $"{(int)value.TotalHours}h {value.Minutes}m"
                : $"{value.Minutes}m";
        }
    }
}
