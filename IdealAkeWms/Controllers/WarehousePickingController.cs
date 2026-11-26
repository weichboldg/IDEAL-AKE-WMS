using System.Globalization;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Controllers;

[RequireLagerProcessingAccess]
[RequireLagerbestellungAktiv]
public class WarehousePickingController : Controller
{
    private readonly IWarehouseRequisitionRepository _repo;
    private readonly IProductionWorkplaceRepository _workplaces;
    private readonly IStockMovementRepository _stock;
    private readonly ICurrentUserService _user;
    private readonly IUserViewPreferenceRepository _viewPrefs;
    private readonly IArticleRepository _articles;
    private readonly IStorageLocationRepository _locations;
    private readonly IAppSettingRepository _settings;
    private readonly IServiceSettingRepository _serviceSettings;
    private readonly ApplicationDbContext _db;
    private readonly ISagePurchaseOrderLineRepository _poLines;
    private readonly IPurchaseOrderReceiptRepository _receipts;

    public WarehousePickingController(
        IWarehouseRequisitionRepository repo,
        IProductionWorkplaceRepository workplaces,
        IStockMovementRepository stock,
        ICurrentUserService user,
        IUserViewPreferenceRepository viewPrefs,
        IArticleRepository articles,
        IStorageLocationRepository locations,
        IAppSettingRepository settings,
        IServiceSettingRepository serviceSettings,
        ApplicationDbContext db, // v1.99.53: nur fuer die Transaktion Abschluss + Ausbuchungen (wie BdeBookingsController)
        ISagePurchaseOrderLineRepository poLines,
        IPurchaseOrderReceiptRepository receipts)
    {
        _repo = repo;
        _workplaces = workplaces;
        _stock = stock;
        _user = user;
        _viewPrefs = viewPrefs;
        _articles = articles;
        _locations = locations;
        _settings = settings;
        _serviceSettings = serviceSettings;
        _db = db;
        _poLines = poLines;
        _receipts = receipts;
    }

    /// <summary>
    /// Server-Side-Spaltenfilter: Col-Key (data-col-key der View) -> gerenderter Zell-Text.
    /// Die Getter MUESSEN exakt das liefern, was die View in der Zelle rendert
    /// (deutsche Status-Badge-Texte, Datum im View-Format, "—" fuer leer).
    /// </summary>
    private static readonly Dictionary<string, Func<WarehouseRequisitionListItemViewModel, string?>> ColumnMap = new()
    {
        ["id"] = r => r.Id.ToString(),
        ["workplace"] = r => r.WorkplaceName,
        ["creator"] = r => r.CreatedBy,
        ["submitted"] = r => r.SubmittedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—",
        ["comment"] = r => string.IsNullOrWhiteSpace(r.Comment) ? "—" : r.Comment,
        ["items"] = r => r.ItemCount.ToString(),
        ["status"] = r => r.Status switch
        {
            WarehouseRequisitionStatus.Submitted => "Abgeschickt",
            WarehouseRequisitionStatus.PartiallyDelivered => "Teilgeliefert",
            WarehouseRequisitionStatus.Closed => "Erledigt",
            WarehouseRequisitionStatus.Cancelled => "Storniert",
            _ => r.Status.ToString()
        },
    };

    public async Task<IActionResult> Index(WarehouseRequisitionStatus? statusFilter, int? workplaceId,
        WarehouseRequisitionType type = WarehouseRequisitionType.Lager, int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        // Geforgte ?type=99-URLs: lesend still auf Lager korrigieren (CreateDraft lehnt schreibend ab).
        if (!Enum.IsDefined(type)) type = WarehouseRequisitionType.Lager;
        var userDefaultPageSize = await _user.GetDefaultPageSizeAsync();
        var effectivePageSize = IdealAkeWms.Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = IdealAkeWms.Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var statusList = statusFilter.HasValue
            ? new[] { statusFilter.Value }
            : new[] { WarehouseRequisitionStatus.Submitted, WarehouseRequisitionStatus.PartiallyDelivered };

        // Server-Side-Spaltenfilter: ALLE Rows laden -> ViewModel -> filtern -> zaehlen -> paginieren.
        // (Filter muss ueber alle Eintraege wirken, nicht nur die aktuelle Seite.)
        var (allRows, _) = await _repo.GetForWarehouseAsync(statusList, workplaceId, type, 1, int.MaxValue);
        var allItems = allRows.Select(r => new WarehouseRequisitionListItemViewModel(
            r.Id, r.ProductionWorkplace?.Name ?? "", r.CreatedBy, r.CreatedAt,
            r.SubmittedAt, r.Items.Count, r.Status, r.Comment)).ToList();

        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var filtered = ColumnFilterHelper.Apply(allItems, columnFilters, ColumnMap, ColumnFilterTypes.For("WarehousePicking/Index")).ToList();
        var totalCount = filtered.Count;
        var pagedItems = filtered.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList();

        var allWorkplaces = await _workplaces.GetAllAsync();
        var openCount = (await _repo.GetForWarehouseAsync(
            new[] { WarehouseRequisitionStatus.Submitted, WarehouseRequisitionStatus.PartiallyDelivered },
            null, type, 1, 1)).TotalCount;

        var vm = new WarehouseRequisitionListViewModel
        {
            Items = pagedItems,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = effectivePageSize,
            StatusFilter = statusFilter,
            WorkplaceFilter = workplaceId,
            ActiveType = type,
            CanOrderLager = true,
            CanOrderGlas = true,
            AvailableWorkplaces = allWorkplaces.OrderBy(w => w.Name).ToList(),
            OpenCount = openCount,
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = totalCount
            }
        };
        return View(vm);
    }

    /// <summary>Bestand nur des Positionsartikels (SQL-Gleichheit statt Teiltext; ohne Kommissionierwagen, v1.99.45). Es wird genau
    /// eine Nummer abgefragt, daher gehören alle Einträge zu ihr – kein Nachschlagen per Schlüssel (DB-Schreibweise kann abweichen).
    /// Dieselbe Liste speist Lagerplatz-Text und Entnahme-Vorschlag (v1.99.53).</summary>
    private async Task<List<StockLocationInfo>> BestandAsync(string? articleNumber)
    {
        if (string.IsNullOrWhiteSpace(articleNumber)) return new();
        var dict = await _stock.GetStockByArticleNumbersAsync(new List<string> { articleNumber.Trim() }, mitKommissionierwagen: false);
        return dict.Values.SelectMany(x => x).ToList();
    }

    /// <summary>v1.99.53: Artikel-Id fuer die Verbrauchsbuchung; DUMMY und Nummern ohne Artikel im WMS = null (wird nie gebucht, Antwort 7).</summary>
    private async Task<int?> LagerartikelIdAsync(string? articleNumber)
    {
        if (string.IsNullOrWhiteSpace(articleNumber)
            || articleNumber.Trim().Equals(Article.DummyArticleNumber, StringComparison.OrdinalIgnoreCase)) return null;
        return (await _articles.GetByArticleNumberAsync(articleNumber.Trim()))?.Id;
    }

    private async Task<bool> VerbrauchsbuchungAktivAsync() =>
        (await _settings.GetValueAsync(AppSettingKeys.LagerbestellungVerbrauchsbuchungAktiv))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>v1.99.53 (F1/F3, Antwort 10): waehlbare Entnahme-Plaetze = buchbar wie Kommissionierung/Warenausgang, ohne Sage-Plaetze,
    /// deren Entnahme nicht an Sage ginge – sonst bucht der Lagerbestand-Abgleich sie zurueck.</summary>
    private async Task<List<StorageLocation>> EntnahmePlaetzeAsync()
    {
        // Lesart wie der Sage-Decorator (SageBookingEnqueueingStockMovementRepository.ParseBool): nur "true", ohne Trim.
        var sageAktiv = (await _serviceSettings.GetValueAsync("SageLagerbuchungAktiv"))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        return (await _locations.GetActiveOrderedExcludingPickingTransportAsync()).Where(l => EntnahmeErlaubt(l, sageAktiv)).ToList();
    }

    /// <summary>Reiner WMS-Platz immer; Sage-Platz nur, wenn die Entnahme an Sage geht (Schalter an, „Sage-Buchung erlaubt“, Sage-Referenz).</summary>
    internal static bool EntnahmeErlaubt(StorageLocation l, bool sageLagerbuchungAktiv) =>
        l.Source != StorageLocationSource.Sage || (sageLagerbuchungAktiv && SageWareneingang.IstSageWePlatz(l));

    /// <summary>Auswahl je Zeile: nur waehlbare Plaetze mit Bestand des Artikels plus NAN. Ein Platz ohne Bestand wird beim Abschliessen
    /// ohnehin abgewiesen bzw. vom Ausweichplatz gebucht (<see cref="AusbuchungBestandsregel"/>). Vorher stand JEDER waehlbare Platz in
    /// JEDER Zeile – mit Sage-Lagerbuchung an (AKE ~3300 Plaetze) wurde die Seite dadurch mehrere MB gross und extrem langsam.
    /// Bleibt nichts uebrig (kein Bestand, NAN nicht waehlbar), volle Liste wie bisher.</summary>
    internal static List<StorageLocation> EntnahmeOptionen(List<StorageLocation> plaetze, IEnumerable<StockLocationInfo> bestand, int? nanId)
    {
        var mitBestand = bestand.Where(b => b.Quantity > 0).Select(b => b.StorageLocationId).ToHashSet();
        var optionen = plaetze.Where(p => p.Id == nanId || mitBestand.Contains(p.Id)).ToList();
        return optionen.Count > 0 ? optionen : plaetze;
    }

    /// <summary>Hinweis zum Vorschlag, aus dem Ergebnis von <see cref="QuellLagerplatzVorschlag.Waehle"/> abgeleitet (Regel unveraendert).</summary>
    /// <remarks>v1.99.57 (Spec 2026-11-23 Teil 4, Variante c): Liegt Bestand nur auf nicht waehlbaren Plaetzen (v. a. Sage-Plaetze ohne
    /// Sage-Buchung), nennt der Hinweis diese Plaetze statt „Kein Bestand“ – die Lagerplatz-Spalte derselben Zeile zeigt den Bestand ja.</remarks>
    internal static string VorschlagHinweis(int? vorschlagId, IEnumerable<StockLocationInfo> bestand, string? nanCode, ISet<int>? erlaubteIds = null)
    {
        if (vorschlagId == null) return "Kein Vorschlag – bitte Platz wählen";
        var platz = bestand.FirstOrDefault(b => b.StorageLocationId == vorschlagId && b.Quantity > 0);
        if (platz == null)
        {
            var gesperrt = bestand.Where(b => b.Quantity > 0 && erlaubteIds != null && !erlaubteIds.Contains(b.StorageLocationId))
                .OrderByDescending(b => b.Quantity).ThenBy(b => b.Code).ToList();
            if (gesperrt.Count == 0) return $"Kein Bestand – Vorschlag {nanCode}";
            var liste = string.Join(", ", gesperrt.Take(3).Select(b => $"{b.Code}: {b.Quantity:0.###}")) +
                        (gesperrt.Count > 3 ? $" +{gesperrt.Count - 3}" : "");
            return $"Bestand nur auf nicht wählbaren Plätzen ({liste}) – Vorschlag {nanCode}. " +
                   "Sage-Plätze sind nur mit Sage-Lagerbuchung wählbar (Schalter aus bzw. am Platz nicht erlaubt).";
        }
        return platz.IsPrimaryStorageLocation ? "Vorschlag: Hauptlagerplatz" : "Vorschlag: Platz mit dem meisten Bestand";
    }

    private static bool IstOffen(WarehouseRequisitionStatus s) =>
        s is WarehouseRequisitionStatus.Submitted or WarehouseRequisitionStatus.PartiallyDelivered;

    private static string StatusText(WarehouseRequisitionStatus s) => s switch
    {
        WarehouseRequisitionStatus.Draft => "ein Entwurf",
        WarehouseRequisitionStatus.Closed => "erledigt",
        WarehouseRequisitionStatus.Cancelled => "storniert",
        _ => s.ToString()
    };

    public async Task<IActionResult> Details(int id)
    {
        var r = await _repo.GetByIdAsync(id);
        if (r == null || r.Status == WarehouseRequisitionStatus.Draft) return NotFound();

        var aktiv = await VerbrauchsbuchungAktivAsync();
        var mitAuswahl = aktiv && IstOffen(r.Status);
        var plaetze = mitAuswahl ? await EntnahmePlaetzeAsync() : new List<StorageLocation>();
        var erlaubteIds = plaetze.Select(p => p.Id).ToHashSet();
        var nan = mitAuswahl ? await _locations.GetByCodeAsync("NAN") : null;
        var nanId = nan != null && erlaubteIds.Contains(nan.Id) ? nan.Id : (int?)null; // NAN nur, wenn waehlbar (wie Kommissionierung: buchbar)
        var gebucht = aktiv ? await _stock.GetAusgebuchtJeLagerbestellPositionAsync(r.Items.Select(i => i.Id).ToList()) : new();

        var detailItems = new List<WarehouseRequisitionDetailItemViewModel>();
        // ponytail: je Position eine Bestands- und eine Artikelabfrage; Batch-Abfrage erst bei messbar langen Listen.
        foreach (var i in r.Items.OrderBy(x => x.Position))
        {
            var bestand = await BestandAsync(i.ArticleNumber);
            var istLagerartikel = !aktiv || await LagerartikelIdAsync(i.ArticleNumber) != null;
            int? vorschlag = null;
            string? hinweis = null;
            List<StorageLocation>? optionen = null;
            if (mitAuswahl && istLagerartikel)
            {
                vorschlag = QuellLagerplatzVorschlag.Waehle(bestand, erlaubteIds, nanId);
                hinweis = VorschlagHinweis(vorschlag, bestand, nan?.Code, erlaubteIds);
                optionen = EntnahmeOptionen(plaetze, bestand, nanId);
            }
            detailItems.Add(new WarehouseRequisitionDetailItemViewModel(
                i.Id, i.Position, i.ArticleNumber, i.ArticleDescription, i.Unit,
                i.QuantityRequested, i.QuantityPicked, StockLocationInfo.LagerplatzText(bestand), i.Note, i.ShortageStatus, i.NoteEinkauf,
                istLagerartikel, vorschlag, hinweis, gebucht.GetValueOrDefault(i.Id), optionen));
        }

        var vm = new WarehouseRequisitionDetailViewModel
        {
            Id = r.Id,
            WorkplaceName = r.ProductionWorkplace?.Name ?? "",
            CreatedBy = r.CreatedBy,
            SubmittedAt = r.SubmittedAt,
            ClosedAt = r.ClosedAt,
            CancelledAt = r.CancelledAt,
            CancellationReason = r.CancellationReason,
            Comment = r.Comment,
            Status = r.Status,
            Type = r.Type,
            RowVersion = r.RowVersion,
            Items = detailItems,
            VerbrauchsbuchungAktiv = aktiv,
            FraesenPfade = await RequireFraesenPfadAktivFilter.IstAktivAsync(_settings) ? await _articles.GetFraesenPfadeAsync() : null,
        };
        // v1.99.85 (Spec 2026-11-26-liefertermin-offene-bestellungen F2): naechster Liefertermin je Artikel, feste Zahl Abfragen
        // (Stand, Positionen, Erfasst) unabhaengig von der Positionszahl. Cache leer = null = keine Spalte.
        (vm.LieferterminStand, vm.Liefertermine) = await LieferterminTexte.JeArtikelAsync(_poLines, _receipts, r.Items.Select(i => i.ArticleNumber));
        if (vm.Liefertermine != null)
            vm.ArtikelinfoLink = await _user.HasMasterDataReadAccessAsync();
        return View(vm);
    }

    /// <summary>Eingaben je Position (Index-gleich zu <paramref name="itemIds"/>). Leeres Ist-Feld bindet als null am selben Index
    /// (NullableIntArrayBindingTests) — nie als 0.</summary>
    private static (Dictionary<int, decimal?> Qty, Dictionary<int, string?> Notes, Dictionary<int, string?> NotesEk, Dictionary<int, ShortageStatus> Status)
        Eingaben(int[] itemIds, int?[]? quantitiesPicked, string?[]? notes, string?[]? notesEinkauf, int[]? shortageStatuses)
    {
        var qtyDict = new Dictionary<int, decimal?>();
        if (quantitiesPicked != null)
            for (int idx = 0; idx < itemIds.Length; idx++)
                qtyDict[itemIds[idx]] = idx < quantitiesPicked.Length ? quantitiesPicked[idx] : null;
        var noteDict = new Dictionary<int, string?>();
        if (notes != null)
            for (int idx = 0; idx < itemIds.Length; idx++)
                noteDict[itemIds[idx]] = idx < notes.Length ? notes[idx] : null;
        var noteEkDict = new Dictionary<int, string?>();
        if (notesEinkauf != null)
            for (int idx = 0; idx < itemIds.Length; idx++)
                noteEkDict[itemIds[idx]] = idx < notesEinkauf.Length ? notesEinkauf[idx] : null;
        var statusDict = new Dictionary<int, ShortageStatus>();
        if (shortageStatuses != null)
        {
            for (int idx = 0; idx < itemIds.Length; idx++)
            {
                var raw = idx < shortageStatuses.Length ? shortageStatuses[idx] : 0;
                statusDict[itemIds[idx]] = raw switch
                {
                    1 => ShortageStatus.WillBeRestocked,
                    2 => ShortageStatus.NoRestock,
                    _ => ShortageStatus.None
                };
            }
        }
        return (qtyDict, noteDict, noteEkDict, statusDict);
    }

    /// <summary>Ergebnis des gemeinsamen Abschlusses: <c>Fehler</c> gesetzt = nichts abgeschlossen, nichts gebucht.</summary>
    private sealed record Abschluss(string? Fehler, bool Konflikt = false, string? Erfolg = null, string? Warnung = null,
        WarehouseRequisitionType Typ = WarehouseRequisitionType.Lager);

    private sealed record Buchung(WarehouseRequisitionItem Item, int ArticleId, decimal Menge, int PlatzId, string PlatzCode);

    /// <summary>Gemeinsamer Ablauf von <see cref="Close"/> und <see cref="PrintAndClose"/> (v1.99.53): Eingabepruefung → Status-Guard (F4)
    /// → bei Schalter an Verbrauchsbuchung planen (Δ = Ist − schon ausgebucht, Platz, Sage-Platz-Regel, Bestand) → in EINER Transaktion
    /// Abschluss (inkl. Pflichtpruefung) und Ausbuchungen. Alles oder nichts; Fehler als Sammelmeldung.</summary>
    private async Task<Abschluss> AbschliessenAsync(int id, int[] itemIds, int?[] quantitiesPicked, string?[]? notes,
        string?[]? notesEinkauf, int[]? shortageStatuses, int?[]? storageLocationIds, byte[] rowVersion)
    {
        // Ein nicht ganzzahliger Wert (z. B. 1.5) wird vom Binder verworfen statt als null gebunden — das
        // Array wird kuerzer, alle Folgewerte verrutschen (NullableIntArrayBindingTests). Dann nichts buchen.
        if (!ModelState.IsValid || quantitiesPicked.Length != itemIds.Length)
            return new(InvalidQuantityMessage);
        if (quantitiesPicked.Any(q => q < 0)) // null < 0 ist false: leere Felder faengt die Pflichtpruefung
            return new("Ist-Mengen duerfen nicht negativ sein.");

        var (qtyDict, noteDict, noteEkDict, statusDict) = Eingaben(itemIds, quantitiesPicked, notes, notesEinkauf, shortageStatuses);
        Task SichernAsync() => _repo.SaveProgressAsync(id, qtyDict, noteDict, noteEkDict, statusDict,
            _user.GetDisplayName(), _user.GetWindowsUserName());

        // F4: an den Abschluss haengt eine Buchung – nur abgeschickte oder teilgelieferte Listen (gilt auch bei Schalter aus).
        var r = await _repo.GetByIdAsync(id);
        if (r == null) return new($"Liste #{id} nicht gefunden – nichts gebucht.");
        if (!IstOffen(r.Status)) return new($"Liste #{id} ist {StatusText(r.Status)} – nichts gebucht.", Typ: r.Type);

        var plan = new List<Buchung>();
        var warnungen = new List<string>();
        var nichtGebucht = new List<string>();
        if (await VerbrauchsbuchungAktivAsync())
        {
            // Leerer Select bzw. das Hidden-Feld einer Zeile ohne Auswahl bindet als null am selben Index; andere Laenge = Seite vor dem
            // Einschalten geladen oder manipuliert.
            if (storageLocationIds == null || storageLocationIds.Length != itemIds.Length)
            {
                await SichernAsync();
                return new("Nicht abgeschlossen – nichts gebucht: Die Entnahme-Plätze fehlen oder passen nicht zu den Positionen. Bitte Liste neu laden.", Typ: r.Type);
            }
            var platzJeItem = new Dictionary<int, int?>();
            for (int idx = 0; idx < itemIds.Length; idx++) platzJeItem[itemIds[idx]] = storageLocationIds[idx];
            var erlaubt = (await EntnahmePlaetzeAsync()).ToDictionary(p => p.Id);
            var gebucht = await _stock.GetAusgebuchtJeLagerbestellPositionAsync(r.Items.Select(i => i.Id).ToList());

            var fehler = new List<string>();
            foreach (var item in r.Items.OrderBy(i => i.Position))
            {
                // Leeres Ist (nur mit Fehlteil zulaessig) = 0 geliefert.
                var ist = qtyDict.TryGetValue(item.Id, out var q) ? q ?? 0m : 0m;
                var articleId = await LagerartikelIdAsync(item.ArticleNumber);
                if (articleId == null)
                {
                    if (ist > 0) nichtGebucht.Add($"Pos {item.Position} ({item.ArticleNumber})");
                    continue;
                }
                var schon = gebucht.GetValueOrDefault(item.Id);
                var delta = ist - schon;
                if (delta < 0)
                {
                    fehler.Add($"Pos {item.Position}: Ist {ist:0.###} kleiner als bereits ausgebucht {schon:0.###} – Korrektur über Wareneingang/Umbuchung");
                    continue;
                }
                if (delta == 0) continue;

                var platzId = platzJeItem.GetValueOrDefault(item.Id);
                if (platzId == null)
                {
                    fehler.Add($"Pos {item.Position}: kein Entnahme-Platz gewählt");
                    continue;
                }
                if (!erlaubt.TryGetValue(platzId.Value, out var platz))
                {
                    var l = await _locations.GetByIdAsync(platzId.Value);
                    fehler.Add(l?.Source == StorageLocationSource.Sage
                        ? $"Pos {item.Position}: {l.Code} ist ein Sage-Platz ohne Sage-Buchung – der Lagerbestand-Abgleich würde die Entnahme zurückbuchen. Bitte WMS-Platz wählen oder Sage-Buchung am Platz einschalten."
                        : $"Pos {item.Position}: Platz {l?.Code ?? "#" + platzId} ist als Entnahme-Platz nicht wählbar");
                    continue;
                }
                // QA v1.99.53: derselbe Artikel kann in mehreren Positionen vom selben Platz kommen – die schon geplanten Mengen zaehlen mit,
                // sonst bestuende jede Zeile die Pruefung einzeln und zusammen ginge der Platz ohne NegativeBuchungErlaubt ins Minus.
                var geplant = plan.Where(b => b.ArticleId == articleId.Value && b.PlatzId == platz.Id).Sum(b => b.Menge);
                var bestand = await AusbuchungBestandsregel.PruefeAsync(_stock, _settings, _locations, articleId.Value, platz.Id, delta + geplant);
                if (bestand.Abgewiesen)
                {
                    fehler.Add($"Pos {item.Position}: nicht genug Bestand auf {platz.Code}, verfügbar {bestand.Verfuegbar - geplant:N3}");
                    continue;
                }
                if (bestand.Warnung != null) warnungen.Add($"Pos {item.Position}: {bestand.Warnung}");
                plan.Add(new Buchung(item, articleId.Value, delta, bestand.StorageLocationId, bestand.AusweichCode ?? platz.Code));
            }
            if (fehler.Count > 0)
            {
                await SichernAsync();
                return new("Nicht abgeschlossen – nichts gebucht: " + string.Join("; ", fehler), Typ: r.Type);
            }
        }

        // Abschluss und Ausbuchungen in EINER Transaktion (Muster BdeBookingsController): ein RowVersion-Konflikt am Kopf rollt alles zurueck,
        // zwei parallele Abschluesse buchen nicht doppelt. InMemory kennt keine Transaktion (Manual-UAT TS-178.8).
        IReadOnlyList<(int Position, string ArticleNumber)> incomplete;
        await using (var tx = _db.Database.IsRelational() ? await _db.Database.BeginTransactionAsync() : null)
        {
            try
            {
                incomplete = await _repo.CloseAsync(id, qtyDict, noteDict, noteEkDict, statusDict,
                    _user.GetCurrentAppUserId() ?? 0,
                    _user.GetDisplayName(), _user.GetWindowsUserName(), rowVersion);
            }
            catch (DbUpdateConcurrencyException)
            {
                return new("Bestellung wurde inzwischen geaendert", Konflikt: true, Typ: r.Type);
            }
            if (incomplete.Count == 0)
            {
                var belegText = r.Type == WarehouseRequisitionType.Glas ? "Glasbestellung" : "Lagerbestellung";
                var werkbank = r.ProductionWorkplace?.Name;
                foreach (var b in plan)
                {
                    var note = $"{belegText} #{r.Id} Pos {b.Item.Position}" + (string.IsNullOrWhiteSpace(werkbank) ? "" : $" – {werkbank}");
                    // Ueber das Interface = Sage-Decorator: Entnahme an Sage genau dann, wenn SageLagerbuchungAktiv und Platz-Flag (F3).
                    await _stock.AddAsync(new StockMovement
                    {
                        ArticleId = b.ArticleId,
                        Quantity = b.Menge,
                        StorageLocationId = b.PlatzId,
                        MovementType = MovementType.Ausbuchung,
                        WarehouseRequisitionItemId = b.Item.Id,
                        Note = note.Length > 500 ? note[..500] : note,
                        Timestamp = DateTime.Now,
                        UserId = _user.GetCurrentAppUserId(),
                        WindowsUser = _user.GetWindowsUserName(),
                        CreatedAt = DateTime.Now,
                        CreatedBy = _user.GetDisplayName(),
                        CreatedByWindows = _user.GetWindowsUserName()
                    });
                }
                if (tx != null) await tx.CommitAsync();
            }
        }
        if (incomplete.Count > 0)
        {
            // Eingaben nicht verlieren: als Zwischenstand sichern (wie der Autosave; leer bleibt NULL).
            await SichernAsync();
            return new(IncompleteMessage(incomplete), Typ: r.Type);
        }

        var erfolg = $"Liste #{id} abgeschlossen.";
        if (plan.Count > 0)
            erfolg += $" {plan.Count} Position(en) ausgebucht: " + string.Join(", ", plan.Select(b =>
                $"Pos {b.Item.Position} {b.Menge:0.###}{(string.IsNullOrWhiteSpace(b.Item.Unit) ? "" : " " + b.Item.Unit)} von {b.PlatzCode}")) + ".";
        if (nichtGebucht.Count > 0)
            erfolg += $" {nichtGebucht.Count} Position(en) nicht gebucht (kein Lagerartikel/DUMMY): {string.Join(", ", nichtGebucht)}.";
        return new(null, Erfolg: erfolg, Warnung: warnungen.Count > 0 ? string.Join(" ", warnungen) : null, Typ: r.Type);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id, int[] itemIds, int?[] quantitiesPicked,
        string?[]? notes, string?[]? notesEinkauf, int[]? shortageStatuses, byte[] rowVersion, int?[]? storageLocationIds = null)
    {
        var a = await AbschliessenAsync(id, itemIds, quantitiesPicked, notes, notesEinkauf, shortageStatuses, storageLocationIds, rowVersion);
        if (a.Fehler != null)
        {
            TempData["WarningMessage"] = a.Konflikt ? "Bestellung wurde inzwischen geaendert — bitte Liste neu laden." : a.Fehler;
            return RedirectToAction(nameof(Details), new { id });
        }
        TempData["SuccessMessage"] = a.Erfolg;
        if (a.Warnung != null) TempData["WarningMessage"] = a.Warnung;
        return RedirectToAction(nameof(Index), new { type = a.Typ });
    }

    /// <summary>
    /// AJAX-Endpoint: speichert nur die Notizen der Positionen (Autosave).
    /// Wird vom Details-View beim Blur des Notiz-Inputs und vor "Drucken" aufgerufen.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveNotes(int id, [FromForm] int[] itemIds, [FromForm] string?[]? notes)
    {
        if (itemIds == null || itemIds.Length == 0)
            return BadRequest("itemIds required");

        var noteDict = new Dictionary<int, string?>();
        for (int idx = 0; idx < itemIds.Length; idx++)
            noteDict[itemIds[idx]] = notes != null && idx < notes.Length ? notes[idx] : null;

        await _repo.SaveNotesAsync(id, noteDict,
            _user.GetDisplayName(), _user.GetWindowsUserName());

        return Ok();
    }

    /// <summary>Autosave (Zwischenstand). Bucht nie (v1.99.53, AK 18); Entnahme-Plaetze werden nicht gespeichert.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProgress(int id,
        [FromForm] int[] itemIds,
        [FromForm] int?[]? quantitiesPicked,
        [FromForm] string?[]? notes,
        [FromForm] string?[]? notesEinkauf,
        [FromForm] int[]? shortageStatuses)
    {
        if (itemIds == null || itemIds.Length == 0) return BadRequest("itemIds required");

        var (qtyDict, noteDict, noteEkDict, statusDict) = Eingaben(itemIds, quantitiesPicked, notes, notesEinkauf, shortageStatuses);
        await _repo.SaveProgressAsync(id, qtyDict, noteDict, noteEkDict, statusDict,
            _user.GetDisplayName(), _user.GetWindowsUserName());
        return Ok();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PrintAndClose(int id, int[] itemIds, int?[] quantitiesPicked,
        string?[]? notes, string?[]? notesEinkauf, int[]? shortageStatuses, byte[] rowVersion, int?[]? storageLocationIds = null)
    {
        var a = await AbschliessenAsync(id, itemIds, quantitiesPicked, notes, notesEinkauf, shortageStatuses, storageLocationIds, rowVersion);
        if (a.Konflikt) return Conflict(new { error = "Bestellung wurde inzwischen geaendert." });
        if (a.Fehler != null) return BadRequest(new { error = a.Fehler });
        // Die Seite springt danach auf die Liste (Index) und zeigt die Meldungen dort; der Druck (Layout = null) liest kein TempData.
        TempData["SuccessMessage"] = a.Erfolg;
        if (a.Warnung != null) TempData["WarningMessage"] = a.Warnung;
        return Ok(new { redirectUrl = Url.Action(nameof(Print), new { id }) });
    }

    private const string InvalidQuantityMessage =
        "Nicht abgeschlossen — Ist-Mengen muessen ganze Zahlen ab 0 sein. Bitte Eingaben pruefen.";

    private static string IncompleteMessage(IReadOnlyList<(int Position, string ArticleNumber)> incomplete) =>
        "Nicht abgeschlossen — Ist-Menge oder Fehlteil-Markierung fehlt bei: "
        + string.Join(", ", incomplete.Select(x => $"Pos {x.Position} ({x.ArticleNumber})"));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason, byte[] rowVersion)
    {
        try
        {
            await _repo.CancelAsync(id, reason, _user.GetCurrentAppUserId() ?? 0,
                _user.GetDisplayName(), _user.GetWindowsUserName(), rowVersion);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            TempData["WarningMessage"] = "Bestellung wurde inzwischen geaendert — bitte Liste neu laden.";
            return RedirectToAction(nameof(Details), new { id });
        }
        TempData["SuccessMessage"] = $"Liste #{id} storniert.";
        var cancelled = await _repo.GetByIdAsync(id, includeItems: false);
        return RedirectToAction(nameof(Index), new { type = cancelled?.Type ?? WarehouseRequisitionType.Lager });
    }

    public async Task<IActionResult> Print(int id, string? sortCol = null, string? sortDir = null)
    {
        var r = await _repo.GetByIdAsync(id);
        if (r == null || r.Status == WarehouseRequisitionStatus.Draft) return NotFound();

        var detailItems = new List<WarehouseRequisitionDetailItemViewModel>();
        foreach (var i in r.Items.OrderBy(x => x.Position))
        {
            var locationStr = StockLocationInfo.LagerplatzText(await BestandAsync(i.ArticleNumber));
            detailItems.Add(new WarehouseRequisitionDetailItemViewModel(
                i.Id, i.Position, i.ArticleNumber, i.ArticleDescription, i.Unit,
                i.QuantityRequested, i.QuantityPicked, locationStr, i.Note, i.ShortageStatus, i.NoteEinkauf));
        }

        // Spalten-Preferences (Sichtbarkeit/Reihenfolge + konfigurierter Default-Sort) des Users lesen.
        WarehousePickingPrintLayout.PrintPrefs? prefs = null;
        var userId = _user.GetCurrentAppUserId();
        if (userId.HasValue)
        {
            var pref = await _viewPrefs.GetByUserAndViewAsync(userId.Value, "WarehousePickingDetails");
            prefs = WarehousePickingPrintLayout.ParsePrefs(pref?.SettingsJson);
        }

        // Filter (live aus ?colf_*) -> Sortierung (live ?sortCol/sortDir, sonst Default-Sort) -> sichtbare Spalten.
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        // v1.99.85: Spalten, die nur der Bildschirm zeigt (Liefertermin, Fräsen-Pfad), filtern und sortieren im Druck mit demselben
        // Zelltext wie dort. Geladen nur, wenn ein Filter oder die (Default-)Sortierung sie braucht.
        bool Braucht(string key) => columnFilters.ContainsKey(key)
            || string.Equals(string.IsNullOrWhiteSpace(sortCol) ? prefs?.DefaultSortColumn : sortCol, key, StringComparison.Ordinal);
        var colMap = new Dictionary<string, Func<WarehouseRequisitionDetailItemViewModel, string?>>(WarehousePickingPrintLayout.ColumnMap);
        if (Braucht("liefertermin") && (await LieferterminTexte.JeArtikelAsync(_poLines, _receipts, r.Items.Select(i => i.ArticleNumber))).Texte is { } lt)
            colMap["liefertermin"] = i => lt.GetValueOrDefault(i.ArticleNumber.Trim()) ?? "—";
        if (Braucht("fraesen-pfad") && await RequireFraesenPfadAktivFilter.IstAktivAsync(_settings))
        {
            var pfade = await _articles.GetFraesenPfadeAsync();
            colMap["fraesen-pfad"] = i => pfade.GetValueOrDefault(i.ArticleNumber.Trim());
        }
        var filtered = ColumnFilterHelper.Apply(detailItems, columnFilters, colMap, ColumnFilterTypes.For("WarehousePicking/Details"));
        var sorted = WarehousePickingPrintLayout.SortItems(
            filtered, sortCol, sortDir, prefs?.DefaultSortColumn, prefs?.DefaultSortDirection, colMap);
        var columns = WarehousePickingPrintLayout.ResolveColumns(prefs);

        var vm = new WarehouseRequisitionPrintViewModel
        {
            Id = r.Id,
            WorkplaceName = r.ProductionWorkplace?.Name ?? "",
            CreatedBy = r.CreatedBy,
            SubmittedAt = r.SubmittedAt,
            Status = r.Status,
            Columns = columns,
            Items = sorted
        };
        return View(vm);
    }
}
