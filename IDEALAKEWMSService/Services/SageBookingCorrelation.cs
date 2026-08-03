namespace IDEALAKEWMSService.Services;

/// <summary>
/// Korrelations-Schluessel fuer die Idempotenz (B2): die WMS-<c>StockMovement.Id</c> wird als
/// delimitierter Marker <c>SM#&lt;id&gt;#</c> in das Sage-<c>Memo</c> eingebettet. Der Marker ist so
/// gewaehlt, dass er (a) keine LIKE-Metazeichen enthaelt und (b) durch das abschliessende '#' keine
/// Praefix-Kollision hat (<c>SM#12#</c> matcht nicht <c>SM#123#</c>). Payload-Builder und
/// Sage-Lookup nutzen denselben Marker, damit eine bereits gebuchte Bewegung vor einem Requeue
/// zuverlaessig wiedergefunden wird.
/// </summary>
public static class SageBookingCorrelation
{
    public static string Marker(int stockMovementId) => $"SM#{stockMovementId}#";

    public static string Memo(int stockMovementId) => $"IdealAkeWms Lagerbuchung {Marker(stockMovementId)}";

    /// <summary>T-SQL-LIKE-Muster fuer den Sage-Lookup (kein Escaping noetig — keine Metazeichen).</summary>
    public static string LikePattern(int stockMovementId) => $"%{Marker(stockMovementId)}%";
}
