namespace IdealAkeWms.Services;

/// <summary>v1.99.20 Dringlichkeit eines Bestellvorschlags (Text-Badge, Farbe nur zusaetzlich).</summary>
public enum PurchaseProposalDringlichkeit { KeinBedarf, Nachbestellen, Kritisch }

/// <summary>Eingabe je Artikel, alles in der Lagermengeneinheit (<c>Article.Unit</c>).</summary>
/// <param name="OffenBestellt">Summe der Reste offener Sage-Bestellungen (E2); 0 = keine bzw. nicht gelesen.</param>
/// <param name="LetzteBestellmenge">Letzte Bestellmenge L (E3, bis dahin immer null).</param>
/// <param name="LetzteBestellungLieferant">null = L stammt vom gewaehlten Lieferanten; sonst Name des anderen Lieferanten (Rueckfrage 13 a).</param>
/// <param name="Mindestbestellmenge">E4: <c>ArticleSupplier.Mindestbestellmenge</c> des gewaehlten Lieferanten.</param>
/// <param name="Einkaufsmengeneinheit">E4: <c>ArticleSupplier.Einkaufsmengeneinheit</c>.</param>
/// <param name="UmrechnungsfaktorEK">E4: <c>ArticleSupplier.UmrechnungsfaktorEK</c>.</param>
/// <param name="ArtikelEinheit">E4: <c>Article.Unit</c> (Lagermengeneinheit).</param>
public sealed record PurchaseProposalInput(decimal BestandWms, decimal OffenBestellt, decimal OffeneBedarfe, decimal Meldebestand,
    decimal? LetzteBestellmenge = null, string? LetzteBestellungLieferant = null,
    decimal? Mindestbestellmenge = null, string? Einkaufsmengeneinheit = null, decimal? UmrechnungsfaktorEK = null, string? ArtikelEinheit = null);

public sealed record PurchaseProposalResult(decimal Verfuegbar, decimal Fehlmenge, PurchaseProposalDringlichkeit Dringlichkeit,
    decimal? Vorschlag, string Grund, IReadOnlyList<string> Hinweise)
{
    public bool IstVorschlag => Dringlichkeit != PurchaseProposalDringlichkeit.KeinBedarf;
}

/// <summary>
/// v1.99.20 (Spec 2026-11-08-einkauf-bestellvorschlaege F2/F3/F5): die EINE Rechenregel der Bestellvorschlaege – rein, ohne DB.
/// verfuegbar = Bestand WMS + offen bestellt − offene Bedarfe; Vorschlag, wenn verfuegbar ≤ Meldebestand; kritisch bei verfuegbar &lt; 0.
/// Menge: letzte Bestellmenge L, bei Fehlmenge F ≥ L (⌊F/L⌋+1)×L (Rueckfrage 11); ohne L: F, bei F = 0 der Meldebestand (Rueckfrage 13 b);
/// danach Aufrunden auf die Mindestbestellmenge (E4) nur mit Einheiten-Waechter <see cref="MindestmengeVergleichbar"/>.
/// </summary>
public static class PurchaseProposalCalculator
{
    public const string GrundLetzteBestellmenge = "letzte Bestellmenge";
    public const string GrundOhneLetzteBestellung = "keine letzte Bestellung – Menge prüfen";
    public const string GrundMindestbestellmenge = "auf Mindestbestellmenge aufgerundet";

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
        var dringlichkeit = verfuegbar < 0 ? PurchaseProposalDringlichkeit.Kritisch : PurchaseProposalDringlichkeit.Nachbestellen;
        decimal vorschlag;
        string grund;
        if (e.LetzteBestellmenge is > 0m and var l)
        {
            if (f >= l)
            {
                var n = Math.Floor(f / l) + 1;
                vorschlag = n * l;
                grund = $"{n:0} × {GrundLetzteBestellmenge}";
            }
            else
            {
                vorschlag = l;
                grund = GrundLetzteBestellmenge;
            }
            if (e.LetzteBestellungLieferant != null) hinweise.Add($"letzte Bestellung bei {e.LetzteBestellungLieferant}");
        }
        else
        {
            vorschlag = f > 0 ? f : e.Meldebestand;
            grund = GrundOhneLetzteBestellung;
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
        return new(verfuegbar, f, dringlichkeit, vorschlag, grund, hinweise);
    }

    public static string Text(PurchaseProposalDringlichkeit d) => d switch
    {
        PurchaseProposalDringlichkeit.Kritisch => "kritisch",
        PurchaseProposalDringlichkeit.Nachbestellen => "nachbestellen",
        _ => "kein Bedarf",
    };
}
