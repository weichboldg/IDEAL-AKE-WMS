# Bedarfsmeldungen aus Stückliste — Design-Spezifikation

**Datum:** 2026-04-02  
**Status:** Entwurf  
**Autor:** Gerald Weichbold + Claude

---

## Zusammenfassung

Aus der Stückliste (BOM) eines Werkstattauftrags sollen Fehlteile per Button als **interne Bedarfsmeldung** erfasst und per **E-Mail** an konfigurierbare Empfängergruppen versendet werden können. Beim Wareneingang werden offene Bedarfsmeldungen angezeigt und können mit der Einbuchung verknüpft werden.

---

## 1. Datenmodell

### 1.1 Tabelle `OrderRecipientGroups`

Empfängergruppen (z.B. "Blechfertigung", "Lager", "Einkauf").

| Spalte | Typ | Constraint | Beschreibung |
|--------|-----|-----------|--------------|
| `Id` | `int` | PK, Identity | Primärschlüssel |
| `Name` | `nvarchar(200)` | NOT NULL | Gruppenname |
| `Description` | `nvarchar(500)` | NULL | Optionale Beschreibung |
| `CreatedAt` | `datetime2` | NOT NULL | AuditableEntity |
| `CreatedBy` | `nvarchar(200)` | NULL | AuditableEntity |
| `CreatedByWindows` | `nvarchar(200)` | NULL | AuditableEntity |
| `ModifiedAt` | `datetime2` | NULL | AuditableEntity |
| `ModifiedBy` | `nvarchar(200)` | NULL | AuditableEntity |
| `ModifiedByWindows` | `nvarchar(200)` | NULL | AuditableEntity |

### 1.2 Tabelle `OrderRecipients`

Einzelne Empfänger innerhalb einer Gruppe.

| Spalte | Typ | Constraint | Beschreibung |
|--------|-----|-----------|--------------|
| `Id` | `int` | PK, Identity | Primärschlüssel |
| `OrderRecipientGroupId` | `int` | FK → OrderRecipientGroups, NOT NULL | Zugehörige Gruppe |
| `Name` | `nvarchar(200)` | NOT NULL | Empfängername |
| `Email` | `nvarchar(300)` | NOT NULL | E-Mail-Adresse |
| `IsActive` | `bit` | NOT NULL, Default 1 | Aktiv/Inaktiv |
| `CreatedAt` | `datetime2` | NOT NULL | AuditableEntity |
| `CreatedBy` | `nvarchar(200)` | NULL | AuditableEntity |
| `CreatedByWindows` | `nvarchar(200)` | NULL | AuditableEntity |
| `ModifiedAt` | `datetime2` | NULL | AuditableEntity |
| `ModifiedBy` | `nvarchar(200)` | NULL | AuditableEntity |
| `ModifiedByWindows` | `nvarchar(200)` | NULL | AuditableEntity |

### 1.3 Tabelle `ArticleGroupRecipientMappings`

N:M-Zuordnung Artikelgruppe ↔ Empfängergruppe. Eine Artikelgruppe kann mehreren Empfängergruppen zugeordnet sein, und eine Empfängergruppe kann mehrere Artikelgruppen abdecken.

| Spalte | Typ | Constraint | Beschreibung |
|--------|-----|-----------|--------------|
| `Id` | `int` | PK, Identity | Primärschlüssel |
| `ArticleGroup` | `nvarchar(100)` | NOT NULL | Artikelgruppe (aus BOM/Article) |
| `OrderRecipientGroupId` | `int` | FK → OrderRecipientGroups, NOT NULL | Zugeordnete Empfängergruppe |
| `CreatedAt` | `datetime2` | NOT NULL | AuditableEntity |
| `CreatedBy` | `nvarchar(200)` | NULL | AuditableEntity |
| `CreatedByWindows` | `nvarchar(200)` | NULL | AuditableEntity |
| `ModifiedAt` | `datetime2` | NULL | AuditableEntity |
| `ModifiedBy` | `nvarchar(200)` | NULL | AuditableEntity |
| `ModifiedByWindows` | `nvarchar(200)` | NULL | AuditableEntity |

- **Unique Composite** auf `(ArticleGroup, OrderRecipientGroupId)` — verhindert Doppel-Zuordnungen
- **Index** `IX_ArticleGroupRecipientMappings_ArticleGroup` (non-unique) für Lookups

### 1.4 Tabelle `PartRequisitions`

Die eigentliche Bedarfsmeldung.

| Spalte | Typ | Constraint | Beschreibung |
|--------|-----|-----------|--------------|
| `Id` | `int` | PK, Identity | Primärschlüssel |
| `ProductionOrderId` | `int` | FK → ProductionOrders, NOT NULL | Auslösender Werkstattauftrag |
| `ArticleNumber` | `nvarchar(100)` | NOT NULL | Ressourcenummer des Bedarfsteils |
| `ArticleDescription` | `nvarchar(500)` | NULL | Bezeichnung (Snapshot zum Meldezeitpunkt) |
| `ArticleGroup` | `nvarchar(100)` | NULL | Artikelgruppe (Snapshot zum Meldezeitpunkt) |
| `Position` | `nvarchar(50)` | NULL | BOM-Position (z.B. "15.1.2") |
| `Quantity` | `decimal(18,3)` | NOT NULL | Benötigte Menge |
| `Unit` | `nvarchar(20)` | NULL | Einheit (Stück, kg, m, ...) |
| `Status` | `nvarchar(20)` | NOT NULL, Default `'Offen'` | `Offen`, `Erfuellt`, `Storniert` |
| `Priority` | `nvarchar(20)` | NOT NULL, Default `'Normal'` | `Normal`, `Dringend`, `Eilt` |
| `Notes` | `nvarchar(1000)` | NULL | Freitext-Bemerkung des Bestellers |
| `OrderRecipientGroupId` | `int` | FK → OrderRecipientGroups, NULL | Empfängergruppe (Nachverfolgung) |
| `SentToEmails` | `nvarchar(1000)` | NULL | Tatsächlich verwendete E-Mail-Adressen (kommasepariert) |
| `EmailSentAt` | `datetime2` | NULL | Zeitpunkt des E-Mail-Versands (NULL = noch nicht gesendet) |
| `FulfilledByStockMovementId` | `int` | FK → StockMovements, NULL | Referenz zum auflösenden Wareneingang |
| `FulfilledAt` | `datetime2` | NULL | Zeitpunkt der Erfüllung |
| `CancelledAt` | `datetime2` | NULL | Zeitpunkt der Stornierung |
| `CancelledBy` | `nvarchar(200)` | NULL | Wer hat storniert |
| `CreatedAt` | `datetime2` | NOT NULL | AuditableEntity (= Bestellzeitpunkt) |
| `CreatedBy` | `nvarchar(200)` | NULL | AuditableEntity (= Besteller App-Name) |
| `CreatedByWindows` | `nvarchar(200)` | NULL | AuditableEntity (= Windows-User) |
| `ModifiedAt` | `datetime2` | NULL | AuditableEntity |
| `ModifiedBy` | `nvarchar(200)` | NULL | AuditableEntity |
| `ModifiedByWindows` | `nvarchar(200)` | NULL | AuditableEntity |

### 1.5 Indizes

| Index | Spalte(n) | Typ |
|-------|----------|-----|
| `IX_PartRequisitions_ProductionOrderId` | `ProductionOrderId` | Non-unique |
| `IX_PartRequisitions_ArticleNumber` | `ArticleNumber` | Non-unique |
| `IX_PartRequisitions_Status` | `Status` | Non-unique |
| `IX_PartRequisitions_EmailSentAt_Status` | `EmailSentAt, Status` | Non-unique (für Service-Abfrage) |
| `IX_OrderRecipients_GroupId` | `OrderRecipientGroupId` | Non-unique |
| `IX_ArticleGroupRecipientMappings_ArticleGroup` | `ArticleGroup` | Non-unique |
| `UX_ArticleGroupRecipientMappings_Group_Recipient` | `(ArticleGroup, OrderRecipientGroupId)` | Unique Composite |

---

## 2. AppSettings

| Key | Default | Beschreibung |
|-----|---------|-------------|
| `BestellungenAktiv` | `false` | Globaler Schalter: Bedarfsmeldungen aktiviert |

Steuert:
- Sichtbarkeit der Bestell-Buttons in der Stückliste
- E-Mail-Versand im Service
- Sichtbarkeit des Menüpunkts "Empfänger-Verwaltung" in den Stammdaten

### Service-Konfiguration (`appsettings.json`)

| Key | Default | Beschreibung |
|-----|---------|-------------|
| `Sync:PartRequisitionEmailEnabled` | `false` | E-Mail-Versand im Service aktiv |

**SMTP:** Bestehender `MailService` + `MailSettings`-Konfiguration wird wiederverwendet (bereits vorhanden in `IDEALAKEWMSService/Services/MailService.cs`). Keine neuen SMTP-Settings nötig.

---

## 3. Workflows

### 3.1 Stückliste — Bedarfsmeldung auslösen

**Voraussetzung:** `BestellungenAktiv = true`, Anwender hat `picking`-Zugriff.

#### Einzelbestellung

1. Pro BOM-Zeile: Button mit Bestell-Icon (z.B. Warenkorb)
2. Klick öffnet **Bootstrap-Modal** mit:
   - Ressourcenummer + Bezeichnung (readonly)
   - Menge (vorbelegt mit BOM-Menge, editierbar)
   - Einheit (readonly)
   - Empfängergruppe(n) (auto-vorbelegt aus Artikelgruppe-Mapping)
   - Empfänger-Checkboxen (alle aktiven Empfänger der zugeordneten Gruppen, alle vorselektiert, einzeln abwählbar)
   - **Priorität**: Checkbox-Optionen für Standardphrasen: `Eilt`, `Dringend`, `Normal` (Default: `Normal`)
   - Bemerkung (Freitext, optional — zusätzlich zur Priorität)
3. "Bestellen"-Button: Erstellt `PartRequisition` mit `Status = 'Offen'`, `EmailSentAt = NULL`
4. Bei Priorität `Eilt` oder `Dringend`: E-Mail-Betreff enthält Prefix `[EILT]` bzw. `[DRINGEND]`

#### Sammelbestellung

1. Checkboxen an BOM-Zeilen (analog Picking-Checkboxen)
2. Button "Ausgewählte bestellen" öffnet Modal mit Tabelle aller markierten Teile
3. Pro Zeile: Menge editierbar, Empfängergruppe vorbelegt
4. Gemeinsame Priorität + Bemerkung + "Alle bestellen"-Button
5. Erstellt je eine `PartRequisition` pro Zeile

#### Doppelbestellungs-Schutz

- Beim Öffnen des Modals: Prüfung ob für diesen Artikel + WA bereits eine offene Bedarfsmeldung existiert
- Falls ja: **Warnung** im Modal anzeigen: "Bereits bestellt am DD.MM.YYYY von [Name] (Menge: X)"
- Bestellung trotzdem möglich (kein Blocker) — manchmal ist eine zweite Bestellung gewollt

#### Einheit (Unit)

- BOM-Items aus der SAGE-View haben kein Unit-Feld
- Einheit wird aus der `Articles`-Tabelle nachgeschlagen (`Article.Unit` per `ArticleNumber`)
- Falls kein Match: Feld bleibt leer (NULL)

#### Visueller Hinweis

- BOM-Zeilen mit offener Bedarfsmeldung: **oranges Badge/Icon** (z.B. Warenkorb-Symbol im AKE-Orange `#E87A1E`), damit erkennbar ist, dass bereits bestellt wurde
- Tooltip mit Info: "Bestellt am DD.MM.YYYY von [Name]"

### 3.2 Wareneingang — Offene Bedarfsmeldungen anzeigen

**Ort:** `StockMovements/Inbound` (Einbuchung)

1. Nach Auswahl des Artikels: Prüfung ob offene Bedarfsmeldungen für diese `ArticleNumber` existieren (`Status = 'Offen'`)
2. Falls ja: Tabelle unterhalb des Einbuchungs-Formulars:
   - Sortierung: `CreatedAt ASC` (älteste zuerst)
   - Spalten: **Checkbox** | WA-Nummer | Menge | Besteller | Datum | Bemerkung
3. Anwender hakt zutreffende Bedarfsmeldung(en) an
4. Beim Speichern der Einbuchung:
   - Für angehakte Meldungen: `FulfilledByStockMovementId` = neue StockMovement-Id, `FulfilledAt` = jetzt, `Status` → `Erfuellt`
   - Audit-Felder (`ModifiedAt`, `ModifiedBy`, `ModifiedByWindows`) aktualisieren

### 3.3 Stornierung

- In der Stückliste: Bei offenen Bedarfsmeldungen ein "Stornieren"-Button/Icon
- Setzt `Status` → `Storniert`, `CancelledAt`, `CancelledBy`
- Berechtigung: `picking`-Zugriff

---

## 4. E-Mail-Versand (IDEALAKEWMSService)

### 4.1 Ablauf

1. Service prüft im Sync-Intervall (alle `SyncIntervalMinutes`) auf `PartRequisitions` mit `EmailSentAt = NULL` AND `Status = 'Offen'`
2. Gruppiert Meldungen nach `SentToEmails` + `ProductionOrderId` (gleiche Empfänger + gleicher WA → eine Mail mit mehreren Teilen)
3. Baut HTML-Mail im AKE Corporate Design
4. **Betreff**: `Bedarfsmeldung — WA {OrderNumber}` bzw. `[EILT] Bedarfsmeldung — WA {OrderNumber}` / `[DRINGEND] Bedarfsmeldung — WA {OrderNumber}` je nach Priorität (höchste Priorität der enthaltenen Teile gewinnt)
5. Versendet via SMTP
5. Setzt `EmailSentAt` nach erfolgreichem Versand
6. Bei Fehler: Loggt Warnung, versucht beim nächsten Intervall erneut

### 4.2 E-Mail-Inhalt (HTML)

```
┌─────────────────────────────────────────────┐
│  [IDEAL AKE Logo]                           │
│                                             │
│  Bedarfsmeldung — Werkstattauftrag 2607151  │
│                                             │
│  Kunde:            BETEC GmbH, Dortmund     │
│  Artikelbezeichnung: Green GE-80-54-E R290  │
│  Produktionsdatum: 16.04.2026               │
│  Lieferdatum:      19.03.2026               │
│                                             │
│  ┌──────────────┬────────────┬──────┬─────┐ │
│  │ Ressourcen-Nr│ Bezeichnung│ Menge│ ME  │ │
│  ├──────────────┼────────────┼──────┼─────┤ │
│  │ 87050064     │ Seitenwand │ 2,000│ Stk │ │
│  │ 87050072     │ Rückwand   │ 1,000│ Stk │ │
│  └──────────────┴────────────┴──────┴─────┘ │
│                                             │
│  Bestellt von: Max Mustermann               │
│  Zeitpunkt:    02.04.2026, 14:30            │
│  Bemerkung:    Dringend benötigt            │
│                                             │
│  ─────────────────────────────────────────  │
│  IDEAL AKE WMS — Automatisch generiert      │
│  Farben: #053153 (Header), #43A6E2 (Akzent) │
└─────────────────────────────────────────────┘
```

### 4.3 Service-Toggle

- `Sync:PartRequisitionEmailEnabled` in `appsettings.json` — unabhängig vom Web-App-Setting `BestellungenAktiv`
- Beide müssen aktiv sein damit E-Mails tatsächlich versendet werden

---

## 5. Empfänger-Verwaltung (Stammdaten)

**Zugriff:** `RequireMasterDataAccess` (Rollen: `admin`, `masterdata`)  
**Menüpunkt:** Unter "Stammdaten" in der Navbar (nur sichtbar wenn `BestellungenAktiv = true`)

### 5.1 Empfängergruppen

- CRUD-Seite: Liste aller Gruppen mit Name, Beschreibung, Anzahl Empfänger
- Erstellen/Bearbeiten: Name (Pflicht), Beschreibung (optional)
- Löschen: Nur möglich wenn keine offenen Bedarfsmeldungen referenzieren

### 5.2 Empfänger

- Inline in der Gruppendetail-Ansicht oder als eigene Unter-Seite
- Felder: Name (Pflicht), E-Mail (Pflicht), Aktiv-Toggle
- Inaktive Empfänger werden im Bestell-Modal nicht vorselektiert

### 5.3 Artikelgruppen-Zuordnung

- Eigener Tab/Bereich in der Empfänger-Verwaltung
- Zeigt alle bekannten Artikelgruppen (aus `Articles`-Tabelle, DISTINCT)
- Pro Artikelgruppe: Multi-Select oder Checkbox-Liste der Empfängergruppen
- N:M-Beziehung — eine Artikelgruppe kann mehreren Gruppen zugeordnet sein

---

## 6. Bestellübersicht (eigene View)

**Zugriff:** `picking` oder `stock`-Zugriff (wer bestellt oder einbucht)  
**Menüpunkt:** Eigener Punkt in der Navbar (nur sichtbar wenn `BestellungenAktiv = true`)

### 6.1 Ansicht

- Tabellarische Übersicht aller Bedarfsmeldungen
- **Standard-Filter**: Nur offene Bestellungen (`Status = 'Offen'`)
- **Toggle**: "Erledigte/Stornierte anzeigen" (zeigt alle Status)
- **Sortierung**: Neueste zuerst (`CreatedAt DESC`), umschaltbar
- **Spalten**:
  - Status (farbiges Badge: orange=Offen, grün=Erfüllt, grau=Storniert)
  - Priorität (Badge: rot=Eilt, gelb=Dringend, grau=Normal)
  - WA-Nummer (Link zur Stückliste)
  - Kunde
  - Ressourcenummer + Bezeichnung
  - Menge + Einheit
  - Bestellt von (App-User)
  - Bestellt am
  - E-Mail versendet (Ja/Nein mit Zeitstempel)
  - Bemerkung
- **Aktionen**: Stornieren-Button bei offenen Meldungen
- **Spaltenfilter**: Client-seitig (wie bestehende Tabellen mit `table-filter.js`)

### 6.2 Pagination

- Server-seitig, 25 Einträge pro Seite (analog OSEON-Tracking)

---

## 7. Referenzielle Integrität

| FK-Beziehung | On Delete |
|-------------|-----------|
| `PartRequisitions.ProductionOrderId` → `ProductionOrders` | `RESTRICT` — WA mit offenen Bedarfsmeldungen kann nicht gelöscht werden |
| `PartRequisitions.OrderRecipientGroupId` → `OrderRecipientGroups` | `SET NULL` — Gruppe kann gelöscht werden, Meldung behält historische Daten (`SentToEmails`) |
| `PartRequisitions.FulfilledByStockMovementId` → `StockMovements` | `SET NULL` — WE-Löschung entfernt nur die Referenz |
| `OrderRecipients.OrderRecipientGroupId` → `OrderRecipientGroups` | `CASCADE` — Gruppe löschen entfernt zugehörige Empfänger |
| `ArticleGroupRecipientMappings.OrderRecipientGroupId` → `OrderRecipientGroups` | `CASCADE` — Gruppe löschen entfernt Zuordnungen |

---

## 8. Berechtigungen

| Aktion | Rolle(n) |
|--------|---------|
| Bedarfsmeldung auslösen (Stückliste) | `admin`, `picking` |
| Bedarfsmeldung stornieren | `admin`, `picking` |
| Bestellübersicht einsehen | `admin`, `picking`, `stock`, `stock_keyuser` |
| Offene Meldungen beim WE sehen + zuordnen | `admin`, `stock`, `stock_keyuser`, `picking` |
| Empfängergruppen/-empfänger verwalten | `admin`, `masterdata` |
| Artikelgruppen-Zuordnung verwalten | `admin`, `masterdata` |

---

## 9. Neue Dateien (geschätzt)

### Models
- `Models/OrderRecipientGroup.cs` — Entity (AuditableEntity)
- `Models/OrderRecipient.cs` — Entity (AuditableEntity)
- `Models/ArticleGroupRecipientMapping.cs` — Entity
- `Models/PartRequisition.cs` — Entity (AuditableEntity)
- `Models/PartRequisitionStatus.cs` — Statische Konstanten (`Offen`, `Erfuellt`, `Storniert`)

### Repositories
- `Data/Repositories/OrderRecipientRepository.cs` — CRUD für Gruppen + Empfänger + Mappings
- `Data/Repositories/PartRequisitionRepository.cs` — CRUD + Abfragen (nach Artikel, nach WA, offene)

### Controllers
- `Controllers/OrderRecipientsController.cs` — Stammdaten-CRUD (MVC)
- `Controllers/PartRequisitionsController.cs` — Bestellübersicht (MVC, Index + Stornieren)
- `Controllers/Api/PartRequisitionsApiController.cs` — API für Modal-Interaktion (Erstellen, Empfänger laden)

### Views
- `Views/OrderRecipients/Index.cshtml` — Empfängergruppen-Liste
- `Views/OrderRecipients/Edit.cshtml` — Gruppe bearbeiten mit Empfänger-Liste
- `Views/OrderRecipients/ArticleGroupMappings.cshtml` — Artikelgruppen-Zuordnung
- `Views/PartRequisitions/Index.cshtml` — Bestellübersicht (Tabelle mit Filter, Pagination)

### Service
- `IDEALAKEWMSService/Services/PartRequisitionEmailService.cs` — E-Mail-Versand (nutzt bestehenden `IMailService`)
- `IDEALAKEWMSService/Templates/PartRequisitionEmail.cs` — HTML-Template-Builder

### SQL
- `SQL/36_AddPartRequisitions.sql` — Migration (alle 4 Tabellen + Indizes)

### Anpassungen bestehender Dateien
- `Data/ApplicationDbContext.cs` — 4 neue DbSets + OnModelCreating
- `Views/ProductionOrders/Bom.cshtml` — Bestell-Buttons + Modal + Badge
- `Views/StockMovements/Inbound.cshtml` — Offene Bedarfsmeldungen-Tabelle
- `Views/Shared/_Layout.cshtml` — Menüpunkt "Empfänger" unter Stammdaten + Menüpunkt "Bestellungen" in Navbar
- `Program.cs` — DI-Registrierung der neuen Repositories
- `IDEALAKEWMSService/Workers/SyncWorker.cs` — E-Mail-Versand aufrufen
- `AppVersion.cs` (beide Projekte) — Version hochzählen
- `Views/Help/Changelog.cshtml` — Changelog ergänzen
- `Views/Help/Index.cshtml` — Hilfeseite ergänzen
- `SQL/00_FreshInstall.sql` — Konsolidieren
- `CLAUDE.md` — Dokumentation ergänzen
- `PROJECT_STATUS.md` — Status aktualisieren
