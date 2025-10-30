using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using Microsoft.Extensions.DependencyInjection;
using ScreeningLogicServiceApp.Repository;

namespace ScreeningLogicServiceApp.Views
{
    public partial class PasswordsView : UserControl
    {
        private readonly IPasswordRepository _repo;
        private bool _slSyncing;
        private bool _jxSyncing;

        public PasswordsView()
        {
            InitializeComponent();
            _repo = App.Services.GetRequiredService<IPasswordRepository>();
            Loaded += PasswordsView_Loaded;
        }

        private async void PasswordsView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var keys = new[]
                {
                    "ScreeningLogicAccount",
                    "ScreeningLogicUserId",
                    "ScreeningLogicPassword",
                    "JusticeExchangeUserId",
                    "JusticeExchangePassword"
                };

                var map = await _repo.GetConfigValuesAsync(keys);

                string slAccount = map.GetValueOrDefault("ScreeningLogicAccount", string.Empty) ?? string.Empty;
                string slUser = map.GetValueOrDefault("ScreeningLogicUserId", string.Empty) ?? string.Empty;
                string slPassword = map.GetValueOrDefault("ScreeningLogicPassword", string.Empty) ?? string.Empty;

                // Fallbacks if only ScreeningLogicAccount exists
                if (string.IsNullOrWhiteSpace(slUser)) slUser = slAccount;
                if (string.IsNullOrWhiteSpace(slPassword)) slPassword = slAccount;

                SlAccountTextBox.Text = slAccount;
                SlUserTextBox.Text = slUser;
                SlPasswordBox.Password = slPassword;
                if (FindName("SlPasswordTextBox") is TextBox slPwText) slPwText.Text = slPassword;

                JxUserTextBox.Text = map.GetValueOrDefault("JusticeExchangeUserId", string.Empty) ?? string.Empty;
                string jxPw = map.GetValueOrDefault("JusticeExchangePassword", string.Empty) ?? string.Empty;
                JxPasswordBox.Password = jxPw;
                if (FindName("JxPasswordTextBox") is TextBox jxPwText) jxPwText.Text = jxPw;

                ShowStatus(string.Empty, false);
            }
            catch (Exception ex)
            {
                ShowStatus($"Failed to load configuration: {ex.Message}", true, isError: true);
            }
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
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
            Mark(JxUserTextBox);
            MarkPw(JxPasswordBox);

            if (!allValid)
            {
                ShowStatus("Please correct the highlighted fields.", true, isError: true);
                return; // highlight only
            }

            try
            {
                var updates = new Dictionary<string, string>
                {
                    ["ScreeningLogicAccount"] = SlAccountTextBox.Text.Trim(),
                    ["ScreeningLogicUserId"] = SlUserTextBox.Text.Trim(),
                    ["ScreeningLogicPassword"] = SlPasswordBox.Password,
                    ["JusticeExchangeUserId"] = JxUserTextBox.Text.Trim(),
                    ["JusticeExchangePassword"] = JxPasswordBox.Password
                };

                await _repo.UpsertConfigValuesAsync(updates);
                ShowStatus("Configuration Updated", true);
            }
            catch (Exception ex)
            {
                ShowStatus($"Failed to update configuration: {ex.Message}", true, isError: true);
            }
        }

        private void ShowStatus(string message, bool visible, bool isError = false)
        {
            if (StatusTextBlock == null) return;
            StatusTextBlock.Text = message;
            StatusTextBlock.FontWeight = isError ? FontWeights.Normal : FontWeights.SemiBold;
            StatusTextBlock.FontSize = isError ? 16 : 24; // larger for success
            StatusTextBlock.Foreground = isError ? Brushes.IndianRed : new SolidColorBrush(Color.FromRgb(46, 125, 50));
            StatusTextBlock.Visibility = visible && !string.IsNullOrWhiteSpace(message) ? Visibility.Visible : Visibility.Collapsed;
        }

        // Toggle handlers
        private void SlShowToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (FindName("SlPasswordTextBox") is TextBox slPwText)
            {
                slPwText.Text = SlPasswordBox.Password;
                slPwText.Visibility = Visibility.Visible;
                SlPasswordBox.Visibility = Visibility.Collapsed;
            }
            if (sender is ToggleButton tb) tb.Content = "Hide";
        }

        private void SlShowToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (FindName("SlPasswordTextBox") is TextBox slPwText)
            {
                SlPasswordBox.Password = slPwText.Text;
                SlPasswordBox.Visibility = Visibility.Visible;
                slPwText.Visibility = Visibility.Collapsed;
            }
            if (sender is ToggleButton tb) tb.Content = "Show";
        }

        private void JxShowToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (FindName("JxPasswordTextBox") is TextBox jxPwText)
            {
                jxPwText.Text = JxPasswordBox.Password;
                jxPwText.Visibility = Visibility.Visible;
                JxPasswordBox.Visibility = Visibility.Collapsed;
            }
            if (sender is ToggleButton tb) tb.Content = "Hide";
        }

        private void JxShowToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (FindName("JxPasswordTextBox") is TextBox jxPwText)
            {
                JxPasswordBox.Password = jxPwText.Text;
                JxPasswordBox.Visibility = Visibility.Visible;
                jxPwText.Visibility = Visibility.Collapsed;
            }
            if (sender is ToggleButton tb) tb.Content = "Show";
        }

        // Sync handlers to keep both controls in sync when user edits either
        private void SlPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_slSyncing) return;
            try
            {
                _slSyncing = true;
                if (FindName("SlPasswordTextBox") is TextBox slPwText)
                    slPwText.Text = SlPasswordBox.Password;
            }
            finally { _slSyncing = false; }
        }

        private void SlPasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_slSyncing) return;
            try
            {
                _slSyncing = true;
                SlPasswordBox.Password = ((TextBox)sender).Text;
            }
            finally { _slSyncing = false; }
        }

        private void JxPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_jxSyncing) return;
            try
            {
                _jxSyncing = true;
                if (FindName("JxPasswordTextBox") is TextBox jxPwText)
                    jxPwText.Text = JxPasswordBox.Password;
            }
            finally { _jxSyncing = false; }
        }

        private void JxPasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_jxSyncing) return;
            try
            {
                _jxSyncing = true;
                JxPasswordBox.Password = ((TextBox)sender).Text;
            }
            finally { _jxSyncing = false; }
        }
    }
}
