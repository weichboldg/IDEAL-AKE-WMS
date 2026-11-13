using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

/// <summary>
/// v1.99.20 (Spec 2026-11-08-einkauf-bestellvorschlaege E1): „Einkauf → Bestellvorschläge“, nur lesend. Rolle <c>einkauf</c> (oder admin) und
/// Gate <c>EinkaufAktiv</c> kumulativ (ADR 0006/0011). Gerechnet wird ausschliesslich im <see cref="PurchaseProposalCalculator"/>;
/// Spaltenfilter nach der Berechnung in C# (ADR 0005). Die Uebergabe an den Sage-Dispo-Stapel (E5) ist blockiert – keine POST-Action.
/// </summary>
[RequireEinkaufAccess]
[RequireEinkaufAktiv]
public class PurchaseProposalsController : Controller
{

    private readonly IPurchaseProposalRepository _repo;
    private readonly IStockMovementRepository _stock;
    private readonly IAppSettingRepository _settings;
    private readonly ICurrentUserService _user;

    public PurchaseProposalsController(IPurchaseProposalRepository repo, IStockMovementRepository stock, IAppSettingRepository settings,
        ICurrentUserService user)
    {
        _repo = repo;
        _stock = stock;
        _settings = settings;
        _user = user;
    }

    private static string Zahl(decimal? d) => d?.ToString("0.###") ?? "–";

    // Spaltenfilter (Server-Mode) – Zelltexte wie in der View.
    private static readonly Dictionary<string, Func<PurchaseProposalRow, string?>> ColumnMap = new()
    {
        ["lieferant"] = r => r.LieferantText,
        ["artikel"] = r => r.Article.ArticleNumber,
        ["bezeichnung"] = r => r.Article.Description,
        ["bestand"] = r => Zahl(r.BestandWms),
        ["sage-verfuegbar"] = r => Zahl(r.Sage?.LagerbestandVerfuegbar),
        ["bedarfe"] = r => Zahl(r.OffeneBedarfe),
        ["verfuegbar"] = r => Zahl(r.Ergebnis.Verfuegbar),
        ["meldebestand"] = r => Zahl(r.Article.ReorderLevel),
        ["vorschlag"] = r => r.Ergebnis.Vorschlag?.ToString("0.###"),
        ["grund"] = r => r.Ergebnis.Grund,
        ["einheit"] = r => r.Article.Unit,
        ["sage-bestellmenge"] = r => Zahl(r.Sage?.Bestellmenge),
        ["bestellnummer"] = r => r.Lieferant?.SageBestellnummer,
        ["wbz"] = r => r.Wbz?.ToString(),
        ["wunschtermin"] = r => r.Wunschtermin?.ToString("dd.MM.yyyy"),
        ["letzter-lieferant"] = r => r.Sage?.LetzterLieferant,
        ["abc"] = r => r.Sage?.ABCKlasse,
        ["user-mindestbestand"] = r => r.Sage?.UserMindestbestand?.ToString("0.###"),
        ["dringlichkeit"] = r => PurchaseProposalCalculator.Text(r.Ergebnis.Dringlichkeit),
        ["hinweis"] = r => r.Hinweis,
    };

    public async Task<IActionResult> Index(string? lieferant = null, string? artikelgruppe = null, string? umfang = null,
        string? dringlichkeit = null, int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var umfangWert = umfang == PurchaseProposalListViewModel.UmfangAlle ? PurchaseProposalListViewModel.UmfangAlle : PurchaseProposalListViewModel.UmfangVorschlaege;

        var artikel = await _repo.GetNachbestellartikelAsync();
        var lieferanten = await _repo.GetHauptlieferantenAsync();
        var bestand = artikel.Count == 0
            ? new Dictionary<string, List<StockLocationInfo>>()
            : await _stock.GetStockByArticleNumbersAsync(artikel.Select(a => a.ArticleNumber).Distinct().ToList());

        // Einstellung EinkaufOffeneBedarfe: Zeile fehlt = beide, leer = keine (ADR 0020, Definition in AppSettingDefinitions).
        var bedarfeEinstellung = await _settings.GetValueAsync(AppSettingKeys.EinkaufOffeneBedarfe)
            ?? $"{AppSettingKeys.EinkaufBedarfWerte.Lagerbestellungen},{AppSettingKeys.EinkaufBedarfWerte.Bedarfsmeldungen}";
        var mitLager = InListe(bedarfeEinstellung, AppSettingKeys.EinkaufBedarfWerte.Lagerbestellungen);
        var mitBedarfsmeldungen = InListe(bedarfeEinstellung, AppSettingKeys.EinkaufBedarfWerte.Bedarfsmeldungen);
        var bedarfe = new List<OffenerBedarf>();
        if (mitLager) bedarfe.AddRange(await _repo.GetOffeneLagerbestellungenAsync());
        if (mitBedarfsmeldungen) bedarfe.AddRange(await _repo.GetOffeneBedarfsmeldungenAsync());

        var (alle, bedarfeEinheitAbweichend) = BaueZeilen(artikel, lieferanten, bestand, bedarfe, DateTime.Today);

        IEnumerable<PurchaseProposalRow> q = alle;
        if (umfangWert == PurchaseProposalListViewModel.UmfangVorschlaege) q = q.Where(r => r.Ergebnis.IstVorschlag);
        if (dringlichkeit == "kritisch") q = q.Where(r => r.Ergebnis.Dringlichkeit == PurchaseProposalDringlichkeit.Kritisch);
        else if (dringlichkeit == "nachbestellen") q = q.Where(r => r.Ergebnis.IstVorschlag);
        if (!string.IsNullOrWhiteSpace(lieferant)) q = q.Where(r => string.Equals(r.LieferantKto, lieferant.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(artikelgruppe)) q = q.Where(r => string.Equals(r.Article.ArticleGroup?.Trim(), artikelgruppe.Trim(), StringComparison.OrdinalIgnoreCase));

        // Standardsortierung Lieferant, dann Artikelnummer (ohne Lieferant zuletzt).
        var sortiert = q.OrderBy(r => r.LieferantText == null).ThenBy(r => r.LieferantText, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(r => r.Article.ArticleNumber, StringComparer.Ordinal).ToList();
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var gefiltert = ColumnFilterHelper.Apply(sortiert, columnFilters, ColumnMap, ColumnFilterTypes.For("PurchaseProposals/Index")).ToList();

        var userDefault = await _user.GetDefaultPageSizeAsync();
        var effectivePageSize = PageSize.Resolve(pageSize, userDefault);
        return View(new PurchaseProposalListViewModel
        {
            Items = gefiltert.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList(),
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = PageSize.ResolveRaw(pageSize, userDefault),
                TotalCount = gefiltert.Count,
            },
            Lieferant = lieferant, Artikelgruppe = artikelgruppe, Umfang = umfangWert, Dringlichkeit = dringlichkeit,
            Lieferanten = alle.Where(r => r.LieferantKto != null).GroupBy(r => r.LieferantKto!, StringComparer.OrdinalIgnoreCase)
                .Select(g => (g.Key, g.First().LieferantText ?? g.Key)).OrderBy(l => l.Item2, StringComparer.CurrentCultureIgnoreCase).ToList(),
            Artikelgruppen = alle.Select(r => r.Article.ArticleGroup?.Trim()).Where(g => !string.IsNullOrEmpty(g)).Select(g => g!)
                .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.CurrentCultureIgnoreCase).ToList(),
            ArtikelGesamt = alle.Count,
            OhneSageDetail = alle.Count(r => r.Sage == null),
            OhneArtikelLieferant = alle.Count(r => r.Lieferant == null),
            BedarfeEinheitAbweichend = bedarfeEinheitAbweichend,
            BedarfeText = (mitLager, mitBedarfsmeldungen) switch
            {
                (true, true) => "Lagerbestellungen und Bedarfsmeldungen",
                (true, false) => "nur Lagerbestellungen",
                (false, true) => "nur Bedarfsmeldungen",
                _ => "keine (Einstellung leer)",
            },
        });
    }

    private static bool InListe(string liste, string wert) =>
        liste.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Contains(wert, StringComparer.OrdinalIgnoreCase);

    private static string Schluessel(string artikelnummer) => artikelnummer.Trim().ToUpperInvariant();

    private static bool EinheitWeichtAb(string? a, string? b) =>
        !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b) && !string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>Datensammlung → Zeilen: Bestand je Artikel (Summe der Plaetze, ohne Bewegung 0, Regel der Meldebestand-Mail), offene Bedarfe
    /// (Artikelnummer getrimmt, Gross/Klein egal; abweichende Einheit nicht gerechnet, gezaehlt), Lieferant, WBZ, Rechner.</summary>
    internal static (List<PurchaseProposalRow> Zeilen, int BedarfeEinheitAbweichend) BaueZeilen(List<Article> artikel,
        IReadOnlyDictionary<int, ArticleSupplierRow> lieferanten, IReadOnlyDictionary<string, List<StockLocationInfo>> bestand,
        IReadOnlyList<OffenerBedarf> bedarfe, DateTime heute)
    {
        var artikelJeNummer = artikel.GroupBy(a => Schluessel(a.ArticleNumber)).ToDictionary(g => g.Key, g => g.First());
        var bedarfJeNummer = new Dictionary<string, decimal>();
        var abweichend = 0;
        foreach (var b in bedarfe)
        {
            var key = Schluessel(b.ArticleNumber);
            if (!artikelJeNummer.TryGetValue(key, out var a)) continue;   // kein Nachbestellartikel
            if (EinheitWeichtAb(b.Unit, a.Unit)) { abweichend++; continue; }
            bedarfJeNummer[key] = bedarfJeNummer.GetValueOrDefault(key) + b.Menge;
        }

        var zeilen = artikel.Select(a =>
        {
            var hinweise = new List<string>();
            lieferanten.TryGetValue(a.Id, out var l);
            string? kto = l?.SageKto;
            string? text = l == null ? null : string.IsNullOrWhiteSpace(l.SageKto) ? l.Name : $"{l.Name} ({l.SageKto})";
            if (l == null)
            {
                kto = string.IsNullOrWhiteSpace(a.SageDetail?.Hauptlieferant) ? null : a.SageDetail!.Hauptlieferant!.Trim();
                text = kto;
                hinweise.Add(kto != null ? "Kreditor fehlt im WMS" : "kein Lieferant");
            }
            if (a.SageDetail == null) hinweise.Add("Sage-Zusatzfelder fehlen");
            var wbz = l?.Wiederbeschaffungszeit ?? a.SageDetail?.Wiederbeschaffungszeit;
            var bestandWms = bestand.TryGetValue(a.ArticleNumber, out var plaetze) ? plaetze.Sum(p => p.Quantity) : 0m;
            var offeneBedarfe = bedarfJeNummer.GetValueOrDefault(Schluessel(a.ArticleNumber));
            return new PurchaseProposalRow
            {
                Article = a, Lieferant = l, LieferantText = text, LieferantKto = kto, BestandWms = bestandWms, OffeneBedarfe = offeneBedarfe,
                Ergebnis = PurchaseProposalCalculator.Berechne(new PurchaseProposalInput(bestandWms, 0m, offeneBedarfe, a.ReorderLevel ?? 0m)),
                Wbz = wbz, Wunschtermin = wbz is { } t ? heute.AddDays(t) : null, Hinweise = hinweise,
            };
        }).ToList();
        return (zeilen, abweichend);
    }
}
