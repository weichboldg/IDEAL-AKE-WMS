namespace IDEALAKEWMSService.Services;

/// <summary>
/// Idempotenz-Lookup (B2): prueft ueber die Sage-<c>SageConnection</c> (Raw-SQL), ob zu einer
/// WMS-Bewegung bereits eine Buchung in <c>KHKLagerplatzbuchungen</c> existiert (Korrelation ueber
/// den <c>SM#&lt;id&gt;#</c>-Marker im <c>Memo</c>). Verhindert Doppelbuchungen bei Requeue/Recovery.
/// </summary>
public interface ISageBuchungLookupReader
{
    /// <summary>True, wenn bereits eine Sage-Buchung mit dieser Kennung im Memo existiert (v1.99.9: Kennung statt Id, wegen System-Kuerzel;
    /// <see cref="SageBookingCorrelation.Kennung"/>).</summary>
    Task<bool> ExistsAsync(string marker, CancellationToken ct = default);
}
