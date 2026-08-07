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
  - IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs (3 neue InlineData-Eintraege, Drift-Guard)
  - IDEALAKEWMSService/Services/FaHierarchySyncService.cs (neu, injiziert ISyncErrorNotifier)
  - IDEALAKEWMSService/Services/FaHierarchySql.cs (neu, Whitelist-Regex + QUOTENAME + SQL-Aufbau)
  - IDEALAKEWMSService/Services/SyncLogServices.cs
  - IDEALAKEWMSService/Workers/SyncWorker.cs (neuer Sync-Block, RunResilientAsync)
  - SQL/87_AddFaHierarchy.sql (neu, naechste freie Nummer — vor Dev-Lauf pruefen; Platzhalter, siehe H-1)
  - SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql (neu, DDL-Dokumentation)
  - SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAInfos.sql (neu, DDL-Dokumentation)
  - SQL/00_FreshInstall.sql
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Exaktes ASCII-Whitelist-Regex-Pattern je Namensteil (ergaenzend zur jetzt verbindlichen QUOTENAME-Absicherung) im Dev-Lauf final festlegen — Grundstruktur ([Schema].[Name], ASCII-only, kein \\w) ist bereits in dieser Spec vorgegeben, nur Feinschliff offen — kein Blocker"
  - "Full-Refresh-Mechanik (Staging-Tabelle + sp_rename-Swap, empfohlen) setzt ALTER-Recht der Sync-Service-SQL-Login voraus — vor dem Dev-Lauf am Zielsystem pruefen; Fallback (kurze Einzel-Transaktion je Tabelle) ist in dieser Spec bereits als Alternative vorgegeben — kein Blocker, aber vor Implementierung zu bestaetigen"
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
  lokalen Tabellen per Full-Refresh aktualisiert (Staging-Tabellen + kurzer Swap, siehe Technischer
  Loesungsentwurf — **kein** langlaufendes Delete-All+Insert auf den Zieltabellen).
- Datenverfuegbarkeits-Regel: Eine `FAListe`-Zeile wird nur importiert, wenn zum `HauptFA`
  mindestens ein `FAInfos`-Eintrag existiert — als reine **Existenzpruefung** (`WHERE EXISTS`/
  `INNER JOIN (SELECT DISTINCT HauptFA FROM FAInfos)`), **nicht** als Zeilen-Join. Ein echter
  Zeilen-Join auf `FAInfos` wuerde jede Position pro `FAInfos`-Zeile desselben `HauptFA`
  vervielfachen (Kombinationsgeraete, mehrere `[Montage-Abteilung]`-Zeilen) — siehe Anforderung 2.
- Repository + Cache-Decorator (ADR 0001) fuer den Web-Lesezugriff auf die importierten Tabellen.
- View-DDL-Dokumentation unter `SQL/sage-views/` (keine WMS-Migration, reine Doku der Fremd-DB).
- Aktivitaets-Protokoll-Pflicht (ADR 0010) fuer den neuen Sync, inklusive expliziter Fehlermail
  ueber `ISyncErrorNotifier` bei ungueltigem View-Namen und bei leerem View-Read (siehe Technischer
  Loesungsentwurf, S-4).
- Feature-/Sync-Toggle ueber `ServiceSettingDefinitions` (ADR 0008) — **inklusive** Eintragung der
  drei neuen Keys als `[InlineData]` im Drift-Guard-Test (siehe Technischer Loesungsentwurf, S-3).

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
8. **View-Namen konfigurierbar — Whitelist UND `QUOTENAME` (ueberarbeitet, siehe Kritische Pruefung
   S-2).** `FAListe`- und `FAInfos`-View-Name sind je Standort unterschiedlich benennbar
   (Testsystem heisst `IDEAL_TEST_2026_05_03`, Produktivname noch offen — siehe offene Rueckfrage)
   und werden **nicht** hartkodiert, sondern aus `ServiceSettings` gelesen. Die Absicherung ist
   **zweistufig**, nicht nur eine Regex:
   1. Whitelist-Regex zerlegt den konfigurierten Namen in `[Schema].[Name]`-Teile und prueft jeden
      Teil **ASCII-explizit** gegen `^[A-Za-z0-9_]+$` — bewusst **nicht** `\w` (unter
      Unicode-Regex matcht `\w` Homoglyphen, genau der relevante Angriff bei einem extern
      konfigurierbaren Objektnamen). Verworfen werden zusaetzlich: Whitespace, `;`, `--`, `/*`,
      unausgeglichene `[`/`]`.
   2. Die geprueften Teile werden danach per **`QUOTENAME()`** wieder zu `[Schema].[Name]`
      zusammengesetzt, bevor sie in den SQL-Text eingesetzt werden — Objektnamen lassen sich in
      T-SQL nicht parametrisieren, `QUOTENAME` ist die strukturelle Abwehr, falls die Regex
      spaeter versehentlich gelockert wird. Whitelist **und** `QUOTENAME`, nicht entweder/oder.

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

**Staging-Pendants (S-1, technisch, kein eigenes Domaenen-Modell):** `FaHierarchyNode_Staging` /
`FaHierarchyOrderInfo_Staging` — schema-identisch zu den Zieltabellen (inkl. `SyncedAt`), aber ohne
eigene Repository-Anbindung. Sie sind reine sync-interne Zwischenablagen fuer die
Full-Refresh-Strategie (siehe Sync-Service unten) und werden in derselben Migration mit angelegt.

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
- Die `_Staging`-Tabellen haben **keine** Repository-Anbindung — sie sind reines Sync-internes
  Detail, nicht Teil des Lesemodells.

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
  ueberarbeitet, siehe Anforderung 8 und Kritische Pruefung S-2): zerlegt `[Schema].[Name]` in
  Teile, prueft jeden Teil ASCII-explizit (`^[A-Za-z0-9_]+$`, **nicht** `\w`), lehnt Whitespace,
  `;`, `--`, `/*`, unausgeglichene `[`/`]` und alles jenseits ASCII ab, setzt die geprueften Teile
  danach per `QUOTENAME()` zusammen. Bei Verstoss: Lauf bricht mit `FinishFailedAsync` ab, **kein**
  SQL wird ausgefuehrt, **zusaetzlich** `ISyncErrorNotifier.NotifyAsync(...)` (siehe Fehlermail
  unten). Exaktes Regex-Pattern im Detail (Feinschliff) ist offene Rueckfrage — die Grundstruktur
  ist hier bereits verbindlich vorgegeben.
- **Full-Refresh-Strategie (ueberarbeitet, siehe Kritische Pruefung S-1).** Kein Praezedenzfall im
  Code deckt „Delete-All + Bulk-Insert in einer langen Transaktion": `CachedBomHeader` ist
  Hash-inkrementell (Upsert), enaio ist MERGE-Full-Sync. Fuer eine potenziell zehntausende Zeilen
  grosse Stuecklisten-Tabelle wuerde ein klassisches Delete-All+Insert waehrend der gesamten
  Ladezeit sperren und gleichzeitige Web-Reads blockieren/eskalieren lassen. Stattdessen:
  1. **Vor jeder Mutation:** Roh-Zeilenzahl **beider** Views ungefiltert lesen (`SELECT COUNT(*)`
     auf `FaHierarchyListeViewName` bzw. `FaHierarchyInfosViewName`, **ohne** Existenzpruefung/Join).
     Guard: Ist eine der beiden Rohzahlen `0`, **kein** Replace — Warn-Log + `NotifyAsync` (siehe
     unten), bestehende Zieltabellen bleiben unangetastet. Dieser Check laeuft **vor** dem Aufbau
     der Zielzeilen, damit die Existenzpruefung aus Anforderung 2 einen leeren `FAInfos`-Read nicht
     als „0 Struktur-Zeilen" maskiert (sonst verwechselt der Guard Ausfall mit echtem Leerstand).
  2. Erst danach: beide Views vollstaendig lesen, Zielzeilen im Speicher aufbauen (Existenzpruefung
     gemaess Anforderung 2).
  3. **Schreiben ueber Staging-Tabellen** (`FaHierarchyNode_Staging` / `FaHierarchyOrderInfo_Staging`,
     siehe Datenmodell): beide werden bei jedem Lauf per `TRUNCATE` + Bulk-Insert frisch befuellt
     (unkritisch, da nicht die Leseziele des Web). Danach werden **beide** Zieltabellen in **einer**
     kurzen Transaktion per `sp_rename`-Swap gegen ihre Staging-Pendants getauscht — eine reine
     Metadaten-Operation ohne Sperre auf Zeilenebene fuer die Ladezeit. Web-Reads sehen bis zum Swap
     den alten Inhalt, danach sofort den neuen; kein Blocking waehrend des Ladens.
     **Voraussetzung:** die Sync-Service-SQL-Login braucht `ALTER`-Recht fuer `sp_rename` — am
     Zielsystem vor dem Dev-Lauf zu pruefen (siehe offene Rueckfrage).
  4. **Fallback**, falls dieses Recht nicht vergeben werden kann: `TRUNCATE` + Bulk-Insert **je
     Zieltabelle einzeln in einer eigenen kurzen Transaktion** (kein zeilenweises Delete), auf einer
     DB mit RCSI/Snapshot-Isolation bevorzugt, damit gleichzeitige Web-Reads den alten Stand sehen
     statt zu blockieren.
  5. Beide Tabellen werden in **jedem Fall als ein logischer Schritt** ersetzt (nicht zeitlich
     versetzt) — nie steht `FaHierarchyNode` (neu) neben einem alten `FaHierarchyOrderInfo` oder
     umgekehrt.
  6. Kein inkrementelles Delta, kein MERGE-Aufwand — die Views selbst liefern bereits den
     vollstaendigen Soll-Stand.
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
- Protokoll (ADR 0010): `ISyncLogger` als letzter Ctor-Parameter, Counts `neu`/`geloescht`
  (deutschsprachig, hier praktisch „komplette Ersetzung" abgebildet als zwei Zahlen), eigener Lauf
  getrennt vom `ProductionOrder`-Import.
- Sync-Block in `SyncWorker` ueber `RunResilientAsync` gekapselt (ein Fehler killt die anderen
  Sync-Bloecke nicht; deckt zusaetzlich unerwartete Exceptions ab, die die beiden expliziten
  `NotifyAsync`-Pfade oben nicht abdecken).

### Migrations-/SQL-Auswirkungen

1. Model → `dotnet ef migrations add AddFaHierarchy` (aktueller Timestamp!) → idempotentes
   `SQL/87_AddFaHierarchy.sql` (Platzhalter-Nummer, siehe H-1) mit `OBJECT_ID`-Guard, Tabellen-DDL
   in eigenem Batch (`GO`) fuer **alle vier** Tabellen (`FaHierarchyNode`, `FaHierarchyOrderInfo`
   und ihre `_Staging`-Pendants, siehe S-1), `__EFMigrationsHistory`-Insert in separatem Batch.
2. `SQL/00_FreshInstall.sql` an **beiden** Stellen nachziehen: Schema-Objekte (**alle vier** neuen
   Tabellen) **und** `MigrationId` im History-Insert-Block.
3. **Additive Migration** — kein Datenverlust, kein Backup-Hinweis noetig (neue, leere Tabellen).
4. `SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql` +
   `..._FAInfos.sql`: View-DDL-Dokumentation der Fremd-DB — **keine** WMS-Migration, nur
   Versionskontrolle der Sage-Objekte (wie in der Notiz vereinbart).
5. **Migrationsnummer beim Dev-Start final festlegen.** `SQL/82`/`83` sind durch v1.28.0
   (Sage-Lagerbuchungen) belegt; die WmsBugs-Batches belegen mindestens `84`-`87`
   (`87_AddUserDefaultFilterBomDescription1.sql` im noch nicht gemergten Worktree
   `2026-08-05-wms-bugs-improvements-teil-1-2-3`). Nach Merge beider Batches ist die naechste freie
   Nummer **voraussichtlich `88`** — die `87`-Referenz in dieser Spec (auch in `affected_code`) ist
   ein Platzhalter und unmittelbar vor dem Dev-Lauf gegen den dann gemergten Stand zu pruefen.

### Audit-Feld-Auswirkungen

`FaHierarchyNode` und `FaHierarchyOrderInfo` (und ihre `_Staging`-Pendants) sind **keine**
`AuditableEntity` — analog zu `CachedBomHeader`/`CachedBomItem` (dokumentierte Ausnahme: reine, vom
Sync-Service befuellte Cache-Tabellen ohne manuelle Bearbeitung durch Anwender). Nachvollziehbarkeit
kommt stattdessen aus dem Aktivitaets-Protokoll (`SyncLog`, ADR 0010) des
`FaHierarchySyncService`-Laufs, nicht aus `ModifiedBy`/`ModifiedAt`-Feldern auf den Zeilen selbst.
Kein bestehendes Audit-Feld ist betroffen, da `ProductionOrders` unangetastet bleibt.

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
4. Ein ungueltiger View-Name (z. B. mit Leerzeichen, Semikolon, Kommentarzeichen oder einem
   Homoglyphen ausserhalb ASCII) fuehrt zu einem fehlgeschlagenen, protokollierten Lauf
   (`FinishFailedAsync`) **ohne** SQL-Ausfuehrung gegen die Sage-DB **und** zu einer Fehlermail
   (`ISyncErrorNotifier.NotifyAsync`); ein gueltiger Name wird vor dem SQL-Aufbau zusaetzlich per
   `QUOTENAME()` abgesichert (verifiziert am Whitelist-Unit-Test).
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
   die Injection-Abwehr (Regex **und** `QUOTENAME`-Zusammenbau) automatisiert geprueft ist.
9. **(neu, S-1)** Nach einem erfolgreichen Lauf sind entweder **beide** Zieltabellen aktualisiert
   oder **beide** unveraendert — kein Zwischenzustand, in dem `FaHierarchyNode` neu und
   `FaHierarchyOrderInfo` alt ist (oder umgekehrt); verifiziert per `SyncedAt`-Zeitstempel-Vergleich
   beider Tabellen nach dem Lauf.
10. **(neu, S-3)** `Sync:HierarchicalFaEnabled`, `Sync:FaHierarchyListeViewName` und
    `Sync:FaHierarchyInfosViewName` sind sowohl in `ServiceSettingDefinitions.All` **als auch** als
    zusaetzliche `[InlineData]`-Eintraege in
    `ServiceSettingDefinitionsTests.All_ContainsDocumentedServiceReadKey` vorhanden — reine
    Katalog-Eintragung ohne `InlineData` gilt **nicht** als erfuellt (Test bliebe sonst gruen ohne
    jede Guard-Wirkung).
11. **(neu, S-1)** Waehrend ein Full-Refresh laeuft (Staging-Phase, vor dem Swap bzw. der kurzen
    Ersetzungs-Transaktion), liefert ein gleichzeitiger Web-Read ueber
    `FaHierarchyNodeRepository`/`CachedFaHierarchyNodeRepository` weiterhin den **alten**
    Tabelleninhalt, ohne Blocking/Timeout (manuell am Testsystem waehrend eines laufenden
    Sync-Laufs verifiziert).

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
- **Schritt 4 — Kombinationsgeraet, keine Verdopplung (ueberarbeitet, B-1).** Falls am Testsystem
  vorhanden, eine `HauptFA` mit **zwei** `MontageAbteilung`-Werten in `FaHierarchyOrderInfo`
  identifizieren. Erwartung: **Positionszahl in `FaHierarchyNode` fuer diesen `HauptFA` ist
  identisch zur Zeilenzahl der Roh-View `FAListe`** fuer denselben `HauptFA` — **keine**
  Verdopplung, obwohl zwei `FAInfos`-Zeilen existieren. `FaHierarchyOrderInfo` enthaelt dagegen
  beide Montage-Abteilungs-Zeilen. Die materialisierungsseitige Behandlung gehoert zu Teil 7.
- **Schritt 5 — Full-Refresh ohne Blocking (neu, S-1):** Waehrend ein Sync-Lauf laeuft
  (Staging-Aufbau vor dem Swap), einen Web-Read gegen `FaHierarchyNodeRepository` ausloesen —
  erwartet: sofortige Antwort mit dem **alten** Datenstand, kein Timeout/Blocking.
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
  legt vier Tabellen an: zwei Ziel- + zwei Staging-Tabellen, siehe S-1).
- **Reihenfolge:** DB-Migration vor Service-Neustart; Web kann parallel deployt werden, da Teil 1
  keine erreichbare Route hinzufuegt. Sync-Toggle bleibt nach dem Deploy **default aus** — muss am
  Zielsystem bewusst aktiviert werden (analog zur ADR-0008-Regel „jeder gewuenschte Sync muss
  einmalig aktiviert werden"). Vor Aktivierung: `ALTER`-Recht der Sync-Service-SQL-Login fuer
  `sp_rename` am Zielsystem pruefen (siehe offene Rueckfrage) — sonst greift der in dieser Spec
  vorgegebene Fallback (kurze Einzel-Transaktionen je Tabelle).
- **Publish-Befehle:**
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```
  (provisorisch — vom Dev-Lauf gegen den tatsaechlichen Diff zu bestaetigen).

## Offene Rueckfragen

Die Schranke-1-Antworten (unten) loesen die urspruenglichen Rueckfragen 1-5; die Kritische Pruefung
vom 2026-08-06 hat die Vorgaben in den Spec-Text eingearbeitet (siehe Nachbesserung am Ende der
Datei). Zwei technische Detailfragen bleiben — beide nicht blockierend, beide mit Empfehlung:

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
6. **OFFEN (Empfehlung: kein Blocker).** Exaktes ASCII-Whitelist-Regex-Pattern je Namensteil im
   Dev-Lauf final festlegen (Grundstruktur — `[Schema].[Name]`, ASCII-only, kein `\w` — ist bereits
   in dieser Spec vorgegeben, siehe Anforderung 8/S-2).
7. **OFFEN (Empfehlung: Staging + `sp_rename`, siehe Full-Refresh-Strategie).** Setzt `ALTER`-Recht
   der Sync-Service-SQL-Login voraus — vor dem Dev-Lauf am Zielsystem pruefen; der Fallback (kurze
   Einzel-Transaktion je Tabelle) ist bereits als Alternative in dieser Spec vorgegeben, falls das
   Recht nicht vergeben werden kann.

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
