using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScreeningLogicServiceApp.Views
{
    public partial class NotificationsView : UserControl
    {
        public NotificationsView()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            bool allValid = true;
            Brush errorBrush = Brushes.IndianRed;

            void Mark(TextBox tb)
            {
                if (string.IsNullOrWhiteSpace(tb.Text))
                {
                    tb.BorderBrush = errorBrush;
                    tb.BorderThickness = new Thickness(2);
                    allValid = false;
                }
                else
                {
                    tb.ClearValue(Border.BorderBrushProperty);
                    tb.ClearValue(Border.BorderThicknessProperty);
                }
            }

            Mark(LoginEmailTextBox);
            Mark(QCTestEmailTextBox);
            Mark(AppProcessEmailTextBox);
            Mark(NotOnlineEmailTextBox);

            if (!allValid)
            {
                return; // highlight only
            }
            // Save logic placeholder
        }
    }
}
