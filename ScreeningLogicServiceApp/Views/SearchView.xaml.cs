using Microsoft.Extensions.DependencyInjection;
using ScreeningLogicServiceApp.Models;
using ScreeningLogicServiceApp.Repository;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScreeningLogicServiceApp.Views;

public partial class SearchView : UserControl
{
    private readonly IIncomingOrderSearchRepository _searchRepository;
    private bool _isFormattingDob;

    public SearchView()
    {
        InitializeComponent();
        _searchRepository = App.Services.GetRequiredService<IIncomingOrderSearchRepository>();
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SearchMessageTextBlock.Foreground = Brushes.IndianRed;
            SearchMessageTextBlock.Text = string.Empty;

            var dobSearchValue = NormalizeDobForSearch(DobTextBox.Text);
            if (!string.IsNullOrWhiteSpace(DobTextBox.Text) && dobSearchValue is null)
            {
                SearchMessageTextBlock.Text = "Enter DOB as MM/dd/yyyy.";
                return;
            }

            var results = await _searchRepository.SearchIncomingOrdersAsync(
                OrderNumberTextBox.Text,
                LastNameTextBox.Text,
                FirstNameTextBox.Text,
                dobSearchValue);

            SearchResultsGrid.ItemsSource = results;

            if (results.Count == 0)
            {
                SearchMessageTextBlock.Foreground = Brushes.IndianRed;
                SearchMessageTextBlock.Text = "No matching records found.";
            }
            else
            {
                SearchMessageTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(27, 91, 106));
                SearchMessageTextBlock.Text = $"Found {results.Count} record(s).";
            }
        }
        catch
        {
            SearchMessageTextBlock.Foreground = Brushes.IndianRed;
            SearchMessageTextBlock.Text = "Search failed. Check configuration and try again.";
        }
    }

    private void DobTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = e.Text.Any(ch => !char.IsDigit(ch));
    }

    private void DobTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(typeof(string)))
        {
            e.CancelCommand();
            return;
        }

        var pastedText = e.DataObject.GetData(typeof(string)) as string ?? string.Empty;
        var digits = new string(pastedText.Where(char.IsDigit).Take(8).ToArray());
        DobTextBox.Text = FormatDobFromDigits(digits);
        DobTextBox.CaretIndex = DobTextBox.Text.Length;
        e.CancelCommand();
    }

    private void DobTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isFormattingDob)
        {
            return;
        }

        var digits = new string(DobTextBox.Text.Where(char.IsDigit).Take(8).ToArray());
        var formatted = FormatDobFromDigits(digits);
        if (DobTextBox.Text == formatted)
        {
            return;
        }

        _isFormattingDob = true;
        DobTextBox.Text = formatted;
        DobTextBox.CaretIndex = DobTextBox.Text.Length;
        _isFormattingDob = false;
    }

    private static string FormatDobFromDigits(string digits)
    {
        if (digits.Length <= 2)
        {
            return digits;
        }

        if (digits.Length <= 4)
        {
            return $"{digits[..2]}/{digits[2..]}";
        }

        return $"{digits[..2]}/{digits.Substring(2, 2)}/{digits[4..]}";
    }

    private static string? NormalizeDobForSearch(string? dobText)
    {
        if (string.IsNullOrWhiteSpace(dobText))
        {
            return null;
        }

        if (!DateTime.TryParseExact(dobText.Trim(), "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDob))
        {
            return null;
        }

        return parsedDob.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
    }

    private void ShowErrorButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not IncomingOrderSearchResult row)
        {
            return;
        }

        var report = string.IsNullOrWhiteSpace(row.FailedSummaryReport)
            ? "No FailedSummaryReport content available."
            : row.FailedSummaryReport;

        var detailsWindow = new Window
        {
            Title = "Failed Summary Report",
            Width = 800,
            Height = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.FromRgb(233, 246, 251)),
            Owner = Window.GetWindow(this),
            Content = new Grid
            {
                Margin = new Thickness(16),
                RowDefinitions =
                {
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto }
                },
                Children =
                {
                    new TextBox
                    {
                        Text = report,
                        IsReadOnly = true,
                        TextWrapping = TextWrapping.Wrap,
                        AcceptsReturn = true,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
                    },
                    new Button
                    {
                        Content = "Close",
                        Width = 120,
                        Height = 38,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(0, 10, 0, 0),
                        Style = (Style)FindResource("CloseButtonStyle")
                    }
                }
            }
        };

        if (detailsWindow.Content is Grid grid && grid.Children[1] is Button closeButton)
        {
            Grid.SetRow(closeButton, 1);
            closeButton.Click += (_, _) => detailsWindow.Close();
        }

        detailsWindow.ShowDialog();
    }
}
