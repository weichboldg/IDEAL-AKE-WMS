---
type: spec
title: "IDEAL-Standort Teil 1 — Struktur-Fundament IdealFaStruktur (Import FAListe/FAInfos)"
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
  - IdealAkeWms/Models/IdealFaStruktur.cs (neu)
  - IdealAkeWms/Models/IdealFaInfo.cs (neu)
  - IdealAkeWms/Data/ApplicationDbContext.cs
  - IdealAkeWms/Data/Repositories/IIdealFaStrukturRepository.cs (neu)
  - IdealAkeWms/Data/Repositories/IdealFaStrukturRepository.cs (neu)
  - IdealAkeWms/Data/Repositories/CachedIdealFaStrukturRepository.cs (neu)
  - IdealAkeWms/Data/Repositories/IIdealFaInfoRepository.cs (neu)
  - IdealAkeWms/Data/Repositories/IdealFaInfoRepository.cs (neu)
  - IdealAkeWms/Program.cs (DI-Registrierung Decorator)
  - IdealAkeWms/Models/ServiceSettingDefinitions.cs
  - IDEALAKEWMSService/Services/IdealFaStrukturSyncService.cs (neu)
  - IDEALAKEWMSService/Services/IdealFaStrukturSql.cs (neu, Whitelist-Regex + SQL-Aufbau)
  - IDEALAKEWMSService/Services/SyncLogServices.cs
  - IDEALAKEWMSService/Workers/SyncWorker.cs (neuer Sync-Block, RunResilientAsync)
  - SQL/86_AddIdealFaStruktur.sql (neu, naechste freie Nummer — vor Dev-Lauf pruefen)
  - SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql (neu, DDL-Dokumentation)
  - SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAInfos.sql (neu, DDL-Dokumentation)
  - SQL/00_FreshInstall.sql
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "B3/B-4: FAListe traegt keine Montage-Abteilung (die ist auftragsbezogen, nur in FAInfos) — wie werden Kombinationsgeraete mit zwei Montage-Abteilungen auf Struktur-Ebene getrennt, wenn ueberhaupt?"
  - "Produktiv-DB/Server der IDEAL-Instanz (Name, Connection-String) noch unbekannt"
  - "Toggle-Heimat: ServiceSettings (Sync-Verhalten, ADR 0008) fuer View-Namen/Import-Enable bestaetigen — konsistent mit Uebersichts-Rueckfrage 2"
  - "Whitelist-Regex-Pattern fuer View-Namen und Fehlerverhalten bei Verstoss (Reject+Log vs. Exception) offen"
  - "Alter Worktree ideal-anpassungen-v1: existiert er noch (Testszenarien-Wiederverwendung V1.12.0-VERIFICATION.md)?"
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
- Zwei neue Tabellen: `IdealFaStruktur` (Projektion von `FAListe`, Struktur **und** Stueckliste —
  ein Lesepfad, zwei spaetere Projektionen laut B4) und `IdealFaInfo` (Projektion von `FAInfos`,
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
   Nachfolgers, `0` = Blatt). `IdealFaStruktur` bildet das 1:1 ab — **kein** zweistufiges Modell.
   Die alten Alt-Entscheidungen „zwei Ebenen" (D1/D3 aus der historischen Referenz-Spec) sind
   ueberholt und duerfen nicht als Vorlage dienen.
2. **Datenverfuegbarkeits-Regel.** Eine `FAListe`-Zeile wird nur dann importiert, wenn zum
   `HauptFA` mindestens ein `FAInfos`-Datensatz existiert (`FA_Nr` gefuellt **und** `Status`
   gefuellt). Import-SQL joint deshalb `FAListe INNER JOIN FAInfos ON i.HauptFA = f.HauptFA` —
   **kein** LEFT JOIN.
3. **Kombinationsgeraete (B3/B-4, NICHT abschliessend entschieden — siehe offene Rueckfrage 1).**
   `HauptFA` allein ist bei Kombinationsgeraeten kein eindeutiger Struktur-Schluessel; erst
   `[Montage-Abteilung]` aus `FAInfos` trennt sie. `FAListe`-Zeilen selbst tragen **keine**
   Montage-Abteilung (die ist laut Anhang auftragsbezogen, nicht positionsbezogen — nicht mit dem
   positionsbezogenen `Arbeitsbereich` verwechseln). Diese Spec fuehrt `MontageAbteilung` daher
   **nur** auf `IdealFaInfo` (dort ist es Teil des fachlichen Schluessels `HauptFA` +
   `MontageAbteilung`), **nicht** auf `IdealFaStruktur` — das ist eine bewusste Annahme, die am
   IDEAL-Testsystem zu verifizieren ist (siehe offene Rueckfrage 1).
4. **`FAListe` ist die Stueckliste, nicht nur eine FA-Liste (B4).** Eine Zeile ist entweder die
   Wurzel (`VaterFA IS NULL`, `Position IS NULL`, `SubFA = HauptFA`) oder eine Position in der
   Stueckliste eines Vater-Sub-FA. `SubFA <> 0` markiert eine hausintern gefertigte Baugruppe mit
   eigenem FA, `SubFA = 0` ein Blatt (Kaufteil/Endmaterial). `IdealFaStruktur` ist damit **die
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
6. **`IdealFaInfo`-Attribute:** `ABNr`, `Pos`, `Kunde`, `KO_Termin`, `FE_Termin`,
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

**`IdealFaStruktur`** (NICHT `AuditableEntity` — reine Cache-Tabelle, analog `CachedBomHeader`,
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

**`IdealFaInfo`** (ebenfalls kein `AuditableEntity`):

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
**Stueckliste-Ansicht**, die beide **aus `IdealFaStruktur` allein** ableitbar sind — nicht auf eine
Verschmelzung mit `IdealFaInfo`.

### Repository-Schicht (ADR 0001)

- `IIdealFaStrukturRepository` / `IdealFaStrukturRepository` (EF-Zugriff auf die lokale Tabelle:
  `GetByHauptFaAsync`, `GetAllAsync` mit Filtern) + `CachedIdealFaStrukturRepository`-Decorator
  (`IMemoryCache`, 5 min analog zum BOM-Cache — Tabelle wird ohnehin nur alle paar Minuten vom
  Sync-Service neu befuellt, ein Web-seitiger Cache reduziert wiederholte Reads bei
  Baum-/Listen-Aufrufen).
- `IIdealFaInfoRepository` / `IdealFaInfoRepository` analog, ohne Cache-Decorator vorerst (kleine
  Tabelle, ein Datensatz je Struktur/Montage-Abteilung).
- Beide Repositories liefern die EF-Entitaeten direkt als Lesemodell (keine zusaetzliche
  DTO-Schicht noetig — die Tabellen sind bereits eine getreue, flache Projektion).

### Sync-Service (Windows-Service, `IDEALAKEWMSService`)

- `IdealFaStrukturSyncService` (neuer Service-Name in `SyncLogServices.All`), gated ueber
  `Sync:IdealFaStrukturEnabled` (Bool, Default `false`).
- View-Namen aus `ServiceSettings`: `Sync:IdealFaListeViewName` (String, Default
  `[vw_IDEAL-AKE_Kommissionierung_FAListe]`), `Sync:IdealFaInfosViewName` (String, Default
  `[vw_IDEAL-AKE_Kommissionierung_FAInfos]`) — beide neue Eintraege in
  `ServiceSettingDefinitions.All` (Drift-Guard-Pflicht, ADR 0008).
- **Whitelist-Regex** vor jedem SQL-Aufbau (`IdealFaStrukturSql.ValidateViewName`): erlaubt nur
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

1. Model → `dotnet ef migrations add AddIdealFaStruktur` (aktueller Timestamp!) → idempotentes
   `SQL/86_AddIdealFaStruktur.sql` mit `OBJECT_ID`-Guard, Tabellen-DDL in eigenem Batch (`GO`),
   `__EFMigrationsHistory`-Insert in separatem Batch.
2. `SQL/00_FreshInstall.sql` an **beiden** Stellen nachziehen: Schema-Objekte (beide neuen
   Tabellen) **und** `MigrationId` im History-Insert-Block.
3. **Additive Migration** — kein Datenverlust, kein Backup-Hinweis noetig (neue, leere Tabellen).
4. `SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql` +
   `..._FAInfos.sql`: View-DDL-Dokumentation der Fremd-DB — **keine** WMS-Migration, nur
   Versionskontrolle der Sage-Objekte (wie in der Notiz vereinbart).
5. **Vor dem Dev-Lauf erneut pruefen, ob `SQL/86` noch frei ist** — konkurrierende Arbeit (u. a.
   die WmsBugs-Teil-7-Spec auf `84`/`85`) kann bis dahin gemergt sein.

### Audit-Feld-Auswirkungen

`IdealFaStruktur` und `IdealFaInfo` sind **keine** `AuditableEntity` — analog zu `CachedBomHeader`/
`CachedBomItem` (dokumentierte Ausnahme: reine, vom Sync-Service befuellte Cache-Tabellen ohne
manuelle Bearbeitung durch Anwender). Nachvollziehbarkeit kommt stattdessen aus dem
Aktivitaets-Protokoll (`SyncLog`, ADR 0010) des `IdealFaStrukturSyncService`-Laufs, nicht aus
`ModifiedBy`/`ModifiedAt`-Feldern auf den Zeilen selbst. Kein bestehendes Audit-Feld ist betroffen,
da `ProductionOrders` unangetastet bleibt.

## Akzeptanzkriterien

1. Bei deaktiviertem `Sync:IdealFaStrukturEnabled` (Default) laeuft der Service unveraendert wie
   heute — kein neuer Sync-Block wird ausgefuehrt, keine Fehlermeldung.
2. Ist der Toggle aktiv und beide View-Namen gueltig, fuellt ein Lauf `IdealFaStruktur` und
   `IdealFaInfo` vollstaendig aus den konfigurierten Views; eine `FAListe`-Zeile erscheint **nur**,
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
   (`IdealFaStrukturSql.ValidateViewName`) ist als eigenstaendiger, unit-testbarer Baustein
   auszulegen (analog `ProductionOrderReconciler`/`LagerbestandZeroingPlanner`), damit wenigstens
   die Injection-Abwehr automatisiert geprueft ist.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` („IDEAL Teil 1 — Struktur-Import"):

- **Vorbedingung:** Zugriff auf das IDEAL-Testsystem (`AKESQL20.ake.at` / `IDEAL_TEST_2026_05_03`
  laut Anhang), `Sync:IdealFaStrukturEnabled = true`, View-Namen korrekt konfiguriert.
- **Schritt 1 — Erstimport:** Service-Lauf ausloesen, Aktivitaets-Protokoll pruefen (Lauf
  erfolgreich, Counts plausibel).
- **Schritt 2 — Datenverfuegbarkeits-Regel:** Eine bekannte Struktur ohne `FAInfos`-Eintrag
  darf **nicht** in `IdealFaStruktur` erscheinen.
- **Schritt 3 — Mehrstufigkeit:** Eine bekannte Struktur mit Sub-Sub-FA (Baugruppe unter
  Baugruppe) pruefen — `VaterFA`-Kette laesst sich bis zur Wurzel zurueckverfolgen.
- **Schritt 4 — Kombinationsgeraet:** Falls am Testsystem vorhanden, eine `HauptFA` mit zwei
  `MontageAbteilung`-Werten in `IdealFaInfo` identifizieren und dokumentieren, wie sich die
  zugehoerigen `IdealFaStruktur`-Zeilen (nicht) trennen lassen — Grundlage fuer die Aufloesung von
  offener Rueckfrage 1.
- **Negativfall — ungueltiger View-Name:** `Sync:IdealFaListeViewName` auf einen Wert mit
  Semikolon setzen, Lauf ausloesen, erwarten: fehlgeschlagener, protokollierter Lauf, keine
  SQL-Ausfuehrung (per Server-seitigem Audit/Profiler oder Code-Review bestaetigt).
- **Regressionsfall:** Alle bestehenden AKE-Testszenarien (FA-Liste, Kommissionierung, BDE)
  unveraendert durchspielen — kein Unterschied zum Vor-Zustand.
- **Wiederverwendbare Altlast:** Falls der Worktree `ideal-anpassungen-v1` noch existiert, enthaelt
  `docs/V1.12.0-VERIFICATION.md` manuelle Testszenarien (BOM-Mengen-Check, Scan/QR-Lookup,
  Tree-Expand/Filter) — vor dem Schreiben neuer Szenarien pruefen und wiederverwenden (offene
  Rueckfrage 5).

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen.

## Deploy

- **Web-App:** ja (neue Repository-/Model-Klassen, DI-Registrierung — auch wenn noch keine
  Controller/Views darauf zugreifen).
- **Service:** ja (neuer Sync-Block).
- **Migration:** ja (`SQL/86_AddIdealFaStruktur.sql` additiv, kein Backup-Zwang).
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

1. **B3/B-4 — Montage-Abteilung auf Struktur-Ebene.** `FAListe` traegt keine Montage-Abteilung
   (nur `FAInfos`, auftragsbezogen). Bei Kombinationsgeraeten mit zwei Montage-Abteilungen und
   gleichem `HauptFA`: Teilen sich beide Auftraege dieselbe Stueckliste (dann ist die Trennung
   ausschliesslich Sache von `IdealFaInfo`, `IdealFaStruktur` braucht **keine**
   `MontageAbteilung`-Spalte), oder muss die Struktur selbst irgendwie getrennt werden (dann fehlt
   dafuer aktuell ein Datenfeld)? Am IDEAL-Testsystem zu verifizieren, bevor die Repository-Schicht
   final steht.
2. **Produktiv-DB/Server der IDEAL-Instanz.** Der Anhang nennt nur die Test-DB
   (`AKESQL20.ake.at` / `IDEAL_TEST_2026_05_03`). Produktivname/-server offen — reines
   Infrastrukturdetail, aber vor dem ersten produktiven Sync zu klaeren.
3. **Toggle-Heimat.** Sollen `Sync:IdealFaListeViewName`/`Sync:IdealFaInfosViewName`/
   `Sync:IdealFaStrukturEnabled` als `ServiceSettings` gefuehrt werden (wie hier entworfen, weil
   der Windows-Service selbst synchronisiert) — Bestaetigung erbeten, siehe auch die
   uebergreifende Toggle-Rueckfrage in der Uebersicht.
4. **Whitelist-Regex-Pattern.** Genaues Pattern und Fehlerverhalten bei Verstoss (nur Ablehnen +
   Protokoll-Eintrag vs. zusaetzlich eine Fehlermail) ist sicherheitsrelevant genug, um nicht
   erraten zu werden — bitte vorgeben oder im Dev-Lauf mit Sicherheitsfokus festlegen.
5. **Alter Worktree `ideal-anpassungen-v1`.** Existiert er noch? Falls ja, `V1.12.0-VERIFICATION.md`
   fuer die Test-Checkliste wiederverwenden (siehe Test-Szenarien).

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. →
2. →
3. →
4. →
5. →
