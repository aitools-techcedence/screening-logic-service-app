using System.Collections.Generic;
using System.Threading.Tasks;

namespace ScreeningLogicServiceApp.Repository
{
    public interface IPasswordRepository
    {
        Task<Dictionary<string, string>> GetConfigValuesAsync(IEnumerable<string> keys);
        Task UpsertConfigValuesAsync(Dictionary<string, string> updates);
    }
}
