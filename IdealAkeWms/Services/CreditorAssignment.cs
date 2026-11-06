using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

/// <summary>Ein Dienstleister-Text ohne aktiven Kreditor: Text (getrimmt, erste Schreibweise), Zahl der Auftragszeilen und ob er nur
/// auf einen INAKTIVEN Kreditor passt („zugeordnet, aber inaktiv“).</summary>
public sealed record UnassignedServiceProvider(string Text, int Rows, bool MatchesInactiveOnly);

/// <summary>
/// Dienstleister-Meldung (E8a, Kreditoren-Spec Anforderung 5): Freitext <see cref="FaHierarchyOrderInfo.Dienstleister"/> → Kreditor ueber
/// <see cref="Creditor.ShortName"/> exakt (getrimmt, Gross/Klein egal). Nicht zuordenbare Texte werden gemeldet, nie still uebergangen.
/// Reine Funktion (Web-Lesefunktion, kein SyncLog).
/// </summary>
public static class CreditorAssignment
{
    public static List<UnassignedServiceProvider> FindUnassigned(IEnumerable<FaHierarchyOrderInfo> infos, IEnumerable<Creditor> creditors)
    {
        var active = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inactive = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in creditors)
        {
            if (string.IsNullOrWhiteSpace(c.ShortName)) continue;
            (c.IsActive ? active : inactive).Add(c.ShortName.Trim());
        }

        return infos
            .Select(i => i.Dienstleister?.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .GroupBy(t => t!, StringComparer.OrdinalIgnoreCase)
            .Where(g => !active.Contains(g.Key))
            .Select(g => new UnassignedServiceProvider(g.First()!, g.Count(), inactive.Contains(g.Key)))
            .OrderBy(u => u.Text, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
