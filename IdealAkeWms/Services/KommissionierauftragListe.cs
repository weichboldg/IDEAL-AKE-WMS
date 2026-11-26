using System.Globalization;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Services;

/// <summary>
/// v1.99.89 (Spec 2026-11-26 Teil 3, K4/K4a–c): Maske „Liste“ des Kommissionierauftrags – Auftragszeilen je Ressourcenummer
/// (getrimmt, gross) + Komm.-Ziel (getrimmt) + BG-Kennzeichen summiert. DB-frei; Seite und summierter Druck nutzen dieselbe Funktion.
/// </summary>
public static class KommissionierauftragListe
{
    private static readonly NaturalPositionComparer Natuerlich = new();

    /// <summary>
    /// <paramref name="items"/> = Stuecklisten-Zeilen der Auftragsseite (mit <c>PickingItemId</c>), <paramref name="zeilen"/> = Zeilen des
    /// Auftrags, <paramref name="ohneStueckliste"/> = Auftragszeilen ohne Stuecklistenposition (K4c, siehe <see cref="OhneStuecklistenposition"/>).
    /// <paramref name="auftragOffen"/> steuert den Rest-Hinweis (K4b, nur Offen/InArbeit).
    /// </summary>
    public static List<AuftragSummenzeile> Summiere(IEnumerable<BomItemViewModel> items, IEnumerable<PickingOrderLine> zeilen,
        IEnumerable<PickingOrderLine> ohneStueckliste, string? abteilung, bool auftragOffen)
    {
        var zeileById = zeilen.GroupBy(z => z.Id).ToDictionary(g => g.Key, g => g.First());
        // H-2: zwei Stuecklisten-Items mit gleichem Schluessel zeigen auf dieselbe Auftragszeile – nur einmal zaehlen.
        var mitZeile = items.Where(i => i.PickingItemId is { } id && zeileById.ContainsKey(id))
            .GroupBy(i => i.PickingItemId!.Value).Select(g => (Item: g.First(), Zeile: zeileById[g.Key]));

        var ergebnis = mitZeile
            .GroupBy(x => (Art: Norm(x.Item.Ressourcenummer), Ziel: x.Item.Kommissionieren?.Trim() ?? "", x.Item.IsBaugruppe))
            .Select(g =>
            {
                var erstes = g.OrderBy(x => x.Item.Position, Natuerlich).First().Item;
                var s = Basis(g.Key.Art, g.Key.Ziel, g.Key.IsBaugruppe, g.Select(x => (x.Zeile, x.Item.Position)), abteilung, auftragOffen);
                s.Matchcode = erstes.Matchcode;
                s.Bezeichnung1 = erstes.Bezeichnung1;
                s.Bezeichnung2 = erstes.Bezeichnung2;
                s.Beschaffungsartikel = erstes.Beschaffungsartikel;
                s.Artikelgruppe = erstes.Artikelgruppe;
                s.KategorieName = erstes.KategorieName;
                s.Hauptlagerplatz = erstes.Hauptlagerplatz;
                s.StockLocations = erstes.StockLocations;
                s.QuellVorschlagId = erstes.SourceStorageLocationId;
                s.Baugruppe = VormontageService.FormatTexte(g.Select(x => x.Item.Baugruppe));
                s.SagePosition = VormontageService.FormatTexte(g.Select(x => x.Item.SagePosition));
                s.Ebene = VormontageService.FormatTexte(g.Select(x => x.Item.Ebene.ToString(CultureInfo.InvariantCulture)));
                s.VaterSubFa = VormontageService.FormatTexte(g.Select(x => x.Item.VaterSubFa));
                s.Arbeitsbereich = VormontageService.FormatTexte(g.Select(x => x.Item.Arbeitsbereich));
                return s;
            })
            .ToList();

        // K4c: Zeilen ohne Stuecklistenposition gehen nicht still verloren – eigene Summenzeilen (leeres Ziel, ohne Artikeleigenschaften).
        ergebnis.AddRange(ohneStueckliste
            .GroupBy(z => (Art: Norm(z.ArticleNumber), z.IsBaugruppe))
            .Select(g =>
            {
                var s = Basis(g.Key.Art, "", g.Key.IsBaugruppe, g.Select(z => (z, z.BomPosition)), abteilung, auftragOffen);
                s.OhneStuecklistenposition = true;
                return s;
            }));

        // K4: Komm.-Ziel → Hauptlagerplatz → erste Position (natuerlich) → Artikel; leere Werte jeweils zuletzt (H-1).
        return ergebnis
            .OrderBy(s => string.IsNullOrEmpty(s.Kommissionieren)).ThenBy(s => s.Kommissionieren, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => string.IsNullOrEmpty(s.Hauptlagerplatz)).ThenBy(s => s.Hauptlagerplatz, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.Positionen.Count == 0).ThenBy(s => s.Positionen.FirstOrDefault(), Natuerlich)
            .ThenBy(s => s.Ressourcenummer, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>K4c: Auftragszeilen, die keine der (ungefilterten) Stuecklisten-Zeilen der Seite traegt.</summary>
    public static List<PickingOrderLine> OhneStuecklistenposition(IEnumerable<PickingOrderLine> zeilen, IEnumerable<BomItemViewModel> alleItems)
    {
        var getragen = alleItems.Where(i => i.PickingItemId.HasValue).Select(i => i.PickingItemId!.Value).ToHashSet();
        return zeilen.Where(z => !getragen.Contains(z.Id)).ToList();
    }

    /// <summary>K11: Summenzeilen in der Reihenfolge der Positionsliste (erste genannte Position zaehlt), fehlende am Ende in ihrer
    /// bisherigen Reihenfolge (Muster <c>PickingController.SortiereZeilen</c>).</summary>
    public static List<AuftragSummenzeile> NachReihenfolge(IEnumerable<AuftragSummenzeile> zeilen, string? reihenfolge)
    {
        if (string.IsNullOrEmpty(reihenfolge)) return zeilen.ToList();
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var p in reihenfolge.Split(',', StringSplitOptions.RemoveEmptyEntries))
            index.TryAdd(p.Trim(), index.Count);
        return zeilen.OrderBy(s => s.Positionen.Select(p => index.TryGetValue(p, out var i) ? i : int.MaxValue).DefaultIfEmpty(int.MaxValue).Min())
            .ToList();
    }

    private static string Norm(string? artikel) => (artikel ?? "").Trim().ToUpperInvariant();

    private static AuftragSummenzeile Basis(string artikel, string ziel, bool bg, IEnumerable<(PickingOrderLine Zeile, string? Position)> teile,
        string? abteilung, bool auftragOffen)
    {
        var liste = teile.OrderBy(t => string.IsNullOrEmpty(t.Position)).ThenBy(t => t.Position, Natuerlich).ToList();
        var zeilen = liste.Select(t => t.Zeile).ToList();
        var offen = zeilen.Where(Offen).ToList();
        var umgebucht = zeilen.Where(z => z.Status == PickingOrderLineStatus.Umgebucht).ToList();
        var status = zeilen.Select(z => z.Status).Distinct().ToList();
        return new AuftragSummenzeile
        {
            Ressourcenummer = artikel,
            Kommissionieren = ziel.Length == 0 ? null : ziel,
            IsBaugruppe = bg,
            Zeilen = zeilen,
            Positionen = liste.Select(t => t.Position).Where(p => !string.IsNullOrEmpty(p)).Select(p => p!).ToList(),
            SollSumme = zeilen.Sum(z => z.SollMenge),
            OffenSoll = offen.Sum(z => z.SollMenge),
            GebuchtIst = umgebucht.Count == 0 ? null : umgebucht.Sum(z => z.IstMenge ?? 0m),
            Erledigt = offen.Count == 0,
            StatusText = status.Count == 1
                ? PickingOrderListItem.ZeilenStatusText(status[0])
                : umgebucht.Count > 0
                    ? $"teilweise umgebucht ({umgebucht.Count} von {zeilen.Count} Pos.)"
                    : VormontageService.FormatTexte(status.Select(PickingOrderListItem.ZeilenStatusText)) ?? "",
            // K4b: Rest einer teilgebuchten Position ist hier nicht nachbuchbar, er entsteht beim Teilabschluss im Folgeauftrag.
            RestHinweis = auftragOffen && umgebucht.Any(z => (z.IstMenge ?? 0m) < z.SollMenge)
                ? string.Join(", ", umgebucht.Where(z => (z.IstMenge ?? 0m) < z.SollMenge)
                    .Select(z => $"Rest {(z.SollMenge - (z.IstMenge ?? 0m)).ToString("0.###", CultureInfo.GetCultureInfo("de-DE"))} aus Pos. {z.BomPosition}"))
                  + " geht beim Abschluss in den Folgeauftrag"
                : null,
            Abteilung = abteilung,
        };
    }

    private static bool Offen(PickingOrderLine z) => z.Status is PickingOrderLineStatus.Offen or PickingOrderLineStatus.Gepickt;
}
