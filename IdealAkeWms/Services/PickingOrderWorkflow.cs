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
    /// <summary>E3: nach dem Abschluss ist der FA vollstaendig kommissioniert (IsDonePicking gesetzt).</summary>
    public bool FaAbgeschlossen { get; set; }
    public string? Fehler { get; set; }
    /// <summary>v1.89.0 (Q11): FA uebersprungen, weil ein Altbestand-Auftrag noch „Altbestand pruefen“ traegt.</summary>
    public bool AltbestandPruefen { get; set; }
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
        // v1.89.0 (Entscheidung Q11): Altbestand mit Menge ≠ 1 erst nach Bestaetigung im Leitstand nachziehen (kein automatischer Rest).
        // Hier = einziger Erzeugungsweg (Freigabe, „Fehlende … erzeugen“, Oeffnen ohne Leitstand).
        if ((await _repo.GetByProductionOrderAsync(productionOrderId)).Any(o => o.AltbestandPruefen))
        {
            r.AltbestandPruefen = true;
            r.Meldungen.Add($"FA {order.OrderNumber}: Altbestand prüfen – im Leitstand „Altbestand geprüft“ bestätigen, dann Aufträge nachziehen.");
            return r;
        }

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

    /// <summary>
    /// E3: Auftrag abschliessen (nach der Umbuchung, ersetzt „teilkommissioniert/abgeschlossen“). „Vollstaendig“ → Abgeschlossen, nicht
    /// umgebuchte Zeilen Entfallen. „Teilweise“ → TeilweiseAbgeschlossen, Reste (nicht umgebuchte Zeilen ganz, umgebuchte mit Ist &lt; Soll
    /// die Differenz) in einen Folgeauftrag (gleicher Bereich, Prio, Kommissionierer, Bezug auf den Ursprung; Antwort 5); nicht umgebuchte
    /// Zeilen Uebertragen. Danach FA: kein Auftrag mehr offen/in Arbeit → IsDonePicking + „abgeschlossen“, sonst „teilkommissioniert“.
    /// </summary>
    public async Task<PickingOrderErzeugung> AbschliessenAsync(int pickingOrderId, bool vollstaendig)
    {
        var r = new PickingOrderErzeugung();
        var auftrag = await _repo.GetMitZeilenAsync(pickingOrderId);
        if (auftrag == null) { r.Fehler = "Kommissionierauftrag nicht gefunden."; return r; }
        if (auftrag.Status is not (PickingOrderStatus.Offen or PickingOrderStatus.InArbeit))
        {
            r.Fehler = $"Auftrag {auftrag.Nummer} ist bereits {Models.ViewModels.PickingOrderListItem.StatusText(auftrag.Status).ToLowerInvariant()}.";
            return r;
        }

        var (ok, ergebnis) = await Nummernkreis.MitVersuchenAsync(MaxNummerVersuche, () => _repo.InSerializableTransactionAsync(async () =>
        {
            var lauf = new PickingOrderErzeugung();
            // Je Versuch neu lesen: ein gescheiterter Versuch verwirft den Change-Tracker (Nummernkreis).
            auftrag = (await _repo.GetMitZeilenAsync(pickingOrderId))!;
            var now = Jetzt();
            var name = _user.GetDisplayName();
            var win = _user.GetWindowsUserName();
            void Audit(AuditableEntity e) { e.ModifiedAt = now; e.ModifiedBy = name; e.ModifiedByWindows = win; }

            var reste = new List<(PickingOrderLine Zeile, decimal Menge)>();
            foreach (var z in auftrag.Zeilen)
            {
                if (z.Status is PickingOrderLineStatus.Entfallen or PickingOrderLineStatus.Uebertragen) continue;
                var rest = z.Status == PickingOrderLineStatus.Umgebucht ? z.SollMenge - (z.IstMenge ?? 0m) : z.SollMenge;
                if (z.Status != PickingOrderLineStatus.Umgebucht)
                {
                    z.Status = vollstaendig ? PickingOrderLineStatus.Entfallen : PickingOrderLineStatus.Uebertragen;
                    Audit(z);
                }
                if (rest > 0m) reste.Add((z, rest));
            }

            if (vollstaendig || reste.Count == 0)
            {
                auftrag.Status = PickingOrderStatus.Abgeschlossen;
                if (!vollstaendig) lauf.Meldungen.Add($"Kein Rest offen – Auftrag {auftrag.Nummer} vollständig abgeschlossen.");
            }
            else
            {
                auftrag.Status = PickingOrderStatus.TeilweiseAbgeschlossen;
                var lfd = await _repo.GetMaxLaufendeNummerAsync(now.Year) + 1;
                var folge = new PickingOrder
                {
                    Jahr = now.Year, LaufendeNummer = lfd, Nummer = Nummernkreis.Format(Praefix, now.Year, lfd), Typ = auftrag.Typ,
                    ProductionOrderId = auftrag.ProductionOrderId, PickingAreaId = auftrag.PickingAreaId, Status = PickingOrderStatus.Offen,
                    PickingPriority = auftrag.PickingPriority, AssignedPickerId = auftrag.AssignedPickerId, AssignedPickerName = auftrag.AssignedPickerName,
                    UrsprungsauftragId = auftrag.Id, Bemerkung = $"Rest aus {auftrag.Nummer}",
                    CreatedAt = now, CreatedBy = name, CreatedByWindows = win,
                    Zeilen = reste.Select(x => new PickingOrderLine
                    {
                        ProductionOrderId = x.Zeile.ProductionOrderId, ArticleNumber = x.Zeile.ArticleNumber, BomPosition = x.Zeile.BomPosition,
                        IsBaugruppe = x.Zeile.IsBaugruppe, SollMenge = x.Menge, Status = PickingOrderLineStatus.Offen,
                        CreatedAt = now, CreatedBy = name, CreatedByWindows = win,
                    }).ToList(),
                };
                _repo.Add(folge);
                lauf.Nummern.Add(folge.Nummer);
            }
            auftrag.AbgeschlossenAm = now;
            auftrag.AbgeschlossenVon = name;
            Audit(auftrag);
            await _repo.SaveChangesAsync();
            return lauf;
        }), _repo.VerwerfeAenderungen, _logger, "Kommissionierauftrag abschliessen");

        if (!ok)
        {
            r.Fehler = $"Auftrag {auftrag.Nummer}: Abschluss nach {MaxNummerVersuche} Versuchen gescheitert (Details im Log) – bitte erneut versuchen.";
            return r;
        }
        r.Nummern.AddRange(ergebnis!.Nummern);
        r.Meldungen.AddRange(ergebnis.Meldungen);

        if (auftrag.ProductionOrderId is { } faId)
        {
            // FA-Status: gespeichert, nicht berechnet (vier weitere Schreiber, viele Leser im Dienst) – dieselben Methoden wie bisher.
            var offen = (await _repo.GetByProductionOrderAsync(faId))
                .Any(o => o.Status is PickingOrderStatus.Offen or PickingOrderStatus.InArbeit);
            await _status.SetPickingStatusTextAsync(faId, offen ? "teilkommissioniert" : "abgeschlossen", _user.GetDisplayName(), _user.GetWindowsUserName());
            if (!offen) await _status.SetIsDonePickingAsync(faId, true, _user.GetDisplayName(), _user.GetWindowsUserName());
            r.FaAbgeschlossen = !offen;
        }
        return r;
    }

    /// <summary>
    /// E4 (Antwort 4, AK 16): Freigabe zurueckgenommen – Auftraege des FA ohne gepickte/umgebuchte Zeile → Storniert; Auftraege mit Fortschritt
    /// bleiben und werden in der Warnung genannt (melden statt still behandeln).
    /// </summary>
    public async Task<(List<string> Storniert, List<string> Behalten)> FreigabeZuruecknehmenAsync(int productionOrderId)
    {
        var storniert = new List<string>();
        var behalten = new List<string>();
        var now = Jetzt();
        foreach (var a in (await _repo.GetByProductionOrderAsync(productionOrderId))
                     .Where(o => o.Status is PickingOrderStatus.Offen or PickingOrderStatus.InArbeit))
        {
            if (a.Zeilen.Any(z => z.Status is PickingOrderLineStatus.Gepickt or PickingOrderLineStatus.Umgebucht))
            {
                behalten.Add(a.Nummer);
                continue;
            }
            a.Status = PickingOrderStatus.Storniert;
            AuditSetzen(a, now);
            storniert.Add(a.Nummer);
        }
        if (storniert.Count > 0) await _repo.SaveChangesAsync();
        return (storniert, behalten);
    }

    /// <summary>E4 (Antwort 6, AK 17): Prioritaet am FA → alle offenen/in Arbeit befindlichen Auftraege des FA.</summary>
    public Task<int> PrioritaetDurchschreibenAsync(int productionOrderId, int? prioritaet) =>
        DurchschreibenAsync(productionOrderId, a => a.PickingPriority = prioritaet);

    /// <summary>E4 (Antwort 6, AK 17): Kommissionierer am FA → alle offenen/in Arbeit befindlichen Auftraege des FA.</summary>
    public Task<int> KommissioniererDurchschreibenAsync(int productionOrderId, int? pickerId, string? pickerName) =>
        DurchschreibenAsync(productionOrderId, a => { a.AssignedPickerId = pickerId; a.AssignedPickerName = pickerName; });

    /// <summary>v1.89.0 (Q11): Leitstand bestaetigt „Altbestand geprueft“ – Kennzeichen an allen Auftraegen des FA loeschen; Rueckgabe =
    /// Nummern. Danach erzeugt <see cref="ErzeugeOffeneAsync"/> fehlende Auftraege wie gewohnt.</summary>
    public async Task<List<string>> AltbestandGeprueftAsync(int productionOrderId)
    {
        var now = Jetzt();
        var markiert = (await _repo.GetByProductionOrderAsync(productionOrderId)).Where(o => o.AltbestandPruefen).ToList();
        foreach (var a in markiert)
        {
            a.AltbestandPruefen = false;
            AuditSetzen(a, now);
        }
        if (markiert.Count > 0) await _repo.SaveChangesAsync();
        return markiert.Select(a => a.Nummer).ToList();
    }

    private async Task<int> DurchschreibenAsync(int productionOrderId, Action<PickingOrder> setzen)
    {
        var now = Jetzt();
        var offen = (await _repo.GetByProductionOrderAsync(productionOrderId))
            .Where(o => o.Status is PickingOrderStatus.Offen or PickingOrderStatus.InArbeit).ToList();
        foreach (var a in offen)
        {
            setzen(a);
            AuditSetzen(a, now);
        }
        if (offen.Count > 0) await _repo.SaveChangesAsync();
        return offen.Count;
    }

    private void AuditSetzen(AuditableEntity e, DateTime now)
    {
        e.ModifiedAt = now;
        e.ModifiedBy = _user.GetDisplayName();
        e.ModifiedByWindows = _user.GetWindowsUserName();
    }

    /// <summary>Lesend: Auftraege je FA (Leitstand-Zeile).</summary>
    public Task<Dictionary<int, List<PickingOrder>>> AuftraegeJeFaAsync(IReadOnlyCollection<int> faIds) => _repo.GetByProductionOrdersAsync(faIds);

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
