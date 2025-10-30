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
            }
            catch
            {
                MessageBox.Show("Unable to save configuration.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
