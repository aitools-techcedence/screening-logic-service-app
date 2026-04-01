using ScreeningLogicServiceApp.Models;

namespace ScreeningLogicServiceApp.Repository;

public interface IIncomingOrderSearchRepository
{
    Task<IReadOnlyList<IncomingOrderSearchResult>> SearchIncomingOrdersAsync(
        string? orderNumber,
        string? lastName,
        string? firstName,
        string? dob);
}
