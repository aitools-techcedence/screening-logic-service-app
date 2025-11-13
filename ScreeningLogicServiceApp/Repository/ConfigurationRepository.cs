using Microsoft.EntityFrameworkCore;
using ScreeningLogicServiceApp.Models;

namespace ScreeningLogicServiceApp.Repository
{
    public class ConfigurationRepository : IConfigurationRepository
    {
        private readonly IDbContextFactory<ScreeningLogicAutomationContext> _contextFactory;

        public ConfigurationRepository(IDbContextFactory<ScreeningLogicAutomationContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<ProcessStartAndStop> GetProcessStartAndStopAsync()
        {
            using var context = _contextFactory.CreateDbContext();

            return await context.ProcessStartAndStops.AsNoTracking().FirstOrDefaultAsync();
        }

        public async Task UpdateMaxRecordsToProcessAsync(int maxRecordsToProcess)
        {
            using var context = _contextFactory.CreateDbContext();
            var config = await context.ProcessStartAndStops.FirstOrDefaultAsync();
            if (config != null)
            {
                config.MaxRecordsToProcess = maxRecordsToProcess;
                context.ProcessStartAndStops.Update(config);
                await context.SaveChangesAsync();
            }
        }

        public async Task StopProcess()
        {
            using var context = _contextFactory.CreateDbContext();
            var config = await context.ProcessStartAndStops.FirstAsync();
            if (config != null)
            {
                config.Stop = true;
                context.ProcessStartAndStops.Update(config);
                await context.SaveChangesAsync();
            }
        }

        public async Task UndoStop()
        {
            using var context = _contextFactory.CreateDbContext();
            ProcessStartAndStop pss = await context.ProcessStartAndStops.FirstAsync();
            pss.Stop = false;
            context.ProcessStartAndStops.Update(pss);
            await context.SaveChangesAsync();
        }

        public async Task<string?> GetBehaviourAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            var config = await context.Configurations.AsNoTracking().FirstOrDefaultAsync(c => c.ConfigKey == "Behaviour");
            return config?.ConfigValue;
        }

        public async Task SaveBehaviourAsync(string value)
        {
            using var context = _contextFactory.CreateDbContext();
            var config = await context.Configurations.FirstOrDefaultAsync(c => c.ConfigKey == "Behaviour");
            if (config == null)
            {
                config = new Configuration
                {
                    ConfigKey = "Behaviour",
                    ConfigValue = value,
                    UserId = null
                };
                context.Configurations.Add(config);
            }
            else
            {
                config.ConfigValue = value;
                context.Configurations.Update(config);
            }

            await context.SaveChangesAsync();
        }

        public async Task<string?> GetConfigurationValueAsync(string configKey)
        {
            using var context = _contextFactory.CreateDbContext();
            var config = await context.Configurations.AsNoTracking().FirstOrDefaultAsync(c => c.ConfigKey == configKey);
            return config?.ConfigValue;
        }
    }
}
