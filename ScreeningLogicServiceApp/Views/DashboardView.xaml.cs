using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
            HighlightScreeningLogicProcessing();
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
            SetInactive(ScreeningLogicCard);
            SetInactive(JusticeExchangeCard);
            SetStartEnabled(true);
            SetStopEnabled(false);
        }

        public void HighlightScreeningLogicProcessing()
        {
            SetInactive(StoppedCard);
            SetActive(ScreeningLogicCard);
            SetInactive(JusticeExchangeCard);
        }

        public void HighlightJusticeExchangeProcessing()
        {
            SetInactive(StoppedCard);
            SetInactive(ScreeningLogicCard);
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
    }
}
