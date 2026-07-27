# Lagerbestellung aus der Produktion — Design Spec

**Datum:** 2026-04-30
**Branch:** `feature/bde-phase-1`
**Versions-Bump:** v1.8.3 → v1.8.4

## 1. Ziel

Produktionsmitarbeiter erfasst an seinem Werkbankplatz eine Bestellliste für Lagerartikel ("Bestellliste Lagerartikel"). Liste umfasst 1..N Artikel mit Mengen. Beim Abschicken erhält der Lagerempfänger (eine zentral konfigurierte `OrderRecipientGroup`) eine E-Mail mit Deep-Link. Lager öffnet die Liste, druckt sie optional, kommissioniert mit pro-Position Ist-Mengen und schließt ab. Storno mit Mail-Trigger ist möglich.

## 2. Architektur-Übersicht

Neue Aggregat-Entity `WarehouseRequisition` (Header) + `WarehouseRequisitionItem` (Items) mit eigenem Lifecycle `Draft → Submitted → Closed` plus `Cancelled`-Branch. Erfassen in `/WarehouseRequisitions` (Produktion), Lager-Sicht in `/WarehousePicking` (Stock). E-Mail-Versand via neuer `WarehouseRequisitionEmailService` im Worker (15-min Tick, akzeptierte Latenz). Bestehende Infrastruktur wird wiederverwendet: `OrderRecipientGroup`/`OrderRecipient` für Empfänger, `ArticleRepository.SearchAsync` für Artikel-Auswahl, `ProductionWorkplaceUser` für Werkbank-Resolution.

Neuer Top-Level-Menüpunkt **„Bestellungen"** als Dropdown ersetzt den bisherigen flachen PartRequisitions-Eintrag. Untermenüs: Bedarfsmeldungen, Lagerbestellungen, Lager: Eingehende Listen.

## 3. Datenmodell

### 3.1 Status-Enum

```csharp
public enum WarehouseRequisitionStatus : byte
{
    Draft     = 1, // erfasst, noch nicht abgeschickt — sichtbar nur fuer Ersteller
    Submitted = 2, // abgeschickt, Lager kann bearbeiten
    Closed    = 3, // abgeschlossen vom Lager
    Cancelled = 4  // storniert (Ersteller oder Lager)
}
```

### 3.2 `WarehouseRequisition` (Header, AuditableEntity)

```csharp
public class WarehouseRequisition : AuditableEntity
{
    public int ProductionWorkplaceId { get; set; }
    public ProductionWorkplace ProductionWorkplace { get; set; } = null!;

    public WarehouseRequisitionStatus Status { get; set; } = WarehouseRequisitionStatus.Draft;

    public int? OrderRecipientGroupId { get; set; }       // gesetzt beim Submit (resolved aus AppSetting)
    public OrderRecipientGroup? OrderRecipientGroup { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public int? SubmittedByUserId { get; set; }

    public DateTime? ClosedAt { get; set; }
    public int? ClosedByUserId { get; set; }

    public DateTime? CancelledAt { get; set; }
    public int? CancelledByUserId { get; set; }
    [StringLength(500)]
    public string? CancellationReason { get; set; }       // optional (analog BdeBooking)

    public DateTime? EmailSentAt { get; set; }            // Submit-Mail
    public DateTime? CancellationEmailSentAt { get; set; } // Storno-Mail

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<WarehouseRequisitionItem> Items { get; set; } = new List<WarehouseRequisitionItem>();
}
```

### 3.3 `WarehouseRequisitionItem` (AuditableEntity)

```csharp
public class WarehouseRequisitionItem : AuditableEntity
{
    public int WarehouseRequisitionId { get; set; }
    public WarehouseRequisition WarehouseRequisition { get; set; } = null!;

    [Required, MaxLength(100)]
    public string ArticleNumber { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string ArticleDescription { get; set; } = string.Empty; // Snapshot

    [MaxLength(50)]
    public string? Unit { get; set; }                              // Snapshot

    public decimal QuantityRequested { get; set; }
    public decimal? QuantityPicked { get; set; }                   // gesetzt beim Closed (Default = QuantityRequested)

    public int Position { get; set; }                              // Reihenfolge 1..N
}
```

**Bewusst weggelassen:** `ArticleGroup`-Snapshot — kein Mapping-Use-Case, MVP-yagni.

### 3.4 EF-Konfiguration & Indizes

`ApplicationDbContext.OnModelCreating`:

```csharp
modelBuilder.Entity<WarehouseRequisition>(entity =>
{
    entity.HasIndex(e => e.Status);
    entity.HasIndex(e => e.ProductionWorkplaceId);
    entity.HasIndex(e => e.SubmittedAt);

    entity.HasOne(e => e.ProductionWorkplace)
        .WithMany()
        .HasForeignKey(e => e.ProductionWorkplaceId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasOne(e => e.OrderRecipientGroup)
        .WithMany()
        .HasForeignKey(e => e.OrderRecipientGroupId)
        .OnDelete(DeleteBehavior.Restrict);
});

modelBuilder.Entity<WarehouseRequisitionItem>(entity =>
{
    entity.HasIndex(e => new { e.WarehouseRequisitionId, e.Position });
    entity.HasIndex(e => new { e.WarehouseRequisitionId, e.ArticleNumber }).IsUnique();

    entity.HasOne(e => e.WarehouseRequisition)
        .WithMany(r => r.Items)
        .HasForeignKey(e => e.WarehouseRequisitionId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

Unique-Index auf `(RequisitionId, ArticleNumber)` setzt Duplikat-Verbot per DB durch.

## 4. Lifecycle & Transitions

| Von | Nach | Auslöser | Validierung | Side-Effects |
|---|---|---|---|---|
| (none) | Draft | "Neue Liste" | Werkbank-Resolution: 0 → Fehler, 1 → Auto, N → Dropdown alphabetisch | Header anlegen, Items leer |
| Draft | Draft | Items hinzu/aend/loeschen | Duplikat-Check via DB-Unique | `ModifiedAt` |
| Draft | Submitted | "Abschicken" | ≥1 Item; AppSetting `DefaultLagerbestellempfaengerId` gesetzt + Gruppe existiert | `OrderRecipientGroupId`, `SubmittedAt`, `SubmittedByUserId` |
| Submitted | Closed | Lager: "Abschließen" | Pro-Item `QuantityPicked >= 0` | `ClosedAt`, `ClosedByUserId`, Items.QuantityPicked |
| Draft | Cancelled | Ersteller: "Stornieren" | — | `CancelledAt`, `CancelledByUserId`, optional `CancellationReason`. **Keine Mail** (Draft war nie an Lager geschickt) |
| Submitted | Cancelled | Ersteller oder Lager: "Stornieren" | — | wie oben + `Storno-Mail-Trigger` (worker setzt `CancellationEmailSentAt`) |

**RowVersion-Concurrency:** Auf jedem Status-Übergang wird die `RowVersion` mitgegeben. Bei `DbUpdateConcurrencyException` → `TempData["WarningMessage"] = "Bestellung wurde inzwischen geaendert — bitte Liste neu laden."`.

## 5. Komponenten

### 5.1 Neue Files

**Models:**
- `IdealAkeWms/Models/WarehouseRequisitionStatus.cs`
- `IdealAkeWms/Models/WarehouseRequisition.cs`
- `IdealAkeWms/Models/WarehouseRequisitionItem.cs`
- `IdealAkeWms/Models/ViewModels/WarehouseRequisitionEditViewModel.cs`
- `IdealAkeWms/Models/ViewModels/WarehouseRequisitionListViewModel.cs`
- `IdealAkeWms/Models/ViewModels/WarehouseRequisitionDetailViewModel.cs`

**Repositories:**
- `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs`
- `IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs`

**Controllers:**
- `IdealAkeWms/Controllers/WarehouseRequisitionsController.cs` — Erfasser-Sicht, `[RequirePickingOrStockAccess]`
- `IdealAkeWms/Controllers/WarehousePickingController.cs` — Lager-Sicht, `[RequireStockAccess]`
- `IdealAkeWms/Controllers/Api/WarehouseRequisitionsApiController.cs` — Items-CRUD + Stock-Lookup, JSON

**Views:**
- `IdealAkeWms/Views/WarehouseRequisitions/Index.cshtml` — eigene Listen (Drafts + History)
- `IdealAkeWms/Views/WarehouseRequisitions/Edit.cshtml` — Erfassen + Items-Tabelle + Article-Search
- `IdealAkeWms/Views/WarehousePicking/Index.cshtml` — Lager-Liste (Status != Draft)
- `IdealAkeWms/Views/WarehousePicking/Details.cshtml` — Detail mit Print/Close/Cancel
- `IdealAkeWms/Views/WarehousePicking/Print.cshtml` — Print-View analog `PrintBom.cshtml`

**Service-Projekt:**
- `IDEALAKEWMSService/Services/IWarehouseRequisitionEmailService.cs`
- `IDEALAKEWMSService/Services/WarehouseRequisitionEmailService.cs`

**Schema:**
- EF-Migration `<ts>_AddWarehouseRequisitions`
- `SQL/53_AddWarehouseRequisitions.sql` (idempotent)

**Tests:**
- `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs`
- `IdealAkeWms.Tests/Controllers/WarehouseRequisitionsControllerTests.cs`
- `IdealAkeWms.Tests/Controllers/WarehousePickingControllerTests.cs`
- `IDEALAKEWMSService.Tests/Services/WarehouseRequisitionEmailServiceTests.cs`

### 5.2 Geänderte Files

- `IdealAkeWms/Data/Repositories/IProductionWorkplaceRepository.cs` + Impl — neue Methode `GetByUserIdAsync(int userId)` (alphabetisch sortiert)
- `IdealAkeWms/Models/AppSettingKeys.cs` — Konstante `DefaultLagerbestellempfaengerId`
- `IdealAkeWms/Program.cs` — AppSetting-Seed (Default leer/null) + DI-Registrierung der neuen Repositories
- `IdealAkeWms/Data/ApplicationDbContext.cs` — DbSets + Mapping
- `IdealAkeWms/Views/Shared/_Layout.cshtml` — Top-Level-Link "Bestellungen" wird Dropdown mit 3 Untermenüs; `active`-Highlight-Check erweitert auf neue Controller
- `IDEALAKEWMSService/Workers/SyncWorker.cs` — neuer Tick-Block analog `Sync:PartRequisitionEmailEnabled`
- `IDEALAKEWMSService/appsettings.json` + `Program.cs` Service-Settings-Seed — `Sync:WarehouseRequisitionEmailEnabled` (false default)
- `IdealAkeWms/AppVersion.cs` + `IDEALAKEWMSService/AppVersion.cs` — v1.8.4
- `SQL/00_FreshInstall.sql` — neue Tabellen + Indizes + AppSetting-Seed
- `IdealAkeWms/Views/Help/Index.cshtml` — neue Section "Lagerbestellungen"
- `IdealAkeWms/Views/Help/Changelog.cshtml` — v1.8.4-Eintrag
- `PROJECT_STATUS.md` — v1.8.4-Eintrag
- `CLAUDE.md` — AppSetting-Tabelle erweitern, Filter-Tabelle (`[RequireStockAccess]`/`[RequirePickingOrStockAccess]` jetzt mit weiteren Controllern)
- `docs/TESTSZENARIEN.md` — neue Bereich-Nummer (siehe §10)

## 6. AppSettings

| Key | Default | Beschreibung |
|---|---|---|
| `DefaultLagerbestellempfaengerId` | (leer) | ID der `OrderRecipientGroup`, die Lagerbestellungen empfaengt. Leer = Submit blockt mit Fehlermeldung. |

| Service-Setting | Default | Beschreibung |
|---|---|---|
| `Sync:WarehouseRequisitionEmailEnabled` | `false` | Aktiviert E-Mail-Versand fuer Lagerbestellungen im SyncWorker (Tick = `WorkerSettings:SyncIntervalMinutes`, default 15min) |

`Notifications:AppBaseUrl` (existiert bereits) wird vom neuen Service für Deep-Link-Generierung gelesen.

## 7. Datenfluss

### 7.1 Erfasser-Workflow

```
GET /WarehouseRequisitions
  → eigene Listen (CreatedByUserId = currentUser):
    - Drafts (immer angezeigt)
    - Submitted/Closed/Cancelled der letzten 30 Tage
  → Spalten: ID, Werkbank, Items-Anzahl, Status (farbig), Erstellt, Submit, Aktion
  → "Neue Liste"-Button

POST /WarehouseRequisitions/Create
  → Werkbank-Resolution via IProductionWorkplaceRepository.GetByUserIdAsync(currentUser):
     0 → Fehler "Bitte Werkbank-Zuordnung in Stammdaten pflegen", redirect Index
     1 → ProductionWorkplaceId Auto-Set
    >1 → Modal/Form-Field "Werkbank waehlen" (alphabetisch); Resubmit mit ID
  → Header in Status=Draft anlegen
  → Redirect /WarehouseRequisitions/Edit/{id}

GET /WarehouseRequisitions/Edit/{id}
  → ViewModel: Header (read-only Werkbank/Status), Items, Article-Search-Component
  → Article-Search: gleicher Endpoint wie bestehender /api/articles/search (kein neuer)
  → Klick auf Treffer: AJAX POST /api/warehouserequisitions/{id}/items
    body: { articleNumber, quantity = 1 }
    → Snapshot Description+Unit aus Article
    → Position = max(Position) + 1
    → Bei Unique-Constraint-Violation: 400 "Artikel ist bereits in der Liste"
    → Response: kompletter Item-State (zum Re-render)
  → Mengen-Edit: AJAX PUT /api/warehouserequisitions/items/{itemId} { quantity }
  → Loeschen: AJAX DELETE /api/warehouserequisitions/items/{itemId}
  → Stock-Anzeige: nach Add → AJAX GET /api/warehouserequisitions/stock?articleNumber=... → Komma-Liste "Lagerplatz (Menge)"
  → "Abschicken"-Button: POST /WarehouseRequisitions/Submit/{id}
    Validierung: ≥1 Item; AppSetting → resolve OrderRecipientGroup; Group muss existieren; aktive Recipients (sonst Warning, Submit trotzdem ok)
    → Status=Submitted, SubmittedAt=Now, SubmittedByUserId=current, OrderRecipientGroupId=resolved
    → Redirect Index mit Success-Toast "Liste #{id} abgeschickt — wird per E-Mail gesendet (max. 15 Min)"
  → "Stornieren"-Button: POST /WarehouseRequisitions/Cancel/{id} { reason }
    Status=Cancelled, CancelledAt, CancelledByUserId, CancellationReason
    Wenn vorher Submitted → Storno-Email wird vom Worker getriggert
```

### 7.2 Lager-Workflow

```
GET /WarehousePicking
  → Repository filtert IMMER Status != Draft
  → Default-Filter: Status=Submitted (Toggle "Alle Status" zeigt Submitted+Closed+Cancelled)
  → Werkbank-Filter (Dropdown alphabetisch, optional)
  → Tabelle: ID, Werkbank, Erfasser, Submitted, Items, Status

GET /WarehousePicking/Details/{id}
  → Items-Tabelle mit pro-Item-Inputs (default = QuantityRequested)
  → Lagerplatz-Spalte (read-only): "Code (Menge), Code2 (Menge2), …" — gleiche Logik wie PickingController
  → Action-Buttons (nur wenn Status=Submitted):
     "Drucken" → öffnet /WarehousePicking/Print/{id} in neuem Tab
     "Abschliessen" → POST /WarehousePicking/Close/{id} { items: [{itemId, quantityPicked}] }
       → Validierung quantityPicked >= 0
       → Status=Closed, ClosedAt, ClosedByUserId
     "Stornieren" → Modal mit optionalem Reason; POST /WarehousePicking/Cancel/{id}

GET /WarehousePicking/Print/{id}
  → Layout = null
  → Header (AKE-Logo, Listen-#, Werkbank, Erfasser, Submit-Datum, Status)
  → Tabelle: Pos | Artikelnr | Bezeichnung | Bestellt | Ist (leer-Box wenn nicht abgeschlossen) | ME | Lagerplatz | Notiz-Spalte
  → @media print: Buttons + Header-Nav ausblenden, A4-Hochformat
  → Auto-Print-Trigger: window.print() onLoad
```

### 7.3 Email-Worker

```
SyncWorker.cs (15-min Tick):
  if (Sync:WarehouseRequisitionEmailEnabled):
    using scope ...
    var svc = scope.ServiceProvider.GetRequiredService<IWarehouseRequisitionEmailService>();
    await svc.SendPendingEmailsAsync(dryRun: WorkerSettings:SyncDryRun);

WarehouseRequisitionEmailService.SendPendingEmailsAsync(bool dryRun):
  // Submit-Mails
  var submits = ctx.WarehouseRequisitions
      .Include(r => r.OrderRecipientGroup).ThenInclude(g => g.Recipients)
      .Include(r => r.Items)
      .Include(r => r.ProductionWorkplace)
      .Where(r => r.Status == Submitted && r.EmailSentAt == null && r.OrderRecipientGroupId != null)
      .ToList();
  foreach (var r in submits):
      var emails = r.OrderRecipientGroup.Recipients.Where(x => x.IsActive).Select(x => x.Email).Distinct();
      if (emails.empty): errors.Add(...); continue;  // EmailSentAt bleibt null → retry
      Subject = "Lagerbestellung #{Id} — Werkbank {Workplace.Name}"
      Body = HTML mit Header, Items-Tabelle (Pos/Artikelnr/Bezeichnung/Menge/ME), Deep-Link "{Notifications:AppBaseUrl}/WarehousePicking/Details/{id}"
      await SmtpSend(...) (analog PartRequisitionEmailService)
      if (!dryRun): r.EmailSentAt = Now; ctx.SaveChanges()

  // Storno-Mails
  var cancels = ctx.WarehouseRequisitions
      .Where(r => r.Status == Cancelled && r.EmailSentAt != null && r.CancellationEmailSentAt == null)
      .Include(...) ...
      .ToList();
  foreach (var r in cancels):
      Subject = "[STORNO] Lagerbestellung #{Id} — Werkbank {Workplace.Name}"
      Body = HTML mit Header + Storno-Grund + "Bitte nicht weiter bearbeiten"
      await SmtpSend(...)
      if (!dryRun): r.CancellationEmailSentAt = Now; ctx.SaveChanges()

  return new EmailResult(submits.Count, cancels.Count, errors)
```

## 8. Repository-Methoden

### 8.1 `IWarehouseRequisitionRepository`

```csharp
Task<int> CreateDraftAsync(int productionWorkplaceId, int currentUserId, string currentUserName, string windowsUserName);
Task<WarehouseRequisition?> GetByIdAsync(int id, bool includeItems = true);
Task<List<WarehouseRequisition>> GetForUserAsync(int userId, int historyDays = 30);  // Drafts + History
Task<(List<WarehouseRequisition> Items, int TotalCount)> GetForWarehouseAsync(
    WarehouseRequisitionStatus? statusFilter, int? workplaceId, int page, int pageSize);  // immer Status != Draft
Task<List<WarehouseRequisition>> GetPendingSubmitEmailsAsync();
Task<List<WarehouseRequisition>> GetPendingCancellationEmailsAsync();

Task AddItemAsync(int requisitionId, string articleNumber, string description, string? unit, decimal quantity, string user, string winUser);  // wirft DbUpdateException bei Duplikat
Task UpdateItemQuantityAsync(int itemId, decimal quantity, string user, string winUser);
Task RemoveItemAsync(int itemId);

Task SubmitAsync(int id, int recipientGroupId, int submittedByUserId, string user, string winUser, byte[] rowVersion);
Task CloseAsync(int id, IReadOnlyDictionary<int, decimal> itemQuantitiesPicked, int closedByUserId, string user, string winUser, byte[] rowVersion);
Task CancelAsync(int id, string? reason, int cancelledByUserId, string user, string winUser, byte[] rowVersion);

Task MarkEmailSentAsync(int id, DateTime sentAt);
Task MarkCancellationEmailSentAsync(int id, DateTime sentAt);
```

### 8.2 `IProductionWorkplaceRepository` — Erweiterung

```csharp
Task<List<ProductionWorkplace>> GetByUserIdAsync(int userId);  // alphabetisch sortiert via ProductionWorkplaceUsers-JOIN
```

## 9. UI-Layout (kompakt)

**Erfasser-Liste `/WarehouseRequisitions`:**
- Header "Lagerbestellungen — meine Listen" + "Neue Liste"-Button
- Filter-Bar: Status (Draft/Submitted/Closed/Cancelled/Alle, default Alle)
- Tabelle: ID | Werkbank | Items | Status (Badge) | Erstellt | Submit | Aktion (Bearbeiten/Ansehen)

**Erfasser-Edit `/WarehouseRequisitions/Edit/{id}`:**
- Header-Card: Werkbank (read-only nach Anlage), Status-Badge, Erstellt
- Items-Card: Tabelle (Pos | Artikelnr | Bezeichnung | Menge inline-edit | ME | Lagerplatz nach Add | Aktion ×)
- Article-Search: Input + Treffer-Dropdown (max 20). Klick fügt mit Menge=1 hinzu. Bei Duplikat: Toast "Artikel bereits in Liste".
- Footer (nur Status=Draft): "Abschicken" (disabled wenn 0 Items), "Stornieren" (Modal mit optionalem Grund), "Schließen" (zurück)

**Lager-Liste `/WarehousePicking`:**
- Header "Lagerbestellungen — Lager"
- Filter-Bar: Status (Submitted default; Alle = Submitted+Closed+Cancelled), Werkbank
- Tabelle: ID | Werkbank | Erfasser | Submitted | Items | Status
- KPI-Card oben rechts: "Offen" (Count Submitted) — read-only

**Lager-Detail `/WarehousePicking/Details/{id}`:**
- Header-Card: ID, Werkbank, Erfasser, Submit-Zeit, Status-Badge
- Items-Tabelle: Pos | Artikelnr | Bezeichnung | Bestellt | **Ist** (Number-Input, default = Bestellt) | ME | Lagerplatz
- Footer (nur Status=Submitted): "Drucken" (öffnet Print in neuem Tab), "Abschliessen", "Stornieren"

**Print-View `/WarehousePicking/Print/{id}`:**
- Layout=null, A4 Hochformat, AKE-Header, `@media print { … }`, Auto-Print onLoad
- Tabelle: Pos | Artikelnr | Bezeichnung | Bestellt | Ist (leer wenn Status=Submitted) | ME | Lagerplatz | Notiz

**Menü `_Layout.cshtml` (Top-Level „Bestellungen", Dropdown):**
```html
@if (canPickOrStock)
{
    <li class="nav-item dropdown">
        <a class="nav-link dropdown-toggle" data-bs-toggle="dropdown">Bestellungen</a>
        <ul class="dropdown-menu">
            <li><a class="dropdown-item" asp-controller="PartRequisitions" asp-action="Index">Bedarfsmeldungen</a></li>
            <li><a class="dropdown-item" asp-controller="WarehouseRequisitions" asp-action="Index">Lagerbestellungen</a></li>
            @if (canStock)
            {
                <li><hr class="dropdown-divider" /></li>
                <li><a class="dropdown-item" asp-controller="WarehousePicking" asp-action="Index">Lager: Eingehende Listen</a></li>
            }
        </ul>
    </li>
}
```

`active`-Highlight-Check erweitert auf `WarehouseRequisitions`/`WarehousePicking`.

## 10. Tests

### 10.1 Repository-Tests (`WarehouseRequisitionRepositoryTests`, ~7)

- CreateDraftAsync setzt Status=Draft, Audit-Felder
- AddItem mit Duplikat (gleicher ArticleNumber) wirft DbUpdateException
- AddItem schreibt Position = N+1
- SubmitAsync setzt Status/SubmittedAt/SubmittedByUserId/OrderRecipientGroupId
- SubmitAsync mit veralteter RowVersion → DbUpdateConcurrencyException
- CloseAsync schreibt Items.QuantityPicked
- CancelAsync mit Reason

### 10.2 Service-Tests (`WarehouseRequisitionsControllerTests`, `WarehousePickingControllerTests`, ~6 zusammen)

- Erfasser-Index zeigt nur eigene Listen + History-Filter
- Werkbank-Resolution: 0/1/N
- Submit ohne Items → ModelState-Error
- Submit ohne AppSetting → Fehler "Empfänger nicht konfiguriert"
- Lager-Index filtert Status != Draft (auch bei "Alle")
- Close mit Pro-Item-Mengen schreibt korrekt

### 10.3 Email-Service-Tests (`WarehouseRequisitionEmailServiceTests`, ~5)

- SendPending fuer Submitted ohne EmailSentAt → genau 1 Mail pro Recipient-Group
- Mail-Body enthaelt Deep-Link und Items
- Storno-Mail wird nur fuer zuvor Submittete generiert (Cancelled+EmailSentAt!=null+CancellationEmailSentAt==null)
- Storno-Subject hat `[STORNO]`-Prefix
- DryRun setzt keine EmailSentAt-Felder

### 10.4 Manuelle Testszenarien (TESTSZENARIEN.md, neuer Bereich N+1)

- TS-N.1 Erfassen + Submit (1-Werkbank-User) → Liste in Erfasser-Index sichtbar, Lager-Index zeigt sie
- TS-N.2 Werkbank-Auswahl bei N≥2 zugeordneten Werkbänken
- TS-N.3 Werkbank-Fehler bei 0 Zuordnungen
- TS-N.4 Duplikat-Artikel hinzu → Toast-Fehler
- TS-N.5 E-Mail-Versand: Submit → nach Worker-Tick Mail im Postfach (Subject + Deep-Link)
- TS-N.6 Storno nach Submit → Storno-Mail mit `[STORNO]`-Prefix
- TS-N.7 Lager: Detail, Print, Close mit Pro-Item-Ist-Mengen
- TS-N.8 RowVersion: zwei Tabs gleichzeitig, einer schließt → anderer bekommt Hinweis
- TS-N.9 AppSetting-Default-Empfänger leer → Submit-Fehler

## 11. Versionierung & Docs

- AppVersion 1.8.3 → 1.8.4 (Web + Service), Date Implementation-Tag
- CLAUDE.md AppSettings-Tabelle erweitert um `DefaultLagerbestellempfaengerId`
- CLAUDE.md Service-Konfiguration-Tabelle erweitert um `Sync:WarehouseRequisitionEmailEnabled`
- Help/Index neue Section "Lagerbestellungen" (Erfassen, Submit, Storno, Lager-Workflow)
- Changelog v1.8.4-Card
- PROJECT_STATUS.md neuer Block (30.04.2026)
- TESTSZENARIEN.md neuer Bereich

## 12. Out of Scope (YAGNI)

- QR-Scan in der Erfasser-Maske
- Pro-Liste-Empfänger-Override
- ArticleGroup-basierte Empfänger-Verteilung
- ArticleGroup-Snapshot
- Stock-Movement-Erzeugung beim Close (Bestand wird nicht automatisch aus dem Lager ausgebucht — das ist ein eigener Workflow)
- E-Mail-PDF-Anhang
- Mobile-spezifische UI (Bootstrap-Responsive reicht)
- Audit-History fuer Draft-Edits (nur AuditableEntity-Felder)

## 13. Risiken & Mitigationen

- **Default-Empfänger nicht gesetzt:** Submit blockt mit Fehlermeldung; Admin muss in Settings konfigurieren.
- **Empfänger-Gruppe ohne aktive Recipients:** Worker setzt Errors-Eintrag, EmailSentAt bleibt null → Retry. Optional: zusätzlicher Warning-Toast bei Submit "Empfänger-Gruppe hat keine aktiven Empfänger".
- **Stale RowVersion:** sauberer User-facing Hinweis "Liste neu laden".
- **Mail-Latenz 15 Min:** akzeptiert, im Submit-Toast kommuniziert "wird per E-Mail gesendet (max. 15 Min)".
- **Stock-Anzeige Performance:** nur on-click pro hinzugefügtem Item, nicht im Live-Search-Dropdown — keine Last pro Keystroke.
- **EF-Migration-Drift:** schon im Repo-Stand, Drift wird in dieser Migration mit-aufgehoben (Snapshot-Update). SQL/53 idempotent erstellt nur die neuen Tabellen, FreshInstall reflektiert das aktuelle Modell.

## 14. Erfolgskriterien

1. Produktionsmitarbeiter mit 1 Werkbank kann eine Liste mit 3 Artikeln in unter einer Minute erstellen + abschicken.
2. Lager-Mitarbeiter sieht abgeschickte Listen in der Lager-Sicht inkl. Werkbank-Information.
3. E-Mail erreicht den Default-Empfänger nach max. 15 Minuten mit funktionalem Deep-Link.
4. Print-Ausgabe ist druckbar im A4-Format mit allen Positions-Daten.
5. Lager schließt mit Pro-Item-Ist-Mengen ab; History zeigt sowohl Bestell- als auch Ist-Mengen.
6. Storno triggert Storno-E-Mail nur, wenn die Liste vorher submitted wurde.
7. Alle 9 Lock-Down-Punkte aus dem Design-Review sind umgesetzt.
