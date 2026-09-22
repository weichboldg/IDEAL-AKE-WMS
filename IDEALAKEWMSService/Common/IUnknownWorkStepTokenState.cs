namespace IDEALAKEWMSService.Common;

/// <summary>
/// Haelt die zuletzt gemeldeten unbekannten Arbeitsschritt-Token, damit die Sammelmail nur bei
/// AENDERUNG der Token-Menge rausgeht (S1: sonst 96 identische Mails am Tag bei 15-Minuten-Takt).
/// Bewusst nur im Speicher — nach einem Dienst-Neustart einmal zu viel zu melden ist harmlos.
///
/// Eigene Instanz, eigener Meldezustand (frueher gab es daneben ein <c>IUnknownWorkplaceState</c>): unbekannte
/// Arbeitsschritt-Token und unbekannte Arbeitsbereiche sind zwei fachlich unabhaengige
/// Meldezustaende, die sich nicht vermischen duerfen.
/// </summary>
public interface IUnknownWorkStepTokenState
{
    /// <summary>true, wenn sich die Menge gegenueber der letzten Meldung geaendert hat (dann merken).</summary>
    bool HasChanged(IReadOnlyCollection<string> unknownTokens);
}

public sealed class UnknownWorkStepTokenState : IUnknownWorkStepTokenState
{
    private readonly object _gate = new();
    private HashSet<string> _last = new(StringComparer.OrdinalIgnoreCase);

    public bool HasChanged(IReadOnlyCollection<string> unknownTokens)
    {
        var now = new HashSet<string>(unknownTokens, StringComparer.OrdinalIgnoreCase);
        lock (_gate)
        {
            if (_last.SetEquals(now)) return false;
            _last = now;
            return true;
        }
    }
}
