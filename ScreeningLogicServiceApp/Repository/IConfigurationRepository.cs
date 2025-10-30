using ScreeningLogicServiceApp.Models;

namespace ScreeningLogicServiceApp.Repository
{
    public interface IConfigurationRepository
    {
        Task<ProcessStartAndStop> GetProcessStartAndStopAsync();
        Task UpdateMaxRecordsToProcessAsync(int maxRecordsToProcess);
        Task StopProcess();
        Task UndoStop();
        Task<string?> GetBehaviourAsync();
        Task SaveBehaviourAsync(string value);
    }
}
