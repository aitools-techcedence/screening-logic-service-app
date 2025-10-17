using Microsoft.EntityFrameworkCore;
using ScreeningLogicServiceApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            
            return await context.ProcessStartAndStops.FirstOrDefaultAsync();
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

    }
}
