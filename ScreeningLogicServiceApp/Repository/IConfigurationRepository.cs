using ScreeningLogicServiceApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreeningLogicServiceApp.Repository
{
    public interface IConfigurationRepository
    {
        Task<ProcessStartAndStop> GetProcessStartAndStopAsync();
        Task UpdateMaxRecordsToProcessAsync(int maxRecordsToProcess);
    }
}
