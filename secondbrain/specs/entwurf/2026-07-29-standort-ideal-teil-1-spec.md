---
type: spec
title: "IDEAL-Standort Teil 1 — Struktur-Fundament FaHierarchyNode (Import FAListe/FAInfos)"
slug: 2026-07-29-standort-ideal-teil-1-spec
status: Entwurf
created: 2026-08-06
updated: 2026-08-06
source_backlog: "[[2026-07-29-Standort-IDEAL]]"
depends_on: ""
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Models/FaHierarchyNode.cs (neu)
  - IdealAkeWms/Models/FaHierarchyOrderInfo.cs (neu)
  - IdealAkeWms/Data/ApplicationDbContext.cs
  - IdealAkeWms/Data/Repositories/IFaHierarchyNodeRepository.cs (neu)
  - IdealAkeWms/Data/Repositories/FaHierarchyNodeRepository.cs (neu)
  - IdealAkeWms/Data/Repositories/CachedFaHierarchyNodeRepository.cs (neu)
  - IdealAkeWms/Data/Repositories/IFaHierarchyOrderInfoRepository.cs (neu)
  - IdealAkeWms/Data/Repositories/FaHierarchyOrderInfoRepository.cs (neu)
  - IdealAkeWms/Program.cs (DI-Registrierung Decorator)
  - IdealAkeWms/Models/ServiceSettingDefinitions.cs
  - IDEALAKEWMSService/Services/FaHierarchySyncService.cs (neu)
  - IDEALAKEWMSService/Services/FaHierarchySql.cs (neu, Whitelist-Regex + SQL-Aufbau)
  - IDEALAKEWMSService/Services/SyncLogServices.cs
  - IDEALAKEWMSService/Workers/SyncWorker.cs (neuer Sync-Block, RunResilientAsync)
  - SQL/86_AddFaHierarchy.sql (neu, naechste freie Nummer — vor Dev-Lauf pruefen)
  - SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql (neu, DDL-Dokumentation)
  - SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAInfos.sql (neu, DDL-Dokumentation)
  - SQL/00_FreshInstall.sql
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Exaktes Whitelist-Regex-Pattern fuer View-Namen im Dev-Lauf mit Sicherheitsfokus festlegen (Fehlerverhalten bereits geklaert: Reject + Log + Fehlermail, Freigabe-Antwort 4) — kein Blocker"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Der IdealAkeWms soll am zweiten Standort **IDEAL** produktiv laufen (eigenes Deployment, eigene
DB, eigene Sage-Installation — kein Multi-Tenant-Code, siehe Backlog-Notiz). IDEAL bildet
Fertigungsauftraege **mehrstufig** ab (Haupt-FA → Sub-FA → Sub-Sub-FA), waehrend das heutige WMS
(`ProductionOrders`) eine flache, eindeutige `OrderNumber` voraussetzt. Teil 1 legt das
**Fundament**: die IDEAL-Struktur wird getreu aus den beiden Sage-Views
`vw_IDEAL-AKE_Kommissionierung_FAListe` (Stueckliste je Struktur) und
`vw_IDEAL-AKE_Kommissionierung_FAInfos` (PPS-Auftragsdaten) in eine **eigene** Tabelle importiert
— ohne den bestehenden `ProductionOrders`-Kern anzufassen. Damit sind IDEAL-Daten im System und
abfragbar; darauf setzen die Anzeige- und Listen-Features (Teil 2–5) auf, ohne dass irgendein
Schema-Umbau oder eine Einwegtuer noetig waere (Entscheidung B5).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Zwei neue Tabellen: `FaHierarchyNode` (Projektion von `FAListe`, Struktur **und** Stueckliste —
  ein Lesepfad, zwei spaetere Projektionen laut B4) und `FaHierarchyOrderInfo` (Projektion von `FAInfos`,
  PPS-/Auftragsdaten).
- Sync-Service im Windows-Service, der beide Views periodisch liest (raw SQL gegen konfigurierbare
  View-Namen, Whitelist-Regex gegen Injection) und die lokalen Tabellen per Full-Refresh
  aktualisiert.
- Datenverfuegbarkeits-Regel: Structure-Zeilen werden nur importiert, wenn zum `HauptFA` ein
  `FAInfos`-Eintrag existiert (INNER JOIN, kein LEFT JOIN — sonst Anzeige unvollstaendiger
  PPS-Daten).
- Repository + Cache-Decorator (ADR 0001) fuer den Web-Lesezugriff auf die importierten Tabellen.
- View-DDL-Dokumentation unter `SQL/sage-views/` (keine WMS-Migration, reine Doku der Fremd-DB).
- Aktivitaets-Protokoll-Pflicht (ADR 0010) fuer den neuen Sync.
- Feature-/Sync-Toggle ueber `ServiceSettingDefinitions` (ADR 0008).

**Out-of-Scope**
- `ProductionOrders` bleibt **vollstaendig unangetastet** (Schema, Daten, Verhalten). Keine
  Materialisierung — das ist Teil 7.
- Keine UI/Anzeige der importierten Daten — reine Backend-Grundlage. Baumanzeige = Teil 2,
  Kommissionierlisten/Beschichtung/Vormontage = Teil 3–5.
- Kein Standort-Umschalter fuer AKE — `vw_AKE_Kommissionierung_WAListe` bleibt die Datenquelle der
  AKE-Linie, unveraendert.
- Keine Rollen-/Zugriffsaenderungen (Teil 1 hat keine erreichbare Route, reiner Hintergrund-Import
  + Repository-Schicht).

## Fachliche Anforderungen

1. **Mehrstufigkeit (B1, entschieden).** Die View liefert einen echten Elternzeiger `VaterFA`
   (BelID des uebergeordneten Sub-FA, `NULL` nur bei der Wurzel) und `SubFA` (eigene BelID des
   Nachfolgers, `0` = Blatt). `FaHierarchyNode` bildet das 1:1 ab — **kein** zweistufiges Modell.
   Die alten Alt-Entscheidungen „zwei Ebenen" (D1/D3 aus der historischen Referenz-Spec) sind
   ueberholt und duerfen nicht als Vorlage dienen.
2. **Datenverfuegbarkeits-Regel.** Eine `FAListe`-Zeile wird nur dann importiert, wenn zum
   `HauptFA` mindestens ein `FAInfos`-Datensatz existiert (`FA_Nr` gefuellt **und** `Status`
   gefuellt). Import-SQL joint deshalb `FAListe INNER JOIN FAInfos ON i.HauptFA = f.HauptFA` —
   **kein** LEFT JOIN.
3. **Kombinationsgeraete (Freigabe-Antwort 1: in Teil 1 wie normale Auftraege behandeln).**
   Kombinationsgeraete teilen sich denselben `HauptFA` und werden erst ueber `[Montage-Abteilung]`
   aus `FAInfos` unterscheidbar; `FAListe`-Zeilen selbst tragen **keine** Montage-Abteilung (die ist
   auftragsbezogen, nicht positionsbezogen — nicht mit dem positionsbezogenen `Arbeitsbereich`
   verwechseln). Fuer den reinen Struktur-Import (Teil 1) ist **keine Sonderbehandlung** noetig: die
   Zeilen werden wie bei jedem anderen Auftrag getreu importiert. `MontageAbteilung` wird als
   **informatives** Feld auf `FaHierarchyOrderInfo` mitgefuehrt — **nicht** als kuenstlicher
   Struktur-Schluessel auf `FaHierarchyNode`. Die Frage, wie sich Kombinationsgeraete bei der
   spaeteren Materialisierung nach `ProductionOrders` auf die dann nicht mehr eindeutige
   `OrderNumber` auswirken, gehoert zu **Teil 7** und wird dort entschieden — nicht hier.
4. **`FAListe` ist die Stueckliste, nicht nur eine FA-Liste (B4).** Eine Zeile ist entweder die
   Wurzel (`VaterFA IS NULL`, `Position IS NULL`, `SubFA = HauptFA`) oder eine Position in der
   Stueckliste eines Vater-Sub-FA. `SubFA <> 0` markiert eine hausintern gefertigte Baugruppe mit
   eigenem FA, `SubFA = 0` ein Blatt (Kaufteil/Endmaterial). `FaHierarchyNode` ist damit **die
   einzige** Quelle sowohl fuer die Struktur- als auch fuer die Stueckliste-Projektion — kein
   zweiter, separat driftender Import.
5. **Fachliche Attribute** je Position (siehe Anhang-Spaltenliste): `HauptArtnr`, `Artnr`,
   `Matchcode`, `Bezeichnung1/2`, `Sollmenge`, `Fertigungmenge`, `Beschaffungsartikel`,
   `Artikelgruppe`, `Hauptlagerplatz`, `BemerkungPN`, `FertigungsInfoPN`, `Kommissionieren`,
   `Arbeitsbereich`, `Arbeitsschritte`, `Artikeltyp`, `Beschichtet`, `Material`, `Breite`,
   `Hoehe`, `Tiefe`, `EKBedarf`, `VMBedarf` — 1:1 aus der View uebernommen, roh gespeichert (keine
   Normalisierung beim Import; `Artikelgruppe`-Split „CODE - Bezeichnung" und
   `Arbeitsschritte`-Split (Leerzeichen, nicht Komma!) passieren **beim Konsum**, nicht beim
   Import, analog zum bestehenden BOM-Matching-Fallstrick).
6. **`FaHierarchyOrderInfo`-Attribute:** `ABNr`, `Pos`, `Kunde`, `KO_Termin`, `FE_Termin`,
   `MontageAbteilung`, `HauptFA`, `Status`, `Start_Beschichtung`, `Dienstleister`,
   `Montagestunden`, `Prio`, `RAL`, `Beschichten_Retour`, `Neuer_PT_PPS`, `Verladetermin_Vsl`,
   `Bemerkung_Uhrzeit`.
7. **Sage-Booleans.** `Beschaffungsartikel` (`Ja`/`Nein`) und `Beschichtet` (`-1`/`0`) werden beim
   Import in echte `bit`-Spalten uebersetzt (`Beschichtet = -1` ⇒ `true` — Sage-VB6-Konvention,
   siehe Fallstrick „Sage VB6-Booleans"). `EKBedarf` kommt laut Anhang bereits als Bit/Ja-Nein —
   Mapping analog absichern.
8. **View-Namen konfigurierbar.** `FAListe`- und `FAInfos`-View-Name sind je Standort
   unterschiedlich benennbar (Testsystem heisst `IDEAL_TEST_2026_05_03`, Produktivname noch offen
   — siehe offene Rueckfrage 2) und werden **nicht** hartkodiert, sondern aus `ServiceSettings`
   gelesen und gegen eine Whitelist-Regex geprueft, bevor sie in den SQL-Text eingesetzt werden
   (Objektnamen lassen sich in T-SQL nicht parametrisieren).

## Technischer Loesungsentwurf

### Datenmodell

**`FaHierarchyNode`** (NICHT `AuditableEntity` — reine Cache-Tabelle, analog `CachedBomHeader`,
siehe Fallstrick-Praezedenzfall):

| Spalte | Typ | Herkunft | Bemerkung |
|---|---|---|---|
| `Id` | `int` PK identity | — | technischer Key |
| `HauptFA` | `int NOT NULL` | `HauptFA` | Index (nicht unique — mehrere Zeilen je Struktur) |
| `VaterFA` | `int NULL` | `VaterFA` | Index |
| `SubFA` | `int NOT NULL` | `SubFA` | `0` = Blatt |
| `Position` | `int NULL` | `Position` | |
| `HauptArtnr`, `Artnr`, `Matchcode`, `Bezeichnung1`, `Bezeichnung2`, `Artikelgruppe`, `Hauptlagerplatz`, `BemerkungPN`, `FertigungsInfoPN`, `Kommissionieren`, `Arbeitsbereich`, `Arbeitsschritte`, `Artikeltyp`, `Material`, `VMBedarf` | `nvarchar` (Laenge je Quelle, grosszuegig) | 1:1 | roh, unnormalisiert |
| `Sollmenge`, `Fertigungmenge`, `Breite`, `Hoehe`, `Tiefe` | `decimal(18,4)` | 1:1 | |
| `Beschaffungsartikel`, `Beschichtet`, `EKBedarf` | `bit NOT NULL` | gemappt | siehe Anforderung 7 |
| `SyncedAt` | `datetime2 NOT NULL` | — | Zeitpunkt des letzten Full-Refresh (Lokalzeit, analog `CachedBomHeader.CachedAt`) |

**`FaHierarchyOrderInfo`** (ebenfalls kein `AuditableEntity`):

| Spalte | Typ | Herkunft |
|---|---|---|
| `Id` | `int` PK identity | — |
| `HauptFA` | `int NOT NULL` | Index |
| `MontageAbteilung` | `nvarchar(200) NULL` | `[Montage-Abteilung]` — Teil des fachlichen Schluessels bei Kombinationsgeraeten |
| `ABNr`, `Pos`, `Kunde`, `Status`, `Dienstleister`, `RAL`, `Bemerkung_Uhrzeit` | `nvarchar` | 1:1 |
| `KO_Termin`, `FE_Termin`, `Start_Beschichtung`, `Beschichten_Retour`, `Neuer_PT_PPS`, `Verladetermin_Vsl` | `datetime2 NULL` | 1:1 |
| `Montagestunden` | `decimal(18,2) NULL` | 1:1 |
| `Prio` | `int NULL` | 1:1 |
| `SyncedAt` | `datetime2 NOT NULL` | — |

**Warum zwei Tabellen statt einer denormalisierten:** `FAInfos`-Felder sind **auftragsbezogen**
(ein `KO_Termin` gilt fuer die ganze Struktur), `FAListe`-Felder sind **positionsbezogen** (eine
Struktur hat potenziell hunderte Positionszeilen). Wuerde man die Auftragsfelder in jede
Positionszeile denormalisieren, vervielfachte sich die Speichermenge und ein Termin-Update muesste
in hunderten Zeilen synchron gehalten werden. Der Anhang schlaegt selbst zwei Domain-Objekte vor
(`FaListEntry`/`FaInfoEntry`) — diese Spec bildet das 1:1 auf zwei Tabellen ab. Die
„ein Lesepfad, zwei Projektionen"-Aussage aus B4 bezieht sich auf **Struktur-Ansicht** und
**Stueckliste-Ansicht**, die beide **aus `FaHierarchyNode` allein** ableitbar sind — nicht auf eine
Verschmelzung mit `FaHierarchyOrderInfo`.

### Repository-Schicht (ADR 0001)

- `IFaHierarchyNodeRepository` / `FaHierarchyNodeRepository` (EF-Zugriff auf die lokale Tabelle:
  `GetByHauptFaAsync`, `GetAllAsync` mit Filtern) + `CachedFaHierarchyNodeRepository`-Decorator
  (`IMemoryCache`, 5 min analog zum BOM-Cache — Tabelle wird ohnehin nur alle paar Minuten vom
  Sync-Service neu befuellt, ein Web-seitiger Cache reduziert wiederholte Reads bei
  Baum-/Listen-Aufrufen).
- `IFaHierarchyOrderInfoRepository` / `FaHierarchyOrderInfoRepository` analog, ohne Cache-Decorator vorerst (kleine
  Tabelle, ein Datensatz je Struktur/Montage-Abteilung).
- Beide Repositories liefern die EF-Entitaeten direkt als Lesemodell (keine zusaetzliche
  DTO-Schicht noetig — die Tabellen sind bereits eine getreue, flache Projektion).

### Sync-Service (Windows-Service, `IDEALAKEWMSService`)

- `FaHierarchySyncService` (neuer Service-Name in `SyncLogServices.All`), gated ueber
  `Sync:HierarchicalFaEnabled` (Bool, Default `false`).
- View-Namen aus `ServiceSettings`: `Sync:FaHierarchyListeViewName` (String, Default
  `[vw_IDEAL-AKE_Kommissionierung_FAListe]`), `Sync:FaHierarchyInfosViewName` (String, Default
  `[vw_IDEAL-AKE_Kommissionierung_FAInfos]`) — beide neue Eintraege in
  `ServiceSettingDefinitions.All` (Drift-Guard-Pflicht, ADR 0008).
- **Whitelist-Regex** vor jedem SQL-Aufbau (`FaHierarchySql.ValidateViewName`): erlaubt nur
  `[Schema].[Name]`- bzw. `Name`-Muster aus Buchstaben, Ziffern, `_`, `-`, `.`, eckigen Klammern —
  **kein** Leerzeichen, Semikolon, Kommentarzeichen (`--`, `/*`). Bei Verstoss: Lauf bricht mit
  `FinishFailedAsync` ab, **kein** SQL wird ausgefuehrt (exaktes Pattern und Fehlerverhalten sind
  offene Rueckfrage 4 — sicherheitskritisch genug, um nicht erraten zu werden).
- **Full-Refresh-Strategie:** Die Struktur-Tabelle ist laut Notiz „ein Cache, der jederzeit
  komplett neu aufgebaut werden darf". Der Sync-Lauf liest beide Views komplett, baut die
  Zielzeilen im Speicher auf und ersetzt den Tabelleninhalt **in einer Transaktion**
  (Delete-All + Bulk-Insert je Tabelle) — kein inkrementelles Delta, kein MERGE-Aufwand. Guard:
  leerer View-Read (0 Zeilen) → **kein** Replace, Warn + Fehlermail (schuetzt vor
  Blindloeschung bei einem View-/Connection-Ausfall — analog zum Reconciler-Guard).
- Protokoll (ADR 0010): `ISyncLogger` als letzter Ctor-Parameter, Counts `neu`/`geloescht`
  (deutschsprachig, hier praktisch „komplette Ersetzung" abgebildet als zwei Zahlen), eigener Lauf
  getrennt vom `ProductionOrder`-Import.
- Sync-Block in `SyncWorker` ueber `RunResilientAsync` gekapselt (ein Fehler killt die anderen
  Sync-Bloecke nicht).

### Migrations-/SQL-Auswirkungen

1. Model → `dotnet ef migrations add AddFaHierarchy` (aktueller Timestamp!) → idempotentes
   `SQL/86_AddFaHierarchy.sql` mit `OBJECT_ID`-Guard, Tabellen-DDL in eigenem Batch (`GO`),
   `__EFMigrationsHistory`-Insert in separatem Batch.
2. `SQL/00_FreshInstall.sql` an **beiden** Stellen nachziehen: Schema-Objekte (beide neuen
   Tabellen) **und** `MigrationId` im History-Insert-Block.
3. **Additive Migration** — kein Datenverlust, kein Backup-Hinweis noetig (neue, leere Tabellen).
4. `SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql` +
   `..._FAInfos.sql`: View-DDL-Dokumentation der Fremd-DB — **keine** WMS-Migration, nur
   Versionskontrolle der Sage-Objekte (wie in der Notiz vereinbart).
5. **Migrationsnummer beim Dev-Start final festlegen.** `SQL/82`/`83` sind durch v1.28.0
   (Sage-Lagerbuchungen) belegt; die WmsBugs-Batches (v1.29.0/v1.30.0) belegen `84`/`85` + Seed `86`.
   IDEAL-Migrationen liegen damit voraussichtlich **ab `SQL/87`** — die konkrete naechste freie
   Nummer unmittelbar vor dem Dev-Lauf gegen den dann gemergten Stand pruefen (die `86`-Referenzen
   in dieser Spec sind Platzhalter).

### Audit-Feld-Auswirkungen

`FaHierarchyNode` und `FaHierarchyOrderInfo` sind **keine** `AuditableEntity` — analog zu `CachedBomHeader`/
`CachedBomItem` (dokumentierte Ausnahme: reine, vom Sync-Service befuellte Cache-Tabellen ohne
manuelle Bearbeitung durch Anwender). Nachvollziehbarkeit kommt stattdessen aus dem
Aktivitaets-Protokoll (`SyncLog`, ADR 0010) des `FaHierarchySyncService`-Laufs, nicht aus
`ModifiedBy`/`ModifiedAt`-Feldern auf den Zeilen selbst. Kein bestehendes Audit-Feld ist betroffen,
da `ProductionOrders` unangetastet bleibt.

## Akzeptanzkriterien

1. Bei deaktiviertem `Sync:HierarchicalFaEnabled` (Default) laeuft der Service unveraendert wie
   heute — kein neuer Sync-Block wird ausgefuehrt, keine Fehlermeldung.
2. Ist der Toggle aktiv und beide View-Namen gueltig, fuellt ein Lauf `FaHierarchyNode` und
   `FaHierarchyOrderInfo` vollstaendig aus den konfigurierten Views; eine `FAListe`-Zeile erscheint **nur**,
   wenn zum `HauptFA` ein `FAInfos`-Eintrag existiert (Datenverfuegbarkeits-Regel, testbar durch
   gezieltes Fehlen eines `FAInfos`-Datensatzes am Testsystem).
3. Ein ungueltiger View-Name (z. B. mit Leerzeichen oder `;`) fuehrt zu einem fehlgeschlagenen,
   protokollierten Lauf (`FinishFailedAsync`) **ohne** SQL-Ausfuehrung gegen die Sage-DB.
4. Ein leerer View-Read (0 Zeilen von einer oder beiden Views) loest **keinen** Replace der
   Zieltabellen aus, sondern einen Warn-Eintrag + Fehlermail — bestehende Daten bleiben erhalten.
5. `Beschaffungsartikel`/`Beschichtet` werden korrekt nach Sage-VB6-Konvention (`-1` = wahr)
   gemappt — verifiziert an mindestens einer bekannten Test-Struktur mit beschichteten und
   nicht-beschichteten Positionen.
6. `ProductionOrders` (Schema, Zeilenzahl, Verhalten aller bestehenden Controller) ist nach diesem
   Teil **byte-identisch unveraendert** zum Vor-Zustand (Regressionsnachweis: bestehende
   AKE-Testszenarien laufen unveraendert durch).
7. `dotnet build` + `dotnet test` sind gruen; der neue Sync-Pfad (raw SQL) ist gemaess
   Projekt-Konvention **nicht** vollstaendig InMemory-testbar — der Whitelist-Regex-Helfer
   (`FaHierarchySql.ValidateViewName`) ist als eigenstaendiger, unit-testbarer Baustein
   auszulegen (analog `ProductionOrderReconciler`/`LagerbestandZeroingPlanner`), damit wenigstens
   die Injection-Abwehr automatisiert geprueft ist.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` („IDEAL Teil 1 — Struktur-Import"):

- **Vorbedingung:** Zugriff auf das IDEAL-Testsystem (`AKESQL20.ake.at` / `IDEAL_TEST_2026_05_03`
  laut Anhang), `Sync:HierarchicalFaEnabled = true`, View-Namen korrekt konfiguriert.
- **Schritt 1 — Erstimport:** Service-Lauf ausloesen, Aktivitaets-Protokoll pruefen (Lauf
  erfolgreich, Counts plausibel).
- **Schritt 2 — Datenverfuegbarkeits-Regel:** Eine bekannte Struktur ohne `FAInfos`-Eintrag
  darf **nicht** in `FaHierarchyNode` erscheinen.
- **Schritt 3 — Mehrstufigkeit:** Eine bekannte Struktur mit Sub-Sub-FA (Baugruppe unter
  Baugruppe) pruefen — `VaterFA`-Kette laesst sich bis zur Wurzel zurueckverfolgen.
- **Schritt 4 — Kombinationsgeraet:** Falls am Testsystem vorhanden, eine `HauptFA` mit zwei
  `MontageAbteilung`-Werten in `FaHierarchyOrderInfo` identifizieren und pruefen, dass die
  zugehoerigen `FaHierarchyNode`-Zeilen **wie bei einem normalen Auftrag** importiert werden
  (Freigabe-Antwort 1 — keine Sonderbehandlung in Teil 1). Die materialisierungsseitige Behandlung
  gehoert zu Teil 7.
- **Negativfall — ungueltiger View-Name:** `Sync:FaHierarchyListeViewName` auf einen Wert mit
  Semikolon setzen, Lauf ausloesen, erwarten: fehlgeschlagener, protokollierter Lauf, keine
  SQL-Ausfuehrung (per Server-seitigem Audit/Profiler oder Code-Review bestaetigt).
- **Regressionsfall:** Alle bestehenden AKE-Testszenarien (FA-Liste, Kommissionierung, BDE)
  unveraendert durchspielen — kein Unterschied zum Vor-Zustand.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen.

## Deploy

- **Web-App:** ja (neue Repository-/Model-Klassen, DI-Registrierung — auch wenn noch keine
  Controller/Views darauf zugreifen).
- **Service:** ja (neuer Sync-Block).
- **Migration:** ja (`SQL/86_AddFaHierarchy.sql` additiv, kein Backup-Zwang).
- **Reihenfolge:** DB-Migration vor Service-Neustart; Web kann parallel deployt werden, da Teil 1
  keine erreichbare Route hinzufuegt. Sync-Toggle bleibt nach dem Deploy **default aus** — muss am
  Zielsystem bewusst aktiviert werden (analog zur ADR-0008-Regel „jeder gewuenschte Sync muss
  einmalig aktiviert werden").
- **Publish-Befehle:**
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```
  (provisorisch — vom Dev-Lauf gegen den tatsaechlichen Diff zu bestaetigen).

## Offene Rueckfragen

Die Schranke-1-Antworten (unten) loesen die urspruenglichen Rueckfragen — hier der Stand:

1. **Kombinationsgeraete — GEKLAERT (Antwort 1).** In Teil 1 wie normale Auftraege behandeln,
   `MontageAbteilung` nur informativ auf `FaHierarchyOrderInfo`. Materialisierungsseitige
   Konsequenz (nicht mehr eindeutige `OrderNumber`) → Teil 7.
2. **Produktiv-DB/Server — GEKLAERT (Antwort 2).** Vom Menschen notiert; Servername/DB werden direkt
   in den `appsettings` des IDEAL-Deployments gesetzt (kein Spec-Handlungsbedarf, kein Blocker).
3. **Toggle-Heimat — GEKLAERT (Antwort 3).** `ServiceSettings` bestaetigt; Namensschema
   `Sync:HierarchicalFaEnabled` (Master der hierarchischen FA-Logik) +
   `Sync:FaHierarchyListeViewName`/`Sync:FaHierarchyInfosViewName`, ohne Standort im Bezeichner
   (siehe Namens-Hinweis und Umsetzungsnotiz unten).
4. **Whitelist-Regex — Fehlerverhalten GEKLAERT (Antwort 4):** bei Verstoss Ablehnen + Protokoll +
   **zusaetzliche Fehlermail**. OFFEN bleibt nur das **exakte Regex-Pattern**, im Dev-Lauf mit
   Sicherheitsfokus festzulegen (kein Blocker, siehe `open_questions`).
5. **Alter Worktree `ideal-anpassungen-v1` — GEKLAERT (Antwort 5):** existiert nicht mehr; keine
   Test-Altlast wiederzuverwenden.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)
HINWEIS: ICH würde das nicht IDEALFASTRUKTUR etc. Nennen sondern in die Richtung FAHierarchyStruktur, dh. nicht den Standort in die Namensgebung 
1. →kombigeräte können im step 1 wie normale aufträge behandelt werden.
2. →servernamen und db habe ich bei mir notiert. ändere ich dann selber in den appsettings
3. →toggle für HierarchischeFA Logik und die anderen toggle auch. 
4. →zusätzliches Fehlermail
5. →nein, existiert nicht mehr

## Umsetzungsnotiz — Namens-Konvention (2026-08-06)

Der Namens-Hinweis aus den Freigabe-Antworten ist eingearbeitet: **kein Standort ("Ideal") in
Code-Bezeichnern**, stattdessen konzeptbasiert `FaHierarchy*` / `HierarchicalFa`. Umgesetzt in
**allen** Teil-Specs (1–8 + Uebersicht). Zuordnung:

| alt | neu |
|---|---|
| `IdealFaStruktur` (Tabelle/Model) | `FaHierarchyNode` |
| `IdealFaInfo` | `FaHierarchyOrderInfo` |
| Repos / Sync-Service / SQL-Helper | `FaHierarchyNodeRepository`, `FaHierarchyOrderInfoRepository`, `FaHierarchySyncService`, `FaHierarchySql` |
| `Sync:IdealFaStrukturEnabled` | `Sync:HierarchicalFaEnabled` |
| `Sync:IdealFaListeViewName` / `…InfosViewName` | `Sync:FaHierarchyListeViewName` / `…InfosViewName` |
| Migration/SQL `AddIdealFaStruktur` | `AddFaHierarchy` |
| Teil 3/4/5 `IdealKommissionierListen*` / `IdealBeschichtung*` / `IdealVormontage*` | `FaHierarchyKommissionierListen*` / `FaHierarchyBeschichtung*` / `FaHierarchyVormontage*` |

**Bewusst unveraendert:** der Projektname `IdealAkeWms` (Solution/Namespace/Pfade), die Prosa-Verweise
auf den Standort **IDEAL** (Dokumentation), die Sage-View-Namen `vw_IDEAL-AKE_Kommissionierung_*`
(Fremd-DB-Objekte) sowie die Spec-Slugs/-Titel — der Hinweis betrifft ausschliesslich **neue
fachliche Code-Bezeichner**. Exakte Schreibweise im Dev-Lauf bestaetigbar.
