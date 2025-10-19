using ScreeningLogicServiceApp.Models;

namespace ScreeningLogicServiceApp.Repository
{
    public interface IConfigurationRepository
    {
        Task<ProcessStartAndStop> GetProcessStartAndStopAsync();
        Task UpdateMaxRecordsToProcessAsync(int maxRecordsToProcess);
        Task StopProcess();
    }
}
