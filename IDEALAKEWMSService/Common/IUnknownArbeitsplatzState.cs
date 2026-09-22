namespace IDEALAKEWMSService.Common;

/// <summary>
/// Haelt die zuletzt gemeldeten problematischen Sage-Arbeitsplatz-Zeilen (mehrdeutig / Insert am
/// eindeutigen Index gescheitert), damit die Sammelmail nur bei AENDERUNG der Menge rausgeht
/// (S1-Muster). Bewusst nur im Speicher — nach einem Dienst-Neustart einmal zu viel zu melden ist
/// harmlos. Eigene Instanz fuer die Werkbank-Anlage (Baustein a), fachlich unabhaengig vom
/// Arbeitsschritt-Token-Zustand der Struktur-Erkennung.
/// </summary>
public interface IUnknownArbeitsplatzState
{
    /// <summary>true, wenn sich die Menge gegenueber der letzten Meldung geaendert hat (dann merken).</summary>
    bool HasChanged(IReadOnlyCollection<string> problemNumbers);
}

public sealed class UnknownArbeitsplatzState : IUnknownArbeitsplatzState
{
    private readonly object _gate = new();
    private HashSet<string> _last = new(StringComparer.Ordinal);

    public bool HasChanged(IReadOnlyCollection<string> problemNumbers)
    {
        var now = new HashSet<string>(problemNumbers, StringComparer.Ordinal);
        lock (_gate)
        {
            if (_last.SetEquals(now)) return false;
            _last = now;
            return true;
        }
    }
}
