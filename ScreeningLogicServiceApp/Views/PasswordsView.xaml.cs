using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using ScreeningLogicServiceApp.Repository;

namespace ScreeningLogicServiceApp.Views
{
    public partial class PasswordsView : UserControl
    {
        private readonly IPasswordRepository _repo;

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
                    "ScreeningLogicUser",
                    "ScreeningLogicPassword",
                    "JusticeExchangeUserId",
                    "JusticeExchangePassword"
                };

                var map = await _repo.GetConfigValuesAsync(keys);

                string slAccount = map.GetValueOrDefault("ScreeningLogicAccount", string.Empty) ?? string.Empty;
                string slUser = map.GetValueOrDefault("ScreeningLogicUser", string.Empty) ?? string.Empty;
                string slPassword = map.GetValueOrDefault("ScreeningLogicPassword", string.Empty) ?? string.Empty;

                // Fallbacks if only ScreeningLogicAccount exists
                if (string.IsNullOrWhiteSpace(slUser)) slUser = slAccount;
                if (string.IsNullOrWhiteSpace(slPassword)) slPassword = slAccount;

                SlAccountTextBox.Text = slAccount;
                SlUserTextBox.Text = slUser;
                SlPasswordBox.Password = slPassword;

                JxUserTextBox.Text = map.GetValueOrDefault("JusticeExchangeUserId", string.Empty) ?? string.Empty;
                JxPasswordBox.Password = map.GetValueOrDefault("JusticeExchangePassword", string.Empty) ?? string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                return; // highlight only
            }

            try
            {
                var updates = new Dictionary<string, string>
                {
                    ["ScreeningLogicAccount"] = SlAccountTextBox.Text.Trim(),
                    ["ScreeningLogicUser"] = SlUserTextBox.Text.Trim(),
                    ["ScreeningLogicPassword"] = SlPasswordBox.Password,
                    ["JusticeExchangeUserId"] = JxUserTextBox.Text.Trim(),
                    ["JusticeExchangePassword"] = JxPasswordBox.Password
                };

                await _repo.UpsertConfigValuesAsync(updates);
                MessageBox.Show("Configuration updated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
