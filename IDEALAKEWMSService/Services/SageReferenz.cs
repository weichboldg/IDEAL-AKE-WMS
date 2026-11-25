namespace IDEALAKEWMSService.Services;

/// <summary>
/// v1.99.68 (Spec 2026-11-24-sage-buchung-referenz-benutzer-abteilung, Freigabe-Antworten 1/2/5): woher (Kennung), wer (WMS-Benutzer
/// <c>User.Name</c> = <c>CreatedBy</c> der Bewegung bzw. des Receipts, nie der Queue-Eintrag) und fuer welche Abteilung eine Buchung kommt.
/// Rein; alle Eingaben sind gespeicherte Felder, daher senden Requeue/Recovery/Nacherfassung denselben Text.
/// </summary>
public static class SageReferenz
{
    /// <summary>Laenge von <c>KHKLagerplatzbuchungen.Referenz</c> (varchar(31), Schema-Abfrage Gerald 2026-11-24). Sage-Fakt, keine Einstellung.</summary>
    public const int ReferenzMaxLaenge = 31;

    /// <summary>Zeilen-<c>Referenz</c> der Lagerbuchung: <c>&lt;Kennung&gt; &lt;Abteilung&gt; &lt;Benutzer&gt;</c>, auf <see cref="ReferenzMaxLaenge"/>
    /// gekuerzt - am Ende abgeschnitten, also zuerst der Benutzer, dann die Abteilung; die Kennung nie. Leere Teile entfallen.</summary>
    public static string Kurz(string kennung, string? abteilung, string? benutzer)
    {
        var text = string.Join(' ', new[] { kennung, abteilung?.Trim(), benutzer?.Trim() }.Where(t => !string.IsNullOrEmpty(t)));
        return text.Length <= ReferenzMaxLaenge ? text : text[..Math.Max(ReferenzMaxLaenge, kennung.Length)].TrimEnd();
    }

    /// <summary>Voller Text fuer Zeilen-Memo (Lagerbuchung) bzw. Kopf-Memo (Sage-WE), beide varchar(max):
    /// <c>WMS: &lt;Kennung&gt; &lt;Benutzer&gt;[ Abteilung:&lt;x&gt;]</c>. Enthaelt die Kennung, die Memo-Suche trifft weiter.</summary>
    public static string Voll(string kennung, string? benutzer, string? abteilung) =>
        "WMS: " + string.Join(' ', new[] { kennung, benutzer?.Trim(), string.IsNullOrWhiteSpace(abteilung) ? null : "Abteilung:" + abteilung.Trim() }
            .Where(t => !string.IsNullOrEmpty(t)));
}
