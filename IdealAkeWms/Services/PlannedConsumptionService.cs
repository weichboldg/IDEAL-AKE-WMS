using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services.HierarchischeStruktur;

namespace IdealAkeWms.Services;

/// <summary>Ein offener FA, dessen Stueckliste den Artikel enthaelt (bzw. zu dem eine Bedarfsmeldung offen ist).</summary>
/// <param name="Soll">BOM-Menge: flach Menge je Geraet × FA-Stueckzahl, hierarchisch die Sollmenge der Eltern-Sub-FA (bereits Gesamtmenge).
/// 0 = FA nur ueber eine Bedarfsmeldung bekannt (<paramref name="AusStueckliste"/> false).</param>
/// <param name="Ausgebucht">Σ Ausbuchungen dieses Artikels mit diesem FA (nur <c>MovementType.Ausbuchung</c>, P-N8).</param>
/// <param name="Bedarfsmeldungen">Σ offene Bedarfsmeldungen dieses Artikels zu diesem FA (gleiche Einheit).</param>
public sealed record FaBedarf(int ProductionOrderId, string OrderNumber, string? SubOrderNumber, DateTime? ProductionDate, DateTime? DeliveryDate,
    decimal Soll, decimal Ausgebucht, decimal Bedarfsmeldungen, bool AusStueckliste = true)
{
    /// <summary>Antwort 2: nur der Rest zaehlt, nie negativ (Teilkommissionierung ist im Bestand schon abgezogen).</summary>
    public decimal Rest => Math.Max(0m, Soll - Ausgebucht);
    /// <summary>Antwort 1: je FA max(Rest, Σ Bedarfsmeldungen) – die Bedarfsmeldung ist ein Zukauf ZUM FA, nicht zusaetzlich.</summary>
    public decimal Angerechnet => Math.Max(Rest, Bedarfsmeldungen);
}

/// <summary>Bedarfe eines Artikels nach der EINEN Regel (Spec 2026-11-16-bestellvorschlaege-nachbesserung F4, Antworten 1, 2, 7).</summary>
/// <param name="Lagerbestellungen">Σ offene Lagerbestellungen (gleiche Einheit); 0, wenn per <c>EinkaufOffeneBedarfe</c> ausgeschaltet.</param>
/// <param name="EinheitAbweichend">Zahl offener Lagerbestellungs-/Bedarfsmeldungszeilen mit anderer Einheit als der Artikel – nicht gerechnet.</param>
public sealed record ArtikelBedarf(IReadOnlyList<FaBedarf> Fas, decimal Lagerbestellungen, int EinheitAbweichend)
{
    public static readonly ArtikelBedarf Leer = new(Array.Empty<FaBedarf>(), 0m, 0);
    public decimal FaRest => Fas.Sum(f => f.Rest);
    public decimal Bedarfsmeldungen => Fas.Sum(f => f.Bedarfsmeldungen);
    /// <summary>Σ je FA max(Rest, Bedarfsmeldungen).</summary>
    public decimal FaTeil => Fas.Sum(f => f.Angerechnet);
    /// <summary>Antwort 1: je Artikel max(FA-Teil, Σ Lagerbestellungen) – die Lagerbestellung traegt keinen FA, also nie doppelt (ggf. unterschaetzt).</summary>
    public decimal Angerechnet => Math.Max(FaTeil, Lagerbestellungen);
}

/// <param name="JeArtikel">Schluessel: Artikelnummer getrimmt, gross. Artikel ohne Bedarf fehlen (<see cref="ArtikelBedarf.Leer"/>).</param>
public sealed record PlannedConsumptionResult(IReadOnlyDictionary<string, ArtikelBedarf> JeArtikel, bool MitLagerbestellungen, bool MitBedarfsmeldungen,
    bool Hierarchisch)
{
    public ArtikelBedarf Fuer(string articleNumber) => JeArtikel.GetValueOrDefault(PlannedConsumptionService.Schluessel(articleNumber)) ?? ArtikelBedarf.Leer;
}

/// <summary>
/// v1.99.30 (F4/Antwort 7): Bedarfe offener FAs + Lagerbestellungen + Bedarfsmeldungen an EINER Stelle fuer Artikelinfo (ein Artikel) und
/// Bestellvorschlaege (alle Meldebestand-Artikel). Eine Implementierung, Interface fuer Moq wie die uebrigen Services.
/// </summary>
public interface IPlannedConsumptionService
{
    Task<PlannedConsumptionResult> GetJeArtikelAsync(IReadOnlyCollection<Article> artikel);
}

public class PlannedConsumptionService : IPlannedConsumptionService
{
    private readonly IBomCacheRepository _bom;
    private readonly IProductionOrderRepository _orders;
    private readonly IStockMovementRepository _stock;
    private readonly IPurchaseProposalRepository _bedarfe;
    private readonly IAppSettingRepository _settings;
    private readonly IServiceSettingRepository _serviceSettings;

    public PlannedConsumptionService(IBomCacheRepository bom, IProductionOrderRepository orders, IStockMovementRepository stock,
        IPurchaseProposalRepository bedarfe, IAppSettingRepository settings, IServiceSettingRepository serviceSettings)
    {
        _bom = bom; _orders = orders; _stock = stock; _bedarfe = bedarfe; _settings = settings; _serviceSettings = serviceSettings;
    }

    internal static string Schluessel(string articleNumber) => articleNumber.Trim().ToUpperInvariant();

    private static bool IsTrue(string? v) =>
        v != null && (v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1" || v.Equals("on", StringComparison.OrdinalIgnoreCase));

    private static bool EinheitWeichtAb(string? a, string? b) =>
        !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b) && !string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool InListe(string liste, string wert) =>
        liste.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Contains(wert, StringComparer.OrdinalIgnoreCase);

    public async Task<PlannedConsumptionResult> GetJeArtikelAsync(IReadOnlyCollection<Article> artikel)
    {
        var hierarchisch = IsTrue(await _serviceSettings.GetValueAsync(HierarchischeStrukturKeys.Master));
        // Einstellung EinkaufOffeneBedarfe: Zeile fehlt = beide, leer = keine (ADR 0020). FA-Bedarfe zaehlen immer.
        var einstellung = await _settings.GetValueAsync(AppSettingKeys.EinkaufOffeneBedarfe)
            ?? $"{AppSettingKeys.EinkaufBedarfWerte.Lagerbestellungen},{AppSettingKeys.EinkaufBedarfWerte.Bedarfsmeldungen}";
        var mitLager = InListe(einstellung, AppSettingKeys.EinkaufBedarfWerte.Lagerbestellungen);
        var mitMeldungen = InListe(einstellung, AppSettingKeys.EinkaufBedarfWerte.Bedarfsmeldungen);

        var jeNummer = artikel.GroupBy(a => Schluessel(a.ArticleNumber)).ToDictionary(g => g.Key, g => g.First());
        if (jeNummer.Count == 0) return new(new Dictionary<string, ArtikelBedarf>(), mitLager, mitMeldungen, hierarchisch);

        // 1) Stueckliste: je Bauteil die Geraete (flach Artikelnummer, hierarchisch SubOrderNumber) mit Menge, eine Abfrage.
        var bom = await _bom.GetComponentMengePerDeviceAsync(jeNummer.Values.Select(a => a.ArticleNumber).ToList());
        var geraete = bom.Values.SelectMany(d => d.Keys).Distinct().ToList();
        var offen = new List<ProductionOrder>();
        if (geraete.Count > 0)
        {
            var orders = hierarchisch ? await _orders.GetBySubOrderNumbersAsync(geraete) : await _orders.GetByArticleNumbersAsync(geraete);
            // "Abgeschlossen" = Sage-IsDone ODER App-Komm-IsDonePicking (v1.21.1); storniert wie erledigt (v1.25.0).
            offen = orders.Where(o => !o.IsDone && !o.IsCancelled && !(o.PickingStatus != null && o.PickingStatus.IsDonePicking)).ToList();
        }
        var orderJeGeraet = offen.Where(o => (hierarchisch ? o.SubOrderNumber : o.ArticleNumber) != null)
            .GroupBy(o => (hierarchisch ? o.SubOrderNumber : o.ArticleNumber)!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        // 2) Offene Lagerbestellungen / Bedarfsmeldungen (gleiche Einheit), Bedarfsmeldungen je FA.
        var lagerJeArtikel = new Dictionary<string, decimal>();
        var meldungJeArtikelFa = new Dictionary<(string Nr, int FaId), (decimal Menge, string? Bezug)>();
        var abweichend = new Dictionary<string, int>();
        var bedarfe = new List<OffenerBedarf>();
        if (mitLager) bedarfe.AddRange(await _bedarfe.GetOffeneLagerbestellungenAsync());
        var meldungenAb = bedarfe.Count;
        if (mitMeldungen) bedarfe.AddRange(await _bedarfe.GetOffeneBedarfsmeldungenAsync());
        for (var i = 0; i < bedarfe.Count; i++)
        {
            var b = bedarfe[i];
            var key = Schluessel(b.ArticleNumber);
            if (!jeNummer.TryGetValue(key, out var a)) continue;
            if (EinheitWeichtAb(b.Unit, a.Unit)) { abweichend[key] = abweichend.GetValueOrDefault(key) + 1; continue; }
            if (i < meldungenAb) lagerJeArtikel[key] = lagerJeArtikel.GetValueOrDefault(key) + b.Menge;
            else
            {
                // ProductionOrderId ist bei Bedarfsmeldungen Pflicht; fehlt sie doch, zaehlt die Meldung wie eine eigene FA-Gruppe 0.
                var faKey = (key, b.ProductionOrderId ?? 0);
                var bisher = meldungJeArtikelFa.GetValueOrDefault(faKey);
                meldungJeArtikelFa[faKey] = (bisher.Menge + b.Menge, bisher.Bezug ?? b.Bezug);
            }
        }

        // 3) FA-Zeilen je Artikel (Soll aus der Stueckliste), danach Ausbuchungen je (FA, Artikel) in einer Abfrage.
        var soll = new Dictionary<string, List<(ProductionOrder Fa, decimal Soll)>>();
        foreach (var (key, a) in jeNummer)
        {
            if (!bom.TryGetValue(a.ArticleNumber, out var mengeJeGeraet)) continue;
            var liste = new List<(ProductionOrder, decimal)>();
            foreach (var (geraet, menge) in mengeJeGeraet)
                foreach (var o in orderJeGeraet.GetValueOrDefault(geraet) ?? new List<ProductionOrder>())
                    liste.Add((o, hierarchisch ? menge : menge * o.Quantity));   // hierarchisch: Sollmenge ist bereits die Gesamtmenge
            if (liste.Count > 0) soll[key] = liste;
        }
        var faIds = soll.Values.SelectMany(l => l.Select(x => x.Fa.Id)).Distinct().ToList();
        var ausgebucht = await _stock.GetAusbuchungenJeFaAsync(faIds);

        var ergebnis = new Dictionary<string, ArtikelBedarf>();
        foreach (var key in jeNummer.Keys)
        {
            var fas = (soll.GetValueOrDefault(key) ?? new List<(ProductionOrder Fa, decimal Soll)>())
                .GroupBy(x => x.Fa.Id)
                .Select(g =>
                {
                    var o = g.First().Fa;
                    return new FaBedarf(o.Id, o.OrderNumber, o.SubOrderNumber, o.ProductionDate, o.DeliveryDate, g.Sum(x => x.Soll),
                        ausgebucht.GetValueOrDefault((o.Id, key)), meldungJeArtikelFa.GetValueOrDefault((key, o.Id)).Menge);
                }).ToList();
            var bekannt = fas.Select(f => f.ProductionOrderId).ToHashSet();
            // Bedarfsmeldungen zu FAs ohne Stuecklisten-Treffer (z. B. ausserhalb des BOM-Cache-Fensters) zaehlen voll, ohne Termin.
            // ponytail: kein Nachladen des FA-Termins; bei Bedarf GetByIdsAsync ergaenzen (Dringlichkeit rechnet ohne Termin als sofort).
            fas.AddRange(meldungJeArtikelFa.Where(m => m.Key.Nr == key && !bekannt.Contains(m.Key.FaId))
                .Select(m => new FaBedarf(m.Key.FaId, m.Value.Bezug ?? "", null, null, null, 0m, 0m, m.Value.Menge, AusStueckliste: false)));
            var lager = lagerJeArtikel.GetValueOrDefault(key);
            var abw = abweichend.GetValueOrDefault(key);
            if (fas.Count > 0 || lager > 0 || abw > 0) ergebnis[key] = new ArtikelBedarf(fas, lager, abw);
        }
        return new(ergebnis, mitLager, mitMeldungen, hierarchisch);
    }
}
