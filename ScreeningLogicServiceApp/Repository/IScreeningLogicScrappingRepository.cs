namespace ScreeningLogicServiceApp.Repository
{
    public interface IScreeningLogicScrappingRepository
    {
        Task<int> GetScreeningLogicScrappingInProgressInJusticeExchangeAsync();
    }
}
