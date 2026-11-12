using IdealAkeWms.Models;
using IdealAkeWms.Services;

namespace IDEALAKEWMSService.Services;

/// <summary>Einstellungen des Laufs „Lieferverzug-Mails“ (ServiceSettings <c>Lieferverzug:*</c>, je Standort; gelesen in
/// <see cref="LieferverzugMailService.LesenAsync"/>).</summary>
public sealed record LieferverzugEinstellungen(
    bool EinkaufAktiv, IReadOnlyList<string> EinkaufEmpfaenger, bool KreditorAktiv, bool Testbetrieb,
    int TageNachTermin, int Uhrzeit, int WiederholTage, int MaxJeLauf,
    string? Absender, string? AntwortAdresse, string Standort, string? AppBaseUrl);

/// <summary>Kreditor-Daten, die der Planer braucht (aus <see cref="Creditor"/>).</summary>
public sealed record LieferverzugKreditor(int Id, string Name, string? Email, string? ReminderEmail, bool ReminderDisabled, bool IsActive);

/// <summary>Eine faellige Position mit Rest (Rest-Regel) und Tagen seit dem Liefertermin.</summary>
public sealed record LieferverzugPosition(SagePurchaseOrderLine Line, decimal Rest, int TageUeberfaellig);

/// <summary>Grund/Status der Kreditor-Mail je Kreditor (Statuszeile der Einkauf-Liste und Count).</summary>
public enum LieferverzugGrund
{
    Gesendet, Testbetrieb, NichtDran, OhneAdresse, Abgeschaltet, Gesperrt, KeinKreditor, Inaktiv, KreditorMailsAus, StandAlt, Grenze,
    Fehler, NichtGesendet
}

/// <summary>Alle faelligen Positionen eines Lieferanten (Sage-Konto) mit dem Ergebnis fuer die Kreditor-Mail.</summary>
public sealed class LieferverzugGruppe
{
    public required string? Kto { get; init; }
    public required string Name { get; init; }
    public int? CreditorId { get; init; }
    public required List<LieferverzugPosition> Positionen { get; init; }
    /// <summary>Zieladresse aus dem Kreditor-Stamm (Erinnerungsadresse, sonst E-Mail), auch im Testbetrieb die „waere an“-Adresse.</summary>
    public string? Adresse { get; set; }
    /// <summary>Positionen, die heute in die Kreditor-Mail gehen; null = keine Kreditor-Mail.</summary>
    public List<LieferverzugPosition>? MailPositionen { get; set; }
    public LieferverzugGrund Grund { get; set; }
    public string Status { get; set; } = "";
}

public sealed record LieferverzugPlan(IReadOnlyList<LieferverzugGruppe> Gruppen, bool StandAlt, bool GrenzeUeberschritten, int AnzahlEchteMails)
{
    public int AnzahlFaellig => Gruppen.Sum(g => g.Positionen.Count);
    public IEnumerable<LieferverzugGruppe> MitMail => Gruppen.Where(g => g.MailPositionen != null);
}

/// <summary>
/// v1.99.10 (Spec 2026-11-12-offene-bestellungen-mahnmail-lieferverzug F1, F3–F6): reine Entscheidung „wer bekommt heute welche Mail“ –
/// ohne DB, ohne Versand (Muster „Entscheidung im Helper, Ausfuehrung im Dienst“). Ausfuehrung: <see cref="LieferverzugMailService"/>.
/// </summary>
public static class LieferverzugPlaner
{
    /// <summary>Bestelldaten aelter als das gelten als veraltet: keine Kreditor-Mails (F5, Empty-Guard haelt alte Daten fest).</summary>
    public static readonly TimeSpan MaxStandAlter = TimeSpan.FromHours(24);

    /// <summary>F6 / Freigabe-Antwort 3: Montag bis Freitag ab der eingestellten Stunde (Feiertage nicht beruecksichtigt).</summary>
    public static bool ImZeitfenster(DateTime jetzt, int uhrzeit) =>
        jetzt.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && jetzt.Hour >= uhrzeit;

    /// <summary>F1 / Freigabe-Antworten 2 und 8: Termin gesetzt, heute ≥ Termin + N Kalendertage, Rest &gt; 0.</summary>
    public static bool TerminUeberschritten(SagePurchaseOrderLine l, DateTime heute, int tageNachTermin) =>
        l.Liefertermin is { } t && heute.Date >= t.Date.AddDays(tageNachTermin);

    /// <param name="restJeBelPos">Rest nach der Rest-Regel; fehlt eine Position, gilt sie als nicht faellig.</param>
    /// <param name="letzteMail">Letzte Kreditor-Mail je (BelPosId, Liefertermin) – nur Zeilen des aktuellen Modus (echt bzw. Testbetrieb).</param>
    /// <param name="stand">MAX(SyncedAt) des Caches; null = Cache leer.</param>
    public static LieferverzugPlan Planen(IEnumerable<SagePurchaseOrderLine> zeilen, IReadOnlyDictionary<int, decimal> restJeBelPos,
        IReadOnlyDictionary<int, LieferverzugKreditor> kreditoren, IReadOnlyDictionary<(int BelPosId, DateTime? Termin), DateTime> letzteMail,
        LieferverzugEinstellungen e, DateTime jetzt, DateTime? stand)
    {
        var heute = jetzt.Date;
        var standAlt = stand == null || jetzt - stand.Value > MaxStandAlter;

        var faellig = zeilen
            .Where(l => TerminUeberschritten(l, heute, e.TageNachTermin) && restJeBelPos.TryGetValue(l.BelPosId, out var r) && r > 0m)
            .Select(l => new LieferverzugPosition(l, restJeBelPos[l.BelPosId], (heute - l.Liefertermin!.Value.Date).Days));

        var gruppen = faellig
            .GroupBy(p => p.Line.LieferantKto ?? "")
            .Select(g =>
            {
                var pos = g.OrderBy(p => p.Line.Liefertermin).ThenBy(p => p.Line.Belegnummer).ThenBy(p => p.Line.BelPosId).ToList();
                var cid = pos.Select(p => p.Line.CreditorId).FirstOrDefault(c => c != null);
                var k = cid is { } id && kreditoren.TryGetValue(id, out var kk) ? kk : null;
                return new LieferverzugGruppe
                {
                    Kto = g.Key.Length == 0 ? null : g.Key,
                    Name = k?.Name ?? pos.Select(p => p.Line.LieferantName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? (g.Key.Length == 0 ? "(ohne Lieferant)" : g.Key),
                    CreditorId = k?.Id,
                    Positionen = pos,
                };
            })
            .OrderBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(g => g.Kto)
            .ToList();

        foreach (var g in gruppen)
            Entscheiden(g, g.CreditorId is { } id ? kreditoren[id] : null, letzteMail, e, heute, standAlt);

        // F5 Grenze je Lauf: nur echte Kreditor-Mails; alle oder keine
        var echte = e.Testbetrieb ? 0 : gruppen.Count(g => g.MailPositionen != null);
        var grenze = echte > e.MaxJeLauf;
        if (grenze)
            foreach (var g in gruppen.Where(g => g.MailPositionen != null))
                Setzen(g, LieferverzugGrund.Grenze, $"ausgesetzt – mehr als {e.MaxJeLauf} Kreditor-Mails fällig (Grenze je Lauf)", null);

        return new LieferverzugPlan(gruppen, standAlt, grenze, grenze ? 0 : echte);
    }

    private static void Entscheiden(LieferverzugGruppe g, LieferverzugKreditor? k, IReadOnlyDictionary<(int, DateTime?), DateTime> letzteMail,
        LieferverzugEinstellungen e, DateTime heute, bool standAlt)
    {
        var adresse = k == null ? null : string.IsNullOrWhiteSpace(k.ReminderEmail) ? k.Email?.Trim() : k.ReminderEmail.Trim();
        g.Adresse = string.IsNullOrWhiteSpace(adresse) ? null : adresse;

        if (!e.KreditorAktiv) { Setzen(g, LieferverzugGrund.KreditorMailsAus, "Kreditor-Mails aus", null); return; }
        if (k == null) { Setzen(g, LieferverzugGrund.KeinKreditor, "kein Kreditor im WMS", null); return; }
        if (g.Positionen.Any(p => p.Line.LieferantGesperrt)) { Setzen(g, LieferverzugGrund.Gesperrt, "Lieferant gesperrt", null); return; }
        if (!k.IsActive) { Setzen(g, LieferverzugGrund.Inaktiv, "Kreditor inaktiv", null); return; }
        if (k.ReminderDisabled) { Setzen(g, LieferverzugGrund.Abgeschaltet, "abgeschaltet (keine Erinnerungsmails)", null); return; }
        if (g.Adresse == null) { Setzen(g, LieferverzugGrund.OhneAdresse, "keine Adresse", null); return; }
        if (!MailAdressen.IstGueltig(g.Adresse)) { Setzen(g, LieferverzugGrund.OhneAdresse, $"ungültige Adresse „{g.Adresse}“", null); return; }
        if (standAlt) { Setzen(g, LieferverzugGrund.StandAlt, "ausgesetzt – Bestelldaten veraltet", null); return; }

        // Freigabe-Antwort 4: erstmals beim Faelligwerden, dann alle WiederholTage; geaenderter Termin = neuer Schluessel
        var dran = new List<LieferverzugPosition>();
        DateTime? naechste = null;
        foreach (var p in g.Positionen)
        {
            if (letzteMail.TryGetValue((p.Line.BelPosId, p.Line.Liefertermin?.Date), out var zuletzt) && heute < zuletzt.Date.AddDays(e.WiederholTage))
            {
                var n = zuletzt.Date.AddDays(e.WiederholTage);
                if (naechste == null || n < naechste) naechste = n;
            }
            else dran.Add(p);
        }
        if (dran.Count == 0) { Setzen(g, LieferverzugGrund.NichtDran, $"nächste Erinnerung am {naechste:dd.MM.yyyy}", null); return; }

        if (e.Testbetrieb) Setzen(g, LieferverzugGrund.Testbetrieb, $"TESTBETRIEB – an Einkauf umgeleitet (wäre an {g.Adresse} gegangen)", dran);
        else Setzen(g, LieferverzugGrund.Gesendet, $"gesendet an {g.Adresse}", dran);
    }

    private static void Setzen(LieferverzugGruppe g, LieferverzugGrund grund, string status, List<LieferverzugPosition>? mail)
    {
        g.Grund = grund;
        g.Status = status;
        g.MailPositionen = mail;
    }
}
