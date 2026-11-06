using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

/// <summary>
/// Vorgemerkte Uebergabe eines im WMS neu angelegten Stammdatensatzes an Sage (Spec 2026-11-06-sage-sdata-stammdaten-sync F11, E9).
/// Wird bei der WMS-Neuanlage befuellt, aber <b>nicht gesendet</b> – es gibt (noch) keinen Worker und keinen Sage-Service (G5).
/// Aufbau analog <see cref="SageBookingQueueItem"/>, damit ein spaeterer <c>SageStammdatenWorker</c> wie der <c>SageBookingWorker</c>
/// andockt (Status-Automat <see cref="SageBookingQueueStatus"/>: Offen → Gesendet → Bestaetigt / Fehler). Findet der Lese-Abgleich den
/// Datensatz vorher in Sage, setzt er den Eintrag auf <see cref="SageBookingQueueStatus.Bestaetigt"/> (nichts mehr zu senden).
/// </summary>
public class SageStammdatenOutboxItem : AuditableEntity
{
    /// <summary>Art des Datensatzes, z. B. <c>StorageLocation</c>, <c>BdeOperator</c> (siehe <c>SageStammdatenOutbox.Typ*</c>).</summary>
    [StringLength(50)]
    [Display(Name = "Typ")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>WMS-Id des Datensatzes als Text (generisch fuer kuenftige Stammdaten); der Worker liest die aktuellen Felder darueber.</summary>
    [StringLength(100)]
    [Display(Name = "Schlüssel")]
    public string EntityKey { get; set; } = string.Empty;

    /// <summary>JSON-Momentaufnahme der Felder zum Zeitpunkt der Anlage (nur zur Nachvollziehbarkeit).</summary>
    [Display(Name = "Inhalt")]
    public string PayloadJson { get; set; } = "{}";

    [Display(Name = "Status")]
    public SageBookingQueueStatus Status { get; set; } = SageBookingQueueStatus.Offen;

    [Display(Name = "Versuche")]
    public int AttemptCount { get; set; }

    [StringLength(1000)]
    [Display(Name = "Fehler")]
    public string? LastError { get; set; }
}
