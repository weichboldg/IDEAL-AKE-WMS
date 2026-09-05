using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

public interface IBdeBookingRepository
{
    Task<BdeBooking?> GetByIdAsync(int id);
    Task<BdeBooking?> GetActiveForOperatorAsync(int operatorId);
    Task<BdeBooking?> GetActiveForWorkOperationAsync(int workOperationId);
    Task<BdeBooking?> GetLatestPausedForWorkOperationAsync(int workOperationId);
    Task<List<BdeBooking>> GetActiveCockpitAsync();

    /// <summary>
    /// IDEAL Teil 7/8 (Kaskade-Dialog, Freigabe-Antwort 6): Anzahl der Sub-FAs
    /// (<see cref="ProductionOrder"/>) dieser HauptFA-<c>OrderNumber</c> mit mindestens EINER offenen
    /// Buchung. „Offen" = <c>EndedAt == null &amp;&amp; !IsCancelled</c> — also auch pausierte/auto-pausierte
    /// Buchungen (die Warnung gilt „unfertige Arbeit als fertig erklaeren", nicht „stoert gerade
    /// jemanden"). Zaehlt distinkte ProductionOrderIds, nicht Buchungen. Dialog-Text: „mit offener Buchung".
    /// </summary>
    Task<int> CountSubFasWithOpenBookingForOrderNumberAsync(string orderNumber);
    Task<List<BdeBooking>> GetHistoryAsync(int skip, int take, int? operatorId, int? workplaceId, DateTime? from, DateTime? to, IReadOnlyDictionary<string, string>? columnFilters = null);
    Task<int> GetHistoryCountAsync(int? operatorId, int? workplaceId, DateTime? from, DateTime? to, IReadOnlyDictionary<string, string>? columnFilters = null);
    Task AddAsync(BdeBooking booking);
    Task UpdateAsync(BdeBooking booking);
    Task<decimal> GetTotalGoodAsync(int bookingId);
    Task<decimal> GetTotalScrapAsync(int bookingId);
}
