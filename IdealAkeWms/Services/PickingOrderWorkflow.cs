using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Services;

/// <summary>v1.88.0: Ergebnis von <see cref="PickingOrderWorkflow.ErzeugeOffeneAsync"/> (fuer Sammelmeldungen).</summary>
public sealed class PickingOrderErzeugung
{
    public List<string> Nummern { get; } = new();
    public List<string> Meldungen { get; } = new();
    public bool OhneStueckliste { get; set; }
    public bool OhneRest { get; set; }
    public string? Fehler { get; set; }
}

/// <summary>
/// v1.88.0 (Spec 2026-11-08-kommissionierung-auftraege-kopf-zeilen): einziger Schreibweg fuer <see cref="PickingOrder"/>.
/// E2: <see cref="ErzeugeOffeneAsync"/> legt je FA und Bereich einen Auftrag ueber die offenen Restmengen an (idempotent: zweimal aufgerufen
/// erzeugt nichts doppelt; neue Stuecklistenpositionen kommen beim Nachziehen an). Positionsumfang (Antwort 2): AKE alle, IDEAL nur
/// <see cref="KommissionierRelevanzFilter.IsRelevant"/>. Soll (Antwort 3): <see cref="BomQuantityResolver.Resolve"/>.
/// </summary>
public class PickingOrderWorkflow
{
    public const int MaxNummerVersuche = 3;
    public const string Praefix = "KO";

    private readonly IPickingOrderRepository _repo;
    private readonly IPickingAreaRepository _areas;
    private readonly IProductionOrderRepository _orders;
    private readonly IProductionOrderPickingStatusRepository _status;
    private readonly IBomRepository _bom;
    private readonly ICurrentUserService _user;
    private readonly ILogger<PickingOrderWorkflow> _logger;

    public PickingOrderWorkflow(IPickingOrderRepository repo, IPickingAreaRepository areas, IProductionOrderRepository orders,
        IProductionOrderPickingStatusRepository status, IBomRepository bom, ICurrentUserService user, ILogger<PickingOrderWorkflow> logger)
    {
        _repo = repo;
        _areas = areas;
        _orders = orders;
        _status = status;
        _bom = bom;
        _user = user;
        _logger = logger;
    }

    /// <summary>Uhr fuer Tests (Jahreswechsel der Nummer).</summary>
    public Func<DateTime> Jetzt { get; set; } = () => DateTime.Now;

    /// <summary>Offene Restmengen eines HauptFA als Kommissionierauftraege anlegen (Freigabe, Nachziehen, Auswahlliste ohne Leitstand).</summary>
    public async Task<PickingOrderErzeugung> ErzeugeOffeneAsync(int productionOrderId)
    {
        var r = new PickingOrderErzeugung();
        var order = await _orders.GetByIdAsync(productionOrderId);
        if (order == null) { r.Fehler = $"FA (Id {productionOrderId}) unbekannt."; return r; }
        if (order.IsSubFa) { r.Fehler = $"Kommissionierung erfolgt nur am HauptFA {order.OrderNumber}."; return r; }

        var bom = await _bom.GetBomItemsAsync(BomKey.ForOrder(order), BomScopes.ForOrder(order));
        if (bom.Items.Count == 0)
        {
            r.OhneStueckliste = true;
            r.Meldungen.Add($"FA {order.OrderNumber} ohne Stückliste – kein Kommissionierauftrag.");
            return r;
        }

        var hierarchisch = bom.DataSource == BomDataSources.FaHierarchie;
        var baugruppen = bom.Items.Where(b => !string.IsNullOrEmpty(b.Baugruppe)).Select(b => b.Baugruppe!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var soll = bom.Items
            .Where(b => !string.IsNullOrWhiteSpace(b.Ressourcenummer))
            .Where(b => !hierarchisch || KommissionierRelevanzFilter.IsRelevant((b as FaHierarchyBomItem)?.Kommissionieren))
            .Select(b => new PickingOrderRestmengen.Soll(b.Ressourcenummer!.Trim(), b.Position, BomQuantityResolver.Resolve(bom, b, order),
                b is FaHierarchyBomItem fh ? fh.IsBaugruppe : baugruppen.Contains(b.Ressourcenummer!),
                (b as FaHierarchyBomItem)?.Kommissionieren))
            .ToList();

        var bereiche = await _areas.GetAllAsync();
        var standard = bereiche.FirstOrDefault(a => a.IsDefault && a.IsActive);
        if (standard == null)
        {
            r.Fehler = "Kein aktiver Standardbereich – bitte unter Stammdaten → Kommissionierbereiche einen Standard festlegen.";
            return r;
        }
        var regeln = await _areas.GetRulesAsync();
        var artikel = await _repo.GetArtikelBereichsdatenAsync(soll.Select(s => s.ArticleNumber).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        var ps = await _status.GetByProductionOrderIdAsync(productionOrderId);

        var (ok, ergebnis) = await Nummernkreis.MitVersuchenAsync(MaxNummerVersuche, () => _repo.InSerializableTransactionAsync(async () =>
        {
            var lauf = new PickingOrderErzeugung();
            var reste = PickingOrderRestmengen.Berechne(soll, await _repo.GetAktiveZeilenAsync(productionOrderId));
            if (reste.Count == 0) { lauf.OhneRest = true; return lauf; }

            var jeBereich = new Dictionary<PickingArea, List<PickingOrderRestmengen.Rest>>();
            foreach (var rest in reste)
            {
                artikel.TryGetValue(rest.Position.ArticleNumber, out var a);
                var b = KommissionierbereichRegel.Resolve(
                    new(rest.Position.ArticleNumber, rest.Position.Kommissionierziel, a.Lagerort, a.Artikelgruppe), regeln, standard);
                if (b.Meldung != null && !lauf.Meldungen.Contains(b.Meldung)) lauf.Meldungen.Add(b.Meldung);
                (jeBereich.TryGetValue(b.Bereich, out var l) ? l : jeBereich[b.Bereich] = new()).Add(rest);
            }

            var now = Jetzt();
            var lfd = await _repo.GetMaxLaufendeNummerAsync(now.Year);
            foreach (var (bereich, zeilen) in jeBereich.OrderBy(kv => kv.Key.Reihenfolge).ThenBy(kv => kv.Key.Name))
            {
                lfd++;
                var po = new PickingOrder
                {
                    Jahr = now.Year, LaufendeNummer = lfd, Nummer = Nummernkreis.Format(Praefix, now.Year, lfd), Typ = PickingOrderType.FA,
                    ProductionOrderId = productionOrderId, PickingAreaId = bereich.Id, Status = PickingOrderStatus.Offen,
                    PickingPriority = ps?.PickingPriority, AssignedPickerId = ps?.AssignedPickerId, AssignedPickerName = ps?.AssignedPickerName,
                    CreatedAt = now, CreatedBy = _user.GetDisplayName(), CreatedByWindows = _user.GetWindowsUserName(),
                    Zeilen = zeilen.Select(z => new PickingOrderLine
                    {
                        ProductionOrderId = productionOrderId, ArticleNumber = z.Position.ArticleNumber, BomPosition = z.Position.BomPosition,
                        IsBaugruppe = z.Position.IsBaugruppe, SollMenge = z.Menge, Status = PickingOrderLineStatus.Offen,
                        CreatedAt = now, CreatedBy = _user.GetDisplayName(), CreatedByWindows = _user.GetWindowsUserName(),
                    }).ToList(),
                };
                _repo.Add(po);
                lauf.Nummern.Add(po.Nummer);
            }
            await _repo.SaveChangesAsync();
            return lauf;
        }), _repo.VerwerfeAenderungen, _logger, "Kommissionierauftrag anlegen");

        if (!ok)
        {
            r.Fehler = $"FA {order.OrderNumber}: Konnte nach {MaxNummerVersuche} Versuchen keine freie Auftragsnummer vergeben (gleichzeitige " +
                       "Freigabe oder Datenbankfehler, Details im Log) – bitte „Fehlende Kommissionieraufträge erzeugen“ erneut ausführen.";
            return r;
        }
        r.Nummern.AddRange(ergebnis!.Nummern);
        r.Meldungen.AddRange(ergebnis.Meldungen);
        r.OhneRest = ergebnis.OhneRest;
        return r;
    }

    /// <summary>Sammelmeldung fuer mehrere FAs (Leitstand-Knopf, Bulk-Freigabe): Erfolgs- und Warntext.</summary>
    public static (string Erfolg, string? Warnung) Sammelmeldung(IReadOnlyCollection<(string Fa, PickingOrderErzeugung E)> ergebnisse)
    {
        var angelegt = ergebnisse.SelectMany(e => e.E.Nummern).ToList();
        var faMitAuftrag = ergebnisse.Count(e => e.E.Nummern.Count > 0);
        var ohneRest = ergebnisse.Count(e => e.E.OhneRest);
        var erfolg = $"{angelegt.Count} Kommissionierauftrag/-aufträge für {faMitAuftrag} FA angelegt"
                     + (angelegt.Count > 0 ? $" ({string.Join(", ", angelegt)})" : "")
                     + (ohneRest > 0 ? $"; {ohneRest} FA ohne offene Restmenge." : ".");
        var warn = ergebnisse.SelectMany(e => e.E.Meldungen.Concat(e.E.Fehler != null ? new[] { e.E.Fehler } : Array.Empty<string>()))
            .Distinct().ToList();
        return (erfolg, warn.Count > 0 ? string.Join(" ", warn) : null);
    }
}
