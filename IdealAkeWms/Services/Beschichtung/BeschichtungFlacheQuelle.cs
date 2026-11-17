using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services.HierarchischeStruktur;
using Microsoft.Extensions.Logging;

namespace IdealAkeWms.Services.Beschichtung;

/// <summary>Flache Positionen fuer Vorschlagsliste und Beschichtungsauftrag samt Sammelmeldung (WA ohne Lackierteil-Zeile).</summary>
public sealed record FlachePositionen(List<FaHierarchyNode> Knoten, List<string> OhneLackierteile, List<ProductionOrder> Auftraege,
    int OhneLackierteileKommissioniert = 0);

/// <summary>
/// v1.99.34 (Spec 2026-11-16-beschichtungslogik-je-standort E5, Nachtrag 3): Positionsquelle der Beschichtung an der AKE.
/// <see cref="IstFlachAsync"/> = Kennzeichen-Quelle <see cref="BeschichtungKennzeichenQuelle.Artikelkategorie"/> (aufgeloest ueber
/// <see cref="BeschichtungResolver.ResolveQuellen"/>, in der Praxis Master <c>ProduktionsauftragHierarchisch = false</c>); dann
/// kommen die Positionen aus der WA-Stueckliste (Stuecklisten-Cache), projiziert als <see cref="FaHierarchyNode"/> im Speicher:
/// HauptFA = WA-Nummer, Wurzel-Felder (VaterFA null, SubFA 0, Position null), <see cref="FaHierarchyNode.PositionText"/> =
/// Stuecklisten-Position (getrimmt, nie null), Menge ueber <see cref="BomQuantityResolver"/>, Masse seit v1.99.36 aus dem Sage-Artikel
/// (<see cref="ArticleSageDetail"/> cm × 10 = mm, Nachtrag 4; ohne Sage-Zeile 0 mit Logger-Warnung). Sonst bleibt die
/// FA-Struktur die Quelle (Aufrufer lesen dann wie bisher die Knoten).
/// </summary>
public class BeschichtungFlacheQuelle
{
    private readonly IBeschichtungsauftragRepository _repo;
    private readonly IArticleRepository _articles;
    private readonly IAppSettingRepository _settings;
    private readonly IServiceSettingRepository _serviceSettings;
    private readonly IHolidayRepository _holidays;
    private readonly IBusinessDayService _days;
    private readonly ILogger<BeschichtungFlacheQuelle> _logger;

    public BeschichtungFlacheQuelle(IBeschichtungsauftragRepository repo, IArticleRepository articles, IAppSettingRepository settings,
        IServiceSettingRepository serviceSettings, IHolidayRepository holidays, IBusinessDayService days,
        ILogger<BeschichtungFlacheQuelle> logger)
    {
        _repo = repo;
        _articles = articles;
        _settings = settings;
        _serviceSettings = serviceSettings;
        _holidays = holidays;
        _days = days;
        _logger = logger;
    }

    public virtual async Task<bool> IstFlachAsync()
    {
        var hierarchisch = string.Equals((await _serviceSettings.GetValueAsync(HierarchischeStrukturKeys.Master))?.Trim(), "true",
            StringComparison.OrdinalIgnoreCase);
        var q = BeschichtungResolver.ResolveQuellen(await _settings.GetValueAsync(AppSettingKeys.BeschichtungKennzeichenQuelle), null, hierarchisch);
        return q.Kennzeichen == BeschichtungKennzeichenQuelle.Artikelkategorie;
    }

    /// <summary>Alle offenen WA (<paramref name="hauptFas"/> null) bzw. nur diese WA. Die Sammelmeldung wird nur fuer die
    /// Gesamtliste geloggt (Logger-Warnung, A15); die Anzeige uebernimmt der Aufrufer. v1.99.36 (Nachtrag 4 Punkt 5): in der
    /// Gesamtliste fehlen kommissionierte WA (<see cref="ProductionOrderPickingStatus.IsDonePicking"/>), ausser
    /// <paramref name="mitKommissionierten"/>; mit <paramref name="hauptFas"/> (Anlage, Hinzufuegen, Vergabe) zaehlt jeder WA.</summary>
    public virtual async Task<FlachePositionen> LadenAsync(IReadOnlyCollection<int>? hauptFas = null, bool mitKommissionierten = false)
    {
        var daten = await _repo.GetFlacheLackierteileAsync(await _settings.GetValueAsync(AppSettingKeys.LackierteilKategorieName),
            hauptFas?.Select(h => h.ToString(CultureInfo.InvariantCulture)).ToList());
        var matchcodes = await MatchcodeLookup.BuildAsync(_articles, daten.Zeilen.Select(z => z.Item.Ressourcenummer), _logger,
            "Vorschlagsliste Beschichtung");

        var knoten = new List<FaHierarchyNode>();
        var nichtNumerisch = new SortedSet<string>(StringComparer.Ordinal);
        var ohneMasse = 0;
        foreach (var z in daten.Zeilen.Where(z => hauptFas is not null || mitKommissionierten || z.Auftrag.PickingStatus?.IsDonePicking != true))
        {
            if (z.BreiteCm is null && z.HoeheCm is null && z.LaengeCm is null) ohneMasse++;
            if (!int.TryParse(z.Auftrag.OrderNumber, NumberStyles.None, CultureInfo.InvariantCulture, out var fa))
            {
                nichtNumerisch.Add(z.Auftrag.OrderNumber);
                continue;
            }
            knoten.Add(Projektion(fa, z, matchcodes));
        }
        if (nichtNumerisch.Count > 0)
            _logger.LogWarning("Vorschlagsliste Beschichtung: {Count} WA mit nicht numerischer Nummer nicht gelistet: {Nummern}",
                nichtNumerisch.Count, string.Join(", ", nichtNumerisch));
        if (hauptFas is null && daten.OhneLackierteile.Count > 0)
            _logger.LogWarning("Vorschlagsliste Beschichtung: {Count} WA mit Beschichtungskennzeichen ohne Lackierteile im Stuecklisten-Cache: {Nummern}",
                daten.OhneLackierteile.Count, string.Join(", ", daten.OhneLackierteile));
        if (ohneMasse > 0)
            _logger.LogWarning("Vorschlagsliste Beschichtung: {Count} Lackierteil-Positionen ohne Sage-Artikeldaten (ArticleSageDetails) – Masse 0",
                ohneMasse);
        var auftraege = knoten.Select(k => k.HauptFA).ToHashSet();
        return new FlachePositionen(knoten, daten.OhneLackierteile,
            daten.Zeilen.Select(z => z.Auftrag).Distinct()
                .Where(o => int.TryParse(o.OrderNumber, NumberStyles.None, CultureInfo.InvariantCulture, out var n) && auftraege.Contains(n)).ToList(),
            daten.OhneLackierteileKommissioniert);
    }

    /// <summary>v1.99.36 (Nachtrag 4 Punkt 1): Kopfzeile je WA an der AKE statt FAInfos — Kunde, Fertigungstermin und berechneter
    /// Beschichtungstermin (<see cref="StartBeschichtungAsync"/>). <see cref="FaHierarchyOrderInfo.Id"/> = 0 kennzeichnet die aus dem WA
    /// gebildete Zeile (keine FAInfos-Zeile); die Kopf-Partials zeigen dann die WA-Felder.</summary>
    public virtual async Task<List<FaHierarchyOrderInfo>> KopfInfosAsync(IEnumerable<ProductionOrder> auftraege)
    {
        var liste = auftraege.ToList();
        if (liste.Count == 0) return new();
        var t = await TerminKontextAsync();
        var infos = new List<FaHierarchyOrderInfo>();
        foreach (var o in liste)
            if (int.TryParse(o.OrderNumber, NumberStyles.None, CultureInfo.InvariantCulture, out var fa))
                infos.Add(new FaHierarchyOrderInfo
                {
                    Id = 0, HauptFA = fa, Kunde = o.Customer, FE_Termin = o.ProductionDate, Start_Beschichtung = StartBeschichtung(t, o),
                });
        return infos;
    }

    /// <summary>AKE-Kopfvorbelegung: Start Beschichtung = berechneter Beschichtungstermin des WA (wie die FA-Liste:
    /// Fertigungsdatum − KommissionierTage − Vorkommissioniertage inkl. Werkbank-Override, dann <see cref="BeschichtungResolver.Bestimmen"/>).
    /// Null, wenn der WA nicht (mehr) offen ist oder kein Fertigungsdatum hat.</summary>
    public virtual async Task<DateTime?> StartBeschichtungAsync(ProductionOrder o) =>
        o.ProductionDate is null ? null : StartBeschichtung(await TerminKontextAsync(), o);

    private sealed record TerminKontext(BeschichtungKontext K, int KommissionierTage, int VorTage);

    private async Task<TerminKontext> TerminKontextAsync() => new(
        await BeschichtungResolver.LadenAsync(_settings, _holidays, _days, hierarchisch: false, _logger),
        await _settings.GetIntValueAsync("KommissionierTage", 4), await _settings.GetIntValueAsync("VorkommissionierTage", 1));

    private DateTime? StartBeschichtung(TerminKontext t, ProductionOrder o)
    {
        if (o.ProductionDate is not DateTime fertigung) return null;
        var vorTage = PrePickingDaysResolver.Resolve(o.ProductionWorkplace?.OverridePrePickingDays, t.VorTage);
        var vk = _days.SubtractBusinessDays(_days.SubtractBusinessDays(fertigung, t.KommissionierTage, t.K.Feiertage), vorTage, t.K.Feiertage);
        return BeschichtungResolver.Bestimmen(t.K, hasCoatingParts: true, vk, null).Termin;
    }

    internal static FaHierarchyNode Projektion(int hauptFa, FlacheLackierteilZeile z, IReadOnlyDictionary<string, string?> matchcodes)
    {
        var item = BomCacheRepository.ToBomItem(z.Item);
        return new FaHierarchyNode
        {
            HauptFA = hauptFa,
            HauptArtnr = z.Auftrag.ArticleNumber,
            VaterFA = null,
            SubFA = 0,
            Position = null,
            PositionText = (z.Item.Position ?? "").Trim(),
            Artnr = z.Item.Ressourcenummer,
            Matchcode = z.Item.Ressourcenummer is { } a && matchcodes.TryGetValue(a, out var m) ? m : null,
            Bezeichnung1 = z.Item.Bezeichnung1,
            Bezeichnung2 = z.Item.Bezeichnung2,
            Artikelgruppe = z.Item.Artikelgruppe,
            Beschichtet = true,
            Sollmenge = BomQuantityResolver.Resolve(new BomQueryResult(new List<BomItem> { item }, z.Source), item, z.Auftrag),
            // Nachtrag 4 Punkt 2: Sage-Artikel in cm (Laenge = Tiefe), Anzeige in mm; Einheit 2026-11-17 bestaetigt (113.473 von 113.523
            // Artikeln mit Massen: Breite/Hoehe/Laenge × 10 = USER_Breitemm/Hoehemm/Tiefemm, ake_TEST2026).
            Breite = (z.BreiteCm ?? 0m) * 10m,
            Hoehe = (z.HoeheCm ?? 0m) * 10m,
            Tiefe = (z.LaengeCm ?? 0m) * 10m,
        };
    }

    /// <summary>Text der Sammelmeldung (AK 15b): hoechstens 20 Nummern, dann „…“. Null = nichts zu melden. v1.99.36 (Nachtrag 4 Punkt 4):
    /// <paramref name="ohneLackierteile"/> = nicht kommissionierte WA mit Nummer, kommissionierte nur als Anzahl.</summary>
    public static string? Sammelmeldung(IReadOnlyCollection<string> ohneLackierteile, int kommissioniert = 0)
    {
        var kommText = kommissioniert > 0 ? $"{kommissioniert} bereits kommissionierte WA ohne Lackierteile (nicht einzeln genannt)." : null;
        if (ohneLackierteile.Count == 0) return kommText;
        var text = $"{ohneLackierteile.Count} WA mit Beschichtungskennzeichen ohne Lackierteile im Stücklisten-Cache – nicht in der Vorschlagsliste: " +
                   string.Join(", ", ohneLackierteile.Take(20)) + (ohneLackierteile.Count > 20 ? ", …" : "");
        return kommText is null ? text : text + ". Dazu " + kommText;
    }
}
