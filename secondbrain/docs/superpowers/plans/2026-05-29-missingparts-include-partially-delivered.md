# MissingParts: PartiallyDelivered mitzaehlen — Implementation Plan v1.18.1

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Repository-Filter in `GetMissingPartsAsync` und `GetFinalShortagesCountForUserAsync` von `Status==Closed` auf `Status IN (Closed, PartiallyDelivered)` erweitern, damit endgueltige Fehlteile sofort sichtbar werden — auch wenn die Bestellung noch in Restlieferungs-Erwartung ist.

**Architecture:** Reiner Repository-Filter-Fix in einer Datei. Tests werden geflippt + erweitert. Cancelled bleibt ausgeschlossen.

**Tech Stack:** ASP.NET Core 10, EF Core, xUnit + FluentAssertions

**Worktree:** `.claude/worktrees/missingparts-include-pd`, Branch `bugfix/missingparts-include-pd`

**Spec:** [secondbrain/docs/superpowers/specs/2026-05-29-missingparts-include-partially-delivered-design.md](../specs/2026-05-29-missingparts-include-partially-delivered-design.md) (Commit 92d3e9a)

---

## File Structure

**Modify:**
- `IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs` — 2 LINQ-Where-Conditions in `GetMissingPartsAsync` + `GetFinalShortagesCountForUserAsync`
- `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs` — 2 bestehende Tests anpassen, 2 neue Tests fuer PartiallyDelivered
- `IdealAkeWms/AppVersion.cs` — Version "1.18.1"
- `IDEALAKEWMSService/AppVersion.cs` — Version "1.18.1"
- `IdealAkeWms/Views/Help/Changelog.cshtml` — neue v1.18.1-Hotfix-Card
- `CLAUDE.md` — 1 Fallstrick aktualisieren
- `docs/TESTSZENARIEN.md` — Szenario 32.3 anpassen + neues 32.11 (Hotfix-Verifikation)
- `PROJECT_STATUS.md` — Hotfix-Notiz oben

---

## Task 0: Pre-Flight Baseline

**Files:** keine Aenderungen

- [ ] **Step 1: Branch + Worktree verifizieren**

```
git rev-parse --abbrev-ref HEAD
```

Expected: `bugfix/missingparts-include-pd`. Letzte 2 Commits: `92d3e9a docs(spec)`, `5cc204a merge feature/teilgeliefert-fehlteile into main (v1.18.0)`.

- [ ] **Step 2: Baseline-Build**

```
dotnet build IdealAkeWms.slnx
```

Expected: `Build succeeded. 0 Error(s)`. Warnungen (NU1902 MailKit, CS8602 TrackingController) sind ok.

- [ ] **Step 3: Baseline-Tests**

```
dotnet test IdealAkeWms.slnx --no-build
```

Expected: Web `617 passed / 1 skipped` (Total 618), Service `99 passed`.

- [ ] **Step 4: Bestehende Filter-Stellen verifizieren**

```
grep -n "Status == WarehouseRequisitionStatus.Closed" IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs
```

Expected: zwei Treffer in `GetMissingPartsAsync` und `GetFinalShortagesCountForUserAsync`.

---

## Task 1: Repository-Filter erweitern + Tests (TDD)

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs`
- Modify: `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs`

### Step 1: Bestehende Tests identifizieren

```
grep -n "GetMissingPartsAsync_OnlyIncludesItemsWithIsFinalShortageTrue\|GetMissingPartsAsync_ReturnsOnlyClosedRequisitions" IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs
```

Expected: 2 Treffer (Test-Names).

### Step 2: Neue Tests + geflippte Tests schreiben

Bestehende Tests in der Datei lesen um Test-Setup-Pattern (`SeedRequisitionAsync`-Helper, `TestDbContextFactory.Create`) zu kennen.

**Test A — bestehend `GetMissingPartsAsync_ReturnsOnlyClosedRequisitions_WithFinalShortages` umbauen:**

Aktuell (vermutlicher Inhalt):
```csharp
[Fact]
public async Task GetMissingPartsAsync_ReturnsOnlyClosedRequisitions_WithFinalShortages()
{
    // ... setup ...
    var (items, total) = await repo.GetMissingPartsAsync(null, null, null, null, 1, 100);
    items.Should().HaveCount(1);
    items[0].RequisitionId.Should().Be(closedId);
    items[0].QuantityMissing.Should().Be(5m);
    total.Should().Be(1);
}
```

Umbenennen + Erwartung anpassen:

```csharp
[Fact]
public async Task GetMissingPartsAsync_IncludesClosedAndPartiallyDelivered_WithFinalShortages()
{
    using var db = TestDbContextFactory.Create();
    db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
    await db.SaveChangesAsync();
    var repo = new WarehouseRequisitionRepository(db);

    // Closed-Bestellung mit Final-Shortage
    var closedId = await SeedRequisitionAsync(db, (10, 5m, true));
    var closedItems = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == closedId).ToList();
    await repo.CloseAsync(closedId,
        new Dictionary<int, decimal> { [closedItems[0].Id] = 5m },
        new Dictionary<int, string?>(),
        new Dictionary<int, bool> { [closedItems[0].Id] = true },
        1, "u", "w", new byte[0]);

    // PartiallyDelivered-Bestellung mit Final-Shortage auf einem Item, anderes offen
    var pdId = await SeedRequisitionAsync(db, (10, 0m, true), (5, 0m, false));
    var pdItems = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == pdId).OrderBy(i => i.Position).ToList();
    await repo.CloseAsync(pdId,
        new Dictionary<int, decimal> { [pdItems[0].Id] = 0m, [pdItems[1].Id] = 0m },
        new Dictionary<int, string?>(),
        new Dictionary<int, bool> { [pdItems[0].Id] = true, [pdItems[1].Id] = false },
        1, "u", "w", new byte[0]);
    // Verifiziere dass Bestellung wirklich PartiallyDelivered ist
    (await db.WarehouseRequisitions.FindAsync(pdId))!.Status.Should().Be(WarehouseRequisitionStatus.PartiallyDelivered);

    var (items, total) = await repo.GetMissingPartsAsync(null, null, null, null, 1, 100);
    items.Should().HaveCount(2);   // BEIDE Items mit IsFinalShortage=true
    items.Select(i => i.RequisitionId).Should().BeEquivalentTo(new[] { closedId, pdId });
    total.Should().Be(2);
}
```

**Test B — bestehend `GetMissingPartsAsync_OnlyIncludesItemsWithIsFinalShortageTrue` aktualisieren:**

Aktuell endet er mit `none.Should().HaveCount(0)` weil PartiallyDelivered + Item 2 (kein final) → 0 Treffer (Item 1 IS markiert als final, war aber per altem Filter ausgeschlossen).

Geflippt — Item 1 ist jetzt sichtbar:

```csharp
[Fact]
public async Task GetMissingPartsAsync_OnlyIncludesItemsWithIsFinalShortageTrue_InPartiallyDelivered()
{
    using var db = TestDbContextFactory.Create();
    db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
    await db.SaveChangesAsync();
    var repo = new WarehouseRequisitionRepository(db);
    var id = await SeedRequisitionAsync(db, (10, null, false), (5, null, false));
    var items = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).OrderBy(i => i.Position).ToList();
    await repo.CloseAsync(id,
        new Dictionary<int, decimal> { [items[0].Id] = 8m, [items[1].Id] = 3m },
        new Dictionary<int, string?>(),
        new Dictionary<int, bool> { [items[0].Id] = true, [items[1].Id] = false },
        1, "u", "w", new byte[0]);
    // Resultat-Bestellung ist PartiallyDelivered (Item 2 nicht final) — Item 1 IS jetzt sichtbar
    (await db.WarehouseRequisitions.FindAsync(id))!.Status.Should().Be(WarehouseRequisitionStatus.PartiallyDelivered);

    var (result, _) = await repo.GetMissingPartsAsync(null, null, null, null, 1, 100);
    result.Should().HaveCount(1);
    result[0].ItemId.Should().Be(items[0].Id);   // nur Item 1 (final)
}
```

**Test C — neu, expliziter Test fuer Cancelled-Ausschluss:**

```csharp
[Fact]
public async Task GetMissingPartsAsync_ExcludesCancelledRequisitions()
{
    using var db = TestDbContextFactory.Create();
    db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
    await db.SaveChangesAsync();
    var repo = new WarehouseRequisitionRepository(db);

    // Bestellung mit IsFinalShortage=true, dann storniert
    var id = await SeedRequisitionAsync(db, (10, 0m, true));
    var item = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == id).Single();
    var r = await db.WarehouseRequisitions.FindAsync(id);
    r!.Status = WarehouseRequisitionStatus.Cancelled;
    await db.SaveChangesAsync();

    var (result, _) = await repo.GetMissingPartsAsync(null, null, null, null, 1, 100);
    result.Should().HaveCount(0);
}
```

**Test D — neu, `GetFinalShortagesCountForUserAsync` zaehlt PartiallyDelivered mit:**

```csharp
[Fact]
public async Task GetFinalShortagesCountForUserAsync_IncludesPartiallyDelivered()
{
    using var db = TestDbContextFactory.Create();
    db.ProductionWorkplaces.Add(new ProductionWorkplace { Id = 1, Name = "WB1" });
    db.ProductionWorkplaceUsers.Add(new ProductionWorkplaceUser
    {
        UserId = 42, ProductionWorkplaceId = 1,
        CreatedAt = DateTime.Now, CreatedBy = "test", CreatedByWindows = "test\\test"
    });
    await db.SaveChangesAsync();
    var repo = new WarehouseRequisitionRepository(db);

    // PartiallyDelivered-Bestellung mit 1 final + 1 offen
    var pdId = await SeedRequisitionAsync(db, (10, 0m, true), (5, 0m, false));
    var pdItems = db.WarehouseRequisitionItems.Where(i => i.WarehouseRequisitionId == pdId).OrderBy(i => i.Position).ToList();
    await repo.CloseAsync(pdId,
        new Dictionary<int, decimal> { [pdItems[0].Id] = 0m, [pdItems[1].Id] = 0m },
        new Dictionary<int, string?>(),
        new Dictionary<int, bool> { [pdItems[0].Id] = true, [pdItems[1].Id] = false },
        1, "u", "w", new byte[0]);
    (await db.WarehouseRequisitions.FindAsync(pdId))!.Status.Should().Be(WarehouseRequisitionStatus.PartiallyDelivered);

    var (itemCount, reqCount) = await repo.GetFinalShortagesCountForUserAsync(42);
    itemCount.Should().Be(1);   // nur Item 1 (final)
    reqCount.Should().Be(1);
}
```

In `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs`:
1. Tests A und B durch Suchen/Ersetzen umflippen (alte Test-Namen + Bodies durch neue ersetzen).
2. Tests C und D am Ende der Klasse einfuegen.

### Step 3: Tests laufen, FAIL erwarten

```
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~GetMissingPartsAsync_IncludesClosedAndPartiallyDelivered_WithFinalShortages|FullyQualifiedName~GetMissingPartsAsync_OnlyIncludesItemsWithIsFinalShortageTrue_InPartiallyDelivered|FullyQualifiedName~GetMissingPartsAsync_ExcludesCancelledRequisitions|FullyQualifiedName~GetFinalShortagesCountForUserAsync_IncludesPartiallyDelivered"
```

Expected: 4 FAIL (alle erwarten PartiallyDelivered-Items in der Liste, aber bestehender Filter schliesst sie aus).

### Step 4: Repository-Filter erweitern

In `IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs`:

**Stelle 1 — `GetMissingPartsAsync`:**

Aktuell:
```csharp
        var q = _context.WarehouseRequisitionItems
            .Include(i => i.WarehouseRequisition)
                .ThenInclude(r => r.ProductionWorkplace)
            .Where(i => i.IsFinalShortage
                && i.WarehouseRequisition.Status == WarehouseRequisitionStatus.Closed);
```

Ersetzen durch:
```csharp
        var q = _context.WarehouseRequisitionItems
            .Include(i => i.WarehouseRequisition)
                .ThenInclude(r => r.ProductionWorkplace)
            .Where(i => i.IsFinalShortage
                && (i.WarehouseRequisition.Status == WarehouseRequisitionStatus.Closed
                    || i.WarehouseRequisition.Status == WarehouseRequisitionStatus.PartiallyDelivered));
```

**Stelle 2 — `GetFinalShortagesCountForUserAsync`:**

Aktuell:
```csharp
        var q = _context.WarehouseRequisitionItems
            .Where(i => i.IsFinalShortage
                && i.WarehouseRequisition.Status == WarehouseRequisitionStatus.Closed
                && userWorkplaceIds.Contains(i.WarehouseRequisition.ProductionWorkplaceId));
```

Ersetzen durch:
```csharp
        var q = _context.WarehouseRequisitionItems
            .Where(i => i.IsFinalShortage
                && (i.WarehouseRequisition.Status == WarehouseRequisitionStatus.Closed
                    || i.WarehouseRequisition.Status == WarehouseRequisitionStatus.PartiallyDelivered)
                && userWorkplaceIds.Contains(i.WarehouseRequisition.ProductionWorkplaceId));
```

### Step 5: Tests laufen, PASS erwarten

```
dotnet test IdealAkeWms.Tests/IdealAkeWms.Tests.csproj --filter "FullyQualifiedName~WarehouseRequisitionRepository"
```

Expected: alle Tests passing, inkl. die 4 neuen/geflippten.

### Step 6: Volle Test-Suite

```
dotnet test IdealAkeWms.slnx --no-build
```

Expected: Web 619 passed (Baseline 617 + 2 neue, 2 sind Umbau bestehender — netto +2), 1 skipped, Service 99 passed.

**Hinweis:** Test A ersetzt einen bestehenden Test (gleiche Anzahl). Test B ersetzt einen bestehenden Test (gleiche Anzahl). Tests C und D sind neu (+2). Netto: +2.

Falls die Test-Counts abweichen: das ist nicht zwingend kritisch, solange alle gruen sind und keine Tests ueberraschend skipped sind. Im Bericht den genauen Wert nennen.

### Step 7: Commit

```
git add IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs
git commit -m "fix(repo): MissingParts/Werkbank-Karte zeigen jetzt auch PartiallyDelivered

GetMissingPartsAsync + GetFinalShortagesCountForUserAsync schliessen
PartiallyDelivered-Bestellungen jetzt EIN. Items mit IsFinalShortage=true
erscheinen sofort in der Fehlteile-Liste, unabhaengig davon ob die
Bestellung noch in Restlieferungs-Erwartung ist. Cancelled bleibt
ausgeschlossen.

2 bestehende Tests umgebaut (PartiallyDelivered zaehlt jetzt mit),
2 neue Tests fuer Cancelled-Ausschluss + Werkbank-Karten-Count.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: AppVersion + Changelog + CLAUDE.md + TESTSZENARIEN + PROJECT_STATUS

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs`
- Modify: `IDEALAKEWMSService/AppVersion.cs`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `CLAUDE.md`
- Modify: `docs/TESTSZENARIEN.md`
- Modify: `PROJECT_STATUS.md`

### Step 1: Web-AppVersion

`IdealAkeWms/AppVersion.cs`:

```csharp
namespace IdealAkeWms;

public static class AppVersion
{
    public const string Version = "1.18.1";
    public const string Date = "2026-05-29";
}
```

### Step 2: Service-AppVersion

`IDEALAKEWMSService/AppVersion.cs`:

```csharp
namespace IDEALAKEWMSService;

public static class AppVersion
{
    public const string Version = "1.18.1";
    public const string Date = "2026-05-29";
}
```

### Step 3: Changelog v1.18.1-Card

In `IdealAkeWms/Views/Help/Changelog.cshtml` direkt nach `<div class="col-lg-8">` und VOR dem v1.18.0-Card einfuegen:

```html
        <div class="card mb-3">
            <div class="card-header text-white" style="background-color: var(--ake-primary);">
                <strong>v1.18.1</strong> <span class="text-white-50 ms-2">29.05.2026</span>
            </div>
            <div class="card-body">
                <h6>Hotfix: Endgueltige Fehlteile sofort sichtbar (auch in teilgelieferten Bestellungen)</h6>
                <ul>
                    <li><strong>Bug-Fix:</strong> Items die als endgueltig Fehlteil markiert wurden,
                        erschienen erst dann in der Fehlteile-Liste (<code>/MissingParts</code>) und in der
                        Werkbank-Karte "Meine Fehlteile", wenn die <em>gesamte</em> Bestellung auf Closed
                        wechselte. Bei Teillieferungen mit einem endgueltig markierten Fehlteil und
                        gleichzeitiger Restlieferungs-Erwartung auf anderen Positionen blieb das Item
                        unsichtbar.</li>
                    <li><strong>Neues Verhalten:</strong> Items mit <em>Endgueltig Fehlteil</em> erscheinen
                        sofort in der Fehlteile-Liste, sobald die Checkbox gesetzt ist — unabhaengig
                        davon ob die Bestellung noch teilgeliefert ist oder bereits abgeschlossen wurde.
                        Cancelled-Bestellungen bleiben weiterhin ausgeschlossen.</li>
                </ul>
            </div>
        </div>
```

### Step 4: CLAUDE.md Fallstrick aktualisieren

In `CLAUDE.md` im Abschnitt `## Bekannte Fallstricke` den bestehenden Eintrag zu MissingParts ersetzen:

Suche:
```
- **MissingParts zeigt nur `IsFinalShortage=true` UND Status=Closed (seit v1.18.0)**: ...
```

Ersetze durch:
```markdown
- **MissingParts zeigt `IsFinalShortage=true` aus Closed UND PartiallyDelivered (seit v1.18.1)**: Items mit `IsFinalShortage=true` erscheinen sofort in `/MissingParts` und in der Werkbank-Karte "Meine Fehlteile", sobald der Flag gesetzt ist — der Bestell-Status (Closed oder PartiallyDelivered) spielt keine Rolle mehr. Cancelled-Bestellungen sind weiterhin ausgeschlossen (storniert ≠ Fehlteil). Submitted ist normalerweise irrelevant, da Lager IsFinalShortage erst beim Picking setzt. **Vorher (v1.18.0):** nur Closed — fuehrte zu unsichtbaren Fehlteilen in teilgelieferten Bestellungen.
```

### Step 5: TESTSZENARIEN Anpassungen

In `docs/TESTSZENARIEN.md`:

**32.3 aktualisieren** — Suche das bestehende Szenario `### 32.3 Eine offen, eine final -> PartiallyDelivered` und ersetze den Block durch:

```markdown
### 32.3 Eine offen, eine final -> PartiallyDelivered (Item 2 erscheint sofort in Fehlteile-Liste)
**Vorbedingung:** 2 Items.
**Schritte:** Ist=3 (kein final), Ist=2 (final). Abschliessen.
**Erwartet:** Status PartiallyDelivered. Item 1 NICHT in Fehlteile-Liste (kein final). Item 2 IST in Fehlteile-Liste (final markiert, seit v1.18.1 unabhaengig vom Bestell-Status).
```

**Neues Szenario 32.11** am Ende von Kapitel 32 einfuegen (nach 32.10):

```markdown
### 32.11 Hotfix v1.18.1: Endgueltig Fehlteil aus PartiallyDelivered erscheint sofort
**Vorbedingung:** Bestellung in Status Submitted mit 2 Items (Soll=2, Soll=1).
**Schritte:**
1. Picking/Details. Item 1: Ist=1, Checkbox "Endgueltig Fehlteil" anhaken. Item 2: Ist=0 lassen (keine Aenderung am Flag).
2. "Speichern + Abschliessen" — Status wird PartiallyDelivered (Item 2 erwartet noch Restlieferung).
3. Lager-Menue "Fehlteile" (`/MissingParts`) aufrufen.
4. Werkbank-User "Meine Listen" pruefen — Karte "Meine Fehlteile".
**Erwartet:**
- `/MissingParts` zeigt 1 Eintrag: Item 1 mit Fehlt=1, Bestell-ID = die Bestellung, Werkbank korrekt.
- Werkbank-Karte zeigt "1 endgueltigen Fehlteil aus 1 Bestellung".
- Detail-Link auf die Bestellung funktioniert und zeigt sie in PartiallyDelivered (editierbar).
**Negativ:** Vorher (v1.18.0) war die Liste leer — das war der Bug.
```

### Step 6: PROJECT_STATUS Hotfix-Notiz

In `PROJECT_STATUS.md` direkt nach der `## Aktueller Fortschritt`-Zeile und VOR dem bestehenden `### v1.18.0`-Block einfuegen:

```markdown
### v1.18.1 — Hotfix MissingParts (PartiallyDelivered mitzaehlen)

Hintergrund: In v1.18.0 wurden Items mit `IsFinalShortage=true` aus `PartiallyDelivered`-Bestellungen nicht in `/MissingParts` und nicht in der Werkbank-Karte "Meine Fehlteile" angezeigt. User-Erwartung: sobald das Flag gesetzt ist, soll das Item sofort in der Liste auftauchen. Hotfix: Filter erweitert auf `Status IN (Closed, PartiallyDelivered)`.

| # | Sub-Task | Status |
|---|---------|--------|
| 0 | Pre-Flight Baseline | ✅ erledigt |
| 1 | Repository-Filter + Tests (TDD) | ✅ erledigt |
| 2 | Version + Doku (Changelog, CLAUDE.md, TESTSZENARIEN, PROJECT_STATUS) | ✅ erledigt |
| 3 | Final-Check Build + Tests | ⏳ offen |
| 4 | Merge in main (nach User-Bestaetigung) | ⏳ offen |

---
```

### Step 7: Build verifizieren

```
dotnet build IdealAkeWms.slnx
```

Expected: `Build succeeded.`.

### Step 8: Tests gleich

```
dotnet test IdealAkeWms.slnx --no-build
```

Expected: gleich wie nach Task 1.

### Step 9: Commit

```
git add IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs IdealAkeWms/Views/Help/Changelog.cshtml CLAUDE.md docs/TESTSZENARIEN.md PROJECT_STATUS.md
git commit -m "docs+version: v1.18.1 Hotfix-Notiz, Changelog, CLAUDE.md, TESTSZENARIEN

Web + Service AppVersion auf 1.18.1. Changelog-Card v1.18.1 prependet.
CLAUDE.md-Fallstrick zu MissingParts aktualisiert (PartiallyDelivered
zaehlt jetzt mit). TESTSZENARIEN: 32.3 angepasst + neues 32.11 fuer
Hotfix-Verifikation. PROJECT_STATUS v1.18.1-Block.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: Final-Check Build + Tests

**Files:** keine Aenderungen — Verifikation

- [ ] **Step 1: Vollstaendiger Build**

```
dotnet build IdealAkeWms.slnx
```

Expected: `Build succeeded. 0 Error(s)`. Warnungen wie Baseline ok.

- [ ] **Step 2: Volle Test-Suite**

```
dotnet test IdealAkeWms.slnx --no-build
```

Expected: Web ~619 passed + 1 skipped, Service 99 passed.

- [ ] **Step 3: Versions-Sanity**

```
grep "1.18.1" IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs
grep "v1.18.1" IdealAkeWms/Views/Help/Changelog.cshtml PROJECT_STATUS.md
```

Expected: alle 4 Files enthalten `1.18.1`/`v1.18.1`.

- [ ] **Step 4: Filter-Sanity (Repo)**

```
grep -A1 "WarehouseRequisitionStatus.PartiallyDelivered" IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs | head -10
```

Expected: mind. 2 Vorkommen in den beiden geaenderten Methoden (jeweils im `||`-Block).

```
grep -c "Status == WarehouseRequisitionStatus.Closed" IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs
```

Expected: 2 (unveraendert — beide Stellen behalten Closed, kombinieren es jetzt mit PartiallyDelivered).

- [ ] **Step 5: Working-Tree clean**

```
git status
git log --oneline 5cc204a..HEAD
```

Expected: clean. 3 neue Commits seit `92d3e9a docs(spec)`: Task 1-Commit, Task 2-Commit, dann der spec-Commit (Task 1 + Task 2 = 2 Commits, plus den Spec-Commit = 3 in der Range).

---

## Task 4: Merge in main (NACH User-Bestaetigung)

**Files:** keine Code-Aenderungen — git-Operationen

**WICHTIG:** Diese Task NICHT automatisch ausfuehren. Per Memory-Feedback (`feedback_worktree_cleanup_ask_first`): Vor Merge + Cleanup explizit User-Bestaetigung einholen.

Nach Task 3 stoppen und melden:
> "Bugfix ist fertig auf Branch `bugfix/missingparts-include-pd`. Build + Tests gruen. Bereit fuer Merge in main + Push? Worktree danach loeschen, ja/nein?"

Erst nach explizitem Go:

- [ ] **Step 1: Auf main wechseln**

```
git -C C:/Git/IDEAL-AKE-WMS checkout main
git -C C:/Git/IDEAL-AKE-WMS pull origin main
```

- [ ] **Step 2: Merge --no-ff**

```
git -C C:/Git/IDEAL-AKE-WMS merge --no-ff bugfix/missingparts-include-pd -m "merge bugfix/missingparts-include-pd into main (v1.18.1)"
```

- [ ] **Step 3: Build + Tests auf main**

```
dotnet build C:/Git/IDEAL-AKE-WMS/IdealAkeWms.slnx
dotnet test C:/Git/IDEAL-AKE-WMS/IdealAkeWms.slnx --no-build
```

Expected: gruen.

- [ ] **Step 4: Push**

```
git -C C:/Git/IDEAL-AKE-WMS push origin main
```

- [ ] **Step 5: Worktree + Branch entfernen (NUR wenn User in vorheriger Bestaetigung 'ja' zu Cleanup sagte)**

```
git -C C:/Git/IDEAL-AKE-WMS worktree remove .claude/worktrees/missingparts-include-pd
git -C C:/Git/IDEAL-AKE-WMS branch -d bugfix/missingparts-include-pd
```

Falls Windows-File-Lock: melden, nicht erzwingen — User informieren dass er den Ordner spaeter manuell entfernen kann.

---

## Final-Review-Subagent (nach Task 4)

Code-Reviewer-Subagent mit Diff-Range `5cc204a..HEAD` (alle v1.18.1-Commits + Merge-Commit). Pruefkriterien:

1. Repository-Filter: beide Methoden enthalten jetzt `Status==Closed || Status==PartiallyDelivered`?
2. Cancelled bleibt ausgeschlossen?
3. Test-Counts: ~619 Web + 1 skip, 99 Service?
4. AppVersion synchron Web + Service auf 1.18.1?
5. Changelog-Card v1.18.1 prependet, v1.18.0 unveraendert?
6. CLAUDE.md: alter Fallstrick ersetzt durch neuen?
7. TESTSZENARIEN: 32.3 angepasst + 32.11 ergaenzt?
8. PROJECT_STATUS: v1.18.1-Block oben?
9. Keine UI-Aenderungen (Status-Badges, Views) — bleibt wie in v1.18.0?
10. Keine Datenmodell-Aenderungen (Enum, Spalten)?
11. Out-of-Scope-Code: keine kollateralen Aenderungen ausserhalb der erwarteten 7 Files?
