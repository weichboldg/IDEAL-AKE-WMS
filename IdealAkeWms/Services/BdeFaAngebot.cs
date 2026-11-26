using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Services;

/// <summary>Eine Zeile der FA-Sicht bzw. ein Knopf der Terminal-Liste; <see cref="LaeuftBeiMir"/> = der gewaehlte Werker bucht darauf.</summary>
public sealed record BdeFaZeile(BdeTerminalArbeitsgang Ag, bool LaeuftBeiMir);

/// <summary>v1.99.76 (E4-2): Zeile der FA-Sicht mit allen AG der Sub-FA (<see cref="AgSymbole"/>, der eigene markiert), Strukturzeile
/// der Sub-FA (IDEAL; AKE und Haupt-FA-Ebene null) und Kopfwerten (IDEAL; AKE null).</summary>
public sealed record BdeFaSichtZeile(BdeFaZeile Zeile, IReadOnlyList<FaArbeitsschrittSymbol> AgSymbole, FaHierarchyNode? Knoten,
    VormontageService.KopfWerte? Kopf, string? SageStatus)
{
    public WorkOperation Op => Zeile.Ag.Op;
}

/// <summary>
/// v1.99.76 (Spec 2026-11-25-bde-masken-vereinheitlichung E4-1, Antwort 3): EINE Zeilenquelle fuer die Terminal-Liste
/// (<c>BdeApiController.GetAvailableOperations</c>, Nicht-NurFA-Zweig) und die FA-Sicht der neuen BDE-Seite: offene
/// Arbeitsgaenge der Kuerzel-Familie ohne fertig gemeldete, geordnet ueber <see cref="BdeTerminalAuftraege.Ordnen"/>.
/// </summary>
public class BdeFaAngebot
{
    private readonly ApplicationDbContext _ctx;
    private readonly IWorkOperationRepository _workOps;
    private readonly IBdeBookingRepository _bookings;
    private readonly IFaHierarchyOrderInfoRepository _orderInfos;
    private readonly IHolidayRepository _holidays;
    private readonly IBusinessDayService _businessDays;
    private readonly IOperationConfigRepository _operationConfigs;

    public BdeFaAngebot(ApplicationDbContext ctx, IWorkOperationRepository workOps, IBdeBookingRepository bookings,
        IFaHierarchyOrderInfoRepository orderInfos, IHolidayRepository holidays, IBusinessDayService businessDays,
        IOperationConfigRepository operationConfigs)
    {
        _ctx = ctx; _workOps = workOps; _bookings = bookings; _orderInfos = orderInfos;
        _holidays = holidays; _businessDays = businessDays; _operationConfigs = operationConfigs;
    }

    /// <summary>v1.99.77 (E4-4): Kopf-Buchungen des Werkers ueber dieselbe Abfrage wie BdeTerminal ActiveBookings/PausedBookings.</summary>
    public async Task<(List<BdeKopfBuchung> Laufend, List<BdeKopfBuchung> Pausiert)> KopfBuchungenAsync(int operatorId) =>
        (await BdeKopfBuchungen.LaufendeAsync(_ctx, operatorId), await BdeKopfBuchungen.PausierteAsync(_ctx, operatorId));

    /// <summary>v1.99.81 (QA B-3, Gerald): bisher Gut je Arbeitsgang (alle nicht stornierten Mengen, gleiche Summe wie der Kopf) –
    /// „Fertig melden“ der FA-Sicht belegt mit dem Rest vor und rechnet die Teilmengen in die Unter-Soll-Rueckfrage ein.</summary>
    public async Task<Dictionary<int, decimal>> BisherGutAsync(IReadOnlyCollection<int> workOperationIds) =>
        await _ctx.BdeBookingQuantities
            .Where(m => !m.BdeBooking!.IsCancelled && m.BdeBooking.WorkOperationId != null && workOperationIds.Contains(m.BdeBooking.WorkOperationId.Value))
            .GroupBy(m => m.BdeBooking!.WorkOperationId!.Value)
            .Select(g => new { g.Key, Gut = g.Sum(m => m.GoodQuantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Gut);

    /// <summary>Terminal (<paramref name="eigeneLaufendeAusblenden"/> = true): wie vor v1.99.76 ohne die eigenen laufenden.
    /// FA-Sicht (false): eigene laufende bleiben mit <see cref="BdeFaZeile.LaeuftBeiMir"/> sichtbar. Von anderen gebuchte bleiben
    /// sichtbar als „laeuft“ (Antwort 6 = A, v1.57.0); die Startpruefung bleibt im BdeBookingService.</summary>
    public async Task<List<BdeFaZeile>> LadenAsync(ProductionWorkplace workplace, int? operatorId, bool eigeneLaufendeAusblenden)
    {
        var familie = WerkbankKuerzelFamilie.FamilieIds(workplace, await _ctx.ProductionWorkplaces.AsNoTracking().ToListAsync());
        var workOps = await _workOps.GetOpenByWorkplaceIdsAsync(familie, excludePackedOrders: true);
        var activeWoIds = (await _bookings.GetActiveCockpitAsync())
            .Where(b => b.WorkOperationId.HasValue)
            .Select(b => b.WorkOperationId!.Value)
            .ToHashSet();

        // fertig gemeldet = IsFinal, nicht storniert
        var finishedWoIdSet = (await _ctx.BdeBookingQuantities
            .Where(q => q.IsFinal && !q.BdeBooking!.IsCancelled && q.BdeBooking.WorkOperationId.HasValue)
            .Select(q => q.BdeBooking!.WorkOperationId!.Value)
            .Distinct()
            .ToListAsync()).ToHashSet();

        var myActiveWoIdSet = operatorId.HasValue
            ? (await _ctx.BdeBookings
                .Where(b => b.BdeOperatorId == operatorId.Value && b.EndedAt == null && !b.IsCancelled && b.WorkOperationId != null)
                .Select(b => b.WorkOperationId!.Value)
                .Distinct()
                .ToListAsync()).ToHashSet()
            : new HashSet<int>();

        var angebot = workOps.Where(wo => !finishedWoIdSet.Contains(wo.Id)
            && !(eigeneLaufendeAusblenden && myActiveWoIdSet.Contains(wo.Id))).ToList();
        if (angebot.Count == 0) return new List<BdeFaZeile>();

        // Je Aufruf hoechstens drei Abfragen mehr: Kopfzeilen (IDEAL FE_Termin; AKE leer), Feiertage, Offsets (beide gecacht).
        var hauptFas = angebot.Select(wo => int.TryParse(wo.ProductionOrder.OrderNumber, out var h) ? h : (int?)null)
            .OfType<int>().Distinct().ToList();
        var kopf = (await _orderInfos.GetByHauptFaKeysAsync(hauptFas)).ToLookup(o => o.HauptFA);
        var holidays = await _holidays.GetHolidayDatesAsync();
        var offsets = await _operationConfigs.GetDueDateOffsetsAsync();

        return BdeTerminalAuftraege.Ordnen(angebot, activeWoIds, kopf, offsets,
                (d, o) => OseonDueDateCalculator.Calculate(d, o, _businessDays, holidays), DateTime.Today)
            .Select(a => new BdeFaZeile(a, myActiveWoIdSet.Contains(a.Op.Id)))
            .ToList();
    }

    /// <summary>FA-Sicht (E4-1/E4-2): Zeilen wie <see cref="LadenAsync"/> mit eigenen laufenden, angereichert um AG-Status aller AG des
    /// Auftrags (Antwort 4, Regel <see cref="ArbeitsgangStatusRegel"/>), Strukturzeile, Kopfwerte und Sage-Status. Je Aufruf vier Abfragen mehr.</summary>
    public async Task<List<BdeFaSichtZeile>> FaSichtAsync(ProductionWorkplace workplace, int operatorId)
    {
        var zeilen = await LadenAsync(workplace, operatorId, eigeneLaufendeAusblenden: false);
        if (zeilen.Count == 0) return new List<BdeFaSichtZeile>();

        var poIds = zeilen.Select(z => z.Ag.Op.ProductionOrderId).Distinct().ToList();
        var alleAg = await _ctx.WorkOperations.AsNoTracking().Where(w => poIds.Contains(w.ProductionOrderId)).ToListAsync();
        var bde = await _workOps.GetBdeStatusAsync(alleAg.Select(w => w.Id).ToList());
        var sage = await _ctx.ProductionOrderExtraInfos.AsNoTracking().Where(e => poIds.Contains(e.ProductionOrderId))
            .ToDictionaryAsync(e => e.ProductionOrderId, e => e.SageStatus);

        var hauptFas = zeilen.Select(z => int.TryParse(z.Ag.Op.ProductionOrder.OrderNumber, out var h) ? h : (int?)null).OfType<int>().Distinct().ToList();
        var infos = await _orderInfos.GetByHauptFaKeysAsync(hauptFas);
        var kopfWerte = VormontageService.BuildKopfLookup(infos);
        // Strukturzeile = Knoten mit SubFA == SubOrderNumber (wie FaHierarchyBomRepository.GetBomItemsAsync); SubFA 0 = Blechteil, nie ein Auftrag.
        var knoten = (await _ctx.FaHierarchyNodes.AsNoTracking()
                .Where(n => hauptFas.Contains(n.HauptFA) && n.SubFA != 0 && n.VerschwundenSeit == null).ToListAsync())
            .GroupBy(n => (n.HauptFA, n.SubFA)).ToDictionary(g => g.Key, g => g.OrderBy(n => n.Position).First());

        var holidays = await _holidays.GetHolidayDatesAsync();
        var offsets = await _operationConfigs.GetDueDateOffsetsAsync();
        var kopf = infos.ToLookup(o => o.HauptFA);
        var agJeAuftrag = alleAg.ToLookup(w => w.ProductionOrderId);

        return zeilen.Select(z =>
        {
            var po = z.Ag.Op.ProductionOrder;
            var termin = BdeTerminalAuftraege.Fertigungstermin(po, kopf);
            var symbole = agJeAuftrag[po.Id].OrderBy(w => w.Sequence).Select(w =>
            {
                var soll = ArbeitsgangStatusRegel.Soll(termin, w.OperationNumber, offsets,
                    (d, o) => OseonDueDateCalculator.Calculate(d, o, _businessDays, holidays), out _);
                return new FaArbeitsschrittSymbol(w.OperationNumber.Trim(), string.IsNullOrWhiteSpace(w.Name) ? null : w.Name.Trim())
                {
                    Status = ArbeitsgangStatusRegel.Ableiten(bde.Erledigt.Contains(w.Id), bde.Laeuft.Contains(w.Id), soll, DateTime.Today),
                    Soll = soll,
                    Markiert = w.Id == z.Ag.Op.Id
                };
            }).ToList();
            var haupt = int.TryParse(po.OrderNumber, out var h) ? h : (int?)null;
            var sub = int.TryParse(po.SubOrderNumber, out var sf) ? sf : 0;
            return new BdeFaSichtZeile(z, symbole,
                haupt is int hk && knoten.TryGetValue((hk, sub), out var n) ? n : null,
                haupt is int hk2 && kopfWerte.TryGetValue(hk2, out var k) ? k : null,
                sage.TryGetValue(po.Id, out var st) ? st : null);
        }).ToList();
    }

    private static string? Datum(DateTime? d) => d?.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.GetCultureInfo("de-AT"));

    /// <summary>Statustext der Zeile (Zelle und Screenreader; Farbe nie allein).</summary>
    public static string StatusText(BdeFaSichtZeile z) => z.Zeile.LaeuftBeiMir ? "läuft bei Ihnen"
        : z.Zeile.Ag.Symbol.Status is FaAgStatus.Laeuft or FaAgStatus.LaeuftUeberfaellig ? "läuft"
        : z.Zeile.Ag.Symbol.Status == FaAgStatus.Ueberfaellig ? "überfällig" : "offen";

    /// <summary>E4-2: Zelle = Filter (ADR 0005); Datumsspalten nach der Termin-Berechnung. Register <c>BdeTeileliste/Fa</c>.</summary>
    public static readonly IReadOnlyDictionary<string, Func<BdeFaSichtZeile, string?>> Spalten = new Dictionary<string, Func<BdeFaSichtZeile, string?>>
    {
        ["hauptfa"] = z => z.Op.ProductionOrder.OrderNumber,
        ["subfa"] = z => z.Op.ProductionOrder.SubOrderNumber,
        ["arbeitsgang"] = z => $"{z.Op.OperationNumber.Trim()} {z.Op.Name}".Trim(),
        ["ag-status"] = z => z.AgSymbole.Count == 0 ? null : string.Join(", ", z.AgSymbole.Select(a => a.AriaLabel)),
        ["bezeichnung"] = z => z.Op.ProductionOrder.Description1,
        ["artnr"] = z => z.Op.ProductionOrder.ArticleNumber,
        ["matchcode"] = z => z.Knoten?.Matchcode,
        ["sollmenge"] = z => VormontageService.FormatMenge(z.Op.ProductionOrder.Quantity),
        ["fe-termin"] = z => Datum(z.Zeile.Ag.Fertigungstermin),
        ["soll-termin"] = z => Datum(z.Zeile.Ag.Symbol.Soll),
        ["prio"] = z => z.Zeile.Ag.Prio?.ToString(),
        ["kunde"] = z => z.Kopf?.Kunde ?? z.Op.ProductionOrder.Customer,
        ["montage-abteilung"] = z => z.Kopf?.Abteilung,
        ["material"] = z => z.Knoten?.Material,
        ["arbeitsschritte"] = z => z.Knoten?.Arbeitsschritte,
        ["arbeitsbereich"] = z => z.Knoten?.Arbeitsbereich,
        ["bemerkungpn"] = z => z.Knoten?.BemerkungPN,
        ["fertigungsinfopn"] = z => z.Knoten?.FertigungsInfoPN,
        ["sage-status"] = z => z.SageStatus,
        ["vaterfa"] = z => z.Knoten?.VaterFA?.ToString(),
        ["position"] = z => z.Knoten?.Position?.ToString(),
        ["status"] = StatusText,
    };
}
