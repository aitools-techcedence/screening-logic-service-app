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
        Task<string?> GetConfigurationValueAsync(string configKey);
        Task SetConfigurationValueAsync(string configKey, string value);
        Task<List<ProcessingSchedule>> GetProcessingScheduleAsync();
        Task SaveProcessingScheduleAsync(IEnumerable<ProcessingSchedule> schedules);
    }
}
