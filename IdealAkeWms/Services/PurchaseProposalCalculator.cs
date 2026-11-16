namespace IdealAkeWms.Services;

/// <summary>v1.99.20 Dringlichkeit eines Bestellvorschlags (Text-Badge, Farbe nur zusaetzlich).</summary>
public enum PurchaseProposalDringlichkeit { KeinBedarf, Nachbestellen, Kritisch }

/// <summary>Eingabe je Artikel, alles in der Lagermengeneinheit (<c>Article.Unit</c>).</summary>
/// <param name="OffenBestellt">Summe der Reste offener Sage-Bestellungen (E2); 0 = keine bzw. nicht gelesen.</param>
/// <param name="LetzteBestellmenge">Letzte Bestellmenge L (E3, juengste Bestellung/Rahmenabruf der letzten 3 Jahre, Basiseinheit = Lagereinheit).</param>
/// <param name="LetzteBestellungLieferant">null = L stammt vom gewaehlten Lieferanten; sonst Name des anderen Lieferanten (Rueckfrage 13 a).</param>
/// <param name="Mindestbestellmenge">E4: <c>ArticleSupplier.Mindestbestellmenge</c> des gewaehlten Lieferanten.</param>
/// <param name="Einkaufsmengeneinheit">E4: <c>ArticleSupplier.Einkaufsmengeneinheit</c>.</param>
/// <param name="UmrechnungsfaktorEK">E4: <c>ArticleSupplier.UmrechnungsfaktorEK</c>.</param>
/// <param name="ArtikelEinheit">E4: <c>Article.Unit</c> (Lagermengeneinheit).</param>
/// <param name="FallbackSageBestellmenge">E3 Freigabe-Nachtrag 2: <c>ArticleSageDetail.Bestellmenge</c> – nur gesetzt, wenn es im Rueckblick bei
/// KEINEM Lieferanten eine Bestellung gibt; ersetzt dann L (inkl. Erhoehung n × Menge, Rueckfrage 11) mit Hinweis <see cref="PurchaseProposalCalculator.HinweisFallback"/>.</param>
/// <param name="LetzteBestellungOhneArtikelLieferant">v1.99.30 F3 (Antwort 4): L stammt von einem Lieferanten ohne Artikel-Lieferant-Zeile;
/// <paramref name="LetzteBestellungLieferant"/> ist dann „{Kto} {Name}“.</param>
/// <param name="KeineBestellungImRueckblick">v1.99.30 F3: letzte Bestellungen sind gelesen und es gibt im Rueckblick bei keinem Lieferanten eine
/// (unterscheidet „noch nicht gelesen“ von „keine Bestellung“ fuer den Hinweis ohne Sage-Bestellmenge).</param>
/// <param name="VerfuegbarFenster">v1.99.30 F5: verfuegbar bis heute + WBZ (<see cref="PurchaseProposalCalculator.VerfuegbarBis"/>); entscheidet
/// „kritisch“. null = wie bisher <c>verfuegbar &lt; 0</c>.</param>
public sealed record PurchaseProposalInput(decimal BestandWms, decimal OffenBestellt, decimal OffeneBedarfe, decimal Meldebestand,
    decimal? LetzteBestellmenge = null, string? LetzteBestellungLieferant = null,
    decimal? Mindestbestellmenge = null, string? Einkaufsmengeneinheit = null, decimal? UmrechnungsfaktorEK = null, string? ArtikelEinheit = null,
    decimal? FallbackSageBestellmenge = null, bool LetzteBestellungOhneArtikelLieferant = false, bool KeineBestellungImRueckblick = false,
    decimal? VerfuegbarFenster = null);

/// <param name="Fallback">E3: Menge stammt aus der Sage-Bestellmenge, weil es im Rueckblick keine Bestellung gibt (Banner zaehlt diese Zeilen).</param>
public sealed record PurchaseProposalResult(decimal Verfuegbar, decimal Fehlmenge, PurchaseProposalDringlichkeit Dringlichkeit,
    decimal? Vorschlag, string Grund, IReadOnlyList<string> Hinweise, bool Fallback = false)
{
    public bool IstVorschlag => Dringlichkeit != PurchaseProposalDringlichkeit.KeinBedarf;
}

/// <summary>
/// v1.99.20 (Spec 2026-11-08-einkauf-bestellvorschlaege F2/F3/F5): die EINE Rechenregel der Bestellvorschlaege – rein, ohne DB.
/// verfuegbar = Bestand WMS + offen bestellt − offene Bedarfe; Vorschlag, wenn verfuegbar ≤ Meldebestand; kritisch bei verfuegbar &lt; 0.
/// Menge: letzte Bestellmenge L, bei Fehlmenge F ≥ L (⌊F/L⌋+1)×L (Rueckfrage 11); ohne L die Sage-Bestellmenge als Fallback (Freigabe-Nachtrag 2,
/// gleiche Erhoehungsregel), sonst F, bei F = 0 der Meldebestand (Rueckfrage 13 b);
/// danach Aufrunden auf die Mindestbestellmenge (E4) nur mit Einheiten-Waechter <see cref="MindestmengeVergleichbar"/>.
/// </summary>
public static class PurchaseProposalCalculator
{
    public const string GrundLetzteBestellmenge = "letzte Bestellmenge";
    public const string GrundOhneLetzteBestellung = "keine letzte Bestellung – Menge prüfen";
    public const string GrundMindestbestellmenge = "auf Mindestbestellmenge aufgerundet";
    public const string GrundSageBestellmenge = "Sage-Bestellmenge";
    /// <summary>F3 (v1.99.30): Hinweis nach Ursache statt pauschal. Gemeinsamer Anfang „Keine Bestellung in 3 Jahren“ (filterbar in der Hinweis-Spalte).</summary>
    public static readonly string HinweisKeineBestellung = $"Keine Bestellung in {Models.ArticleSupplier.LetzteBestellungRueckblickJahre} Jahren";
    /// <summary>Hinweistext je Fallback-Zeile (Sage-Bestellmenge als Menge).</summary>
    public static readonly string HinweisFallback = HinweisKeineBestellung + " – Sage-Bestellmenge";
    /// <summary>Keine Bestellung im Rueckblick und keine Sage-Bestellmenge: Menge aus Fehlmenge bzw. Meldebestand.</summary>
    public static readonly string HinweisOhneSageBestellmenge = HinweisKeineBestellung + " – Meldebestand";
    public const string HinweisOhneArtikelLieferant = "kein Artikel-Lieferant in Sage";

    /// <summary>E4-Waechter (E0-Abweichung A2, konservativ): Mindestbestellmenge nur vergleichbar, wenn die Einkaufsmengeneinheit leer oder gleich
    /// der Artikel-Einheit ist UND der Umrechnungsfaktor EK genau 1 betraegt (gleiche Einheit mit Faktor ≠ 1 kommt in Sage vor, z. B. Stk/Stk ×5).
    /// TODO(P2): Einheit der Mindestbestellmenge laut Sage-Lieferantenmaske offen (Spec 2026-11-08-einkauf-bestellvorschlaege P2, A2) – bis dahin
    /// keine Umrechnung, nur Hinweis.</summary>
    public static bool MindestmengeVergleichbar(PurchaseProposalInput e) =>
        (string.IsNullOrWhiteSpace(e.Einkaufsmengeneinheit)
         || string.Equals(e.Einkaufsmengeneinheit.Trim(), e.ArtikelEinheit?.Trim(), StringComparison.OrdinalIgnoreCase))
        && e.UmrechnungsfaktorEK == 1m;

    public static PurchaseProposalResult Berechne(PurchaseProposalInput e)
    {
        var verfuegbar = e.BestandWms + e.OffenBestellt - e.OffeneBedarfe;
        var hinweise = new List<string>();
        if (verfuegbar > e.Meldebestand)
            return new(verfuegbar, 0m, PurchaseProposalDringlichkeit.KeinBedarf, null, "", hinweise);

        var f = e.Meldebestand - verfuegbar;
        // F5 (Antwort 6): kritisch, wenn der Bestand nicht bis zum fruehesten Liefertermin einer neuen Bestellung (heute + WBZ) reicht.
        // ponytail: liegt verfuegbar (alle) ueber dem Meldebestand, gibt es keinen Vorschlag – auch wenn das Fenster negativ ist (dann ist schon
        // genug bestellt, nur spaet); kein eigener Status dafuer.
        var dringlichkeit = (e.VerfuegbarFenster ?? verfuegbar) < 0 ? PurchaseProposalDringlichkeit.Kritisch : PurchaseProposalDringlichkeit.Nachbestellen;
        decimal vorschlag;
        string grund;
        var fallback = false;
        if ((e.LetzteBestellmenge is > 0m ? e.LetzteBestellmenge : e.FallbackSageBestellmenge) is > 0m and var l)
        {
            fallback = e.LetzteBestellmenge is not > 0m;
            var name = fallback ? GrundSageBestellmenge : GrundLetzteBestellmenge;
            if (f >= l)
            {
                var n = Math.Floor(f / l) + 1;
                vorschlag = n * l;
                grund = $"{n:0} × {name}";
            }
            else
            {
                vorschlag = l;
                grund = name;
            }
            if (fallback) hinweise.Add(HinweisFallback);
            else if (e.LetzteBestellungOhneArtikelLieferant) hinweise.Add($"Letzte Bestellung bei {e.LetzteBestellungLieferant} – {HinweisOhneArtikelLieferant}");
            else if (e.LetzteBestellungLieferant != null) hinweise.Add($"letzte Bestellung bei {e.LetzteBestellungLieferant}");
        }
        else
        {
            vorschlag = f > 0 ? f : e.Meldebestand;
            grund = GrundOhneLetzteBestellung;
            if (e.KeineBestellungImRueckblick) hinweise.Add(HinweisOhneSageBestellmenge);
        }
        if (e.Mindestbestellmenge is > 0m and var m)
        {
            if (!MindestmengeVergleichbar(e))
                hinweise.Add($"Mindestbestellmenge {(m.ToString("0.###") + " " + e.Einkaufsmengeneinheit?.Trim()).Trim()} (Faktor {e.UmrechnungsfaktorEK?.ToString("0.###") ?? "–"}) nicht vergleichbar – nicht aufgerundet");
            else if (vorschlag < m)
            {
                vorschlag = m;
                grund += ", " + GrundMindestbestellmenge;
            }
        }
        return new(verfuegbar, f, dringlichkeit, vorschlag, grund, hinweise, fallback);
    }

    public const string HinweisOhneLiefertermin = "offene Bestellung ohne Liefertermin – für Dringlichkeit nicht gezählt";
    public const string HinweisOhneWbz = "ohne WBZ – Dringlichkeit nur mit heute fälligen Bedarfen";

    /// <summary>v1.99.30 F5 (Antwort 6): verfuegbar im Fenster bis <c>heute + WBZ</c> (ohne WBZ 0 Tage) = Bestand + Rest der offenen Bestellungen mit
    /// Liefertermin ≤ Grenze (ueberfaellige zaehlen, ohne Termin nicht) − angerechnete Bedarfe mit Termin ≤ Grenze.</summary>
    public static decimal VerfuegbarBis(decimal bestandWms, IEnumerable<(decimal Menge, DateTime? Liefertermin)> bestellungen, ArtikelBedarf bedarf,
        DateTime heute, int? wbz)
    {
        var grenze = heute.Date.AddDays(wbz ?? 0);
        return bestandWms + bestellungen.Where(b => b.Liefertermin is { } t && t.Date <= grenze).Sum(b => b.Menge) - bedarf.AngerechnetBis(grenze);
    }

    public static string Text(PurchaseProposalDringlichkeit d) => d switch
    {
        PurchaseProposalDringlichkeit.Kritisch => "kritisch",
        PurchaseProposalDringlichkeit.Nachbestellen => "nachbestellen",
        _ => "kein Bedarf",
    };
}
