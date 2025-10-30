using System.Windows;
using System.Windows.Controls;
using ScreeningLogicServiceApp.Repository;
using ScreeningLogicServiceApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ScreeningLogicServiceApp.Views
{
    public partial class BehaviorView : UserControl
    {
        private readonly IConfigurationRepository _configRepo;

        public BehaviorView()
        {
            InitializeComponent();
            // Resolve repository from App.Services at runtime
            _configRepo = App.Services?.GetService(typeof(IConfigurationRepository)) as IConfigurationRepository;

            // Hide success message when user changes selection
            rbSlow.Checked += (_, __) => tbSuccess.Visibility = Visibility.Collapsed;
            rbStandard.Checked += (_, __) => tbSuccess.Visibility = Visibility.Collapsed;
            rbFast.Checked += (_, __) => tbSuccess.Visibility = Visibility.Collapsed;

            // Clear success message when tab is unloaded (navigated away)
            Unloaded += BehaviorView_Unloaded;
        }

        private void BehaviorView_Unloaded(object sender, RoutedEventArgs e)
        {
            tbSuccess.Text = string.Empty;
            tbSuccess.Visibility = Visibility.Collapsed;
        }

        private async void BehaviorView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_configRepo == null) return;

            try
            {
                var val = await _configRepo.GetBehaviourAsync();
                switch (val)
                {
                    case "Slow":
                        rbSlow.IsChecked = true;
                        break;
                    case "Standard":
                        rbStandard.IsChecked = true;
                        break;
                    case "Fast":
                        rbFast.IsChecked = true;
                        break;
                    default:
                        rbStandard.IsChecked = true; // default
                        break;
                }

                // Ensure success message is hidden on load
                tbSuccess.Text = string.Empty;
                tbSuccess.Visibility = Visibility.Collapsed;
            }
            catch
            {
                // ignore load errors
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_configRepo == null) return;

            string selected = "Standard";
            if (rbSlow.IsChecked == true) selected = "Slow";
            else if (rbFast.IsChecked == true) selected = "Fast";

            try
            {
                await _configRepo.SaveBehaviourAsync(selected);

                // Show success message centered below radio buttons
                tbSuccess.Text = "Behaviour has been updated successfully";
                tbSuccess.Visibility = Visibility.Visible;
            }
            catch
            {
                MessageBox.Show("Unable to save configuration.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
