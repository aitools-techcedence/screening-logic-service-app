using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScreeningLogicServiceApp.Views
{
    public partial class PasswordsView : UserControl
    {
        public PasswordsView()
        {
            InitializeComponent();
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
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

            void MarkPw(PasswordBox pb)
            {
                if (string.IsNullOrWhiteSpace(pb.Password))
                {
                    pb.BorderBrush = errorBrush;
                    pb.BorderThickness = new Thickness(2);
                    allValid = false;
                }
                else
                {
                    pb.ClearValue(Border.BorderBrushProperty);
                    pb.ClearValue(Border.BorderThicknessProperty);
                }
            }

            Mark(SlAccountTextBox);
            Mark(SlUserTextBox);
            MarkPw(SlPasswordBox);
            Mark(SlAuthCodeTextBox);
            Mark(JxUserTextBox);
            MarkPw(JxPasswordBox);

            if (!allValid)
            {
                return; // highlight only
            }
        }
    }
}
