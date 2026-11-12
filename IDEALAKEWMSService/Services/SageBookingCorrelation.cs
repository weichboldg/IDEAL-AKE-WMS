using IdealAkeWms.Models;
using IdealAkeWms.Services;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// Korrelations-Schluessel fuer die Idempotenz (B2): die WMS-<c>StockMovement.Id</c> wird als
/// delimitierter Marker <c>SM#&lt;id&gt;#</c> in das Sage-<c>Memo</c> eingebettet. Der Marker ist so
/// gewaehlt, dass er (a) keine LIKE-Metazeichen enthaelt und (b) durch das abschliessende '#' keine
/// Praefix-Kollision hat (<c>SM#12#</c> matcht nicht <c>SM#123#</c>). Payload-Builder und
/// Sage-Lookup nutzen denselben Marker, damit eine bereits gebuchte Bewegung vor einem Requeue
/// zuverlaessig wiedergefunden wird.
/// v1.99.9: mit System-Kuerzel <c>SM#&lt;K&gt;#&lt;id&gt;#</c> (<see cref="SageSystemKuerzel"/>) - mehrere WMS-Datenbanken buchen
/// gegen dieselbe Sage-Testdatenbank; ohne Kuerzel faende der Lookup die Buchung eines anderen WMS und sendete nie.
/// </summary>
public static class SageBookingCorrelation
{
    public static string Marker(int stockMovementId, string? kuerzel = null) => SageSystemKuerzel.Kennung("SM", kuerzel, stockMovementId);

    /// <summary>v1.99.9: Kennung eines Queue-Eintrags - die beim Senden gespeicherte; nie gesendet (<c>SentAt</c> leer) = mit dem aktuellen
    /// Kuerzel; vor v1.99.9 gesendet (ohne gespeicherte Kennung) = altes Format ohne Kuerzel.</summary>
    public static string Kennung(SageBookingQueueItem item, string? kuerzel) =>
        item.SageKennung ?? (item.SentAt == null ? Marker(item.StockMovementId, kuerzel) : Marker(item.StockMovementId));

    public static string Memo(string marker) => $"IdealAkeWms Lagerbuchung {marker}";

    /// <summary>T-SQL-LIKE-Muster fuer den Sage-Lookup (kein Escaping noetig — keine Metazeichen).</summary>
    public static string LikePattern(string marker) => $"%{marker}%";
}
