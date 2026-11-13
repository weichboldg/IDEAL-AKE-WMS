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
    private readonly ISagePurchaseOrderLineRepository _lines;
    private readonly IPurchaseOrderReceiptRepository _receipts;

    public PurchaseProposalsController(IPurchaseProposalRepository repo, IStockMovementRepository stock, IAppSettingRepository settings,
        ICurrentUserService user, ISagePurchaseOrderLineRepository lines, IPurchaseOrderReceiptRepository receipts)
    {
        _lines = lines;
        _receipts = receipts;
        _repo = repo;
        _stock = stock;
        _settings = settings;
        _user = user;
    }

    private static string Zahl(decimal? d) => d?.ToString("0.###") ?? "–";

    /// <summary>Zelltext Mindestbestellmenge mit Einkaufseinheit, wenn sie von der Artikel-Einheit abweichen kann.</summary>
    internal static string? MindestText(ArticleSupplierRow? l) => l?.Mindestbestellmenge is { } m
        ? (m.ToString("0.###") + " " + l.Einkaufsmengeneinheit?.Trim()).Trim() : null;

    // Spaltenfilter (Server-Mode) – Zelltexte wie in der View.
    private static readonly Dictionary<string, Func<PurchaseProposalRow, string?>> ColumnMap = new()
    {
        ["lieferant"] = r => r.LieferantText,
        ["artikel"] = r => r.Article.ArticleNumber,
        ["bezeichnung"] = r => r.Article.Description,
        ["bestand"] = r => Zahl(r.BestandWms),
        ["sage-verfuegbar"] = r => Zahl(r.Sage?.LagerbestandVerfuegbar),
        ["offen-bestellt"] = r => Zahl(r.OffenBestellt),
        ["liefertermin"] = r => r.NaechsterLiefertermin?.ToString("dd.MM.yyyy"),
        ["bedarfe"] = r => Zahl(r.OffeneBedarfe),
        ["verfuegbar"] = r => Zahl(r.Ergebnis.Verfuegbar),
        ["meldebestand"] = r => Zahl(r.Article.ReorderLevel),
        ["letzte-bestellmenge"] = r => r.LetzteBestellung?.LetzteBestellmenge?.ToString("0.###"),
        ["letzte-bestellung-am"] = r => r.LetzteBestellung?.LetzteBestellungAm?.ToString("dd.MM.yyyy"),
        ["vorschlag"] = r => r.Ergebnis.Vorschlag?.ToString("0.###"),
        ["grund"] = r => r.Ergebnis.Grund,
        ["einheit"] = r => r.Article.Unit,
        ["mindestbestellmenge"] = r => MindestText(r.Lieferant),
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
        var letzte = await _repo.GetLetzteBestellungenAsync();
        var letzteGelesen = await _repo.HatLetzteBestellungenAsync();
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

        // E2: offene Sage-Bestellungen nur aus dem Cache (Teil B), Rest nach der EINEN Rest-Regel (ErfasstJeBelPos.Rest).
        var stand = await _lines.GetStandAsync();
        List<SagePurchaseOrderLine>? bestellungen = null;
        var erfasst = new Dictionary<int, ErfasstJeBelPos>();
        if (stand != null)
        {
            var ids = artikel.Select(a => a.Id).ToHashSet();
            bestellungen = (await _lines.GetAllAsync()).Where(l => l.ArticleId is { } id && ids.Contains(id)).ToList();
            erfasst = await _receipts.GetErfasstJeBelPosAsync(bestellungen.Select(l => l.BelPosId).ToList());
        }

        var (alle, bedarfeEinheitAbweichend, positionenEinheitAbweichend) =
            BaueZeilen(artikel, lieferanten, bestand, bedarfe, bestellungen, erfasst, DateTime.Today, letzte, letzteGelesen);

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
            BestellungenStand = stand,
            PositionenEinheitAbweichend = positionenEinheitAbweichend,
            LetzteBestellungenGelesen = letzteGelesen,
            FallbackZeilen = alle.Count(r => r.Ergebnis.Fallback),
            OhneMengenquelle = alle.Count(r => r.Ergebnis.Grund.StartsWith(PurchaseProposalCalculator.GrundOhneLetzteBestellung, StringComparison.Ordinal)),
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
    /// (Artikelnummer getrimmt, Gross/Klein egal; abweichende Einheit nicht gerechnet, gezaehlt), offene Bestellungen (E2: Rest je Position,
    /// <c>EinheitAbweichend</c> nicht gerechnet, gezaehlt; <paramref name="bestellungen"/> null = Cache leer), Lieferant, WBZ, Rechner.
    /// E3: letzte Bestellung beim Hauptlieferanten, sonst die juengste bei einem anderen Lieferanten (<paramref name="letzte"/>, Rueckfrage 13 a);
    /// Basis ≠ Lager (Sage-Zusatzfelder) → nicht verwendet mit Hinweis; gar keine Bestellung im Rueckblick und Daten gelesen
    /// (<paramref name="letzteGelesen"/>) → Fallback Sage-Bestellmenge (Freigabe-Nachtrag 2).</summary>
    internal static (List<PurchaseProposalRow> Zeilen, int BedarfeEinheitAbweichend, int PositionenEinheitAbweichend) BaueZeilen(List<Article> artikel,
        IReadOnlyDictionary<int, ArticleSupplierRow> lieferanten, IReadOnlyDictionary<string, List<StockLocationInfo>> bestand,
        IReadOnlyList<OffenerBedarf> bedarfe, IReadOnlyList<SagePurchaseOrderLine>? bestellungen, IReadOnlyDictionary<int, ErfasstJeBelPos> erfasst,
        DateTime heute, IReadOnlyDictionary<int, ArticleSupplierRow>? letzte = null, bool letzteGelesen = false)
    {
        var positionenAbweichend = bestellungen?.Count(l => l.EinheitAbweichend) ?? 0;
        var restJeArtikel = (bestellungen ?? new List<SagePurchaseOrderLine>()).Where(l => !l.EinheitAbweichend && l.ArticleId != null)
            .Select(l => (ArticleId: l.ArticleId!.Value, l.Liefertermin, Menge: erfasst.GetValueOrDefault(l.BelPosId, ErfasstJeBelPos.Leer).Rest(l)))
            .GroupBy(x => x.ArticleId)
            .ToDictionary(g => g.Key, g => (Summe: g.Sum(x => x.Menge), Termin: g.Where(x => x.Menge > 0).Min(x => x.Liefertermin)));

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
            decimal? offenBestellt = null;
            DateTime? termin = null;
            if (bestellungen == null) hinweise.Add("ohne offene Bestellungen");
            else if (restJeArtikel.TryGetValue(a.Id, out var o)) { offenBestellt = o.Summe; termin = o.Termin; }
            else offenBestellt = 0m;
            var quelle = l?.LetzteBestellmenge != null ? l : letzte?.GetValueOrDefault(a.Id);
            var fallback = letzteGelesen && quelle == null ? a.SageDetail?.Bestellmenge : null;
            if (quelle != null && EinheitWeichtAb(a.SageDetail?.Basismengeneinheit, a.SageDetail?.Lagermengeneinheit))
            {
                hinweise.Add("Einheit Basis ≠ Lager – letzte Bestellmenge nicht verwendet");
                quelle = null;
            }
            return new PurchaseProposalRow
            {
                Article = a, Lieferant = l, LieferantText = text, LieferantKto = kto, BestandWms = bestandWms, OffeneBedarfe = offeneBedarfe,
                OffenBestellt = offenBestellt, NaechsterLiefertermin = termin,
                LetzteBestellung = quelle,
                Ergebnis = PurchaseProposalCalculator.Berechne(new PurchaseProposalInput(bestandWms, offenBestellt ?? 0m, offeneBedarfe, a.ReorderLevel ?? 0m,
                    LetzteBestellmenge: quelle?.LetzteBestellmenge,
                    LetzteBestellungLieferant: quelle != null && quelle.CreditorId != l?.CreditorId
                        ? (string.IsNullOrWhiteSpace(quelle.SageKto) ? quelle.Name : $"{quelle.Name} ({quelle.SageKto})") : null,
                    Mindestbestellmenge: l?.Mindestbestellmenge, Einkaufsmengeneinheit: l?.Einkaufsmengeneinheit, UmrechnungsfaktorEK: l?.UmrechnungsfaktorEK,
                    ArtikelEinheit: a.Unit, FallbackSageBestellmenge: fallback)),
                Wbz = wbz, Wunschtermin = wbz is { } t ? heute.AddDays(t) : null, Hinweise = hinweise,
            };
        }).ToList();
        return (zeilen, abweichend, positionenAbweichend);
    }
}
