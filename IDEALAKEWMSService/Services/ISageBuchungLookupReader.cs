namespace IDEALAKEWMSService.Services;

/// <summary>
/// Idempotenz-Lookup (B2): prueft ueber die Sage-<c>SageConnection</c> (Raw-SQL), ob zu einer
/// WMS-Bewegung bereits eine Buchung in <c>KHKLagerplatzbuchungen</c> existiert (Korrelation ueber
/// den <c>SM#&lt;id&gt;#</c>-Marker im <c>Memo</c>). Verhindert Doppelbuchungen bei Requeue/Recovery.
/// </summary>
public interface ISageBuchungLookupReader
{
    /// <summary>True, wenn zur Bewegung bereits eine Sage-Buchung existiert.</summary>
    Task<bool> ExistsAsync(int stockMovementId, CancellationToken ct = default);
}
