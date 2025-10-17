using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScreeningLogicServiceApp.Models;
using ConfigEntity = ScreeningLogicServiceApp.Models.Configuration;

namespace ScreeningLogicServiceApp.Repository
{
    public class PasswordRepository : IPasswordRepository
    {
        private readonly IDbContextFactory<ScreeningLogicAutomationContext> _contextFactory;

        public PasswordRepository(IDbContextFactory<ScreeningLogicAutomationContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<Dictionary<string, string>> GetConfigValuesAsync(IEnumerable<string> keys)
        {
            var list = keys?.Distinct(StringComparer.Ordinal).ToList() ?? new();
            if (list.Count == 0) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using var db = _contextFactory.CreateDbContext();
            var map = await db.Configurations
                              .Where(c => list.Contains(c.ConfigKey))
                              .ToDictionaryAsync(c => c.ConfigKey, c => c.ConfigValue)
                              .ConfigureAwait(false);

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in map)
            {
                result[kvp.Key] = kvp.Value ?? string.Empty;
            }
            return result;
        }

        public async Task UpsertConfigValuesAsync(Dictionary<string, string> updates)
        {
            if (updates == null || updates.Count == 0) return;

            using var db = _contextFactory.CreateDbContext();
            var keys = updates.Keys.Distinct(StringComparer.Ordinal).ToList();
            var existing = await db.Configurations
                                   .Where(c => keys.Contains(c.ConfigKey))
                                   .ToListAsync()
                                   .ConfigureAwait(false);

            var existingByKey = existing.ToDictionary(c => c.ConfigKey, StringComparer.Ordinal);
            foreach (var kvp in updates)
            {
                if (existingByKey.TryGetValue(kvp.Key, out var config))
                {
                    config.ConfigValue = kvp.Value ?? string.Empty;
                    db.Configurations.Update(config);
                }
                else
                {
                    await db.Configurations.AddAsync(new ConfigEntity
                    {
                        ConfigKey = kvp.Key,
                        ConfigValue = kvp.Value ?? string.Empty,
                        UserId = null
                    }).ConfigureAwait(false);
                }
            }

            await db.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
