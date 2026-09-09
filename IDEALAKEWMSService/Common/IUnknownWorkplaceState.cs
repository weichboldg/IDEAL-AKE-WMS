namespace IDEALAKEWMSService.Common;

/// <summary>
/// Haelt die zuletzt gemeldeten unbekannten Arbeitsbereiche, damit die Sammelmail nur bei
/// AENDERUNG rausgeht (S1: sonst 96 identische Mails am Tag bei 15-Minuten-Takt).
/// Bewusst nur im Speicher — nach einem Dienst-Neustart einmal zu viel zu melden ist harmlos.
/// </summary>
public interface IUnknownWorkplaceState
{
    /// <summary>true, wenn sich die Menge gegenueber der letzten Meldung geaendert hat (dann merken).</summary>
    bool HasChanged(IReadOnlyCollection<string> unknownNames);
}

public sealed class UnknownWorkplaceState : IUnknownWorkplaceState
{
    private readonly object _gate = new();
    private HashSet<string> _last = new(StringComparer.OrdinalIgnoreCase);

    public bool HasChanged(IReadOnlyCollection<string> unknownNames)
    {
        var now = new HashSet<string>(unknownNames, StringComparer.OrdinalIgnoreCase);
        lock (_gate)
        {
            if (_last.SetEquals(now)) return false;
            _last = now;
            return true;
        }
    }
}
