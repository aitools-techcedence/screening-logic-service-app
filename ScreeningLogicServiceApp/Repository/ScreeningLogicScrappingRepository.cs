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

        // Returns the number of JusticeExchangeBatch records currently in process (EndDateTime is null)
        // for ScreeningLogic batches that have completed (EndDateTime is not null).
        public async Task<int> GetBatchInProcessInJusticeExchangeAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.JusticeExchangeBatches
                .CountAsync(j => j.EndDateTime == null && j.ScreeningLogicBatch.EndDateTime != null);
        }
    }
}
