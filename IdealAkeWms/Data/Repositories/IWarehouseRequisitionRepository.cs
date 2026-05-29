using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Data.Repositories;

public interface IWarehouseRequisitionRepository
{
    Task<int> CreateDraftAsync(int productionWorkplaceId, int currentUserId, string currentUserName, string windowsUserName);
    Task<WarehouseRequisition?> GetByIdAsync(int id, bool includeItems = true);
    Task<List<WarehouseRequisition>> GetForUserAsync(int userId, int historyDays = 30);
    Task<(List<WarehouseRequisition> Items, int TotalCount)> GetForWarehouseAsync(
        WarehouseRequisitionStatus[] statuses, int? workplaceId, int page, int pageSize);
    Task<List<WarehouseRequisition>> GetPendingSubmitEmailsAsync();
    Task<List<WarehouseRequisition>> GetPendingCancellationEmailsAsync();

    Task AddItemAsync(int requisitionId, string articleNumber, string description, string? unit,
        decimal quantity, string user, string winUser);
    Task UpdateItemQuantityAsync(int itemId, decimal quantity, string user, string winUser);
    Task RemoveItemAsync(int itemId);

    Task SubmitAsync(int id, int recipientGroupId, int submittedByUserId, string user, string winUser, byte[] rowVersion);
    Task CloseAsync(int id, IReadOnlyDictionary<int, decimal> itemQuantitiesPicked,
                    IReadOnlyDictionary<int, string?> itemNotes,
                    IReadOnlyDictionary<int, ShortageStatus> itemShortageStatuses,
                    int closedByUserId, string user, string winUser, byte[] rowVersion);

    /// <summary>
    /// Setzt nur die Notizen einzelner Positionen (z.B. AJAX-Autosave).
    /// Aendert weder Status noch Mengen. Ignoriert RowVersion bewusst, weil
    /// Notizen nicht konfliktrelevant sind.
    /// </summary>
    Task SaveNotesAsync(int id, IReadOnlyDictionary<int, string?> itemNotes,
        string user, string winUser);

    Task SaveProgressAsync(int id,
        IReadOnlyDictionary<int, decimal?> itemQuantitiesPicked,
        IReadOnlyDictionary<int, string?> itemNotes,
        IReadOnlyDictionary<int, ShortageStatus> itemShortageStatuses,
        string user, string winUser);

    Task<(IReadOnlyList<MissingPartRow> Items, int TotalCount)>
        GetMissingPartsAsync(int? workplaceFilter,
                             IReadOnlyDictionary<string, string>? columnFilters,
                             DateTime? closedFrom, DateTime? closedUntil,
                             int page, int pageSize);

    /// <summary>
    /// Zaehlt offene Final-Shortage-Positionen und betroffene Closed-Bestellungen
    /// fuer alle Vormontageplaetze, denen <paramref name="userId"/> zugeordnet ist.
    /// </summary>
    Task<(int ItemCount, int RequisitionCount)>
        GetFinalShortagesCountForUserAsync(int userId);

    Task CancelAsync(int id, string? reason, int cancelledByUserId, string user, string winUser, byte[] rowVersion);

    Task MarkEmailSentAsync(int id, DateTime sentAt);
    Task MarkCancellationEmailSentAsync(int id, DateTime sentAt);
}
