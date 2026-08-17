namespace IdealAkeWms.Services.HierarchischeStruktur;

public enum GuardedSetting { Master, AutoErledigt }

/// <summary>Ergebnis der Wächter-Prüfung. Bei <see cref="Allowed"/>=false trägt
/// <see cref="RejectionReason"/> die anwenderlesbare Begründung.</summary>
public sealed record GuardDecision(bool Allowed, string? RejectionReason);

/// <summary>Wird geworfen, wenn ein Schreibversuch auf einen geschützten Key
/// vom Wächter abgelehnt wird (Choke-Point-Decorator).</summary>
public sealed class HierarchischeStrukturGuardException : Exception
{
    public HierarchischeStrukturGuardException(string message) : base(message) { }
}

/// <summary>
/// Reiner, unit-testbarer Wächter des FA-Hierarchie-Einwegtors (Muster
/// <c>ProductionOrderReconciler</c>, keine DB). Die Sperrbedingung ist datengetrieben:
/// <paramref name="hierarchicalDataExists"/> = EXISTS(ProductionOrders WHERE OrderNumber &lt;&gt; SubOrderNumber).
/// Anzeige liest gecacht, der Schreibpfad prüft live (Freigabe-Antwort 3).
/// </summary>
public static class HierarchischeStrukturGuard
{
    public const string MasterLockedReason =
        "Kann nicht mehr geändert werden: es liegen Aufträge mit Sub-FA-Struktur vor. " +
        "Rückbau ist nicht über die Anwendung umkehrbar — nur über das Runbook, " +
        "siehe docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md";

    public const string AutoErledigtLockedReason =
        "Auto-Erledigt kann nicht aktiviert werden, solange hierarchische Aufträge existieren: " +
        "HauptFA und Sub-FAs teilen dieselbe FA-Nummer, ein Komm-Erledigt am HauptFA träfe die ganze Gruppe.";

    public static GuardDecision Evaluate(
        GuardedSetting setting,
        bool currentValue,
        bool requestedValue,
        bool hierarchicalDataExists)
    {
        // No-op oder Tor noch offen -> frei.
        if (currentValue == requestedValue || !hierarchicalDataExists)
            return new GuardDecision(true, null);

        return setting switch
        {
            GuardedSetting.Master => new GuardDecision(false, MasterLockedReason),
            // Ausschalten immer erlaubt (Master-Flip schaltet ab); nur Einschalten sperren.
            GuardedSetting.AutoErledigt => requestedValue
                ? new GuardDecision(false, AutoErledigtLockedReason)
                : new GuardDecision(true, null),
            _ => new GuardDecision(true, null),
        };
    }
}
