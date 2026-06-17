using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IBdeBookingRepository
{
    Task<BdeBooking?> GetByIdAsync(int id);
    Task<BdeBooking?> GetActiveForOperatorAsync(int operatorId);
    Task<BdeBooking?> GetActiveForWorkOperationAsync(int workOperationId);
    Task<BdeBooking?> GetLatestPausedForWorkOperationAsync(int workOperationId);
    Task<List<BdeBooking>> GetActiveCockpitAsync();
    Task<List<BdeBooking>> GetHistoryAsync(int skip, int take, int? operatorId, int? workplaceId, DateTime? from, DateTime? to, IReadOnlyDictionary<string, string>? columnFilters = null);
    Task<int> GetHistoryCountAsync(int? operatorId, int? workplaceId, DateTime? from, DateTime? to, IReadOnlyDictionary<string, string>? columnFilters = null);
    Task AddAsync(BdeBooking booking);
    Task UpdateAsync(BdeBooking booking);
    Task<decimal> GetTotalGoodAsync(int bookingId);
    Task<decimal> GetTotalScrapAsync(int bookingId);
}
