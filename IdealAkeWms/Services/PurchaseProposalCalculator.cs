namespace IdealAkeWms.Services;

/// <summary>v1.99.20 Dringlichkeit eines Bestellvorschlags (Text-Badge, Farbe nur zusaetzlich).</summary>
public enum PurchaseProposalDringlichkeit { KeinBedarf, Nachbestellen, Kritisch }

/// <summary>Eingabe je Artikel, alles in der Lagermengeneinheit (<c>Article.Unit</c>).</summary>
/// <param name="OffenBestellt">Summe der Reste offener Sage-Bestellungen (E2); 0 = keine bzw. nicht gelesen.</param>
/// <param name="LetzteBestellmenge">Letzte Bestellmenge L (E3, bis dahin immer null).</param>
/// <param name="LetzteBestellungLieferant">null = L stammt vom gewaehlten Lieferanten; sonst Name des anderen Lieferanten (Rueckfrage 13 a).</param>
public sealed record PurchaseProposalInput(decimal BestandWms, decimal OffenBestellt, decimal OffeneBedarfe, decimal Meldebestand,
    decimal? LetzteBestellmenge = null, string? LetzteBestellungLieferant = null);

public sealed record PurchaseProposalResult(decimal Verfuegbar, decimal Fehlmenge, PurchaseProposalDringlichkeit Dringlichkeit,
    decimal? Vorschlag, string Grund, IReadOnlyList<string> Hinweise)
{
    public bool IstVorschlag => Dringlichkeit != PurchaseProposalDringlichkeit.KeinBedarf;
}

/// <summary>
/// v1.99.20 (Spec 2026-11-08-einkauf-bestellvorschlaege F2/F3/F5): die EINE Rechenregel der Bestellvorschlaege – rein, ohne DB.
/// verfuegbar = Bestand WMS + offen bestellt − offene Bedarfe; Vorschlag, wenn verfuegbar ≤ Meldebestand; kritisch bei verfuegbar &lt; 0.
/// Menge: letzte Bestellmenge L, bei Fehlmenge F ≥ L (⌊F/L⌋+1)×L (Rueckfrage 11); ohne L: F, bei F = 0 der Meldebestand (Rueckfrage 13 b).
/// </summary>
public static class PurchaseProposalCalculator
{
    public const string GrundLetzteBestellmenge = "letzte Bestellmenge";
    public const string GrundOhneLetzteBestellung = "keine letzte Bestellung – Menge prüfen";

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
        return new(verfuegbar, f, dringlichkeit, vorschlag, grund, hinweise);
    }

    public static string Text(PurchaseProposalDringlichkeit d) => d switch
    {
        PurchaseProposalDringlichkeit.Kritisch => "kritisch",
        PurchaseProposalDringlichkeit.Nachbestellen => "nachbestellen",
        _ => "kein Bedarf",
    };
}
