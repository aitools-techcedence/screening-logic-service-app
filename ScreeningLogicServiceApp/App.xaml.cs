using System;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScreeningLogicServiceApp.Models;
using ScreeningLogicServiceApp.Repository;

namespace ScreeningLogicServiceApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = default!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            // Read connection string from App.config and provide it via IConfiguration
            var defaultCs = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString
                             ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection' in App.config.");

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", defaultCs)
                })
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            // Register pooled factory to safely create contexts on demand (thread-safe per operation)
            services.AddPooledDbContextFactory<ScreeningLogicAutomationContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Repositories using factory
            services.AddScoped<IPasswordRepository, PasswordRepository>();

            Services = services.BuildServiceProvider();
        }
    }
}
