namespace IdealAkeWms.Models.ViewModels;

public class SageBookingQueueViewModel
{
    public List<SageBookingQueueItem> Items { get; set; } = new();
    public SageBookingQueueStatus? FilterStatus { get; set; }
    public PaginationState Pagination { get; set; } = new();

    /// <summary>TLS-Zertifikatspruefung des Sage-Clients ist deaktiviert (Warnhinweis anzeigen).</summary>
    public bool SslCheckDisabled { get; set; }
}
