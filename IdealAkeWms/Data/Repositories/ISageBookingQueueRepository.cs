using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

/// <summary>
/// Zugriff auf die Sage-Lagerbuchungs-Queue (<see cref="SageBookingQueueItem"/>).
/// Enqueue laeuft im Web (Decorator auf <see cref="IStockMovementRepository"/>), die
/// Status-Fortschreibung im Windows-Service (<c>SageBookingWorker</c>).
/// </summary>
public interface ISageBookingQueueRepository
{
    /// <summary>Legt einen Queue-Eintrag mit Status Offen fuer eine gespeicherte Buchung an.</summary>
    Task<SageBookingQueueItem> EnqueueAsync(StockMovement movement);

    /// <summary>Offene Eintraege (Status Offen) inkl. Bewegung/Artikel/Lagerplatz fuer den Worker.</summary>
    Task<List<SageBookingQueueItem>> GetOpenBatchAsync(int max);

    /// <summary>
    /// Haengende Eintraege: Status Gesendet, deren <see cref="SageBookingQueueItem.SentAt"/> aelter
    /// als <paramref name="olderThan"/> ist (S3 — ueber Sage-Memo-Lookup aufzuloesen, nie blind neu senden).
    /// </summary>
    Task<List<SageBookingQueueItem>> GetStuckSentAsync(DateTime olderThan, int max);

    /// <summary>Einzelner Eintrag inkl. Navigation (fuer Requeue aus der Monitoring-UI).</summary>
    Task<SageBookingQueueItem?> GetByIdWithMovementAsync(int id);

    /// <summary>
    /// Alle Eintraege (optional nach Status gefiltert) inkl. Bewegung/Artikel/Lagerplatz, neueste zuerst
    /// — Datenquelle der Monitoring-Liste (Spaltenfilter + Pagination im Controller).
    /// </summary>
    Task<List<SageBookingQueueItem>> GetForMonitoringAsync(SageBookingQueueStatus? status);

    /// <summary>Status auf Gesendet setzen (VOR dem HTTP-Call), SentAt/LastAttemptAt/AttemptCount fortschreiben.</summary>
    Task MarkSentAsync(int id);

    /// <summary>Status auf Bestaetigt setzen (+ ConfirmedAt, rohe Antwort).</summary>
    Task MarkConfirmedAsync(int id, string? sageResponseRaw);

    /// <summary>Status auf Fehler setzen (+ LastError, rohe Antwort).</summary>
    Task MarkFailedAsync(int id, string error, string? sageResponseRaw);

    /// <summary>Setzt einen Fehler-Eintrag zurueck auf Offen (manuelles Requeue aus der UI).</summary>
    Task RequeueAsync(int id, string actor);

    /// <summary>
    /// Reconciliation-Sweep (B4): reiht Ein-/Ausbuchungen auf Sage-freigegebenen Lagerplaetzen ab
    /// <paramref name="since"/> nachtraeglich ein, die (z.B. wegen eines Enqueue-Fehlers) KEINEN
    /// Queue-Eintrag haben. Kurzes Rueckblickfenster, damit keine Buchungen aus einer Toggle-Aus-Phase
    /// nachtraeglich gesendet werden. Liefert die Anzahl neu eingereihter Eintraege.
    /// </summary>
    Task<int> EnqueueMissingAsync(DateTime since, int max);
}
