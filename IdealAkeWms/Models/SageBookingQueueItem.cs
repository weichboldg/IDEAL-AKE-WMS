using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

/// <summary>
/// Ein Auftrag, eine WMS-Lagerbuchung (manuelle Ein-/Ausbuchung) an Sage zu melden.
/// Wird beim Speichern der Buchung durch den Enqueue-Decorator erzeugt (Status
/// <see cref="SageBookingQueueStatus.Offen"/>) und vom <c>SageBookingWorker</c> im
/// Windows-Service abgearbeitet. Traegt die volle Nachvollziehbarkeit (wer/wann/was +
/// rohe Sage-Antwort). FK auf den urspruenglichen <see cref="StockMovement"/>.
/// </summary>
public class SageBookingQueueItem : AuditableEntity
{
    [Display(Name = "Lagerbewegung")]
    public int StockMovementId { get; set; }

    public StockMovement StockMovement { get; set; } = null!;

    [Display(Name = "Status")]
    public SageBookingQueueStatus Status { get; set; } = SageBookingQueueStatus.Offen;

    /// <summary>Anzahl bisheriger Sende-Versuche (fuer Retry-Cap / Fehlermail-Schwelle).</summary>
    [Display(Name = "Versuche")]
    public int AttemptCount { get; set; }

    [Display(Name = "Letzter Versuch")]
    public DateTime? LastAttemptAt { get; set; }

    /// <summary>Letzter Fehlertext (Netzwerk/Sage-Fehlerantwort/Timeout), null bei Erfolg.</summary>
    [StringLength(2000)]
    [Display(Name = "Letzter Fehler")]
    public string? LastError { get; set; }

    /// <summary>Rohe Sage-Antwort (Erfolg oder Fehler) fuer die Nachvollziehbarkeit.</summary>
    [Display(Name = "Sage-Antwort")]
    public string? SageResponseRaw { get; set; }

    [Display(Name = "Gesendet am")]
    public DateTime? SentAt { get; set; }

    [Display(Name = "Bestaetigt am")]
    public DateTime? ConfirmedAt { get; set; }
}
