namespace IdealAkeWms.Services;

/// <summary>Auswahl einer Auftragszeile beim Umbuchen (v1.88.0: <see cref="PickingItemId"/> = Id der <c>PickingOrderLine</c>; der Name bleibt,
/// weil die Stuecklistenseite ihn als Formularfeld sendet).</summary>
public class PickingSelectionItem
{
    public int PickingItemId { get; set; }
    public int? SourceStorageLocationId { get; set; }
    public bool IsBaugruppe { get; set; }
    /// <summary>v1.88.0: Ist-Menge als Text (keine feste Request-Kultur → Komma/Punkt); Pflicht.</summary>
    public string? IstMenge { get; set; }
}

public class PickingTransferResult
{
    public bool Success { get; set; }
    public int TransferredCount { get; set; }
    public bool IsPickingTransportConflict { get; set; }
    public int? ConflictStorageLocationId { get; set; }
    public string? ConflictStorageLocationCode { get; set; }
    public string? CurrentWaNumbers { get; set; }
    public string? NewWaNumber { get; set; }
}

public interface IPickingTransferService
{
    /// <summary>v1.88.0 (Spec 2026-11-08-kommissionierung-auftraege-kopf-zeilen, E3): gewaehlte Zeilen eines Kommissionierauftrags mit
    /// Ist-Menge umbuchen (Quelle → Kommissionierwagen/Werkbank). Fehler (Menge, Bestand, Status) als <see cref="InvalidOperationException"/>.</summary>
    Task<PickingTransferResult> TransferOrderLinesAsync(
        int pickingOrderId,
        int targetStorageLocationId,
        bool forceTransfer,
        List<PickingSelectionItem> selectedItems,
        int? appUserId,
        string displayName,
        string windowsUser);
}
