namespace IdealAkeWms.Models;

/// <summary>
/// Status-Automat eines <see cref="SageBookingQueueItem"/>:
/// Offen → Gesendet → Bestaetigt / Fehler.
/// <c>Gesendet</c> wird bewusst VOR dem HTTP-Call gesetzt (Idempotenz-Baustein): ein
/// Timeout nach dem Senden fuehrt beim naechsten Worker-Tick nicht zu einem zweiten
/// automatischen Sende-Versuch. Haengende <c>Gesendet</c>-Eintraege werden ueber den
/// Sage-Memo-Lookup (KHKLagerplatzbuchungen) aufgeloest, nie blind neu gesendet.
/// </summary>
public enum SageBookingQueueStatus
{
    Offen = 0,
    Gesendet = 1,
    Bestaetigt = 2,
    Fehler = 3
}
