# Leitstand — Kommissionier-Freigabe & Priorisierung

## Zusammenfassung

Leitstand-User steuern, welche Produktionsaufträge kommissioniert werden und in welcher Reihenfolge. Kommissionierer sehen nur freigegebene Aufträge. Das Feature ist per AppSetting `LeitstandAktiv` aktivierbar — bei Deaktivierung funktioniert alles wie bisher.

## Motivation

Aktuell kann jeder Kommissionierer jeden offenen Werkstattauftrag kommissionieren. Es fehlt eine zentrale Steuerung durch die Produktionsplanung. Folge: unkoordinierte Abarbeitung, keine Priorisierung dringender Aufträge, kein Überblick über den Gesamtfortschritt.

## Akteure & Rollen

| Rolle | Rechte |
|-------|--------|
| **Leitstand** (neu: `leitstand`) | Alle Produktionsaufträge sehen, freigeben, Priorität setzen |
| **Kommissionierer** (`picking`) | Nur freigegebene Aufträge sehen, Stückliste öffnen, kommissionieren, umbuchen |
| **Admin** (`admin`) | Alles (Leitstand + Kommissionierung) |
| **Tracking** (`tracking`) | Produktionsaufträge read-only (wie bisher, keine Freigabe) |

## Feature-Toggle

**AppSetting `LeitstandAktiv`** (Default: `false`)

| Zustand | Verhalten |
|---------|-----------|
| `false` | Alles wie bisher: WA-Liste für Picking+Tracking, Kommissionierung per Dropdown |
| `true` | Freigabe-Workflow aktiv. Menü: "Produktionsaufträge" für Leitstand+Tracking, "Kommissionierung" für Picker mit Tabelle |

Wird per Toggle-Switch in der Settings-Seite gesteuert (wie `BestellungenAktiv`, `TeileverfolgungAktiv`).

## Datenmodell

### Neue Spalten auf `ProductionOrders`

| Feld | Typ | Default | Nullable | Beschreibung |
|------|-----|---------|----------|-------------|
| `IsReleasedForPicking` | BIT | 0 | NOT NULL | Zur Kommissionierung freigegeben |
| `PickingPriority` | INT | — | NULL | Reihenfolge (1 = höchste Priorität, NULL = ans Ende sortiert) |
| `ReleasedAt` | DATETIME2 | — | NULL | Zeitpunkt der letzten Freigabe |
| `ReleasedBy` | NVARCHAR(200) | — | NULL | Display-Name des freigebenden Users |

**Design-Entscheidungen:**
- `PickingPriority` hat KEINEN Unique-Constraint. Gleiche Prio erlaubt — Tiebreaker ist `ProductionDate ASC`.
- `ReleasedAt`/`ReleasedBy` bleiben auch nach Un-Release erhalten (Audit-Trail). Nur `IsReleasedForPicking` wird getoggelt.
- Sage-Import: Neue Spalten haben Defaults (`0`/`NULL`) und werden im MERGE nicht berührt → sicher.
- Kein separates `CommissioningOrder`-Entity nötig (YAGNI).

### Neues AppSetting

| Key | Default | Beschreibung |
|-----|---------|-------------|
| `LeitstandAktiv` | `false` | Leitstand-Funktion: Kommissionier-Freigabe und Priorisierung aktivieren |

### Neue Rolle

| Key | Name | Beschreibung |
|-----|------|-------------|
| `leitstand` | Leitstand | Produktionsaufträge freigeben und Kommissionier-Prioritäten verwalten |

### Performance-Index

```sql
IX_ProductionOrders_PickingRelease
    ON ProductionOrders(IsReleasedForPicking, IsDone)
    INCLUDE (PickingPriority, OrderNumber, ArticleNumber, Customer, ProductionDate, PickingStatus)
```

## Berechtigungen

### Neue Permission-Methode

`CanManagePickingReleaseAsync()` → Rollen: `admin`, `leitstand`

### Neuer Action-Filter

`[RequireLeitstandAccess]` — Prüft `CanManagePickingReleaseAsync()`, Redirect auf `AccessDenied` bei Ablehnung.

### Menü-Sichtbarkeit (wenn LeitstandAktiv=true)

| Menüpunkt | Sichtbar für |
|-----------|-------------|
| Produktionsaufträge | `leitstand`, `tracking`, `admin` |
| Kommissionierung | `picking`, `admin` |

### Menü-Badge

Im Menüpunkt "Kommissionierung" wird ein Badge mit der **Anzahl offener, freigegebener Aufträge** angezeigt.
- Query: `COUNT(*) WHERE IsReleasedForPicking=true AND IsDone=false`
- Ausführung: In `_Layout.cshtml` im Razor-Block (wie die anderen Permission-Checks)
- Performance: Einzelner COUNT-Query, nutzt den `IX_ProductionOrders_PickingRelease` Index
- Nur angezeigt wenn `LeitstandAktiv=true` UND `canPick=true` UND Count > 0

## Funktionen

### F1: Freigabe zur Kommissionierung

**Einzelfreigabe:**
- In der Produktionsaufträge-Tabelle: Toggle-Button pro Auftrag
- Bei Freigabe: `IsReleasedForPicking=true`, `ReleasedAt=jetzt`, `ReleasedBy=User`
- Bei Rücknahme: `IsReleasedForPicking=false` (ReleasedAt/By bleiben als Audit)
- Voraussetzung: Artikelnummer muss vorhanden sein. Ohne → Fehlermeldung.

**Massenfreigabe:**
- Checkboxen pro Zeile in der Produktionsaufträge-Tabelle (nur für nicht-erledigte, nicht-freigegebene Aufträge mit Artikelnummer)
- "Alle auswählen"-Checkbox im Tabellenkopf (selektiert alle sichtbaren/gefilterten Zeilen)
- "Ausgewählte freigeben" Button (nur sichtbar wenn mindestens eine Checkbox gesetzt)
- Bei Massenfreigabe: Prioritäten werden automatisch aufsteigend vergeben (MAX+1, MAX+2, ...)
- Für Rücknahme: Checkboxen bei freigegebenen Aufträgen + "Freigabe zurücknehmen" Button
- Alle Massenoperationen per einzelnem POST mit Liste von IDs (kein AJAX pro Zeile)

### F2: Priorisierung

- Inline Number-Input in der Freigabe-Spalte (nur sichtbar bei freigegebenen Aufträgen)
- AJAX-Save bei Änderung (kein Page-Reload)
- Bei Freigabe: Automatischer Vorschlag `MAX(PickingPriority)+1`, User kann ändern
- Priorität löschen (leeres Feld): Auftrag wird ans Ende sortiert (NULL = niedrigste Priorität)
- Sortierung in Kommissionierliste: `PickingPriority ASC NULLS LAST`, dann `ProductionDate ASC`

### F3: Kommissionierliste (neue View)

Ersetzt die bisherige Dropdown-Auswahl. Tabelle mit freigegebenen, offenen Aufträgen.

**Spalten:**

| Spalte | Quelle | Beschreibung |
|--------|--------|-------------|
| Prio | `PickingPriority` | Numerisch, 1 = höchste |
| WA Nr. | `OrderNumber` | Filterable |
| Artikelnummer | `ArticleNumber` | Filterable |
| Bezeichnung | `Description1` | Filterable |
| Kunde | `Customer` | Filterable |
| Stk. | `Quantity` | Rechtsbündig |
| Komm.-Termin | Berechnet: `ProductionDate - KommissionierTage` | Mit ISO-KW, filterable |
| Status | `PickingStatus` | Badge (offen/teilkomm./abgeschlossen) |

**Verhalten:**
- Nur `IsReleasedForPicking && !IsDone`
- Klick auf Zeile → öffnet Stückliste (Bom-View)
- Spaltenfilter via `filterable-table` (bestehende Infrastruktur)
- Abgeschlossene Aufträge verschwinden sofort (`IsDone=true`)

### F4: Fallback bei deaktiviertem Toggle

Wenn `LeitstandAktiv=false`:
- Menü: "Werkstattaufträge" (Picking+Tracking), "Kommissionierung" (Dropdown)
- Keine Freigabe-Spalte in WA-Liste
- Picking-View zeigt alte Dropdown-Auswahl
- Bisheriges Verhalten 1:1 erhalten

## Un-Release Verhalten

- Freigabe kann zurückgenommen werden (Toggle oder Massenoperation)
- PickingItems werden NICHT gelöscht (bereits geleistete Arbeit bleibt erhalten)
- Auftrag verschwindet aus der Kommissionierliste
- Wird der Auftrag erneut freigegeben, ist der bisherige Fortschritt erhalten
- Bei Rücknahme eines Auftrags mit laufender Kommissionierung: Bestätigung-Dialog

## Nicht im Scope (YAGNI)

- Drag & Drop für Prioritäts-Reihenfolge (inline Number-Input reicht)
- Prioritäts-History (ModifiedAt/ModifiedBy auf ProductionOrder reicht)
- Separates CommissioningOrder-Entity
- Fortschrittsbalken / Progress Bar (spätere Iteration, erfordert Klärung des Picking-Flows)
- Live-Picking-Fortschritt (client-seitige Checkboxen werden noch nicht serverseitig gespeichert)
- E-Mail-Benachrichtigung bei Freigabe
- Zeitbasierte automatische Freigabe

## Bekannte Einschränkungen

1. **Kein Fortschrittsbalken in dieser Iteration**: Der Picking-Flow ist derzeit client-seitig — erst beim Umbuchen wird in die DB geschrieben. Ein Progress Bar würde nur den Transfer-Stand zeigen, nicht den tatsächlichen Picking-Fortschritt. Wird adressiert wenn der Picking-Flow serverseitig speichert.
2. **Keine Freigabe-Validierung gegen BOM-Verfügbarkeit**: Es wird nicht geprüft, ob Teile auf Lager sind. Die Freigabe ist rein organisatorisch.
3. **BOM-Zugriff ist nicht durch Freigabe geschützt**: Direktlink auf `/ProductionOrders/Bom/{id}` funktioniert auch für nicht-freigegebene Aufträge (benötigt nur `RequirePickingAccess`). Das ist beabsichtigt — die Freigabe steuert die Sichtbarkeit in der Kommissionierliste, nicht den technischen BOM-Zugriff.
4. **Keine Concurrency-Protection auf ProductionOrder**: Es gibt kein RowVersion-Feld auf `ProductionOrder` (im Gegensatz zu `PickingItem`). Parallele Freigabe/Prioritäts-Änderungen könnten sich theoretisch überschreiben. In der Praxis unwahrscheinlich (ein Leitstand-User). RowVersion ist als separates Improvement geplant, nicht als Blocker.

## Spätere Erweiterungen (nach dieser Iteration)

- Fortschrittsbalken (wenn Picking-Flow serverseitig speichert)
- RowVersion/Optimistic Concurrency auf ProductionOrder
- Eigene Leitstand-Dashboard-Seite (aktuell in WA-Liste integriert)
- Automatische Freigabe (zeitbasiert oder regelbasiert)
- E-Mail-Benachrichtigung bei Freigabe

## Testszenarien

### Bei LeitstandAktiv = false

| Test | Erwartung |
|------|-----------|
| Menü für Picker | "Werkstattaufträge" + "Kommissionierung" (wie bisher) |
| Kommissionierung öffnen | Dropdown-Auswahl (wie bisher) |
| WA-Liste | Keine Freigabe-Spalte |

### Bei LeitstandAktiv = true

| Test | Erwartung |
|------|-----------|
| Leitstand: Produktionsaufträge | Freigabe-Spalte mit Checkboxen + Priorität |
| Leitstand: Einzelfreigabe | Badge "Freigegeben", ReleasedAt/By in DB, Auto-Priorität vorgeschlagen |
| Leitstand: Massenfreigabe | Mehrere Aufträge gleichzeitig, aufsteigende Prioritäten |
| Leitstand: Priorität ändern | AJAX-Save, kein Reload |
| Leitstand: Freigabe zurücknehmen | Auftrag verschwindet aus Kommissionierliste, PickingItems bleiben |
| Kommissionierer: Menü | Nur "Kommissionierung" (+ Badge mit Anzahl) |
| Kommissionierer: Kommissionierliste | Tabelle mit Prio, WA, Artikel, Komm.-Termin, Status |
| Kommissionierer: Klick auf Auftrag | Öffnet Stückliste (Bom-View) |
| Kommissionierer: Auftrag abschließen | Verschwindet sofort aus Liste |
| Tracking-User | Sieht "Produktionsaufträge" read-only (keine Freigabe) |
| Admin | Sieht beides: Produktionsaufträge + Kommissionierung |
| Freigabe ohne Artikelnummer | Fehlermeldung, Freigabe wird verhindert |
