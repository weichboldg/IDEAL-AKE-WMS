using System.Globalization;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

/// <summary>v1.99.85 (Spec 2026-11-26-liefertermin-offene-bestellungen F2), seit v1.99.86 gemeinsame Stelle fuer Lager-Details,
/// Druck-Filter und den Fehlteil-Hinweis der Lagerbestellung (Spec 2026-11-26-lagerbestellung-hinweis-fehlteil-bereits-bestellt).</summary>
public static class LieferterminTexte
{
    /// <summary>Zelltext „Liefertermin“ je Artikelnummer (getrimmt, Gross/Klein egal), feste Abfragezahl. DUMMY ausgenommen.
    /// Cache leer = (null, null).</summary>
    internal static async Task<(DateTime? Stand, Dictionary<string, string>? Texte)> JeArtikelAsync(
        ISagePurchaseOrderLineRepository poLines, IPurchaseOrderReceiptRepository receipts, IEnumerable<string> artikelnummern)
    {
        var stand = await poLines.GetStandAsync();
        if (stand == null) return (null, null);
        var nummern = artikelnummern.Select(n => n.Trim())
            .Where(n => !string.Equals(n, Article.DummyArticleNumber, StringComparison.OrdinalIgnoreCase)).ToList();
        var lines = await poLines.GetByArtikelnummernAsync(nummern);
        var erfasst = await receipts.GetErfasstJeBelPosAsync(lines.Select(l => l.BelPosId).ToList());
        var heute = DateTime.Today;
        return (stand, lines.GroupBy(l => l.Artikelnummer.Trim(), StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key,
            g => Zelle(g.Select(l => (l.Liefertermin, erfasst.GetValueOrDefault(l.BelPosId, ErfasstJeBelPos.Leer).Rest(l))), heute),
            StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>v1.99.85 F2.3: Zelltext aus den offenen Positionen eines Artikels. Nur Rest &gt; 0 zaehlt; frueheste Termin
    /// (ueberfaellige zaehlen mit, Zusatz „ überfällig“), nur ohne Termin → „ohne Termin“, weitere Positionen → „ (+N)“, keine → „—“.</summary>
    internal static string Zelle(IEnumerable<(DateTime? Liefertermin, decimal Offen)> positionen, DateTime heute)
    {
        var offen = positionen.Where(p => p.Offen > 0).ToList();
        if (offen.Count == 0) return "—";
        var text = offen.Min(p => p.Liefertermin) is { } t
            ? t.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + (t.Date < heute.Date ? " überfällig" : "")
            : "ohne Termin";
        return offen.Count > 1 ? $"{text} (+{offen.Count - 1})" : text;
    }
}
