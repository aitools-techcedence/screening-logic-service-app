using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScreeningLogicServiceApp.Views
{
    public partial class QCView : UserControl
    {
        private const int MinRows = 1;
        private const int MaxRows = 10;

        private TextBox[] _last;
        private TextBox[] _first;
        private TextBox[] _dob;
        private TextBox[] _match;
        private FrameworkElement[] _indices;

        public QCView()
        {
            InitializeComponent();
            CacheControls();
            SetRowVisibility(MinRows);
            CountTextBox.Text = MinRows.ToString(CultureInfo.InvariantCulture);
        }

        private void CacheControls()
        {
            _last = new[] { Last1, Last2, Last3, Last4, Last5, Last6, Last7, Last8, Last9, Last10 };
            _first = new[] { First1, First2, First3, First4, First5, First6, First7, First8, First9, First10 };
            _dob = new[] { Dob1, Dob2, Dob3, Dob4, Dob5, Dob6, Dob7, Dob8, Dob9, Dob10 };
            _match = new[] { Match1, Match2, Match3, Match4, Match5, Match6, Match7, Match8, Match9, Match10 };
            _indices = new FrameworkElement[] { Idx1, Idx2, Idx3, Idx4, Idx5, Idx6, Idx7, Idx8, Idx9, Idx10 };
        }

        private void SetRowVisibility(int count)
        {
            for (int i = 0; i < MaxRows; i++)
            {
                var vis = i < count ? Visibility.Visible : Visibility.Collapsed;
                _indices[i].Visibility = vis;
                _last[i].Visibility = vis;
                _first[i].Visibility = vis;
                _dob[i].Visibility = vis;
                _match[i].Visibility = vis;
            }
        }

        private void CountTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow only digits
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void CountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Allow intermediate empty text during typing/backspace; don't force 1 yet
            if (string.IsNullOrEmpty(CountTextBox.Text))
            {
                SetRowVisibility(0);
                return;
            }

            if (int.TryParse(CountTextBox.Text, NumberStyles.None, CultureInfo.InvariantCulture, out int count) && count >= MinRows && count <= MaxRows)
            {
                SetRowVisibility(count);
            }
            else
            {
                // Don't coerce yet; wait for LostFocus to reset. Keep previous visibility.
            }
        }

        private void CountTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(CountTextBox.Text, NumberStyles.None, CultureInfo.InvariantCulture, out int count) || count < MinRows || count > MaxRows)
            {
                CountTextBox.Text = MinRows.ToString(CultureInfo.InvariantCulture);
                SetRowVisibility(MinRows);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var errorBrush = Brushes.IndianRed;
            bool allValid = true;

            if (!int.TryParse(CountTextBox.Text, out int count) || count < MinRows || count > MaxRows)
            {
                CountTextBox.Text = MinRows.ToString(CultureInfo.InvariantCulture);
                SetRowVisibility(MinRows);
                count = MinRows;
            }

            for (int i = 0; i < count; i++)
            {
                allValid &= ValidateBox(_last[i], errorBrush);
                allValid &= ValidateBox(_first[i], errorBrush);
                allValid &= ValidateBox(_dob[i], errorBrush);
                allValid &= ValidateBox(_match[i], errorBrush);
            }

            if (!allValid)
                return;
            // TODO: persist entries
        }

        private static bool ValidateBox(TextBox tb, Brush errorBrush)
        {
            if (string.IsNullOrWhiteSpace(tb.Text))
            {
                tb.BorderBrush = errorBrush;
                tb.BorderThickness = new Thickness(2);
                return false;
            }
            tb.ClearValue(Border.BorderBrushProperty);
            tb.ClearValue(Border.BorderThicknessProperty);
            return true;
        }
    }
}
