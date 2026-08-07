---
type: spec
title: "IDEAL-Standort Teil 1 — Struktur-Fundament FaHierarchyNode (Import FAListe/FAInfos)"
slug: 2026-07-29-standort-ideal-teil-1-spec
status: Entwurf
created: 2026-08-06
updated: 2026-08-07
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
  - IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs (3 neue InlineData-Eintraege, Drift-Guard)
  - IDEALAKEWMSService/Services/FaHierarchySyncService.cs (neu, injiziert ISyncErrorNotifier)
  - IDEALAKEWMSService/Services/FaHierarchySql.cs (neu, Whitelist-Regex korrigiert nach N-1 + QUOTENAME + SQL-Aufbau; Unit-Test mit den realen View-Namen als Positivfall)
  - IDEALAKEWMSService/Services/SyncLogServices.cs
  - IDEALAKEWMSService/Workers/SyncWorker.cs (neuer Sync-Block, RunResilientAsync)
  - SQL/87_AddFaHierarchy.sql (neu, naechste freie Nummer — vor Dev-Lauf pruefen; Platzhalter, siehe H-1; Inhalt haengt vom RCSI-Check am Zielsystem ab, siehe Nachbesserung 3 (2026-08-07): Primaerweg (RCSI AN, Standard-Scope) legt NUR die zwei Zieltabellen `FaHierarchyNode`/`FaHierarchyOrderInfo` an, KEIN `GRANT ALTER`, KEINE Staging-Tabellen; Fallback (RCSI AUS + Staging/Swap gewaehlt, ausserhalb des Standard-Scopes) legt zusaetzlich zwei Staging-Tabellen an und erteilt `GRANT ALTER` auf die zwei Zieltabellen)
  - SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql (neu, DDL-Dokumentation)
  - SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAInfos.sql (neu, DDL-Dokumentation)
  - SQL/00_FreshInstall.sql
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "RCSI-Status am Zielsystem pruefen (`SELECT is_read_committed_snapshot_on FROM sys.databases WHERE name = DB_NAME()`, Abfrage steht im Abschnitt „Full-Refresh-Strategie: vor dem GRANT ALTER die einfachere Frage stellen", 2026-08-07): Ergebnis entscheidet Primaerweg (RCSI AN — DELETE+Neubefuellung in einer Transaktion, kein GRANT ALTER, keine Staging-Tabellen) vs. Fallback-Weg (RCSI AUS — RCSI aktivieren ODER Staging+sp_rename-Swap mit GRANT ALTER). Ein DBA-Check am Zielsystem, kein Design-Blocker — siehe Nachbesserung 3 (2026-08-07)."
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
  PPS-/Auftragsdaten) — **strikt getrennt, kein Fan-out-Join zwischen ihnen** (siehe Anforderung 2 und
  Kritische Pruefung B-1).
- Sync-Service im Windows-Service, der beide Views periodisch liest (raw SQL gegen konfigurierbare
  View-Namen, Whitelist-Regex **und** `QUOTENAME` gegen Injection, siehe Anforderung 8) und die
  lokalen Tabellen per Full-Refresh aktualisiert. **Der Full-Refresh-Weg wird durch einen
  RCSI-Check am Zielsystem bestimmt (umgestellt in Nachbesserung 3, 2026-08-07 — siehe
  Full-Refresh-Strategie unten und den RCSI-Entscheidungsweg-Abschnitt des Menschen):**
  - **Primaerweg (RCSI AN, Standard-Scope dieser Spec):** `DELETE FROM` beide Zieltabellen und
    Neubefuellung in **einer** Transaktion — kein Staging, kein `sp_rename`-Swap, kein
    `GRANT ALTER`. Mit RCSI blockieren Leser nicht auf Schreibern, daher kein Blocking waehrend der
    Ladezeit.
  - **Fallback (RCSI AUS, ausserhalb des Standard-Scopes):** entweder RCSI aktivieren oder
    Staging-Tabellen + `sp_rename`-Swap mit eng begrenztem `GRANT ALTER` auf die zwei Zieltabellen
    (Details siehe Technischer Loesungsentwurf).
- Datenverfuegbarkeits-Regel: Eine `FAListe`-Zeile wird nur importiert, wenn zum `HauptFA`
  mindestens ein `FAInfos`-Eintrag existiert — als reine **Existenzpruefung** (`WHERE EXISTS`/
  `INNER JOIN (SELECT DISTINCT HauptFA FROM FAInfos)`), **nicht** als Zeilen-Join. Ein echter
  Zeilen-Join auf `FAInfos` wuerde jede Position pro `FAInfos`-Zeile desselben `HauptFA`
  vervielfachen (Kombinationsgeraete, mehrere `[Montage-Abteilung]`-Zeilen) — siehe Anforderung 2.
- Repository + Cache-Decorator (ADR 0001) fuer den Web-Lesezugriff auf die importierten Tabellen.
- View-DDL-Dokumentation unter `SQL/sage-views/` (keine WMS-Migration, reine Doku der Fremd-DB).
- Aktivitaets-Protokoll-Pflicht (ADR 0010) fuer den neuen Sync, inklusive expliziter Fehlermail
  ueber `ISyncErrorNotifier` bei ungueltigem View-Namen und bei leerem View-Read (siehe Technischer
  Loesungsentwurf, S-4). Teil 1 laeuft vollstaendig im Windows-Service — `ISyncErrorNotifier` ist
  dort ein etabliertes, bereits existierendes Muster (siehe `LagerbestandSyncService`); die
  Querschnitts-Regel „Das Web verschickt keine Mails" (Uebersicht, 2026-08-07) betrifft nur
  **Web**-Kontexte (Teil 2/4/5) und steht dieser Verdrahtung nicht entgegen.
- Feature-/Sync-Toggle ueber `ServiceSettingDefinitions` (ADR 0008) — **inklusive** Eintragung der
  drei neuen Keys als `[InlineData]` im Drift-Guard-Test (siehe Technischer Loesungsentwurf, S-3).
- **Nur im Fallback-Weg (RCSI AUS, Staging/Swap gewaehlt):** Die Migration, die
  `FaHierarchyNode`/`FaHierarchyOrderInfo` anlegt, erteilt dem Sync-Service-SQL-Login im selben
  Schritt eng begrenztes `ALTER`-Recht auf genau diese zwei Zieltabellen und legt zusaetzlich zwei
  Staging-Tabellen an — Voraussetzung fuer den `sp_rename`-Swap, kein DDL-Recht auf der Datenbank
  (siehe Kritische Pruefung 2026-08-07, N-2, entschaerft durch den RCSI-Check in Nachbesserung 3).
  Im Primaerweg (RCSI AN) entfaellt dieser Migrationsschritt vollstaendig.

**Out-of-Scope**
- `ProductionOrders` bleibt **vollstaendig unangetastet** (Schema, Daten, Verhalten). Keine
  Materialisierung — das ist Teil 7.
- Keine UI/Anzeige der importierten Daten — reine Backend-Grundlage. Baumanzeige = Teil 2,
  Kommissionierlisten/Beschichtung/Vormontage = Teil 3–5.
- Kein Standort-Umschalter fuer AKE — `vw_AKE_Kommissionierung_WAListe` bleibt die Datenquelle der
  AKE-Linie, unveraendert.
- Keine Rollen-/Zugriffsaenderungen (Teil 1 hat keine erreichbare Route, reiner Hintergrund-Import
  + Repository-Schicht).
- Keine fachliche Sonderbehandlung von Kombinationsgeraeten (Gruppierung/Anzeige nach
  `[Montage-Abteilung]`) — das ist ein spaeterer, separater Backlog-Punkt (siehe Kritische Pruefung
  B-1). Teil 1 stellt nur sicher, dass der Import sie **nicht verdoppelt**.

## Fachliche Anforderungen

1. **Mehrstufigkeit (B1, entschieden).** Die View liefert einen echten Elternzeiger `VaterFA`
   (BelID des uebergeordneten Sub-FA, `NULL` nur bei der Wurzel) und `SubFA` (eigene BelID des
   Nachfolgers, `0` = Blatt). `FaHierarchyNode` bildet das 1:1 ab — **kein** zweistufiges Modell.
   Die alten Alt-Entscheidungen „zwei Ebenen" (D1/D3 aus der historischen Referenz-Spec) sind
   ueberholt und duerfen nicht als Vorlage dienen.
2. **Datenverfuegbarkeits-Regel — Existenzpruefung, kein Fan-out-Join (ueberarbeitet, siehe
   Kritische Pruefung B-1).** Eine `FAListe`-Zeile wird nur dann importiert, wenn zum `HauptFA`
   mindestens ein `FAInfos`-Datensatz existiert (`FA_Nr` gefuellt **und** `Status` gefuellt — die
   View filtert das laut Anhang bereits selbst). `FAInfos` hat die Granularitaet **ein Datensatz =
   ein Auftrag (`ABNr` + `Pos`)**; Kombinationsgeraete tragen **denselben `HauptFA` mit mehreren
   `[Montage-Abteilung]`-Zeilen**. Ein `INNER JOIN FAListe f ON i.HauptFA = f.HauptFA` (wie im
   Anhang beispielhaft skizziert) ist deshalb **falsch**: er vervielfacht jede `FAListe`-Position
   pro `FAInfos`-Zeile desselben `HauptFA` — stille Mengen-Doppelzaehlung, die in jede spaetere
   Kommissionier-/Beschichtungs-/Vormontage-Liste propagiert. Das Import-SQL verwendet daher eine
   reine **Existenzpruefung**, keinen Zeilen-Join:

   ```sql
   -- FaHierarchyNode-Import: Existenzpruefung statt Zeilen-Join.
   -- Jede FAListe-Position landet GENAU EINMAL, unabhaengig von der Zahl
   -- der FAInfos-Zeilen (Montage-Abteilungen) zum selben HauptFA.
   SELECT f.*
   FROM dbo.[<FaHierarchyListeViewName>] f
   WHERE EXISTS (
       SELECT 1
       FROM dbo.[<FaHierarchyInfosViewName>] i
       WHERE i.HauptFA = f.HauptFA
   );
   -- Aequivalent: INNER JOIN (SELECT DISTINCT HauptFA FROM dbo.[<...FAInfos>]) d
   --              ON d.HauptFA = f.HauptFA
   ```

   `FaHierarchyOrderInfo` wird **separat** und **vollstaendig** (alle Zeilen, eine je
   Montage-Abteilung) aus `FAInfos` importiert — **kein** Join in die Node-Tabelle hinein (siehe
   Datenmodell unten). Beziehung Node-Gruppe (`HauptFA`) → OrderInfo ist fachlich 1:n.
3. **Kombinationsgeraete (Freigabe-Antwort 1: in Teil 1 wie normale Auftraege behandeln — technisch
   praezisiert, siehe Kritische Pruefung B-1).** Kombinationsgeraete teilen sich denselben
   `HauptFA` und werden erst ueber `[Montage-Abteilung]` aus `FAInfos` unterscheidbar;
   `FAListe`-Zeilen selbst tragen **keine** Montage-Abteilung (die ist auftragsbezogen, nicht
   positionsbezogen — nicht mit dem positionsbezogenen `Arbeitsbereich` verwechseln). „Wie normale
   Auftraege behandeln" heisst **fachlich**: keine Sonderlogik, keine Unterscheidung ueber
   `[Montage-Abteilung]`, keine eigene Gruppierung/Anzeige in Teil 1 — die Zeilen werden wie bei
   jedem anderen Auftrag getreu importiert. **Technisch** heisst es **nicht** „Zeilen-Join
   zulassen" — die Existenzpruefung aus Anforderung 2 sorgt dafuer, dass Kombinationsgeraete weder
   verdoppelt noch aus dem Import herausgefiltert werden (beides waere falsch: Verdopplung
   verfaelscht Mengen, Herausfiltern liesse die Auftraege komplett fehlen). `MontageAbteilung` wird
   als **informatives** Feld auf `FaHierarchyOrderInfo` mitgefuehrt — **nicht** als kuenstlicher
   Struktur-Schluessel auf `FaHierarchyNode`. Die Frage, wie sich Kombinationsgeraete bei der
   spaeteren Materialisierung nach `ProductionOrders` auf die dann nicht mehr eindeutige
   `OrderNumber` auswirken, gehoert zu **Teil 7** und wird dort entschieden — nicht hier. Die
   fachliche Unterscheidung/Anzeige nach Montage-Abteilung ist als eigener, spaeterer
   Backlog-Punkt festzuhalten (nicht Teil 1).
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
8. **View-Namen konfigurierbar — Whitelist UND `QUOTENAME`, Zeichensatz korrigiert nach N-1
   (Kritische Pruefung 2026-08-07).** `FAListe`- und `FAInfos`-View-Name sind je Standort
   unterschiedlich benennbar (Testsystem heisst `IDEAL_TEST_2026_05_03`, Produktivname noch offen
   — siehe `open_questions`) und werden **nicht** hartkodiert, sondern aus `ServiceSettings`
   gelesen. Die Absicherung ist **zweistufig**, nicht nur eine Regex:
   1. Der konfigurierte Name wird an `.` in `[Schema].[Name]`-Segmente zerlegt; jedes Segment wird
      **ASCII-explizit** gegen `^[A-Za-z0-9_\-]+$` geprueft — bewusst **nicht** `\w` (unter
      Unicode-Regex matcht `\w` Homoglyphen, genau der relevante Angriff bei einem extern
      konfigurierbaren Objektnamen). Der **Bindestrich ist bewusst erlaubt**: Der reale
      Default-Viewname `vw_IDEAL-AKE_Kommissionierung_FAListe` (und `..._FAInfos`) enthaelt selbst
      einen Bindestrich — eine fruehere Fassung dieser Regel (`^[A-Za-z0-9_]+$`, ohne `-`) haette
      den eigenen Default abgelehnt und den IDEAL-Sync nie zum Laufen gebracht (siehe Kritische
      Pruefung 2026-08-07, N-1 — Fehler eingestanden und korrigiert). Zusaetzlich verworfen:
      Whitespace, `;`, `/*`, `[`/`]` innerhalb eines Segments, `.` innerhalb eines Segments (nur
      als Trenner zwischen Segmenten zulaessig), sowie explizit die Zweizeichenfolge `--`
      (SQL-Kommentar-Einleiter — kann in keinem echten Objektnamen vorkommen, Defense-in-Depth
      zusaetzlich zu `QUOTENAME`).
   2. Die geprueften Segmente werden danach per **`QUOTENAME()`** wieder zu `[Schema].[Name]`
      zusammengesetzt, bevor sie in den SQL-Text eingesetzt werden — Objektnamen lassen sich in
      T-SQL nicht parametrisieren, `QUOTENAME` ist die strukturelle Abwehr, falls die Regex
      spaeter versehentlich gelockert wird. Whitelist **und** `QUOTENAME`, nicht entweder/oder.

   **Testpflicht (neu, N-1):** Der Unit-Test der Validierung muss die **realen
   Produktions-View-Namen als Positivfaelle** enthalten (`vw_IDEAL-AKE_Kommissionierung_FAListe`,
   `vw_IDEAL-AKE_Kommissionierung_FAInfos`), nicht nur erfundene Beispiele — genau dieser Testfall
   haette den urspruenglichen Fehler in Sekunden gefunden. Negativfaelle (Semikolon, Leerzeichen,
   `--`, kyrillisches `а` als Homoglyph) daneben — siehe AK 4/8.

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

**Staging-Pendants — NUR im Fallback-Weg, RCSI AUS (umgestellt in Nachbesserung 3, 2026-08-07;
technisch, kein eigenes Domaenen-Modell):** `FaHierarchyNode_Staging` / `FaHierarchyOrderInfo_Staging`
— schema-identisch zu den Zieltabellen (inkl. `SyncedAt`), ohne eigene Repository-Anbindung. Sie
werden **nur** angelegt, wenn der RCSI-Check am Zielsystem `is_read_committed_snapshot_on = 0`
ergibt und RCSI nicht aktiviert werden soll (siehe Full-Refresh-Strategie unten und den
RCSI-Entscheidungsweg-Abschnitt des Menschen). **Im Primaerweg (RCSI AN, Standard-Scope dieser
Spec) existieren diese Tabellen nicht** — der Full-Refresh laeuft direkt auf den zwei Zieltabellen
(`DELETE FROM` + Neubefuellung in einer Transaktion), ohne Zwischenablage.

**Harte Regel (N-4, Kritische Pruefung 2026-08-07) — gilt nur, falls der Fallback-Weg tatsaechlich
gewaehlt wird:** Existieren Staging-Tabellen, **muessen** Ziel- und Staging-Tabelle schema-identisch
bleiben, sonst brechen Bulk-Insert und `sp_rename`-Swap. Jede spaetere Spalten-/Typaenderung an
`FaHierarchyNode`/`FaHierarchyOrderInfo` muss dann das jeweilige `_Staging`-Pendant **im selben
Migrationsschritt** mitaendern — analog zur ADR-0004-Kopplungsregel fuer `SQL/AgentJobs/*`. **Im
Primaerweg entfaellt diese Kopplungspflicht vollstaendig**, weil keine Staging-Tabellen existieren.
Siehe auch Migrations-/SQL-Auswirkungen unten.

**Warum zwei Tabellen statt einer denormalisierten:** `FAInfos`-Felder sind **auftragsbezogen**
(ein `KO_Termin` gilt fuer die ganze Struktur), `FAListe`-Felder sind **positionsbezogen** (eine
Struktur hat potenziell hunderte Positionszeilen). Wuerde man die Auftragsfelder in jede
Positionszeile denormalisieren, vervielfachte sich die Speichermenge und ein Termin-Update muesste
in hunderten Zeilen synchron gehalten werden. Der Anhang schlaegt selbst zwei Domain-Objekte vor
(`FaListEntry`/`FaInfoEntry`) — diese Spec bildet das 1:1 auf zwei Tabellen ab. Die
„ein Lesepfad, zwei Projektionen"-Aussage aus B4 bezieht sich auf **Struktur-Ansicht** und
**Stueckliste-Ansicht**, die beide **aus `FaHierarchyNode` allein** ableitbar sind — nicht auf eine
Verschmelzung mit `FaHierarchyOrderInfo`. **Wichtig:** Der Anhang skizziert unter „Empfohlene
Umsetzung" selbst einen `INNER JOIN FAListe f ... FAInfos i ON i.HauptFA = f.HauptFA` — dieses
Muster ist fan-out-anfaellig (siehe Anforderung 2) und wird in dieser Spec **bewusst nicht**
uebernommen; die Existenzpruefung ersetzt es.

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
- Die `_Staging`-Tabellen — **falls sie im Fallback-Weg (RCSI AUS) ueberhaupt angelegt werden** —
  haben **keine** Repository-Anbindung; sie sind reines Sync-internes Detail, nicht Teil des
  Lesemodells. Im Primaerweg entfaellt der Punkt ganz.

### Sync-Service (Windows-Service, `IDEALAKEWMSService`)

- `FaHierarchySyncService` (neuer Service-Name in `SyncLogServices.All`), gated ueber
  `Sync:HierarchicalFaEnabled` (Bool, Default `false`). Injiziert **zusaetzlich**
  `ISyncErrorNotifier` (analog `LagerbestandSyncService`) als expliziten Ctor-Parameter — siehe
  Fehlermail-Punkt unten.
- View-Namen aus `ServiceSettings`: `Sync:FaHierarchyListeViewName` (String, Default
  `[vw_IDEAL-AKE_Kommissionierung_FAListe]`), `Sync:FaHierarchyInfosViewName` (String, Default
  `[vw_IDEAL-AKE_Kommissionierung_FAInfos]`) — beide neue Eintraege in
  `ServiceSettingDefinitions.All` (Drift-Guard-Pflicht, ADR 0008). **Zusaetzlich** muessen alle
  drei neuen Keys (`Sync:HierarchicalFaEnabled` inklusive) als `[InlineData]` in
  `ServiceSettingDefinitionsTests.All_ContainsDocumentedServiceReadKey` eingetragen werden (siehe
  Kritische Pruefung S-3): Der Drift-Guard-Test ist eine hartcodierte `[InlineData]`-Liste — ein
  Key, der nur im Katalog steht, aber dort nicht gelistet ist, laesst den Test **gruen ohne jede
  Guard-Wirkung**.
- **Whitelist-Regex + `QUOTENAME`** vor jedem SQL-Aufbau (`FaHierarchySql.ValidateViewName`,
  korrigiert nach Kritischer Pruefung 2026-08-07/N-1, siehe Anforderung 8): zerlegt
  `[Schema].[Name]` an `.` in Segmente, prueft jedes Segment ASCII-explizit gegen
  `^[A-Za-z0-9_\-]+$` (**nicht** `\w`, Bindestrich bewusst erlaubt — der reale View-Name traegt
  einen), lehnt Whitespace, `;`, `--`, `/*`, `.` innerhalb eines Segments, unausgeglichene `[`/`]`
  und alles jenseits ASCII ab, setzt die geprueften Segmente danach per `QUOTENAME()` zusammen. Bei
  Verstoss: Lauf bricht mit `FinishFailedAsync` ab, **kein** SQL wird ausgefuehrt, **zusaetzlich**
  `ISyncErrorNotifier.NotifyAsync(...)` (siehe Fehlermail unten). Das Regex-Pattern ist mit dieser
  Korrektur **final** — keine offene Rueckfrage mehr (siehe „Antworten auf die Kritische Pruefung",
  N-1).
- **Full-Refresh-Strategie — RCSI-first (umgestellt in Nachbesserung 3, 2026-08-07, nach dem
  RCSI-Entscheidungsweg-Abschnitt des Menschen; ersetzt die vorherige Entscheidung aus N-2, dass der
  Staging/`sp_rename`-Swap der einzige Pfad sei).** Vor jeder Full-Refresh-Implementierung steht ein
  einmaliger Infra-Check am Zielsystem:

  ```sql
  SELECT name, is_read_committed_snapshot_on
  FROM sys.databases WHERE name = DB_NAME();
  ```

  Kein Praezedenzfall im Code deckt „Delete-All + Bulk-Insert" direkt ab: `CachedBomHeader` ist
  Hash-inkrementell (Upsert), enaio ist MERGE-Full-Sync — das bleibt als Hintergrund richtig, aendert
  aber nichts an der RCSI-first-Entscheidung: Mit RCSI AN traegt ein einfaches `DELETE`+Neubefuellung
  kein Blocking-Risiko, weil lesende Transaktionen einen konsistenten Snapshot statt eines Locks
  bekommen.

  **Primaerweg (RCSI = AN, bevorzugt, Standard-Scope dieser Spec):**
  1. **Vor jeder Mutation:** Roh-Zeilenzahl **beider** Views ungefiltert lesen (`SELECT COUNT(*)`
     auf `FaHierarchyListeViewName` bzw. `FaHierarchyInfosViewName`, **ohne** Existenzpruefung/Join).
     Guard: Ist eine der beiden Rohzahlen `0`, **kein** Replace — Warn-Log + `NotifyAsync` (siehe
     unten), bestehende Zieltabellen bleiben unangetastet. Dieser Check laeuft **vor** dem Aufbau
     der Zielzeilen, damit die Existenzpruefung aus Anforderung 2 einen leeren `FAInfos`-Read nicht
     als „0 Struktur-Zeilen" maskiert (sonst verwechselt der Guard Ausfall mit echtem Leerstand).
  2. Erst danach: beide Views vollstaendig lesen, Zielzeilen im Speicher aufbauen (Existenzpruefung
     gemaess Anforderung 2).
  3. **In EINER Transaktion:** `DELETE FROM dbo.FaHierarchyNode`, `DELETE FROM
     dbo.FaHierarchyOrderInfo`, danach Bulk-Insert der neu aufgebauten Zeilen in beide Tabellen,
     Commit. Kein Staging, kein `sp_rename`, keine zusaetzlichen Objekte, keine Migrations-Kopplung
     (N-4 entfaellt vollstaendig). Mit RCSI AN sehen gleichzeitige Web-Reads
     (`FaHierarchyNodeRepository`/`CachedFaHierarchyNodeRepository`) waehrend der gesamten Ladezeit
     den **alten** Stand aus einem konsistenten Snapshot, **ohne** auf den Schreiber zu warten —
     kein Blocking, kein Timeout-Risiko, keine Sch-M/Sch-S-Kollision wie im Fallback (siehe unten).
  4. Beide Tabellen werden in **jedem Fall als ein logischer Schritt** ersetzt (eine Transaktion) —
     nie steht `FaHierarchyNode` (neu) neben einem alten `FaHierarchyOrderInfo` oder umgekehrt.
  5. Kein inkrementelles Delta, kein MERGE-Aufwand — die Views selbst liefern bereits den
     vollstaendigen Soll-Stand.
  6. **Berechtigung:** nur `DELETE`-/`INSERT`-Recht auf den zwei Zieltabellen — **kein** `ALTER`,
     **kein** `GRANT ALTER` durch die Migration noetig.

  **Fallback (RCSI = AUS) — zwei Optionen, beide ausserhalb des Standard-Scopes dieser Spec:**
  - **(a) RCSI auf der Ziel-DB aktivieren** (`ALTER DATABASE ... SET READ_COMMITTED_SNAPSHOT ON`) —
    eine DB-weite Aenderung mit eigener Abwaegung (Betriebs-/DBA-Entscheidung, nicht Teil dieser
    Spec-Runde); danach gilt der Primaerweg oben unveraendert.
  - **(b) Staging-Tabellen + `sp_rename`-Swap** mit eng begrenztem `GRANT ALTER` — nur falls (a)
    nicht gewuenscht wird. Empfehlung des Menschen als Default, falls nicht weiter abgewogen:
    `GRANT ALTER` erteilen ist vertretbar (enger Grant auf zwei Objekte, kein Schema-/DB-Recht, fuer
    ein Konto, das den Tabelleninhalt ohnehin alle paar Minuten komplett ersetzt). Details:
    1. Gleicher Empty-Guard wie im Primaerweg (Schritt 1 oben).
    2. Gleicher Zielzeilen-Aufbau wie im Primaerweg (Schritt 2 oben).
    3. **Schreiben ueber Staging-Tabellen** (`FaHierarchyNode_Staging` / `FaHierarchyOrderInfo_Staging`,
       siehe Datenmodell): beide werden bei jedem Lauf per `TRUNCATE` + Bulk-Insert frisch befuellt.
       Danach werden **beide** Zieltabellen in **einer** kurzen Metadaten-Operation gegen ihre
       Staging-Pendants getauscht.
    4. **Swap-Mechanik (N-3, 2026-08-07):** `sp_rename` tauscht keine zwei Tabellen in einem
       Schritt — je Tabelle sind **drei** Renames mit einem definierten Zwischennamen noetig:
       1. `FaHierarchyNode` → `FaHierarchyNode_Swap` (alter Inhalt, temporaer beiseitegelegt)
       2. `FaHierarchyNode_Staging` → `FaHierarchyNode` (neuer Inhalt wird produktiv)
       3. `FaHierarchyNode_Swap` → `FaHierarchyNode_Staging` (alter Inhalt wird die neue
          Staging-Basis fuer den naechsten Lauf)

       Analog fuer `FaHierarchyOrderInfo` → `FaHierarchyOrderInfo_Swap` → `FaHierarchyOrderInfo` →
       `FaHierarchyOrderInfo_Staging`. Alle sechs Renames laufen in **einer** kurzen Transaktion.
       Web-Reads sehen bis zum Swap den alten Inhalt, danach sofort den neuen — **waehrend der
       Ladezeit** kein Blocking; **im Swap-Moment selbst** nimmt `sp_rename` kurzzeitig eine
       Schema-Modifikations-Sperre (Sch-M), ein gleichzeitiger Web-Read eine
       Schema-Stabilitaets-Sperre (Sch-S) — die beiden blockieren sich fuer die (vernachlaessigbar
       kurze) Dauer des Renames tatsaechlich gegenseitig. **Bekannter kosmetischer Nebeneffekt:**
       `sp_rename` verschiebt keine Constraint-/PK-/Index-Namen mit — nach dem ersten Swap traegt
       die produktive Tabelle die Index-/PK-Namen ihres Staging-Ursprungs. Rein kosmetisch, aber bei
       einem spaeteren Schema-`ALTER` zu beachten.
    5. **Berechtigung:** Die Migration, die die zwei Zieltabellen **und** ihre Staging-Pendants
       anlegt, erteilt dem Sync-Service-SQL-Login im selben Schritt `GRANT ALTER ON
       dbo.FaHierarchyNode` und `GRANT ALTER ON dbo.FaHierarchyOrderInfo` (eng auf diese zwei
       Tabellen begrenzt, **kein** DDL-Recht auf der Datenbank).
    6. Auch hier: beide Tabellen werden **als ein logischer Schritt** ersetzt (nicht zeitlich
       versetzt) — nie steht `FaHierarchyNode` (neu) neben einem alten `FaHierarchyOrderInfo` oder
       umgekehrt.

  **Entscheidungsstand dieser Spec-Runde:** Der Primaerweg (RCSI AN) ist der Standard-Scope von
  Teil 1. Migrations-/SQL-Auswirkungen, `affected_code` und die Akzeptanzkriterien unten sind darauf
  zugeschnitten; der Fallback bleibt vollstaendig dokumentiert, ist aber nur zu bauen, wenn der
  RCSI-Check am Zielsystem tatsaechlich RCSI AUS ergibt und (a) nicht gewaehlt wird.
- **Fehlermail — `ISyncErrorNotifier` explizit verdrahtet (ueberarbeitet, siehe Kritische Pruefung
  S-4).** `RunResilientAsync` in `SyncWorker` mailt **nur** bei einer geworfenen Exception; der
  Empty-Guard und der Invalid-Name-Fall sind beide **kein throw** (Warn bzw. `FinishFailedAsync`),
  also mailt `RunResilientAsync` dort **nicht**. `FaHierarchySyncService` injiziert deshalb
  `ISyncErrorNotifier` als Ctor-Parameter (analog `LagerbestandSyncService`) und ruft
  `NotifyAsync(stepName, ex, ct)` explizit an **beiden** Stellen auf:
  - nach `FinishFailedAsync` bei ungueltigem View-Namen,
  - nach dem Warn-Log des Empty-Guards.

  `NotifyAsync` erwartet eine `Exception`-Instanz (keinen reinen Text) — an beiden Stellen wird
  analog zum bestehenden Cap-Skip-Fall in `LagerbestandSyncService` eine synthetische
  `InvalidOperationException` mit sprechender Meldung konstruiert und uebergeben (kein echter
  Prozessabbruch, nur Transportvehikel fuer die Mail-Details).

  **Abgrenzung zur Querschnitts-Regel „Das Web verschickt keine Mails" (Uebersicht, 2026-08-07):**
  Diese Regel betrifft **Web**-Kontexte (Teil 2/4/5), in denen kein Mailversand existiert. Teil 1
  laeuft vollstaendig im Windows-Service, wo `ISyncErrorNotifier` bereits ein etabliertes Muster
  ist — die hier vorgeschriebene Fehlermail-Verdrahtung steht damit **nicht** im Widerspruch zu
  dieser Regel.
- Protokoll (ADR 0010): `ISyncLogger` als letzter Ctor-Parameter, Counts `neu`/`geloescht`
  (deutschsprachig, hier praktisch „komplette Ersetzung" abgebildet als zwei Zahlen), eigener Lauf
  getrennt vom `ProductionOrder`-Import.
- Sync-Block in `SyncWorker` ueber `RunResilientAsync` gekapselt (ein Fehler killt die anderen
  Sync-Bloecke nicht; deckt zusaetzlich unerwartete Exceptions ab, die die beiden expliziten
  `NotifyAsync`-Pfade oben nicht abdecken).

### Migrations-/SQL-Auswirkungen

1. **Primaerweg (RCSI AN, Standard-Scope dieser Spec):** Model → `dotnet ef migrations add
   AddFaHierarchy` (aktueller Timestamp!) → idempotentes `SQL/87_AddFaHierarchy.sql`
   (Platzhalter-Nummer, siehe H-1) mit `OBJECT_ID`-Guard, Tabellen-DDL in eigenem Batch (`GO`) fuer
   die **zwei** Zieltabellen (`FaHierarchyNode`, `FaHierarchyOrderInfo`),
   `__EFMigrationsHistory`-Insert in separatem Batch. **Keine** Staging-Tabellen, **kein**
   `GRANT ALTER`, keine zusaetzlichen Berechtigungs-Batches.
2. **Fallback (RCSI AUS, Option (b) gewaehlt — ausserhalb des Standard-Scopes dieser Spec):**
   dieselbe Migration legt **zusaetzlich** die zwei `_Staging`-Pendants an und erteilt
   `GRANT ALTER ON dbo.FaHierarchyNode` / `...FaHierarchyOrderInfo` an das Sync-Service-SQL-Login
   in einem eigenen Batch (siehe Full-Refresh-Strategie oben) — nur falls der RCSI-Check am
   Zielsystem RCSI AUS ergibt und RCSI nicht aktiviert werden soll.
3. **Harte Regel (N-4, 2026-08-07) — gilt nur, falls der Fallback (Punkt 2) tatsaechlich gebaut
   wird:** Weil Ziel- und Staging-Tabelle dann schema-identisch bleiben **muessen** (sonst bricht
   Bulk-Insert/Swap), muss **jede** spaetere Spalten-/Typaenderung an
   `FaHierarchyNode`/`FaHierarchyOrderInfo` das jeweilige `_Staging`-Pendant **im selben
   Migrationsschritt** mitaendern — analog zur ADR-0004-Kopplungsregel fuer `SQL/AgentJobs/*`. **Im
   Primaerweg entfaellt diese Kopplung vollstaendig**, weil keine Staging-Tabellen existieren. (Ein
   entsprechender Fallstrick-Eintrag in `secondbrain/architektur/fallstricke.md` bleibt Folgearbeit
   ausserhalb dieser Spec-Runde, nur relevant, falls der Fallback tatsaechlich gebaut wird.)
4. `SQL/00_FreshInstall.sql` an **beiden** Stellen nachziehen: Schema-Objekte (im Primaerweg
   **zwei** neue Tabellen; im Fallback zusaetzlich die zwei Staging-Tabellen **plus** die zwei
   `GRANT ALTER`-Anweisungen) **und** `MigrationId` im History-Insert-Block.
5. **Additive Migration** — kein Datenverlust, kein Backup-Hinweis noetig (neue, leere Tabellen).
   Im Fallback sind auch die `GRANT ALTER`-Anweisungen additiv (kein Rechte-Entzug an bestehenden
   Logins).
6. `SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql` +
   `..._FAInfos.sql`: View-DDL-Dokumentation der Fremd-DB — **keine** WMS-Migration, nur
   Versionskontrolle der Sage-Objekte (wie in der Notiz vereinbart).
7. **Migrationsnummer beim Dev-Start final festlegen.** `SQL/82`/`83` sind durch v1.28.0
   (Sage-Lagerbuchungen) belegt; die WmsBugs-Batches belegen mindestens `84`-`87`
   (`87_AddUserDefaultFilterBomDescription1.sql` im noch nicht gemergten Worktree
   `2026-08-05-wms-bugs-improvements-teil-1-2-3`). Nach Merge beider Batches ist die naechste freie
   Nummer **voraussichtlich `88`** — die `87`-Referenz in dieser Spec (auch in `affected_code`) ist
   ein Platzhalter und unmittelbar vor dem Dev-Lauf gegen den dann gemergten Stand zu pruefen.

### Audit-Feld-Auswirkungen

`FaHierarchyNode` und `FaHierarchyOrderInfo` (und — nur im Fallback-Weg, RCSI AUS — ihre
`_Staging`-Pendants) sind **keine** `AuditableEntity` — analog zu `CachedBomHeader`/`CachedBomItem`
(dokumentierte Ausnahme: reine, vom Sync-Service befuellte Cache-Tabellen ohne manuelle Bearbeitung
durch Anwender). Nachvollziehbarkeit kommt stattdessen aus dem Aktivitaets-Protokoll (`SyncLog`,
ADR 0010) des `FaHierarchySyncService`-Laufs, nicht aus `ModifiedBy`/`ModifiedAt`-Feldern auf den
Zeilen selbst. Kein bestehendes Audit-Feld ist betroffen, da `ProductionOrders` unangetastet bleibt.

## Akzeptanzkriterien

1. Bei deaktiviertem `Sync:HierarchicalFaEnabled` (Default) laeuft der Service unveraendert wie
   heute — kein neuer Sync-Block wird ausgefuehrt, keine Fehlermeldung.
2. Ist der Toggle aktiv und beide View-Namen gueltig, fuellt ein Lauf `FaHierarchyNode` und
   `FaHierarchyOrderInfo` vollstaendig aus den konfigurierten Views; eine `FAListe`-Zeile erscheint
   **nur**, wenn zum `HauptFA` ein `FAInfos`-Eintrag existiert (Existenzpruefung, testbar durch
   gezieltes Fehlen eines `FAInfos`-Datensatzes am Testsystem).
3. **(neu, B-1)** Positionen eines `HauptFA` mit **mehreren** `FAInfos`-Zeilen (z. B.
   Kombinationsgeraete mit zwei Montage-Abteilungen) erscheinen in `FaHierarchyNode` **genau
   einmal** je `FAListe`-Position — keine Verdopplung durch die Existenzpruefung. Regressionstest:
   Die Positionszahl in `FaHierarchyNode` je `HauptFA` entspricht exakt der Zeilenzahl der Roh-View
   `FAListe` fuer diesen `HauptFA`, unabhaengig von der Zahl der `FAInfos`-Zeilen dazu.
   `FaHierarchyOrderInfo` enthaelt dagegen weiterhin **alle** `FAInfos`-Zeilen (eine je
   Montage-Abteilung).
4. Ein ungueltiger View-Name (z. B. mit Leerzeichen, Semikolon, `--`, Kommentarzeichen oder einem
   Homoglyphen ausserhalb ASCII) fuehrt zu einem fehlgeschlagenen, protokollierten Lauf
   (`FinishFailedAsync`) **ohne** SQL-Ausfuehrung gegen die Sage-DB **und** zu einer Fehlermail
   (`ISyncErrorNotifier.NotifyAsync`); ein gueltiger Name — **einschliesslich** der realen
   Default-Namen mit Bindestrich (`vw_IDEAL-AKE_Kommissionierung_FAListe`,
   `vw_IDEAL-AKE_Kommissionierung_FAInfos`) — wird vor dem SQL-Aufbau zusaetzlich per `QUOTENAME()`
   abgesichert (verifiziert am Whitelist-Unit-Test).
5. **(ueberarbeitet, S-1)** Ein leerer Roh-View-Read (0 Zeilen in `FAListe` **oder** `FAInfos`,
   geprueft **vor** jeder Existenzpruefung/jedem Join) loest **keinen** Replace der Zieltabellen
   aus, sondern einen Warn-Eintrag **und** eine Fehlermail (`ISyncErrorNotifier.NotifyAsync`) —
   bestehende Daten bleiben erhalten. Der Guard wertet die **ungefilterten** Rohzahlen aus, nicht
   die nach der Existenzpruefung gefilterte Zeilenzahl.
6. `Beschaffungsartikel`/`Beschichtet` werden korrekt nach Sage-VB6-Konvention (`-1` = wahr)
   gemappt — verifiziert an mindestens einer bekannten Test-Struktur mit beschichteten und
   nicht-beschichteten Positionen.
7. `ProductionOrders` (Schema, Zeilenzahl, Verhalten aller bestehenden Controller) ist nach diesem
   Teil **byte-identisch unveraendert** zum Vor-Zustand (Regressionsnachweis: bestehende
   AKE-Testszenarien laufen unveraendert durch).
8. `dotnet build` + `dotnet test` sind gruen; der neue Sync-Pfad (raw SQL) ist gemaess
   Projekt-Konvention **nicht** vollstaendig InMemory-testbar — der Whitelist-Regex-Helfer
   (`FaHierarchySql.ValidateViewName`) ist als eigenstaendiger, unit-testbarer Baustein
   auszulegen (analog `ProductionOrderReconciler`/`LagerbestandZeroingPlanner`), damit wenigstens
   die Injection-Abwehr (Regex **und** `QUOTENAME`-Zusammenbau) automatisiert geprueft ist. **(neu,
   N-1)** Der Unit-Test deckt zwingend die realen Produktions-View-Namen als Positivfaelle ab
   (`vw_IDEAL-AKE_Kommissionierung_FAListe`, `vw_IDEAL-AKE_Kommissionierung_FAInfos`, jeweils mit
   Bindestrich) sowie Negativfaelle (Semikolon, Leerzeichen, `--`, kyrillisches `а` als Homoglyph).
9. **(neu, S-1; gilt fuer BEIDE Full-Refresh-Wege)** Nach einem erfolgreichen Lauf sind entweder
   **beide** Zieltabellen aktualisiert oder **beide** unveraendert — kein Zwischenzustand, in dem
   `FaHierarchyNode` neu und `FaHierarchyOrderInfo` alt ist (oder umgekehrt); verifiziert per
   `SyncedAt`-Zeitstempel-Vergleich beider Tabellen nach dem Lauf. Im Primaerweg garantiert die
   gemeinsame `DELETE`+Insert-Transaktion diese Eigenschaft, im Fallback die gemeinsame
   Sechs-Rename-Transaktion (siehe AK 11).
10. **(neu, S-3)** `Sync:HierarchicalFaEnabled`, `Sync:FaHierarchyListeViewName` und
    `Sync:FaHierarchyInfosViewName` sind sowohl in `ServiceSettingDefinitions.All` **als auch** als
    zusaetzliche `[InlineData]`-Eintraege in
    `ServiceSettingDefinitionsTests.All_ContainsDocumentedServiceReadKey` vorhanden — reine
    Katalog-Eintragung ohne `InlineData` gilt **nicht** als erfuellt (Test bliebe sonst gruen ohne
    jede Guard-Wirkung).
11. **(neu, S-1; entschaerft nach N-3, 2026-08-07; NUR Fallback-Weg, RCSI AUS mit Staging/Swap —
    siehe AK 13 fuer den Primaerweg)** Waehrend ein Full-Refresh im Fallback-Weg laeuft
    (Staging-Aufbau vor dem Swap), liefert ein gleichzeitiger Web-Read ueber
    `FaHierarchyNodeRepository`/`CachedFaHierarchyNodeRepository` weiterhin den **alten**
    Tabelleninhalt, **ohne Blocking waehrend der Ladezeit**; im eigentlichen Swap-Moment (sechs
    `sp_rename`-Aufrufe in einer Transaktion) ist eine **vernachlaessigbare Metadaten-Sperre**
    (Sch-M gegen Sch-S) zulaessig, kein Timeout — manuell am Testsystem waehrend eines laufenden
    Sync-Laufs verifiziert. Nur zu pruefen, falls der Fallback tatsaechlich gebaut wird.
12. **(neu, N-2, 2026-08-07; NUR Fallback-Weg, RCSI AUS mit Staging/Swap gewaehlt)** Die Migration
    erteilt dem Sync-Service-SQL-Login `ALTER`-Recht ausschliesslich auf `FaHierarchyNode` und
    `FaHierarchyOrderInfo` (kein DDL-Recht auf der Datenbank) — verifiziert per Rechte-Abfrage
    (`fn_my_permissions`) nach dem Migrations-Lauf am Zielsystem. Nur zu pruefen, falls der
    Fallback tatsaechlich gebaut wird; im Primaerweg (RCSI AN) entfaellt dieses AK vollstaendig, da
    kein `GRANT ALTER` erteilt wird.
13. **(neu, Nachbesserung 3, 2026-08-07; Primaerweg, RCSI AN — Standardfall)** Bei RCSI AN ersetzt
    ein Full-Refresh-Lauf den Inhalt beider Zieltabellen durch `DELETE FROM` + Neubefuellung in
    **einer** Transaktion; ein gleichzeitiger Web-Read ueber
    `FaHierarchyNodeRepository`/`CachedFaHierarchyNodeRepository` liefert waehrend der **gesamten**
    Ladezeit den **alten** Datenstand — **ohne** Blocking, **ohne** Timeout (RCSI-Snapshot statt
    Sch-M/Sch-S-Sperre wie im Fallback). Verifiziert: (a) `SELECT is_read_committed_snapshot_on
    FROM sys.databases WHERE name = DB_NAME()` liefert `1` am Zielsystem vor dem Lauf, (b) manuell
    am Testsystem waehrend eines laufenden Sync-Laufs ein Web-Read ausgeloest und dessen sofortige
    Antwort mit dem Vor-Lauf-Datenstand bestaetigt.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` („IDEAL Teil 1 — Struktur-Import"):

- **Vorbedingung:** Zugriff auf das IDEAL-Testsystem (`AKESQL20.ake.at` / `IDEAL_TEST_2026_05_03`
  laut Anhang), `Sync:HierarchicalFaEnabled = true`, View-Namen korrekt konfiguriert.
- **Schritt 0 — RCSI-Check, entscheidet den Full-Refresh-Weg (neu, Nachbesserung 3, 2026-08-07):**
  Vor dem Migrations-Lauf am Zielsystem pruefen:
  `SELECT name, is_read_committed_snapshot_on FROM sys.databases WHERE name = DB_NAME();`
  - **Ergebnis `1` (RCSI AN, erwarteter Regelfall):** Primaerweg gilt — die Migration legt nur die
    zwei Zieltabellen an, kein `GRANT ALTER` noetig, der Schritt „Berechtigung" unten entfaellt.
  - **Ergebnis `0` (RCSI AUS):** Fallback-Entscheidung treffen (RCSI aktivieren ODER Staging+Swap
    mit `GRANT ALTER`). Falls Staging+Swap gewaehlt wird: nach dem Migrations-Lauf zusaetzlich
    pruefen, dass das Sync-Service-SQL-Login `ALTER`-Recht auf `FaHierarchyNode` und
    `FaHierarchyOrderInfo` hat (Rechte-Abfrage am Zielsystem, z. B. `fn_my_permissions` oder die
    beiden Abfragen im RCSI-Entscheidungsweg-Abschnitt).
- **Schritt 1 — Erstimport:** Service-Lauf ausloesen, Aktivitaets-Protokoll pruefen (Lauf
  erfolgreich, Counts plausibel).
- **Schritt 2 — Datenverfuegbarkeits-Regel:** Eine bekannte Struktur ohne `FAInfos`-Eintrag
  darf **nicht** in `FaHierarchyNode` erscheinen.
- **Schritt 3 — Mehrstufigkeit:** Eine bekannte Struktur mit Sub-Sub-FA (Baugruppe unter
  Baugruppe) pruefen — `VaterFA`-Kette laesst sich bis zur Wurzel zurueckverfolgen.
- **Schritt 4 — Kombinationsgeraet, keine Verdopplung (ueberarbeitet, B-1).** Falls am Testsystem
  vorhanden, eine `HauptFA` mit **zwei** `MontageAbteilung`-Werten in `FaHierarchyOrderInfo`
  identifizieren. Erwartung: **Positionszahl in `FaHierarchyNode` fuer diesen `HauptFA` ist
  identisch zur Zeilenzahl der Roh-View `FAListe`** fuer denselben `HauptFA` — **keine**
  Verdopplung, obwohl zwei `FAInfos`-Zeilen existieren. `FaHierarchyOrderInfo` enthaelt dagegen
  beide Montage-Abteilungs-Zeilen. Die materialisierungsseitige Behandlung gehoert zu Teil 7.
- **Schritt 5 — Full-Refresh ohne Blocking, Primaerweg (neu, Nachbesserung 3, 2026-08-07; RCSI AN,
  Standardfall):** Waehrend ein Sync-Lauf laeuft (`DELETE`+Neubefuellung beider Tabellen in einer
  Transaktion), einen Web-Read gegen `FaHierarchyNodeRepository` ausloesen — erwartet: sofortige
  Antwort mit dem **alten** Datenstand fuer die gesamte Dauer des Laufs, **kein** Blocking, kein
  Timeout (RCSI-Snapshot-Isolation).
- **Schritt 5b — Full-Refresh ohne Blocking, Fallback (nur falls RCSI AUS und Staging/Swap
  gewaehlt; S-1, Swap-Formulierung nach N-3):** Waehrend ein Sync-Lauf laeuft (Staging-Aufbau vor
  dem Swap), einen Web-Read gegen `FaHierarchyNodeRepository` ausloesen — erwartet: sofortige
  Antwort mit dem **alten** Datenstand, kein Blocking waehrend der Ladezeit; waehrend des kurzen
  Swap-Moments selbst ist eine vernachlaessigbare Metadaten-Sperre zulaessig, kein Timeout.
- **Positivfall — realer View-Name mit Bindestrich (neu, N-1):** `Sync:FaHierarchyListeViewName`
  auf dem Default `[vw_IDEAL-AKE_Kommissionierung_FAListe]` belassen, Lauf ausloesen — erwartet:
  Whitelist akzeptiert den Namen (kein `FinishFailedAsync`), SQL wird gegen die echte View
  ausgefuehrt.
- **Negativfall — ungueltiger View-Name:** `Sync:FaHierarchyListeViewName` auf einen Wert mit
  Semikolon setzen, Lauf ausloesen, erwarten: fehlgeschlagener, protokollierter Lauf, keine
  SQL-Ausfuehrung (per Server-seitigem Audit/Profiler oder Code-Review bestaetigt) **und**
  eingegangene Fehlermail.
- **Negativfall — leerer View-Read (neu, S-1/S-4):** Eine der beiden Views testweise leer liefern
  (Testsystem-Praeparation), Lauf ausloesen, erwarten: Warn-Eintrag im Aktivitaets-Protokoll,
  eingegangene Fehlermail, `FaHierarchyNode`/`FaHierarchyOrderInfo` **unveraendert**.
- **Regressionsfall:** Alle bestehenden AKE-Testszenarien (FA-Liste, Kommissionierung, BDE)
  unveraendert durchspielen — kein Unterschied zum Vor-Zustand.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen.

## Deploy

- **Web-App:** ja (neue Repository-/Model-Klassen, DI-Registrierung — auch wenn noch keine
  Controller/Views darauf zugreifen).
- **Service:** ja (neuer Sync-Block).
- **Migration:** ja (`SQL/87_AddFaHierarchy.sql`, Platzhalter-Nummer, additiv, kein Backup-Zwang;
  Inhalt haengt vom RCSI-Check am Zielsystem ab, siehe Nachbesserung 3 (2026-08-07): **Primaerweg**
  (RCSI AN, Standardfall) legt **nur** die zwei Zieltabellen `FaHierarchyNode`/`FaHierarchyOrderInfo`
  an, kein `GRANT ALTER`; **Fallback** (RCSI AUS + Staging/Swap gewaehlt) legt zusaetzlich zwei
  Staging-Tabellen an und erteilt `GRANT ALTER` auf die zwei Zieltabellen an das
  Sync-Service-SQL-Login).
- **Reihenfolge:** **RCSI-Check zuerst** (siehe Test-Szenarien, Schritt 0) — das Ergebnis bestimmt,
  welche Migrationsvariante deployt wird. Danach DB-Migration vor Service-Neustart; Web kann
  parallel deployt werden, da Teil 1 keine erreichbare Route hinzufuegt. **Primaerweg (RCSI AN,
  erwarteter Regelfall):** kein weiterer Rechte-Schritt noetig, der Sync ist nach der Migration
  sofort einsatzbereit. **Fallback (RCSI AUS):** die Migration erteilt `ALTER` auf
  `FaHierarchyNode`/`FaHierarchyOrderInfo` im selben Schritt (siehe Migrations-/SQL-Auswirkungen) —
  vor Aktivierung bestaetigen, dass das Sync-Service-Konto dieses Recht tatsaechlich traegt (z. B.
  falls der Service unter einem anderen Konto laeuft als vom Migrations-Deployer erwartet). Der
  Sync-Toggle bleibt nach dem Deploy in **beiden** Wegen **default aus** — muss am Zielsystem
  bewusst aktiviert werden (analog zur ADR-0008-Regel „jeder gewuenschte Sync muss einmalig
  aktiviert werden").
- **Publish-Befehle:**
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```
  (provisorisch — vom Dev-Lauf gegen den tatsaechlichen Diff zu bestaetigen).

## Offene Rueckfragen

Die Schranke-1-Antworten (unten) loesen die urspruenglichen Rueckfragen 1-5; die Kritischen
Pruefungen vom 2026-08-06 und 2026-08-07 haben die verbleibenden Detailfragen (6, 7) geklaert und
in den Spec-Text eingearbeitet (siehe „Nachbesserung" und „Nachbesserung 2" am Ende der Datei). Es
bleibt **ein** Infra-Check vor dem Dev-Lauf (siehe `open_questions` im Frontmatter), keine offene
fachliche Entscheidung mehr:

1. **GEKLAERT (Antwort 1).** Kombinationsgeraete: In Teil 1 wie normale Auftraege behandeln
   (fachlich keine Sonderlogik), Import technisch dedupliziert (Existenzpruefung, siehe
   Anforderung 2/3). `MontageAbteilung` nur informativ auf `FaHierarchyOrderInfo`.
   Materialisierungsseitige Konsequenz (nicht mehr eindeutige `OrderNumber`) → Teil 7.
2. **GEKLAERT (Antwort 2).** Produktiv-DB/Server vom Menschen notiert; direkt in den `appsettings`
   des IDEAL-Deployments gesetzt (kein Spec-Handlungsbedarf, kein Blocker).
3. **GEKLAERT (Antwort 3).** Toggle-Heimat `ServiceSettings` bestaetigt; Namensschema
   `Sync:HierarchicalFaEnabled` + `Sync:FaHierarchyListeViewName`/`Sync:FaHierarchyInfosViewName`,
   ohne Standort im Bezeichner (siehe Umsetzungsnotiz unten).
4. **GEKLAERT — Fehlerverhalten (Antwort 4), technische Verdrahtung ergaenzt.** Bei
   Whitelist-/QUOTENAME-Verstoss **und** bei leerem View-Read: Ablehnen/Skip + Protokoll +
   zusaetzliche Fehlermail ueber `ISyncErrorNotifier.NotifyAsync` (explizit in
   `FaHierarchySyncService` aufgerufen, da `RunResilientAsync` nur bei Exceptions mailt — siehe
   Technischer Loesungsentwurf S-4).
5. **GEKLAERT (Antwort 5).** Alter Worktree `ideal-anpassungen-v1` existiert nicht mehr; keine
   Test-Altlast wiederzuverwenden.
6. **GEKLAERT (Antwort N-1, Kritische Pruefung 2026-08-07).** Das Whitelist-Regex-Pattern ist
   final: `^[A-Za-z0-9_\-]+$` je Namenssegment (Bindestrich bewusst erlaubt — der reale
   Default-View-Name traegt einen), `--` zusaetzlich explizit abgelehnt. Der Unit-Test deckt die
   realen View-Namen als Positivfall ab (siehe Anforderung 8, AK 4/8).
7. **GEKLAERT (Antwort N-2, Kritische Pruefung 2026-08-07).** Das `ALTER`-Recht wird nicht als
   Umgebungsvoraussetzung abgewartet, sondern **von der Migration selbst erteilt**
   (`GRANT ALTER ON dbo.FaHierarchyNode` / `...FaHierarchyOrderInfo` an das Sync-Service-Login).
   Der `sp_rename`-Staging-Swap ist damit der **einzige** Full-Refresh-Pfad, kein Fallback mehr.
   Ein einziger Infra-Check bleibt vor dem Dev-Lauf: Laeuft der Service unter dem Konto, dem dieses
   Recht erteilt werden darf (siehe `open_questions` im Frontmatter und Deploy-Abschnitt)?
   **Ergaenzung (Nachbesserung 3, 2026-08-07):** Diese Antwort gilt seit dem RCSI-Entscheidungsweg-
   Abschnitt des Menschen nur noch fuer den **Fallback-Zweig** (RCSI AUS). Der neue **Primaerweg**
   (RCSI AN, erwarteter Regelfall) braucht **kein** `GRANT ALTER` — `DELETE FROM` +
   Neubefuellung in einer Transaktion genuegt, weil RCSI Leser nicht auf Schreibern blockieren
   laesst. Siehe „Full-Refresh-Strategie: vor dem `GRANT ALTER` die einfachere Frage stellen"
   weiter unten sowie die Nachbesserung 3 am Dateiende.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)
HINWEIS: ICH würde das nicht IDEALFASTRUKTUR etc. Nennen sondern in die Richtung FAHierarchyStruktur, dh. nicht den Standort in die Namensgebung 
1. →kombigeräte können im step 1 wie normale aufträge behandelt werden.
2. →servernamen und db habe ich bei mir notiert. ändere ich dann selber in den appsettings
3. →toggle für HierarchischeFA Logik und die anderen toggle auch. 
4. →zusätzliches Fehlermail
5. →nein, existiert nicht mehr
6. →
7. →

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

## Kritische Pruefung (2026-08-06)

Anwalt-des-Teufels-Durchsicht **nach** ausgefuellten Freigabe-Antworten (1-5), gegengelesen: die
Spec, die Ideen-Notiz inkl. ihrer eigenen Kritischen Pruefung, der Anhang [[sage-views-ideal]]
(korrekter Ist-Pfad), die Uebersicht, ADR 0001/0004/0008/0010, `fallstricke.md`, sowie der echte
main-Code (`SyncWorker`, `LagerbestandSyncService`, `CachedBomRepository`/`CachedBomHeader`,
`ServiceSettingDefinitions` + Drift-Guard-Test, `SyncLogServices`, aktuelle SQL-Migrationsnummern
inkl. der noch nicht gemergten WmsBugs-Worktrees).

### BLOCKER — Mensch muss entscheiden, bevor Teil 1 in Umsetzung geht

**B-1 — Der INNER JOIN im Import-SQL vervielfacht die Struktur-Zeilen bei Kombinationsgeraeten
(und jedem HauptFA mit >1 FAInfos-Zeile) — das widerspricht Freigabe-Antwort 1 direkt.**
Anforderung 2 + In-Scope schreiben das Import-SQL als `FAListe f INNER JOIN FAInfos i ON
i.HauptFA = f.HauptFA` fest. `FAInfos` hat laut Anhang die Granularitaet **eine Zeile = ein
Auftrag (ABNr + Pos)**, und Kombinationsgeraete tragen **denselben `HauptFA` mit mehreren
`[Montage-Abteilung]`-Zeilen**. Ein echter INNER JOIN auf diese 1:n-Beziehung **dupliziert damit
jede FAListe-Positionszeile pro FAInfos-Zeile** desselben `HauptFA`: ein Kombigeraet mit zwei
Montage-Abteilungen bekommt jede Struktur-/Stuecklisten-Position **doppelt** in `FaHierarchyNode`.
Genau die Faelle, die Antwort 1 „wie normale Auftraege, keine Sonderbehandlung" behandeln will,
werden so **still verfaelscht** — und die Verdopplung propagiert spaeter in jede Kommissionier-,
Beschichtungs- und Vormontage-Liste (Doppelzaehlung von Material). Der Anhang selbst zeigt dasselbe
fan-out-anfaellige Muster (`SELECT f.* FROM FAListe f INNER JOIN FAInfos i …`) — die Spec hat den
Defekt geerbt. Das Test-Szenario „Schritt 4 — Kombinationsgeraet" wuerde die Verdopplung sogar
sichtbar machen, **behauptet** aber „wie bei einem normalen Auftrag importiert", ohne sie als
Fehler zu erkennen.
  **Frage an den Menschen:** Bestaetige, dass die Datenverfuegbarkeits-Regel eine reine
  **Existenzpruefung** ist (Struktur-Position importieren, *wenn* zum `HauptFA` mindestens ein
  FAInfos-Eintrag existiert) und **keine** Zeilen-Multiplikation erzeugen darf. Dann muss das
  Import-SQL statt des fan-out-INNER-JOIN ein `WHERE EXISTS (SELECT 1 FROM FAInfos i WHERE
  i.HauptFA = f.HauptFA)` **oder** `INNER JOIN (SELECT DISTINCT HauptFA FROM FAInfos)` verwenden,
  sodass **jede FAListe-Position genau einmal** in `FaHierarchyNode` landet — unabhaengig davon,
  wie viele FAInfos-/Montage-Abteilungs-Zeilen der `HauptFA` hat. `FaHierarchyOrderInfo` behaelt
  dagegen bewusst **alle** FAInfos-Zeilen (eine je Montage-Abteilung). Anforderung 2, In-Scope,
  AK 2 und Test-Szenario Schritt 4 sind entsprechend zu schaerfen; ein AK „Kombigeraet-Positionen
  erscheinen in `FaHierarchyNode` **genau einmal**" fehlt und ist zu ergaenzen.
=> **ANTWORT (2026-08-06): Kombinationsgeraete werden fachlich NICHT gesondert behandelt — der
   Import muss trotzdem dedupliziert werden.** Praezisierung, weil "ausser Acht lassen" hier
   zweideutig waere:
   - **Import (technisch, VERBINDLICH):** Die Datenverfuegbarkeits-Regel ist eine reine
     **Existenzpruefung**. Import-SQL daher mit
     `WHERE EXISTS (SELECT 1 FROM FAInfos i WHERE i.HauptFA = f.HauptFA)` bzw.
     `INNER JOIN (SELECT DISTINCT HauptFA FROM FAInfos)`. Jede FAListe-Position landet **genau
     einmal** in `FaHierarchyNode`, unabhaengig von der Zahl der FAInfos-Zeilen. Das ist keine
     Sonderbehandlung fuer Kombigeraete, sondern die korrekte Semantik fuer ALLE Auftraege.
   - **NICHT gemeint:** Kombigeraete aus dem Import herausfiltern. Sie wuerden dann still fehlen
     — das waere schlimmer als die Verdopplung.
   - **Fachlich (Scope Teil 1):** keine Sonderlogik, keine Unterscheidung ueber
     `[Montage-Abteilung]`, keine eigene Gruppierung oder Anzeige. Kombigeraete laufen wie normale
     Auftraege durch.
   - `FaHierarchyOrderInfo` behaelt weiterhin **alle** FAInfos-Zeilen (eine je Montage-Abteilung).
   - **AK ergaenzen:** "Positionen eines HauptFA mit mehreren FAInfos-Zeilen erscheinen in
     `FaHierarchyNode` genau einmal."
   - **Test-Szenario Schritt 4 schaerfen:** nicht "wird wie ein normaler Auftrag importiert",
     sondern "Positionszahl identisch zur FAListe-Zeilenzahl, keine Verdopplung".
   - **Backlog-Nachtrag anlegen:** fachliche Behandlung von Kombinationsgeraeten (Unterscheidung
     ueber Montage-Abteilung, Anzeige/Gruppierung) als eigener spaeterer Punkt.
### SOLLTE — vor dem Dev-Lauf schaerfen (konkreter Vorschlag)

**S-1 — Full-Refresh „Delete-All + Bulk-Insert in einer Transaktion" hat KEINEN Praezedenzfall im
Code und ist so, wie beschrieben, riskant.** Kein bestehender Service macht Delete-All+Insert:
`CachedBomHeader`/BomCache ist **Hash-inkrementell** (Upsert je Artikel, kein Truncate), enaio ist
**MERGE-Full-Sync** — der Spec-Satz „analog `CachedBomHeader`" stimmt nur fuer das *Caching*, nicht
fuer die *Schreibstrategie*. `FaHierarchyNode` ist die **Stueckliste** (laut Anhang potenziell
hunderte Positionen je Struktur, ueber viele Strukturen → schnell zehntausende Zeilen). Ein
`DELETE`-all + Bulk-`INSERT` in einer Transaktion haelt fuer die gesamte Ladezeit Sperren, eskaliert
zum Table-Lock und **blockiert jeden gleichzeitigen Web-Read** (Repository/Cache-Miss) bis zum
Commit — bei Teil-Ausfall/Timeout Rollback auf den alten Stand (gut), aber Deadlock- und
Blocking-Risiko real. Vorschlag konkret:
  1. **Beide Tabellen in EINER Transaktion** ersetzen (nicht „je Tabelle" separat) — sonst kann
     `FaHierarchyNode` (neu) neben altem `FaHierarchyOrderInfo` stehen und umgekehrt.
  2. **Empty-Guard auf den ROH-Zeilenzahlen BEIDER Views auswerten, BEVOR** irgendeine Tabelle
     angefasst wird (analog `LagerbestandZeroingPlanner`: `sagePresentKeys` aus `rawSageRows` **vor**
     jeder Mutation). AK 4 sagt „eine oder beide Views 0 → kein Replace" — das muss explizit als
     Vorab-Check auf den unveraenderten Read stehen, nicht als Post-Join-Zaehlung (sonst maskiert
     der INNER JOIN aus B-1 einen leeren FAInfos-Read als „0 Struktur-Zeilen" und der Guard
     verwechselt Ausfall mit Leerstand).
  3. Lock-Zeit begrenzen: entweder Staging-Tabelle + `sp_rename`/Partition-Switch, oder Batch-Delete,
     oder mindestens Snapshot-Isolation, damit Web-Reads den **alten** Stand sehen statt zu blocken.

**S-2 — View-Namen-Absicherung: Whitelist-Regex allein ist zu duenn; QUOTENAME fehlt voellig.**
Die Spec nennt nur eine Regex und sagt selbst „Objektnamen lassen sich nicht parametrisieren" —
richtig, aber die eigentliche Abwehr ist **strukturell**: Namen in `[Schema].[Name]` zerlegen,
jeden Teil validieren, dann per **`QUOTENAME(part)`** wieder zusammensetzen, damit Injection auch
dann unmoeglich ist, wenn die Regex spaeter gelockert wird. QUOTENAME kommt in der ganzen Spec
nicht vor. Fuer das (bewusst offene) exakte Pattern in `open_questions`/Rueckfrage 4 mitnehmen:
  - **ASCII-explizit** `^[A-Za-z0-9_]+$` je Namensteil, **nicht** `\w` (unter Unicode-Regex matcht
    `\w` Homoglyphen — genau der im Auftrag genannte Angriff); `-`/`.` nur innerhalb der erkannten
    Klammer-/Punkt-Struktur, `]` im Namen als `]]` behandeln.
  - Explizit ablehnen: Whitespace, `;`, `--`, `/*`, `[`/`]`-Ungleichgewicht, alles jenseits ASCII.
  - Danach **QUOTENAME** auf die geparsten Teile — Whitelist UND QUOTENAME, nicht entweder/oder.

**S-3 — Drift-Guard greift fuer die 3 neuen Keys NICHT automatisch.** Der reale Test
`ServiceSettingDefinitionsTests.All_ContainsDocumentedServiceReadKey` ist eine **hartcodierte
`[InlineData]`-Liste** — er schlaegt nur fehl, wenn ein *dort gelisteter* Key im Katalog fehlt.
Fuegt man `Sync:HierarchicalFaEnabled` / `Sync:FaHierarchyListeViewName` /
`Sync:FaHierarchyInfosViewName` nur in `ServiceSettingDefinitions.All` ein (fuer den Seed), bleibt
der Test **gruen ohne jede Guard-Wirkung**. Vorschlag: die 3 neuen Keys **zusaetzlich als
`[InlineData]`** in den Drift-Guard-Test eintragen (sonst null Testabdeckung, dass sie katalogisiert
sind). In die Checkliste/AK aufnehmen.

**S-4 — „Zusaetzliche Fehlermail" (Freigabe-Antwort 4) ist im Loesungsentwurf nicht verdrahtet.**
Der Design-Abschnitt nennt als Ctor-Abhaengigkeit nur `ISyncLogger`. Aber: `RunResilientAsync`
schickt eine Fehlermail **nur bei einer geworfenen Exception**. Der Empty-Guard ist ein **Warn**
(kein throw) → RunResilientAsync mailt dort **nicht**. Damit Antwort 4 (Fehlermail bei ungueltigem
View-Namen **und** bei leerem View-Read) haelt, muss `FaHierarchySyncService` — wie
`LagerbestandSyncService` fuer seinen Cap — **`ISyncErrorNotifier` injizieren** und in beiden
Pfaden (Invalid-Name nach `FinishFailedAsync`, Empty-Guard nach Warn) explizit `NotifyAsync`
aufrufen. Abhaengigkeit im Loesungsentwurf benennen.

### HINWEIS — kleinere Beobachtungen und Staerken

**H-1 — Migrationsnummer „ab 87" ist bereits ueberholt.** main ist bei `SQL/83`. Der noch nicht
gemergte Worktree `2026-08-05-wms-bugs-improvements-teil-1-2-3` belegt bereits `84/85/86/**87**`
(`87_AddUserDefaultFilterBomDescription1.sql`), der 4-8-Worktree `84/85/86`. Nach Merge beider
Batches ist die naechste freie Nummer **voraussichtlich `88`**, nicht 87. Die Spec sagt korrekt
„Platzhalter, vor Dev-Lauf gegen den gemergten Stand pruefen" — nur die konkrete Zahl „87" (auch in
`affected_code` als `86_…`) anpassen.

**H-2 — Rekursions-Indizes fehlen fuer spaeteren Konsum.** Das Modell indiziert `HauptFA` und
`VaterFA`, aber **nicht** `SubFA`. Die Mehrstufigkeit wird beim Konsum ueber `VaterFA → SubFA`
traversiert (Teil 2/7); ein Index auf `SubFA` ist dafuer sinnvoll. Fuer Teil 1 (reiner
`HauptFA`-Lookup) unkritisch — nur als Notiz fuer Teil 2 festhalten.

**H-3 — Groesse am oberen Rand eines Dev-Laufs.** 2 Modelle + Migration + FreshInstall (2 Stellen)
+ Sync-Service + SQL-Helper + 2 Repos + 1 Decorator + DI + `SyncLogServices` + Worker-Block + 2
View-DDL-Dokus + Unit-Tests (Whitelist) + Testszenarien. Machbar, weil ueberwiegend
Muster-Nachbau — aber die einzige echte Design-Nuance (B-1, Join-Semantik) vorher klaeren, sonst
wird im Dev-Lauf improvisiert.

**H-4 — Gut geloest (beibehalten):** „kein `AuditableEntity`, Nachvollziehbarkeit via `SyncLog` +
`SyncedAt`" ist sauber am Praezedenzfall `CachedBomHeader`/`AppSettings` begruendet; das
Zwei-Tabellen-Modell (auftrags- vs. positionsbezogen) ist korrekt gegen Denormalisierung
abgewogen; Out-of-Scope (ProductionOrders unberuehrt, keine UI/Rollen, AKE-Linie unveraendert) ist
scharf. Regressionsrisiko fuer `ProductionOrders` real gering (rein additive Tabellen).

**H-5 — Randnotiz ausserhalb dieser Datei:** Die Freigabe-Antworten der Uebersichts-Spec
(`…-uebersicht.md`, Fragen 1-3) stehen noch leer — Schranke 1 des Gesamtpakets ist dort nicht
abgeschlossen (nicht Gegenstand dieser Teil-1-Pruefung, aber fuer den Orchestrator relevant).

NACHBESSERUNG NOETIG: B-1 — der INNER-JOIN-Import verdoppelt Kombigeraet-/Mehrfach-FAInfos-Positionen und widerspricht Freigabe-Antwort 1; Existenz-/DISTINCT-Semantik muss vom Menschen bestaetigt werden (S-1..S-4 begleitend).

## Antworten auf S-1 bis S-4 und H-1 (2026-08-06)

**S-1 bis S-4 werden wie vorgeschlagen umgesetzt** — verbindliche Vorgaben an den Dev-Lauf, keine
weitere Rueckfrage noetig. Im Einzelnen:

- **S-1 (Full-Refresh):** Beide Tabellen in **einer** Transaktion ersetzen. Der Empty-Guard wird auf
  den **Roh-Zeilenzahlen beider Views** ausgewertet, **bevor** irgendeine Tabelle angefasst wird
  (analog `LagerbestandZeroingPlanner`) — sonst maskiert der Join einen leeren FAInfos-Read als
  "0 Struktur-Zeilen" und der Guard verwechselt Ausfall mit Leerstand. Lock-Zeit begrenzen
  (Staging + `sp_rename`, Batch-Delete oder mindestens Snapshot-Isolation), damit gleichzeitige
  Web-Reads den alten Stand sehen statt zu blockieren.
- **S-2 (View-Namen):** Whitelist **UND** `QUOTENAME` — nicht entweder/oder. Namen in
  `[Schema].[Name]` zerlegen, jeden Teil gegen **ASCII-explizites** `^[A-Za-z0-9_]+$` pruefen
  (nicht `\w`, das matcht unter Unicode Homoglyphen), `]` als `]]` behandeln, dann per `QUOTENAME`
  zusammensetzen. Whitespace, `;`, `--`, `/*` und alles jenseits ASCII explizit ablehnen.
- **S-3 (Drift-Guard):** Die drei neuen Keys **zusaetzlich als `[InlineData]`** in
  `ServiceSettingDefinitionsTests` eintragen. Nur im Katalog stehen genuegt nicht — der Test
  bliebe gruen ohne jede Guard-Wirkung. In die Checkliste aufnehmen.
- **S-4 (Fehlermail):** `FaHierarchySyncService` injiziert `ISyncErrorNotifier` (wie
  `LagerbestandSyncService`) und ruft `NotifyAsync` in **beiden** Pfaden explizit auf — nach
  `FinishFailedAsync` bei ungueltigem View-Namen **und** nach dem Warn des Empty-Guards.
  `RunResilientAsync` mailt nur bei geworfener Exception; der Empty-Guard wirft nicht.
  Abhaengigkeit im Loesungsentwurf benennen.

**H-1 (Migrationsnummer):** Keine Zahl festschreiben. Die Spec fuehrt sie als **Platzhalter**; die
tatsaechlich freie Nummer wird **unmittelbar vor dem Dev-Lauf** gegen den dann gemergten
main-Stand bestimmt (aktuell voraussichtlich `87`, nach Merge der beiden WmsBugs-Batches ggf.
hoeher). Auch die `86_…`-Referenz in `affected_code` entsprechend als Platzhalter kennzeichnen.

**H-2 (Index auf `SubFA`):** Fuer Teil 1 nicht noetig (reiner `HauptFA`-Lookup). Als Notiz fuer
Teil 2 vormerken, wo ueber `VaterFA -> SubFA` traversiert wird.

**H-5 (Uebersichts-Spec):** erledigt — die Freigabe-Antworten 1-3 der Uebersicht sind am
2026-08-06 beantwortet worden.

### Nachbesserung (2026-08-06)

Status je Befund aus der Kritischen Pruefung, nach Einarbeitung in den Spec-Text:

- **B-1 (INNER-JOIN-Verdopplung):** behoben im Text (Anforderung 2, In-Scope, Datenmodell/"Warum
  zwei Tabellen", AK 3, Test-Szenario Schritt 4). Import-SQL auf `WHERE EXISTS`/`DISTINCT`-Semantik
  umgestellt, `FaHierarchyOrderInfo` bleibt separat und vollstaendig.
- **S-1 (Full-Refresh-Strategie):** behoben im Text (Technischer Loesungsentwurf > Sync-Service >
  Full-Refresh-Strategie, Datenmodell > Staging-Pendants, AK 9/11, Test-Szenario Schritt 5,
  Deploy-Abschnitt). Staging-Tabelle + `sp_rename`-Swap als Hauptstrategie, kurze
  Einzel-Transaktion je Tabelle als Fallback; Empty-Guard explizit auf Roh-Zeilenzahlen **vor**
  jeder Mutation gelegt. Die konkrete Berechtigungsfrage (`ALTER`/`sp_rename`-Recht der
  Sync-Service-SQL-Login am Zielsystem) ist eine echte Infrastruktur-/Menschen-Entscheidung und
  **bleibt Rueckfrage** (offene Rueckfrage 7).
- **S-2 (Whitelist + QUOTENAME):** behoben im Text (Anforderung 8, Technischer Loesungsentwurf >
  Sync-Service, AK 4/8). Zweistufige Absicherung (ASCII-Regex + `QUOTENAME`) jetzt verbindlich
  vorgegeben; das exakte Regex-Pattern im Detail **bleibt Rueckfrage** (offene Rueckfrage 6, wie
  bereits vor der Kritischen Pruefung als kein Blocker vorgesehen).
- **S-3 (Drift-Guard-Test):** behoben im Text (Technischer Loesungsentwurf > Sync-Service, AK 10,
  affected_code-Eintrag `ServiceSettingDefinitionsTests.cs`). Explizite Pflicht, die drei Keys
  zusaetzlich als `[InlineData]` einzutragen, ist jetzt eigener, objektiv pruefbarer
  Akzeptanzkriterium-Punkt.
- **S-4 (Fehlermail-Verdrahtung):** behoben im Text (Technischer Loesungsentwurf > Sync-Service >
  Fehlermail, AK 4/5, Test-Szenarien Negativfaelle). `ISyncErrorNotifier` als expliziter
  Ctor-Parameter benannt, `NotifyAsync` an beiden Warn-/Fail-Pfaden mit synthetischer Exception
  (Praezedenzfall `LagerbestandSyncService`-Cap-Skip) vorgeschrieben.
- **H-1 (Migrationsnummer):** bereits in der vorherigen Antwortrunde als Platzhalter behandelt;
  in diesem Durchgang zusaetzlich `affected_code` und Migrations-Abschnitt auf `87` (statt `86`)
  aktualisiert und als Platzhalter markiert — bleibt vor dem Dev-Lauf zu pruefen, kein
  Rueckfrage-Blocker.
- **H-2 (Index auf `SubFA`):** unveraendert als Notiz fuer Teil 2 gehalten, kein Handlungsbedarf
  in Teil 1 — bleibt keine offene Rueckfrage.
- **H-3/H-4 (Groesse, Staerken):** keine Textaenderung noetig, reine Beobachtungen.
- **H-5 (Uebersichts-Spec):** ausserhalb dieser Datei, bereits als erledigt vermerkt.

## Kritische Pruefung (2026-08-07)

Zweiter Anwalt-des-Teufels-Durchgang **nach** der Ueberarbeitung/Nachbesserung vom 2026-08-06.
Gegengelesen: der ueberarbeitete Rumpf, alle Antwort-/„=>"-Bloecke, die „Antworten auf S-1..S-4"
und die „Nachbesserung", der Anhang [[sage-views-ideal]], ADR 0004/0008/0010, `fallstricke.md`,
sowie **verifiziert am echten main-Code**: `ServiceSettingDefinitions.cs` (inkl.
`ServiceSettingType.String`), `ServiceSettingDefinitionsTests.All_ContainsDocumentedServiceReadKey`
(hartcodierte `[Theory]/[InlineData]`-Liste — Methodenname exakt wie in der Spec behauptet),
`ISyncErrorNotifier.NotifyAsync(string, Exception, CancellationToken)` + `SyncErrorNotifier`,
`LagerbestandSyncService` (injiziert `ISyncErrorNotifier`, konstruiert synthetische
`InvalidOperationException` fuer den Cap-Skip — S-4-Praezedenzfall stimmt exakt),
`SyncWorker.RunResilientAsync` (mailt nur bei geworfener Exception — S-4-Begruendung korrekt), und
eine Suche nach `sp_rename`/`_Staging`/`TRUNCATE`-Praezedenz (es gibt **keine** — Staging-Swap ist
tatsaechlich neu, wie die Spec offen sagt).

**Zuerst das Positive (nicht mehr zu diskutieren):** B-1 ist im Text sauber ausgeraeumt — das
Import-SQL nutzt jetzt eine echte `WHERE EXISTS`-Existenzpruefung, „genau einmal je FAListe-Position"
ist als AK 3 pruefbar verankert, `FaHierarchyOrderInfo` bleibt separat/vollstaendig. S-3 und S-4
sind korrekt und **codeverifiziert** verdrahtet. Die folgenden Befunde sind **neu** bzw. betreffen
Punkte, die die Nachbesserung nur scheinbar geschlossen hat.

### BLOCKER — vor Umsetzung zu entscheiden/korrigieren

**N-1 — Die als „verbindlich" festgeschriebene Whitelist-Regex `^[A-Za-z0-9_]+$` verwirft die
einzigen real existierenden View-Namen; der von der Spec selbst gesetzte Default verstoesst gegen
seine eigene Validierung.** Anforderung 8, der S-2-Antwortblock und der Sync-Service-Abschnitt
schreiben **verbindlich** vor: den Namen in `[Schema].[Name]` zerlegen und jeden Teil ASCII-explizit
gegen `^[A-Za-z0-9_]+$` pruefen. Der reale View-Name lautet aber laut Anhang **und laut dem in
dieser Spec gesetzten Default** `vw_IDEAL-AKE_Kommissionierung_FAListe` bzw. `…_FAInfos` — er
enthaelt einen **Bindestrich** (`IDEAL-AKE`). `[A-Za-z0-9_]` schliesst `-` aus, also faellt der
Name-Teil durch die eigene Whitelist. Konsequenz: ein Dev, der die Spec woertlich umsetzt, baut
einen Validator, der den ausgelieferten Default (`Sync:FaHierarchyListeViewName` =
`[vw_IDEAL-AKE_Kommissionierung_FAListe]`) sofort ablehnt — der Sync kann gegen die realen Views
**nie** laufen; AK 4 („gueltiger Name wird per `QUOTENAME` abgesichert") ist fuer den kanonischen
Namen unerfuellbar, weil er QUOTENAME nie erreicht. `open_questions`/Rueckfrage 6 stuft genau dieses
Detail als blossen „Feinschliff, kein Blocker" ein — das ist die eigentliche Falle: das
Zeichen-Set ist nicht Feinschliff, sondern fuer die vorhandenen Daten schlicht falsch. Zu
korrigieren: `-` (und ggf. `.` innerhalb der erkannten Struktur) im erlaubten Zeichensatz je
Name-Teil explizit zulassen, **ohne** auf `\w` auszuweichen (Homoglyphen-Argument bleibt gueltig),
und den Widerspruch zwischen „verbindlich `^[A-Za-z0-9_]+$`" und dem Default aufloesen.

**N-2 — Der Fallback-Pfad (Rueckfrage 7) widerspricht der Atomaritaets-Garantie und traegt die
Berechtigungsfrage nicht, fuer die er gedacht ist.** Der ueberarbeitete Full-Refresh nennt als
Fallback (falls das `ALTER`/`sp_rename`-Recht fehlt): „`TRUNCATE` + Bulk-Insert **je Zieltabelle
einzeln in einer eigenen kurzen Transaktion**". Das kollidiert doppelt:
  - **Atomaritaet:** Punkt 5 derselben Strategie, AK 9 („entweder **beide** Tabellen aktualisiert
    oder **beide** unveraendert — kein Zwischenzustand") und der eigene S-1-Antwortblock („Beide
    Tabellen in **einer** Transaktion ersetzen") verlangen einen tabellenuebergreifend atomaren
    Ersatz. Zwei getrennte Transaktionen (je Tabelle eine) erzeugen genau den verbotenen
    Zwischenzustand: `FaHierarchyNode` neu neben `FaHierarchyOrderInfo` alt. Der Fallback kann
    „kurze Lock-Zeit je Tabelle" und „beide als ein logischer Schritt" nicht gleichzeitig erfuellen
    — die Nachbesserung hat hier einen unaufloesbaren Selbstwiderspruch eingebaut.
  - **Berechtigung:** `TRUNCATE TABLE` verlangt in SQL Server mindestens **`ALTER`-Recht** auf der
    Tabelle — dasselbe Recht, dessen moegliches Fehlen den Fallback ueberhaupt ausloest
    (`sp_rename` braucht ebenfalls `ALTER`). Der als „rechtearmer" Ausweg praesentierte Fallback
    braucht also praktisch dieselbe Berechtigung wie die Hauptstrategie. Der einzige echte
    Ohne-`ALTER`-Pfad waere `DELETE FROM` — und den hat S-1 bewusst als langsperrend verworfen.
    Damit ist Rueckfrage 7 nicht wirklich „mit Fallback abgesichert", sondern offen: ohne `ALTER`
    gibt es **keinen** in der Spec tragfaehigen Full-Refresh.

### SOLLTE — vor dem Dev-Lauf schaerfen

**N-3 — Die `sp_rename`-Swap-Mechanik ist unterspezifiziert und in AK 11 zu absolut formuliert.**
Ein Zwei-Tabellen-Tausch Ziel↔Staging ist kein Ein-Schritt-Rename: man braucht **drei** Renames je
Tabelle (Ziel→Temp, Staging→Ziel, Temp→Staging) mit einem Zwischennamen; das fehlt in der Spec.
Zudem verschiebt `sp_rename` **keine** Constraint-/PK-/Index-Namen mit — nach dem ersten Swap traegt
die produktive `FaHierarchyNode` die PK/Index-Namen ihres Staging-Ursprungs (`…_Staging`); rein
kosmetisch, aber ueber Migrationen hinweg driftend und beim naechsten Schema-`ALTER` verwirrend.
`sp_rename` nimmt waehrend des Tauschs eine **Sch-M-Sperre**, ein gleichzeitiger Web-Read eine
Sch-S-Sperre — fuer die (sehr kurze) Swap-Dauer blockieren die sich also doch gegenseitig. AK 11
(„ohne Blocking/Timeout") ist damit fuer den Swap-Moment leicht zu absolut; korrekt waere „kein
Blocking waehrend der Ladezeit, nur eine vernachlaessigbare Metadaten-Sperre im Swap-Moment".

**N-4 — Folge-Migrationen muessen die Staging-Tabellen zwingend mitziehen — als harte Regel
festhalten.** Weil die Zieltabelle und ihr Staging-Pendant schema-identisch bleiben **muessen**
(sonst bricht der Bulk-Insert/Swap), muss **jede** spaetere Spalten-/Typaenderung an
`FaHierarchyNode`/`FaHierarchyOrderInfo` das jeweilige `_Staging` im selben Migrationsschritt
mitaendern. Das ist die gleiche Klasse von Kopplung wie ADR 0004 fuer `SQL/AgentJobs/*` fordert,
steht hier aber nirgends als Pflicht. Ohne diese Notiz reisst die erste Folge-Migration den Swap
still auf.

### HINWEIS

**H-6 — Empty-Guard liest jede View doppelt.** Erst `SELECT COUNT(*)` je View, dann Voll-Read —
bei zehntausenden Zeilen zwei Roundtrips/Scans. Vertretbar (Sekundenbereich, laeuft nur alle paar
Minuten), aber erwaehnenswert; alternativ Count aus dem bereits geladenen Voll-Read ableiten, sobald
gelesen — dann faellt aber die „vor jeder Mutation"-Reihenfolge, die S-1 bewusst will. Bewusst so
lassen ist ok, nur nicht „umsonst".

**H-7 — Drift-Guard-Konvention ist im Bestand bereits uneinheitlich.** Mehrere existierende
Katalog-Keys (`SageLagerbuchungAktiv`, `SData:*`, `Sync:SageLagerbuchung*`) stehen **nicht** als
`[InlineData]` im Guard-Test. Die S-3-Forderung, die drei neuen Keys einzutragen, ist also strenger
als die gelebte Praxis — das ist gut und richtig (mehr Abdeckung), nur kein „so machen es alle".
Kein Handlungsbedarf, nur zur Einordnung.

BEREIT ZUR FREIGABE: nein. NACHBESSERUNG NOETIG: N-1 (Whitelist `^[A-Za-z0-9_]+$` verwirft die realen
View-Namen mit Bindestrich — der eigene Default verstoesst gegen die eigene Validierung; „Feinschliff"
ist es nicht) und N-2 (der Fallback erzeugt den von AK 9 verbotenen Zwischenzustand **und** braucht
via `TRUNCATE` dasselbe `ALTER`-Recht wie die Hauptstrategie — Rueckfrage 7 bleibt real offen).
N-3/N-4 begleitend.

## Antworten auf die Kritische Pruefung (2026-08-07)

**Zu N-1 — Fehler eingestanden. Die Regex war falsch, und zwar meine Schuld. [KORREKTUR]**
Die S-2-Vorgabe `^[A-Za-z0-9_]+$` entstand aus der Sorge vor Unicode-Homoglyphen — dabei ist
uebersehen worden, dass der reale Default-View-Name selbst einen **Bindestrich** traegt
(`vw_IDEAL-AKE_Kommissionierung_FAListe`). Woertlich umgesetzt haette die Sicherung ihre eigene
Datenquelle abgewiesen und den IDEAL-Sync komplett lahmgelegt.

**Korrigierte Vorgabe:**
- Erlaubtes Zeichenset je Segment: **`^[A-Za-z0-9_\-]+$`** (ASCII-explizit, Bindestrich erlaubt).
- Weiterhin **abgelehnt**: Whitespace, `;`, `/*`, `[`, `]`, Punkt innerhalb eines Segments und
  alles jenseits ASCII. Zusaetzlich die Zweizeichenfolge **`--`** explizit ablehnen — sie kann in
  keinem echten Objektnamen vorkommen und ist der SQL-Kommentar-Einleiter (Defense-in-Depth ueber
  `QUOTENAME` hinaus, das den Namen ohnehin klammert).
- Schema/Name weiterhin an `.` **splitten** und je Segment einzeln pruefen, dann per `QUOTENAME`
  zusammensetzen.

**Die eigentliche Lehre — als AK aufnehmen:** Der Unit-Test der Validierung muss die **realen
Produktions-View-Namen als Positivfaelle** enthalten (`vw_IDEAL-AKE_Kommissionierung_FAListe`,
`vw_IDEAL-AKE_Kommissionierung_FAInfos`) und nicht nur erfundene Beispiele. Genau dieser eine
Testfall haette den Fehler in Sekunden gefunden. Negativfaelle (Semikolon, Leerzeichen, `--`,
kyrillisches `а`) daneben.

**Zu N-2 — Befund akzeptiert. Der Fallback wird ersatzlos GESTRICHEN; stattdessen wird das
Berechtigungsproblem geloest, statt es zu umgehen. [ENTSCHEIDUNG]**
Die Pruefung hat recht in beiden Punkten: `TRUNCATE` verlangt dasselbe `ALTER`-Recht wie
`sp_rename`, und ein Fallback „je Tabelle einzeln" erzeugt genau den von AK 9 verbotenen
Zwischenzustand. Ein Ausweg, der dieselbe Berechtigung braucht wie der Hauptweg, ist kein Ausweg.

Entscheidend ist aber etwas anderes: **`FaHierarchyNode` und `FaHierarchyOrderInfo` sind UNSERE
Tabellen in UNSERER Datenbank** — nicht Sage. Die Berechtigung ist keine Naturkonstante, sondern
etwas, das wir setzen. Daher:
- **Die Migration, die die beiden Tabellen anlegt, erteilt dem Service-Konto im selben Schritt
  `ALTER` auf genau diese zwei Tabellen** (`GRANT ALTER ON dbo.FaHierarchyNode TO <konto>` etc.).
  Eng begrenzt, kein DDL-Recht auf der Datenbank.
- Damit ist die `sp_rename`-Strategie **die einzige** — kein zweiter Pfad, kein Selbstwiderspruch,
  keine offene Rueckfrage 7.
- **Vor dem Dev-Lauf pruefen** (eine Abfrage): Laeuft der Service unter einem Konto, dem wir das
  Recht erteilen duerfen? Bei den Migrationen hat ohnehin jemand DDL-Rechte — die Frage ist nur,
  ob es dasselbe Konto ist.
- **Nur falls** die Organisation das Erteilen verbietet: dann `DELETE FROM` beide Tabellen +
  Neubefuellung in **einer** Transaktion (braucht nur `DELETE`, ist voll transaktional, haelt aber
  laenger Sperren). Das ist der **einzige** echte Ohne-`ALTER`-Pfad — `TRUNCATE` ist es nicht. Als
  dokumentierte Notloesung fuehren, nicht als gleichwertige Alternative.
Rueckfrage 7 gilt damit als beantwortet: **Recht erteilen, nicht umgehen.**

**Zu N-3 — uebernommen.** Die Swap-Mechanik wird ausgeschrieben: **drei** Renames je Tabelle
(Ziel→Temp, Staging→Ziel, Temp→Staging) mit definiertem Zwischennamen. Und der Hinweis auf die
nicht mitwandernden Constraint-/PK-/Index-Namen wird als **bekannter kosmetischer Drift** vermerkt
(mit der Konsequenz, dass Namensschemata nach dem ersten Swap nicht mehr zum Tabellennamen passen —
beim naechsten Schema-`ALTER` beruecksichtigen).
**AK 11 entschaerfen:** statt „ohne Blocking/Timeout" → „**kein Blocking waehrend der Ladezeit; im
Swap-Moment nur eine vernachlaessigbare Metadaten-Sperre (Sch-M)**". Die bisherige Formulierung war
nachweislich zu absolut.

**Zu N-4 — uebernommen, als harte Regel.** Jede spaetere Spalten-/Typaenderung an
`FaHierarchyNode`/`FaHierarchyOrderInfo` **muss die zugehoerige `_Staging`-Tabelle im selben
Migrationsschritt mitziehen** — sonst bricht Bulk-Insert oder Swap still auf. Gehoert in die Spec
UND als Fallstrick in `secondbrain/architektur/` bzw. neben die ADR-0004-Kopplungsregel, damit es
auch findet, wer die Spec nicht liest.

**Zu H-6 — bewusst so belassen.** Der Doppel-Read ist der Preis dafuer, dass der Empty-Guard
**vor** jeder Mutation greift. Das ist gewollt (S-1) und im Sekundenbereich. In der Spec als
bewusste Entscheidung benennen, nicht als Versehen.

**Zu H-7 — zur Kenntnis, Vorgabe bleibt.** Dass mehrere Bestands-Keys nicht im Drift-Guard stehen,
ist ein Argument fuer mehr Abdeckung, nicht fuer weniger. Die drei neuen Keys werden eingetragen.

## Full-Refresh-Strategie: vor dem `GRANT ALTER` die einfachere Frage stellen (2026-08-07)

Die offene Infra-Frage lautet „Darf das Sync-Login `ALTER` bekommen?". Davor gehoert aber eine
andere Frage, die sie moeglicherweise erledigt: **Brauchen wir den Staging-/Swap-Mechanismus
ueberhaupt?**

**Warum das zu pruefen ist:** Der Swap wurde gewaehlt, um Sperrzeit zu minimieren. Die Pruefungen
N-3 und N-4 haben aber gezeigt, dass er **dauerhafte Kosten** traegt:
- drei Renames je Tabelle mit Zwischennamen (nicht ein Schritt),
- Constraint-/PK-/Index-Namen wandern nicht mit → Namensdrift nach dem ersten Swap,
- **jede kuenftige Migration muss die `_Staging`-Tabelle im Gleichschritt mitziehen**, sonst bricht
  der Swap still auf. Eine permanente Kopplung, die man in einem Jahr garantiert vergisst.
Das ist viel Dauerkomplexitaet, um wenige Sekunden Sperrzeit zu vermeiden — bei einem Sync, der
alle paar Minuten laeuft, nicht im Sekundentakt.

**Entscheidender Vorabtest — Read Committed Snapshot Isolation:**
```sql
SELECT name, is_read_committed_snapshot_on
FROM sys.databases WHERE name = DB_NAME();
```
- **RCSI ist AN** → Leser blockieren nicht auf Schreibern. Dann ist der Swap ueberfluessig:
  **`DELETE FROM` beide Tabellen + Neubefuellung in EINER Transaktion.** Kein `GRANT ALTER`, keine
  Staging-Tabellen, kein Drei-Rename-Tanz, keine Migrations-Kopplung, AK 9 trivial erfuellt — und
  die Web-Reads sehen waehrend des Laufs den alten Stand statt zu warten. **Das ist die
  bevorzugte Variante.**
- **RCSI ist AUS** → zwei Wege: RCSI aktivieren (breitere Aenderung, eigene Abwaegung, wirkt auf
  die ganze Datenbank) **oder** `ALTER` erteilen und beim Swap bleiben.

**Falls es beim `GRANT ALTER` bleibt — die Risikoeinordnung:** Das Sync-Konto **ersetzt ohnehin
alle paar Minuten den kompletten Inhalt beider Tabellen**. `ALTER` auf genau diese zwei Objekte
fuegt einem Konto, das ihren Inhalt schon vollstaendig kontrolliert, praktisch keine neue
Angriffsflaeche hinzu. Der Grant ist eng (zwei Objekte, kein Schema-, kein DB-Recht) und gehoert in
die anlegende Migration, damit er reproduzierbar ist und nicht von Hand nachgezogen werden muss.

**Aktuelle Rechte pruefen:**
```sql
SELECT dp.permission_name, dp.state_desc, ISNULL(o.name, '(Datenbank)') AS objekt
FROM sys.database_permissions dp
LEFT JOIN sys.objects o ON o.object_id = dp.major_id
WHERE dp.grantee_principal_id = DATABASE_PRINCIPAL_ID('<sync-login>');

SELECT r.name AS rolle
FROM sys.database_role_members m
JOIN sys.database_principals r ON r.principal_id = m.role_principal_id
WHERE m.member_principal_id = DATABASE_PRINCIPAL_ID('<sync-login>');
```

**Reihenfolge der Klaerung:** erst RCSI pruefen. Ist es an, entfaellt die `ALTER`-Frage samt
Staging-Abschnitt, N-3 und N-4 — und die Spec wird an dieser Stelle deutlich schlanker.

### Nachbesserung 2 (2026-08-07)

Status je Befund aus der Kritischen Pruefung 2026-08-07, nach Einarbeitung in den Spec-Text (Rumpf,
Fachliche Anforderungen, Technischer Loesungsentwurf, Akzeptanzkriterien, Test-Szenarien, Deploy,
Frontmatter):

- **N-1 (Bindestrich-Whitelist-Fix, BLOCKER):** eingearbeitet in Anforderung 8, im Sync-Service-
  Abschnitt (Whitelist-Regex + `QUOTENAME`) und in AK 4/8. Zeichenset korrigiert auf
  `^[A-Za-z0-9_\-]+$` je Segment (ASCII-explizit, Bindestrich bewusst erlaubt, weiterhin **kein**
  `\w`), `--` zusaetzlich explizit abgelehnt. `QUOTENAME` bleibt als strukturelle
  Zweitsicherung bestehen. AK 8 fordert jetzt explizit die realen View-Namen als
  Unit-Test-Positivfaelle. `affected_code`-Eintrag zu `FaHierarchySql.cs` entsprechend ergaenzt.
- **N-2 (Fallback-Widerspruch, BLOCKER):** eingearbeitet in die Full-Refresh-Strategie (Technischer
  Loesungsentwurf > Sync-Service), im Migrations-/SQL-Auswirkungen-Abschnitt, im Deploy-Abschnitt
  und in AK 12 (neu). Der bisherige `TRUNCATE`-je-Tabelle-Fallback ist ersatzlos gestrichen. Statt
  eine Umgebungsvoraussetzung abzuwarten, erteilt die Migration selbst `GRANT ALTER` auf die zwei
  Zieltabellen an das Sync-Service-Login — der `sp_rename`-Staging-Swap ist damit die **einzige**
  Full-Refresh-Strategie, kein zweiter Pfad. Die dokumentierte `DELETE FROM`-Notloesung bleibt nur
  fuer den Fall, dass das Erteilen organisatorisch untersagt wird (kein gleichwertiger Fallback,
  bewusste Konfigurationsentscheidung). Der verbleibende Infra-Check (traegt das Service-Konto das
  erteilte Recht tatsaechlich?) ist als einzige verbleibende `open_questions`-Zeile im Frontmatter
  sowie im Deploy-Abschnitt verankert.
- **N-3 (sp_rename-Swap praezisiert):** eingearbeitet in die Full-Refresh-Strategie (drei Renames
  je Tabelle mit Zwischenname `_Swap`, sechs Renames in einer Transaktion, Hinweis auf nicht
  mitwandernde Constraint-/PK-/Index-Namen als bekannten kosmetischen Drift) und in AK 11
  (entschaerft: „kein Blocking waehrend der Ladezeit, vernachlaessigbare Metadaten-Sperre im
  Swap-Moment" statt „ohne Blocking/Timeout"). Test-Szenario Schritt 5 entsprechend nachgezogen.
- **N-4 (Staging-Migrationspflicht als harte Regel):** eingearbeitet als explizite harte Regel im
  Datenmodell-Abschnitt (Staging-Pendants) und im Migrations-/SQL-Auswirkungen-Abschnitt (Punkt 2,
  analog zur ADR-0004-Kopplungsregel fuer `SQL/AgentJobs/*`). Die zusaetzlich vorgeschlagene
  Fallstrick-Dokumentation in `secondbrain/architektur/fallstricke.md` ist bewusst **nicht** Teil
  dieser Datei (Schreibziel dieser Spec-Runde ist ausschliesslich diese Spec) — **bleibt
  Folgearbeit** fuer die naechste Brain-Pflege-Runde, kein Blocker fuer den Dev-Lauf von Teil 1.
- **EXISTS-Dedup / AK „genau einmal":** bereits vor dieser Runde vollstaendig eingearbeitet (siehe
  Nachbesserung 2026-08-06, B-1) — AK 3 verankert „jede FAListe-Position genau einmal" bereits
  pruefbar; keine weitere Aenderung noetig, hier nur bestaetigt.
- **`open_questions` getrimmt:** Frontmatter enthaelt jetzt nur noch **eine** Zeile — den
  verbleibenden Infra-Check, ob das Sync-Service-Konto das per Migration erteilte `ALTER`-Recht
  tragen kann (siehe N-2). Das fruehere Regex-Feinschliff-Item (Rueckfrage 6) ist mit N-1 final
  entschieden und daher aus `open_questions` entfernt; im Fliesstext (Offene Rueckfragen 6/7) als
  GEKLAERT dokumentiert.
- **Fehlermail-Verdrahtung (Kontrollfrage aus dem Auftrag dieser Runde):** bestaetigt korrekt und
  unveraendert lassbar. Teil 1 laeuft vollstaendig im Windows-Service; `ISyncErrorNotifier` ist dort
  bereits ein etabliertes, injizierbares Muster (`LagerbestandSyncService`) — anders als in Teil 2,
  das im Web laeuft und laut Querschnitts-Regel „Das Web verschickt keine Mails" (Uebersicht,
  2026-08-07) auf `ILogger` + UI-Hinweis umgestellt werden musste. AK 4 und AK 5 fordern die
  Fehlermail bereits explizit fuer den ungueltigen View-Namen **und** den Empty-Guard-Warn; ein
  klarstellender Abgrenzungssatz wurde zusaetzlich im Sync-Service-Abschnitt und im
  In-Scope-Abschnitt ergaenzt, um kuenftige Verwechslung mit der Web-Regel auszuschliessen.

**Verbleibt als Rueckfrage (bewusst, kein Blocker):** siehe `open_questions` im Frontmatter — der
einzelne Infra-Check, ob das Sync-Service-SQL-Login das per Migration erteilte `ALTER`-Recht tragen
kann. Alle anderen Befunde aus der Kritischen Pruefung 2026-08-07 (N-1 bis N-4) sind vollstaendig im
Rumpf umgesetzt. **Diese Spec ist damit aus fachlicher/technischer Sicht dev-bereit** — die
verbleibende Freigabe (Schranke 1, `freigabe_entscheidung`) ist weiterhin Sache des Menschen.

### Nachbesserung 3 (2026-08-07)

Umsetzung des RCSI-Entscheidungswegs des Menschen (Abschnitt „Full-Refresh-Strategie: vor dem
`GRANT ALTER` die einfachere Frage stellen", 2026-08-07, oben VERBATIM erhalten) in den Rumpf der
Spec. Der Staging-/`sp_rename`-Swap aus N-2/N-3 (Kritische Pruefung 2026-08-07) und dessen
„einziger Full-Refresh-Pfad"-Festlegung aus der Nachbesserung 2 sind damit **nicht mehr der
Standard-Scope** dieser Spec, sondern der dokumentierte Fallback-Zweig — die Beschluesse selbst
bleiben als historische Aufzeichnung oben unveraendert stehen, ihre praktische Geltung wird hier
neu eingeordnet.

**Umgestellt:**
- **Full-Refresh-Strategie (Technischer Loesungsentwurf > Sync-Service):** neu strukturiert als
  RCSI-Check zuerst, dann **Primaerweg** (RCSI AN — `DELETE FROM` beide Zieltabellen +
  Neubefuellung in **einer** Transaktion, kein Staging, kein `sp_rename`, kein `GRANT ALTER`) und
  **Fallback** (RCSI AUS — RCSI aktivieren ODER Staging + `sp_rename`-Swap mit `GRANT ALTER`, mit
  der bisherigen Drei-Rename-Mechanik aus N-3 unveraendert als Fallback-Detail). Der Empty-Guard
  (Roh-Zeilenzahl beider Views vor jeder Mutation) bleibt in **beiden** Wegen unveraendert Pflicht.
- **Datenmodell:** Staging-Pendants und die zugehoerige N-4-Kopplungsregel als „nur Fallback-Weg"
  gekennzeichnet — im Primaerweg existieren diese Tabellen nicht, die Kopplungspflicht entfaellt.
- **Repository-Schicht:** Hinweis auf die `_Staging`-Tabellen als „nur falls Fallback-Weg" ergaenzt.
- **Migrations-/SQL-Auswirkungen:** in Primaerweg (nur zwei Zieltabellen, kein Grant, keine
  Migrations-Kopplung) und Fallback (zusaetzlich zwei Staging-Tabellen + `GRANT ALTER`) aufgeteilt;
  `SQL/00_FreshInstall.sql`-Punkt entsprechend konditioniert.
- **`affected_code`/Frontmatter:** `SQL/87_AddFaHierarchy.sql`-Eintrag beschreibt jetzt beide
  moeglichen Inhalte (Primaerweg vs. Fallback) statt unbedingtem Staging+Grant.
- **Akzeptanzkriterien:** AK 9 als „gilt fuer beide Wege" praezisiert; AK 11 und AK 12 explizit als
  „nur Fallback-Weg" gekennzeichnet; **neues AK 13** ergaenzt fuer den Primaerweg (`DELETE`+
  Neubefuellung beider Tabellen atomar in einer Transaktion; bei RCSI AN keine Leser-Blockade,
  verifiziert per RCSI-Abfrage und manuellem Test).
- **Test-Szenarien:** Schritt 0 von reinem Berechtigungs-Check auf RCSI-Check umgestellt (Ergebnis
  verzweigt in Primaer-/Fallback-Pruefungen); Schritt 5 in „5 — Primaerweg" (RCSI AN, kein Blocking
  ueber die gesamte Ladezeit) und „5b — Fallback" (Swap-Moment mit vernachlaessigbarer
  Metadaten-Sperre, unveraendert aus N-3) aufgeteilt.
- **Deploy-Abschnitt:** Migrations-Beschreibung und Reihenfolge auf „RCSI-Check zuerst, dann
  Primaer- oder Fallback-Migrationsvariante" umgestellt; der Rechte-Check ist jetzt explizit nur im
  Fallback-Zweig verortet.
- **Offene Rueckfragen, Punkt 7:** Ergaenzungshinweis angefuegt, dass die dortige N-2-Antwort
  („Recht erteilen, nicht umgehen") nur noch fuer den Fallback-Zweig gilt.
- **`open_questions` (Frontmatter):** die bisherige Infra-Check-Zeile („traegt das Service-Konto das
  per Migration erteilte `ALTER`-Recht?") durch den vorgelagerten RCSI-Check ersetzt — dessen
  Ergebnis entscheidet ueberhaupt erst, ob eine `ALTER`-Frage noch relevant wird. Weiterhin als
  DBA-Check, nicht als Design-Blocker gefuehrt: alle fachlichen/technischen Fragen dieser
  Spec-Runde sind entschieden.

**Bewusst NICHT veraendert:** alle „## Kritische Pruefung"-, „## Antworten"- und „### Nachbesserung"-
Bloecke sowie der RCSI-Entscheidungsweg-Abschnitt des Menschen selbst stehen unveraendert (VERBATIM)
im Dokument — sie sind das Protokoll, wie diese Entscheidung entstanden ist, nicht der aktuell
gueltige Bauplan. Der aktuell gueltige Bauplan ist der oben ueberarbeitete Rumpf (Ziel/Nutzen bis
Deploy) in Kombination mit dieser Nachbesserung 3.

**Status:** `status` bleibt `Entwurf`. Diese Spec ist weiterhin fachlich/technisch dev-bereit — die
Freigabe (Schranke 1, `freigabe_entscheidung`) bleibt Sache des Menschen. Keine neue offene
fachliche Frage durch diese Runde; der verbleibende RCSI-Check ist ein einmaliger, risikoarmer
Infra-Schritt vor dem Dev-Lauf/Deploy, kein Design-Blocker.
