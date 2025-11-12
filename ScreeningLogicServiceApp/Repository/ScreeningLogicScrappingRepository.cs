using Microsoft.EntityFrameworkCore;
using ScreeningLogicServiceApp.Models;

namespace ScreeningLogicServiceApp.Repository
{
    public class ScreeningLogicScrappingRepository : IScreeningLogicScrappingRepository
    {
        private readonly IDbContextFactory<ScreeningLogicAutomationContext> _contextFactory;

        public ScreeningLogicScrappingRepository(IDbContextFactory<ScreeningLogicAutomationContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }
        
        public async Task<int> GetScreeningLogicScrappingInProgressInJusticeExchangeAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            var openScreeningLogicBatchId = await context.JusticeExchangeBatches
                .Where(b => b.EndDateTime == null)
                .Select(b => b.ScreeningLogicBatchId)
                .FirstOrDefaultAsync();

            var screeningLogicScrappingCount = await context.ScreeningLogicScrappings
                                                    .Where(s => s.ScreeningLogicBatchId == openScreeningLogicBatchId && !s.IsSentToBackgroundScreening)
                                                    .CountAsync();
            return screeningLogicScrappingCount;
        }

        // Deletes all records from all tables except Configuration and ProcessStartAndStop
        public async Task DeleteAllExceptConfigurationAndProcessAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            await using var tx = await context.Database.BeginTransactionAsync();

            // Delete in FK-safe order (deepest children first)
            await context.PersonAliases.ExecuteDeleteAsync();
            await context.PersonRecords.ExecuteDeleteAsync();
            await context.PersonSummaries.ExecuteDeleteAsync();
            await context.JusticeLogicPersonSummaryLinks.ExecuteDeleteAsync();
            await context.JusticeExchangeBatches.ExecuteDeleteAsync();
            await context.ScreeningLogicScrappings.ExecuteDeleteAsync();
            await context.ScreeningLogicBatches.ExecuteDeleteAsync();

            // Keep Configurations and ProcessStartAndStops as requested

            await tx.CommitAsync();
        }
    }
}
