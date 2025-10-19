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
    }
}
