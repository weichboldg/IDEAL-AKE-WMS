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
    private readonly IPlannedConsumptionService _planned;
    private readonly IServiceSettingRepository _serviceSettings;

    public PurchaseProposalsController(IPurchaseProposalRepository repo, IStockMovementRepository stock, IAppSettingRepository settings,
        ICurrentUserService user, ISagePurchaseOrderLineRepository lines, IPurchaseOrderReceiptRepository receipts,
        IPlannedConsumptionService planned, IServiceSettingRepository serviceSettings)
    {
        _planned = planned;
        _serviceSettings = serviceSettings;
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
        ["matchcode"] = r => r.Article.Matchcode,   // v1.99.30 F11
        ["bezeichnung"] = r => r.Article.Description,
        ["bestand"] = r => Zahl(r.BestandWms),
        ["sage-verfuegbar"] = r => Zahl(r.Sage?.LagerbestandVerfuegbar),
        ["offen-bestellt"] = r => Zahl(r.OffenBestellt),
        ["liefertermin"] = r => r.NaechsterLiefertermin?.ToString("dd.MM.yyyy"),
        ["naechster-bedarf"] = r => r.NaechsterBedarf?.ToString("dd.MM.yyyy"),   // v1.99.30 F5
        ["fa-bedarf"] = r => Zahl(r.FaBedarf),   // v1.99.30 F4
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
        var lieferanten = await _repo.GetLieferantenAsync();
        var letzte = await _repo.GetLetzteBestellungenAsync();
        var letzteGelesen = await _repo.HatLetzteBestellungenAsync();
        var bestand = artikel.Count == 0
            ? new Dictionary<string, List<StockLocationInfo>>()
            : await _stock.GetStockByArticleNumbersAsync(artikel.Select(a => a.ArticleNumber).Distinct().ToList(), mitKommissionierwagen: true);   // Q2 Variante b: wie Artikelinfo

        // v1.99.30 F4/Antwort 7: FA-Bedarfe + Lagerbestellungen + Bedarfsmeldungen aus DEMSELBEN Service wie die Artikelinfo (Einstellung
        // EinkaufOffeneBedarfe wertet der Service aus). ponytail: ein Aufruf fuer alle Meldebestand-Artikel; die BOM-Abfrage nimmt die Liste
        // per OPENJSON (EF 10, fallstricke) – Bloecke wie SdataArtikelReader erst, wenn die Messung auf AKE-DEV es verlangt.
        var bedarfe = await _planned.GetJeArtikelAsync(artikel);
        var (mitLager, mitBedarfsmeldungen) = (bedarfe.MitLagerbestellungen, bedarfe.MitBedarfsmeldungen);

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

        DateTime? bomCacheBis = null;
        if (!bedarfe.Hierarchisch)
        {
            var wochen = int.TryParse(await _serviceSettings.GetValueAsync("Sync:BomCacheWeeks"), out var w) ? w : 8;   // Default wie der Dienst
            bomCacheBis = DateTime.Today.AddDays(7 * wochen);
        }

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
            OhneArtikelLieferant = alle.Count(r => r.Lieferant?.IstHauptlieferant != true),
            BedarfeEinheitAbweichend = bedarfeEinheitAbweichend,
            FaBedarfeHierarchisch = bedarfe.Hierarchisch,
            BomCacheBis = bomCacheBis,
            MitFaBedarf = alle.Count(r => r.FaBedarf > 0),
            ArtikelinfoLink = await _user.HasMasterDataReadAccessAsync(),
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

    private static bool EinheitWeichtAb(string? a, string? b) =>
        !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b) && !string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>v1.99.30 F7 Ersatzloesung (P-N1/P-N4/P-N7 offen): Mengen in der Basiseinheit (letzte Bestellung, Sage-Bestellmenge) gelten nur,
    /// wenn Basis = Lager und <c>UmrechnungsFaktorLME</c> leer oder 1 ist. Sonst nicht umrechenbar – nie still Faktor 1, keine angenommene Richtung.</summary>
    internal static string? BasisNichtLager(ArticleSageDetail? d) =>
        EinheitWeichtAb(d?.Basismengeneinheit, d?.Lagermengeneinheit) ? $"Einheit {d!.Basismengeneinheit!.Trim()} → {d.Lagermengeneinheit!.Trim()} nicht umrechenbar"
        : d?.UmrechnungsFaktorLME is { } f && f != 1m ? $"Einheit gleich, Faktor {f:0.###}" : null;

    /// <summary>v1.99.30 F7 Ersatzloesung (P-N3): eine offene Position zaehlt trotz <c>EinheitAbweichend</c>, wenn ihre Basiseinheit (Einheit der
    /// Cache-Mengen) gleich der Lagereinheit ist – dann weicht nur die Bestelleinheit ab. Fehlt eine der beiden, zaehlt sie nicht.</summary>
    internal static bool PositionRechenbar(SagePurchaseOrderLine l) =>
        !l.EinheitAbweichend || (!string.IsNullOrWhiteSpace(l.MengeBasiseinheit) && !string.IsNullOrWhiteSpace(l.Lagermengeneinheit)
            && string.Equals(l.MengeBasiseinheit.Trim(), l.Lagermengeneinheit.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Datensammlung → Zeilen: Bestand je Artikel (Summe der Plaetze, ohne Bewegung 0, Kommissionierwagen eingeschlossen wie in der Artikelinfo – Q2 Variante b), Bedarfe aus dem
    /// <see cref="IPlannedConsumptionService"/> (v1.99.30: FA-Rest, Bedarfsmeldungen, Lagerbestellungen; abgezogen wird die angerechnete Summe
    /// ohne Doppelzaehlung, dieselbe wie in der Artikelinfo), offene Bestellungen (E2: Rest je Position,
    /// <c>EinheitAbweichend</c> nicht gerechnet, gezaehlt; <paramref name="bestellungen"/> null = Cache leer), Lieferant, WBZ, Rechner.
    /// E3: letzte Bestellung beim Hauptlieferanten, sonst die juengste bei einem anderen Lieferanten (<paramref name="letzte"/>, Rueckfrage 13 a);
    /// Basis ≠ Lager (Sage-Zusatzfelder) → nicht verwendet mit Hinweis; gar keine Bestellung im Rueckblick und Daten gelesen
    /// (<paramref name="letzteGelesen"/>) → Fallback Sage-Bestellmenge (Freigabe-Nachtrag 2).</summary>
    internal static (List<PurchaseProposalRow> Zeilen, int BedarfeEinheitAbweichend, int PositionenEinheitAbweichend) BaueZeilen(List<Article> artikel,
        IReadOnlyDictionary<int, List<ArticleSupplierRow>> lieferanten, IReadOnlyDictionary<string, List<StockLocationInfo>> bestand,
        PlannedConsumptionResult bedarfe, IReadOnlyList<SagePurchaseOrderLine>? bestellungen, IReadOnlyDictionary<int, ErfasstJeBelPos> erfasst,
        DateTime heute, IReadOnlyDictionary<int, ArticleSupplierRow>? letzte = null, bool letzteGelesen = false)
    {
        var nichtRechenbar = (bestellungen ?? new List<SagePurchaseOrderLine>()).Where(l => !PositionRechenbar(l) && l.ArticleId != null)
            .GroupBy(l => l.ArticleId!.Value).ToDictionary(g => g.Key, g => g.ToList());
        var positionenAbweichend = nichtRechenbar.Values.Sum(g => g.Count);
        // Positionen je Artikel (Rest nach der EINEN Rest-Regel, mit Liefertermin fuer das Dringlichkeits-Fenster F5).
        var restJeArtikel = (bestellungen ?? new List<SagePurchaseOrderLine>()).Where(l => PositionRechenbar(l) && l.ArticleId != null)
            .Select(l => (ArticleId: l.ArticleId!.Value, l.Liefertermin, Menge: erfasst.GetValueOrDefault(l.BelPosId, ErfasstJeBelPos.Leer).Rest(l)))
            .GroupBy(x => x.ArticleId)
            .ToDictionary(g => g.Key, g => g.Select(x => (x.Menge, x.Liefertermin)).ToList());

        // Doppelte Artikelnummern (nur theoretisch) zaehlen ihre nicht gerechneten Bedarfszeilen einmal.
        var abweichend = bedarfe.JeArtikel.Values.Sum(b => b.EinheitAbweichend);

        var zeilen = artikel.Select(a =>
        {
            var hinweise = new List<string>();
            var juengste = letzte?.GetValueOrDefault(a.Id);
            var (l, kto, text) = WaehleLieferant(a, lieferanten.GetValueOrDefault(a.Id) ?? new List<ArticleSupplierRow>(), juengste, hinweise);
            if (a.SageDetail == null) hinweise.Add("Sage-Zusatzfelder fehlen");
            var wbz = l?.Wiederbeschaffungszeit ?? a.SageDetail?.Wiederbeschaffungszeit;
            var bedarf = bedarfe.Fuer(a.ArticleNumber);
            var bestandWms = bestand.TryGetValue(a.ArticleNumber, out var plaetze) ? plaetze.Sum(p => p.Quantity) : 0m;
            decimal? offenBestellt = null;
            DateTime? termin = null;
            var positionen = restJeArtikel.GetValueOrDefault(a.Id) ?? new List<(decimal Menge, DateTime? Liefertermin)>();
            if (bestellungen == null) hinweise.Add("ohne offene Bestellungen");
            else
            {
                offenBestellt = positionen.Sum(p => p.Menge);
                termin = positionen.Where(p => p.Menge > 0).Min(p => p.Liefertermin);
            }
            // F5 (Antwort 6): Dringlichkeit im Fenster heute + WBZ; Positionen ohne Liefertermin zaehlen dort nicht (Hinweis), ohne WBZ gilt 0.
            var fenster = PurchaseProposalCalculator.VerfuegbarBis(bestandWms, positionen, bedarf, heute, wbz);
            if (positionen.Any(p => p.Menge > 0 && p.Liefertermin == null)) hinweise.Add(PurchaseProposalCalculator.HinweisOhneLiefertermin);
            if (wbz == null) hinweise.Add(PurchaseProposalCalculator.HinweisOhneWbz);
            if (nichtRechenbar.GetValueOrDefault(a.Id) is { } ohne)
                hinweise.Add($"{ohne.Count} offene Bestellposition(en) Einheit {ohne[0].MengeBasiseinheit?.Trim() ?? "?"} → {ohne[0].Lagermengeneinheit?.Trim() ?? "?"} nicht umrechenbar – nicht gezählt");
            // Antwort 4: eine Bestellung ohne Artikel-Lieferant gilt, wenn sie juenger ist als ALLE zugeordneten (das sichert das Repository zu),
            // auch vor dem Hauptlieferanten; sonst wie bisher Hauptlieferant, dann juengste bei einem anderen (Rueckfrage 13 a).
            var quelle = juengste?.OhneArtikelLieferant == true || l?.LetzteBestellmenge == null ? juengste : l;
            var keineBestellung = letzteGelesen && quelle == null;
            var fallback = keineBestellung ? a.SageDetail?.Bestellmenge : null;
            if (BasisNichtLager(a.SageDetail) is { } einheit)
            {
                if (quelle != null) hinweise.Add($"{einheit} – letzte Bestellmenge nicht verwendet, Menge prüfen");
                // BV-1: Sage-Bestellmenge bis P-N1 nur bei Basis = Lager (heute wurde sie ungeprueft genommen).
                if (fallback is > 0m) hinweise.Add($"{einheit} – Sage-Bestellmenge nicht verwendet, Menge prüfen");
                quelle = null;
                fallback = null;
            }
            return new PurchaseProposalRow
            {
                Article = a, Lieferant = l, LieferantText = text, LieferantKto = kto, BestandWms = bestandWms, Bedarf = bedarf,
                OffenBestellt = offenBestellt, NaechsterLiefertermin = termin, NaechsterBedarf = bedarf.NaechsterBedarf(heute),
                LetzteBestellung = quelle,
                Ergebnis = PurchaseProposalCalculator.Berechne(new PurchaseProposalInput(bestandWms, offenBestellt ?? 0m, bedarf.Angerechnet, a.ReorderLevel ?? 0m,
                    LetzteBestellmenge: quelle?.LetzteBestellmenge,
                    LetzteBestellungLieferant: quelle == null ? null
                        : quelle.OhneArtikelLieferant ? (quelle.Name == quelle.SageKto ? quelle.SageKto : $"{quelle.SageKto} {quelle.Name}")
                        : quelle.CreditorId != l?.CreditorId ? quelle.Anzeige : null,
                    Mindestbestellmenge: l?.Mindestbestellmenge, Einkaufsmengeneinheit: l?.Einkaufsmengeneinheit, UmrechnungsfaktorEK: l?.UmrechnungsfaktorEK,
                    ArtikelEinheit: a.Unit, FallbackSageBestellmenge: fallback,
                    LetzteBestellungOhneArtikelLieferant: quelle?.OhneArtikelLieferant == true, KeineBestellungImRueckblick: keineBestellung,
                    VerfuegbarFenster: fenster, DezimalstellenLager: a.SageDetail?.DezimalstellenLager)),
                Wbz = wbz, Wunschtermin = wbz is { } t ? heute.AddDays(t) : null, Hinweise = hinweise,
            };
        }).ToList();
        return (zeilen, abweichend, positionenAbweichend);
    }

    public const string HinweisKeinLieferant = "Kein Lieferant in Sage hinterlegt";
    public const string HinweisMehrereLieferanten = "mehrere Lieferanten, kein Hauptlieferant";

    /// <summary>v1.99.30 F8: Lieferant-Kaskade an EINER Stelle. 1. Hauptlieferant (Artikel-Lieferant), 2. Sage-Hauptlieferant ohne Kreditor/Zeile
    /// im WMS (nur Kto, Hinweis „Kreditor fehlt im WMS“), 3. der einzige Artikel-Lieferant, 4. der Lieferant der juengsten Bestellung (auch ohne
    /// Zuordnung, F3), sonst leer mit Hinweis. Rueckgabe: Artikel-Lieferant-Zeile (fuer MBM/EK-Einheit/WBZ; null ohne Zeile), Kto, Anzeigetext.
    /// ponytail: Rahmenvertrag (F9) kommt als erste Stufe davor, sobald E8 (P-N6/P-N9) entblockt ist.</summary>
    internal static (ArticleSupplierRow? Zeile, string? Kto, string? Text) WaehleLieferant(Article a, IReadOnlyList<ArticleSupplierRow> alle,
        ArticleSupplierRow? juengste, List<string> hinweise)
    {
        // Mehrere Hauptlieferanten je Artikel kommen laut Abgleich nicht vor; falls doch, gewinnt der erste nach Name (stabil).
        if (alle.Where(x => x.IstHauptlieferant).OrderBy(x => x.Name).FirstOrDefault() is { } haupt) return (haupt, haupt.SageKto, haupt.Anzeige);
        if (a.SageDetail?.Hauptlieferant?.Trim() is { Length: > 0 } sageHaupt)
        {
            hinweise.Add("Kreditor fehlt im WMS");
            return (null, sageHaupt, sageHaupt);
        }
        if (alle.Count == 1)
        {
            hinweise.Add("kein Hauptlieferant – einziger Artikel-Lieferant");
            return (alle[0], alle[0].SageKto, alle[0].Anzeige);
        }
        if (juengste != null)
        {
            hinweise.Add("kein Hauptlieferant – Lieferant der letzten Bestellung");
            var zeile = juengste.OhneArtikelLieferant ? null : alle.FirstOrDefault(x => x.CreditorId == juengste.CreditorId) ?? juengste;
            return (zeile, juengste.SageKto, juengste.Anzeige);
        }
        hinweise.Add(alle.Count > 1 ? HinweisMehrereLieferanten : HinweisKeinLieferant);
        return (null, null, null);
    }
}
