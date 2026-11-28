---
type: spec
title: BDE Arbeitsgang-Altbestand Teileliste bereinigen (Wiederholung Einmal-Skript v1.99.32 als Manifest-Eintrag #2, Testbuchungen IDEAL-Test)
slug: 2026-11-28-bde-ag-altbestand-teileliste-bereinigung-teil-1-spec
status: Entwurf
created: 2026-11-28
updated: 2026-11-28
source_backlog: "[[2026-11-28-bde-fa-sicht-arbeitsgaenge-falsch-zugeordnet]]"
bug: "[[2026-11-28-bde-fa-sicht-arbeitsgaenge-falsch-zugeordnet-bug]]"
ursprungs_spec: "[[2026-11-28-bde-fa-sicht-arbeitsgaenge-falsch-zugeordnet-spec]]"
typ: bugfix
art: bugfix
task: ""
jira: WMS-10
worktree: ""
branch: ""
standort: IDEAL
affected_code:
  - "[[2026-11-28-bde-fa-sicht-arbeitsgaenge-falsch-zugeordnet-bug]]"
  - scripts/deploy/nachlauf.psd1 (zwei neue Eintraege nach :338-342)
  - SQL/Einmalig/2026-11-28_v<neu>_DeleteTestBookingsTeilelisteKandidaten.sql (neu, nur IDEAL-Test)
  - SQL/Einmalig/2026-11-17_v1.99.32_RemoveParentWorkOperationsFromLeafTokensTeileliste.sql (unveraendert wiederverwendet)
  - IdealAkeWms/AppVersion.cs, IDEALAKEWMSService/AppVersion.cs, Views/Help/Changelog.cshtml (Versionssprung des Pakets)
  - docs/TESTSZENARIEN.md (neues TS, voraussichtlich TS-214), docs/betrieb (Update-Nachlauf, falls dort Eintraege gelistet sind)
open_questions: []
epic: false
etappen: []
deploy:
  web: false
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

> [!info] Herkunft
> Ausgeschnitten aus [[2026-11-28-bde-fa-sicht-arbeitsgaenge-falsch-zugeordnet-spec]] (Anforderung **FA-1 Datenbereinigung**), damit die
> Bereinigung ohne die Fachbereichsantwort zur Anzeige (Variante A/B/C, dortige Rückfrage 1) gebaut werden kann. Bug-Record:
> [[2026-11-28-bde-fa-sicht-arbeitsgaenge-falsch-zugeordnet-bug]]. Die Anzeige (FA-2) bleibt in der Ursprungs-Spec.

## Ziel / Nutzen (das Warum)
An IDEAL stehen an Baugruppen-Sub-FAs Arbeitsgänge (KA, ST, EG, SM, IF), die nur von Blechteilen stammen und nach der Teileliste-Regel V1
(ADR 0030) nicht existieren dürften. Sie erzeugen in der BDE-FA-Sicht Zeilen, die zu einem zweiten Meldeweg neben Teile Einzeln/Summiert
führen. Das vorhandene Einmal-Skript entfernt genau diesen Bestand, lief aber auf IDEAL-Test zu früh (nichts gefunden, Marker „Entfaellt“)
und wird seitdem nicht mehr angeboten. Ziel: Das Skript läuft im nächsten Update-Paket noch einmal regulär (Trockenlauf → SCHARF → zweiter
Trockenlauf). Die 8 gebuchten Kandidaten auf IDEAL-Test sind nur Testbuchungen (Gerald 2026-11-28) und werden vorher entfernt, damit sie
mit gelöscht werden.

## Umfang (In-Scope / Out-of-Scope)
**In-Scope**
- Manifest-Eintrag `…#2`: dasselbe Einmal-Skript v1.99.32, unverändert, unter neuer Id.
- Neues kleines Einmal-Skript nur für IDEAL-Test: löscht die BDE-Buchungen der 8 Testkandidaten, damit `#2` diese Arbeitsgänge mit löscht.
- Versionssprung, Anwender-Changelog, neues Testszenario, Brain-Nachzug.

**Out-of-Scope**
- Anzeige AG-Status der FA-Sicht (FA-2, Ursprungs-Spec, wartet auf Fachbereich).
- Regel der Arbeitsgang-Erkennung: unverändert (Nur-hinzufügen, `IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs:18`).
- Änderungen am Update-Skript `Update-IdealAkeWms.ps1` und an `NachlaufManifestDriftTests`: nicht nötig (Belege unten).
- IDEAL-Prod-Buchungen: Das Wegräumen gilt nur für die Testbuchungen auf IDEAL-Test.
- AKE: nicht betroffen (Einträge `Firma='IDEAL'`; `FaHierarchyNodes` sind nur an IDEAL befüllt, Skriptkopf `:5`).

## Fachliche Anforderungen

### Befund (aus der Ursprungs-Spec, Abschnitt „Befund Prüfabfragen“)
Details stehen in [[2026-11-28-bde-fa-sicht-arbeitsgaenge-falsch-zugeordnet-spec#Befund Prüfabfragen]]. Gemessen 2026-11-28 auf
`IDEALSQL2\SAGE100TEST` / `IDEAL_AKE_WMS_TEST` (WMS-Test IDEAL, gespeist aus Sage-Prod), nur SELECT und Trockenlauf:
- P-4: Das Einmal-Skript lief genau einmal: `Entfaellt`, Hits 0, Paket 1.99.34, **2026-11-18 08:29:49**.
- P-2: Die IDEAL-FAs wurden **2026-11-18 08:33:04/05** materialisiert, die Arbeitsgänge legte `WorkOperationStructureDetection` an.
- P-3: Die Option BDE-Teileliste wurde erst **danach** gesetzt bzw. geändert (ST/SL/SM/PU 2026-11-18 09:39, EG/KA 2026-11-26 09:40).
- Die Erkennung fügt nur hinzu und löscht nie, deshalb blieb der Bestand stehen.
- Trockenlauf heute: **`TREFFER: 637`** (EG 206, IF 7, KA 211, SM 10, ST 211). Auf der Meldeliste stehen **8** gebuchte Kandidaten, die das
  Skript stehen lässt: EG 522, 534 (HauptFA 1059463), EG 810, 816, 820 (1060414), KA 1093, 1099, 1103 (1069617, Sub-FA 1069718–1069720).
  Die Zahlen sind `WorkOperationId`s (Spalte der Meldeliste, Skript `:82`).

### Entscheidungen (fest)
- Gerald 2026-11-28 (Ursprungs-Spec, Freigabe-Antwort 3): Die Screenshot-DB ist IDEAL WMS-Test.
- Gerald 2026-11-28 (Ursprungs-Spec, Freigabe-Antwort 4): Die Bereinigung läuft über einen **neuen Manifest-Eintrag #2** im nächsten Paket,
  kein manueller Sofortlauf.
- Gerald 2026-11-28 (Chat, Nachtrag an die Koordination): Die 8 gebuchten Kandidaten sind **nur Testbuchungen** und dürfen mit weg. Damit ist
  die Fachbereichsfrage 2 der Ursprungs-Spec erledigt.

### Anforderungen
- **R-1 Wiederholung über neue Id.** `nachlauf.psd1` bekommt einen zweiten Eintrag auf **dieselbe** Datei mit der Id
  `Einmalig/2026-11-17_v1.99.32_RemoveParentWorkOperationsFromLeafTokensTeileliste#2`. Das Skript selbst bleibt unverändert.
- **R-2 Testbuchungen vorher entfernen (nur IDEAL-Test).** Ein neues Einmal-Skript löscht die BDE-Buchungen der 8 genannten Arbeitsgänge. Im
  Manifest steht es **vor** `#2`, damit `#2` diese Arbeitsgänge im selben Nachlauf als ungebucht findet und löscht.
- **R-3 Ablauf je Eintrag wie ADR 0023:** Trockenlauf automatisch, bei TREFFER > 0 Backup aus diesem Lauf, SCHARF, zweiter Trockenlauf
  TREFFER 0, Marker in `DeployScriptRuns`.
- **R-4 Bestandsverhalten unverändert:** Erkennung, BDE-Buchen, Stornieren und das Update-Skript bleiben, wie sie sind.

### Warum Stornieren nicht reicht (belegt)
Geralds Vorschlag war: Der BDE-Admin storniert die Testbuchungen vor SCHARF, dann löscht das Skript die Arbeitsgänge mit. Das trägt nicht:
- Stornieren ist eine weiche Markierung. `BdeBookingsController.Cancel` setzt nur `IsCancelled = true` und den Grund
  (`IdealAkeWms/Controllers/BdeBookingsController.cs:361-373`). Die Rücknahme einer Teilmeldung storniert die zugehörige AG-Buchung genauso
  weich (`IdealAkeWms/Services/BdeTeilelisteService.cs:839-848`). Die Zeile in `BdeBookings` bleibt in beiden Fällen bestehen.
- Das Skript wertet jede vorhandene Buchungszeile als „gebucht“, auch eine stornierte: Die Bedingung lautet
  `wo.IsReported = 1 OR EXISTS (SELECT 1 FROM dbo.BdeBookings b WHERE b.WorkOperationId = wo.Id)`, ohne Filter auf `IsCancelled`
  (`SQL/Einmalig/2026-11-17_v1.99.32_RemoveParentWorkOperationsFromLeafTokensTeileliste.sql:63`, Doppelsicherung im DELETE `:99-100`,
  Begründung im Kopf `:17` „auch stornierte, FK ist Restrict“).
- Der FK lässt das Löschen auch technisch nicht zu: `BdeBookings.WorkOperationId` → `WorkOperations` mit `DeleteBehavior.Restrict`
  (`IdealAkeWms/Data/ApplicationDbContext.cs:1543`, `SQL/00_FreshInstall.sql:2573`).
- Nach einem Storno stünden die 8 also weiter auf der Meldeliste. Ohne Skriptänderung ist das Ziel nur erreichbar, wenn die
  Buchungszeilen **gelöscht** werden. Eine Ausnahme für stornierte Buchungen in das v1.99.32-Skript einzubauen, würde dagegen dessen
  bewusste Regel (`:17`) auch für IDEAL-Prod ändern und die SHA der Datei. Deshalb die kleinste Lösung R-2: ein eigenes Skript, das nur auf
  IDEAL-Test läuft und nur diese 8 Arbeitsgänge betrifft. Ein Storno vorher ist **nicht** nötig.

### Weitere Verweise auf Buchungen (belegt, bestimmt den Umfang von R-2)
- Auf `WorkOperations` verweist nur `BdeBookings` (einziger FK in `SQL/00_FreshInstall.sql`, `:2573`; einzige `WorkOperationId` in
  `IdealAkeWms/Models`: `BdeBooking.cs:19`).
- Auf `BdeBookings` verweisen drei FKs (`SQL/00_FreshInstall.sql`):
  - `BdeBookingQuantities`: `ON DELETE CASCADE` (`:2646`), die Mengen gehen mit.
  - `BdeBookings.ParentBookingId`: Selbstverweis ohne Cascade (`:2575`, Restrict `ApplicationDbContext.cs:1545`).
  - `BdeTeilMeldungen.BdeBookingId`: ohne Cascade (`:2685`). Das ist die AG-Fertigmeldung einer Baugruppen-Teilmeldung
    (`Models/BdeTeilMeldung.cs:46-48`).
- `IsReported` setzt nur die Rückmeldung in der Verfolgung (`TrackingController.cs:255`), zurücknehmen geht dort (`:284`). Ist einer der 8
  zurückgemeldet, löscht `#2` ihn trotz gelöschter Buchung nicht (`:99`). Das zeigt P-6.

## Technischer Loesungsentwurf

### Manifest-Mechanik (belegt): zwei Einträge auf dieselbe Datei sind erlaubt
- **Der Marker hängt an der Id, nicht an der Datei.** `Get-Marker` sucht `WHERE ManifestId = $m.Id`
  (`scripts/deploy/Update-IdealAkeWms.ps1:328-333`, Abfrage `:330`). `Set-MarkerBefehl` schreibt `ManifestId = $m.Id` und die Datei nur als
  Information in `FileName` (`:334-340`, `:337`). Eindeutig ist nur `ManifestId` (`SQL/142_AddDeployScriptRuns.sql:35-36`
  `UX_DeployScriptRuns_ManifestId`), `FileName` nicht. Der vorhandene Marker `Entfaellt` der ersten Id blockiert `#2` also nicht.
- **Einmalig ist immer fällig.** `Test-Faellig` liefert für `Typ='Einmalig'` unabhängig von der Version true (`:526-527`). Offen ist ein
  Eintrag, solange er weder Marker noch Spur > 0 hat (`Get-OffeneEintraege` `:528-531`, `Invoke-Eintrag` `:537-547`). Das Feld `Version` ist
  für `#2` damit nur Doku.
- **Reihenfolge = Manifest-Reihenfolge.** Die Nachher-Liste filtert das Manifest der Reihe nach und führt Nachher-Einträge in dieser Reihenfolge
  aus (`:907-914`). Steht R-2 vor `#2`, läuft es zuerst.
- **Nur IDEAL-Test:** `System=@('Test')` beschränkt einen Eintrag auf diese Systeme der Firma (`Test-PasstFirma` `:263-266`, Feld im Kopf
  `nachlauf.psd1:5`, Präzedenz `:365`, `:370`).
- **Fluss A** (`:564-594`): Trockenlauf mit der unveränderten Datei. TREFFER 0 → „entfaellt“, Marker `Entfaellt` nach Ja (`:571-574`). Sonst
  Backup aus diesem Lauf Pflicht (`:577`), dann SCHARF (`:578-579`). Das Literal muss genau einmal vorkommen (`:582-583`), der Scharflauf
  ersetzt es nur im Speicher und schreibt den Marker in derselben Transaktion (`:585`). Danach folgt der zweite Trockenlauf (`:588-593`).
- **Der Kopf des Manifests sieht genau diesen Weg vor:** „soll ein Skript bewusst erneut laufen, bekommt es eine neue Id (z. B. SQL/110#2)“
  (`nachlauf.psd1:19-20`). Ein echter Präzedenzfall mit `#n` existiert noch nicht. Das `#` ist unkritisch: Es steht in einem
  Single-Quote-Literal. Der Log-Dateiname ersetzt Sonderzeichen durch `_` (`Update-IdealAkeWms.ps1:558`). `ManifestId` hat `nvarchar(200)`
  (`SQL/142:16`), die Id hat rund 85 Zeichen.
- **Drift-Tests brauchen keine Änderung** (`IdealAkeWms.Tests/Deploy/NachlaufManifestDriftTests.cs`):
  - `Fehlend` sammelt die `Datei`-Werte in einem HashSet und prüft nur, ob jede Datei referenziert ist. Doppelte Referenzen sind erlaubt
    (`:57`, `:60-63`).
  - `Manifest_IdsEindeutig_FelderGueltig` verlangt eindeutige **Ids** (`:297`). `#2` ist eine neue Id.
  - `DatenFehler` (`:132-153`) prüft je Eintrag dieselbe Datei erneut. Sie hat das Literal genau einmal (`:35`), die Zeile `TREFFER: ` (`:89`),
    kein ROLLBACK und keine sqlcmd-Direktive, ist also für beide Einträge grün.
  - Die neue R-2-Datei muss im Manifest stehen (`:60-63`) und dieselben Regeln erfüllen.
  - ponytail: kein zusätzlicher Test. Der Drift-Wächter erzwingt schon, dass die neue Datei referenziert ist und die Regeln einhält.
- **Folgerung (ponytail):** Für R-1 ist kein neues SQL nötig, keine Kopie der Datei und keine Änderung an Update-Skript oder Test. Es ist ein
  Manifest-Block.

### R-1: Manifest-Eintrag `#2`
Direkt nach dem Eintrag `:338-342` einfügen, nach dem R-2-Eintrag:
```powershell
@{ Id='Einmalig/2026-11-17_v1.99.32_RemoveParentWorkOperationsFromLeafTokensTeileliste#2'; Version='<neu>'; Typ='Einmalig'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='Trockenlauf'
   Datei='Einmalig\2026-11-17_v1.99.32_RemoveParentWorkOperationsFromLeafTokensTeileliste.sql'; Braucht=@('SQL/154')
   Voraussetzung='Dienst und Web >= 1.99.32 laufen, Option BDE-Teileliste an den Start-Werkbaenken gesetzt, DB-Backup'
   Text='Wiederholung (WMS-10): der erste Lauf fand an IDEAL-Test nichts, weil die FAs erst danach materialisiert und die Option erst danach gesetzt wurde. Trockenlauf automatisch (Zaehlung je Kuerzel und Meldeliste ins Protokoll). TREFFER 0 -> entfaellt. Sonst Zaehlung freigeben, SCHARF eintippen (Backup aus diesem Lauf Pflicht); zweiter Trockenlauf muss 0 liefern. Meldeliste: an IDEAL-Test nach dem Testbuchungs-Eintrag leer; an IDEAL-Prod gebuchte Arbeitsgaenge bleiben stehen - Liste an Gerald.'
   Quelle='<brain-changelog-slug dieses Laufs>' }
```
Nur ASCII, das verlangt `DeploySkripte_NurAscii_…` (`NachlaufManifestDriftTests.cs:305-316`). Der Text kennt `{…}`-Platzhalter, hier werden
keine gebraucht.

### R-2: neues Einmal-Skript `SQL/Einmalig/2026-11-28_v<neu>_DeleteTestBookingsTeilelisteKandidaten.sql`
Aufbau nach ADR 0023, Vorbild ist der Kopf des v1.99.32-Skripts:
- Kopf: Einmalig, nur IDEAL-Test, **daten-löschend → DB-Backup vor SCHARF**, Herkunft (diese Spec, Gerald 2026-11-28 „nur Testbuchungen“).
- Eine Zeile `DECLARE @Trockenlauf BIT = 1;`, `SET XACT_ABORT ON;`, kein ROLLBACK, keine sqlcmd-Direktiven.
- Guard `COL_LENGTH(N'dbo.ProductionWorkplaces', N'BdeTeileliste') IS NULL` → RAISERROR wie `:37-41`. Das ist keine Wiederholungssperre und
  braucht deshalb kein `TREFFER: 0`.
- `#Ziel` = feste Liste `(522), (534), (810), (816), (820), (1093), (1099), (1103)`. Kommentar: „WorkOperationIds der Meldeliste IDEAL-Test
  2026-11-28“.
- **Sicherung über die Kandidatenregel:** Die Kandidatenermittlung `#Codes`/`#Tok`/`#Cand` wird wörtlich aus `:47-73` übernommen und auf
  `#Ziel` eingeschränkt. Zusätzlich gilt `wo.IsReported = 0`. Es werden also nur Buchungen von Arbeitsgängen angefasst, die `#2` danach
  wirklich löscht. Eine Id aus `#Ziel`, die diese Regel nicht mehr erfüllt (anderer Stand, zurückgemeldet), wird nur gemeldet
  („nicht angefasst: …“).
- `#B` = alle `BdeBookings` dieser Arbeitsgänge plus deren Kind-Buchungen (`ParentBookingId IN #B`, eine Ebene). Hängt danach noch ein
  weiterer Verweis daran, scheitert der DELETE am FK. `XACT_ABORT` rollt dann zurück, das Update-Skript meldet den Fehlschlag und schreibt
  keinen Marker (`:587`). Der Fehler ist sichtbar statt still behandelt.
- Ausgabe in beiden Modi: Liste der Buchungen (Id, WorkOperationId, Werkbank, StartedAt, IsCancelled, Mengen-Anzahl) und Liste der
  `BdeTeilMeldungen` mit `BdeBookingId IN #B`.
- Trockenlauf: `PRINT CONCAT(N'TREFFER: ', <Anzahl #B>)`, RETURN.
- Scharf, in dieser Reihenfolge:
  1. `DELETE BdeTeilMeldungen WHERE BdeBookingId IN #B`. Das sind die Teilmeldungen derselben Testbuchung. Würde man nur den Verweis auf
     NULL setzen, bliebe eine Teilmeldung ohne Buchung zurück.
  2. `UPDATE BdeBookings SET ParentBookingId = NULL WHERE Id IN #B AND ParentBookingId IN #B`, damit der Selbstverweis nicht von der
     Löschreihenfolge abhängt.
  3. `DELETE BdeBookings WHERE Id IN #B`. Die Mengen gehen per Cascade mit.
  4. Je Schritt `PRINT` mit `@@ROWCOUNT`.
- Wiederholbar: Nach SCHARF gibt es keine Buchungen mehr an diesen Ids, der zweite Trockenlauf liefert ohne Sperre `TREFFER: 0`. Laufen
  später neue Testbuchungen auf diese Ids auf, findet ein erneuter Lauf sie. Das ist gewollt, denn die Kandidatenregel bleibt die Sicherung.

Manifest-Eintrag (vor `#2`):
```powershell
@{ Id='Einmalig/2026-11-28_v<neu>_DeleteTestBookingsTeilelisteKandidaten'; Version='<neu>'; Typ='Einmalig'; Firma='IDEAL'; System=@('Test'); Phase='Nachher'; Ausfuehrung='Trockenlauf'
   Datei='Einmalig\2026-11-28_v<neu>_DeleteTestBookingsTeilelisteKandidaten.sql'; Braucht=@('SQL/154')
   Voraussetzung='nur IDEAL-Test, vor ...LeafTokensTeileliste#2, DB-Backup'
   Text='Nur IDEAL-Test (WMS-10, Gerald 2026-11-28: nur Testbuchungen): loescht die BDE-Buchungen (samt Mengen, Kind-Buchungen und zugehoeriger Teilmeldungen) der 8 Arbeitsgaenge der Meldeliste (EG 522 534 810 816 820, KA 1093 1099 1103), damit der folgende Eintrag #2 sie mit loescht. Trockenlauf automatisch (Liste ins Protokoll). TREFFER 0 -> entfaellt. Sonst SCHARF (Backup aus diesem Lauf Pflicht); zweiter Trockenlauf muss 0 liefern.'
   Quelle='<brain-changelog-slug dieses Laufs>' }
```

### Prüfabfrage vor dem Bau (IDEAL-Test, nur lesend; Ergebnis in diese Spec)
Darf der Dev-Lauf selbst ausführen (Freigabe Sage-/WMS-Test-SQL lesend). Sie bestätigt den Umfang von R-2, ändert aber nichts am Design.
```sql
-- P-6 Buchungen, Kind-Buchungen, Teilmeldungen und Rueckmeldung der 8 Testkandidaten
SELECT wo.Id AS WorkOperationId, wo.OperationNumber, wo.IsReported, po.OrderNumber, po.SubOrderNumber,
       b.Id AS BookingId, b.ParentBookingId, b.BookingType, b.Status, b.IsCancelled, b.StartedAt, b.ProductionWorkplaceId,
       (SELECT COUNT(*) FROM dbo.BdeBookingQuantities q WHERE q.BdeBookingId = b.Id) AS Mengen,
       (SELECT COUNT(*) FROM dbo.BdeBookings k WHERE k.ParentBookingId = b.Id) AS Kinder,
       (SELECT COUNT(*) FROM dbo.BdeTeilMeldungen t WHERE t.BdeBookingId = b.Id) AS Teilmeldungen
FROM dbo.WorkOperations wo
JOIN dbo.ProductionOrders po ON po.Id = wo.ProductionOrderId
LEFT JOIN dbo.BdeBookings b ON b.WorkOperationId = wo.Id
WHERE wo.Id IN (522, 534, 810, 816, 820, 1093, 1099, 1103)
ORDER BY wo.Id, b.Id;
-- Erwartung: 8 Arbeitsgaenge, IsReported = 0, je mind. eine Buchung. Kind-Buchungen mit anderer WorkOperationId oder IsReported = 1
-- -> im Befund festhalten (R-2 meldet sie; IsReported = 1 bliebe nach #2 stehen, Ruecknahme in der Verfolgung TrackingController.cs:284).
```

Listen-View-Pattern: keine neue Ansicht. Rollen: keine Änderung.

## Migrations-/SQL-Auswirkungen
- Keine EF-Migration, kein `SQL/XX_*.sql`, kein `00_FreshInstall.sql` (Einmal-Skripte gehören nicht zur nummerierten Reihe, Kopf `:4-5`),
  `SQL/AgentJobs` nicht betroffen.
- R-1: Die vorhandene Datei wird unverändert verwendet, die SHA bleibt gleich. Sie ist **daten-löschend**: nur ungebuchte, von der Erkennung
  angelegte Kandidaten (`:13-19`, DELETE `:94-102`).
- R-2: neue Einmal-Datei, **daten-löschend** (BDE-Buchungen, Mengen, Teilmeldungen der 8 Testkandidaten), nur IDEAL-Test.
- **DB-Backup vor SCHARF.** Das Update-Skript erzwingt es (`Update-IdealAkeWms.ps1:577`, `:516-523`). Ein Backup im Lauf genügt für beide
  Einträge.

## Einstellungen
keine

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen
Kein Update an Entitäten, beide Skripte löschen nur (gleiche Linie wie v1.99.32, Audit-Abschnitt der Ursprungs-Spec). Nachweis sind die Listen
und Zählungen im Trockenlauf-/Scharf-Protokoll (Log-Ordner, `Update-IdealAkeWms.ps1:558`) und die Marker in `DeployScriptRuns` mit
`ExecutedBy`, `PackageVersion`, `FileSha256`, `Output` (`:335-338`).

## Rollen / Zugriff
Unverändert. Die Ausführung liegt beim Admin, der das Update-Skript am Zielserver startet. Es gibt keine neue Rolle und keinen neuen Filter.

## Akzeptanzkriterien
1. `nachlauf.psd1` enthält den Eintrag `Einmalig/2026-11-17_v1.99.32_RemoveParentWorkOperationsFromLeafTokensTeileliste#2` (Typ Einmalig,
   Firma IDEAL, Phase Nachher, Ausführung Trockenlauf, dieselbe `Datei` wie `:339`, `Braucht=@('SQL/154')`). Der ursprüngliche Eintrag
   `:338-342` bleibt unverändert.
2. `nachlauf.psd1` enthält den R-2-Eintrag mit `System=@('Test')` **vor** `#2`. Die R-2-Datei existiert und enthält
   `DECLARE @Trockenlauf BIT = 1;` genau einmal, `TREFFER: `, kein ROLLBACK und keine sqlcmd-Direktive.
3. Die Datei `SQL/Einmalig/2026-11-17_v1.99.32_RemoveParentWorkOperationsFromLeafTokensTeileliste.sql` ist unverändert (`git diff` leer).
   `Update-IdealAkeWms.ps1` und `NachlaufManifestDriftTests.cs` sind unverändert.
4. `NachlaufManifestDriftTests` ist grün, ebenso Build und alle Tests.
5. Update an IDEAL-Test mit `-NurPruefen`: Die Vorprüfungsliste zeigt beide neuen Ids als „offen“, den alten Eintrag als
   „erledigt am 18.11.2026 08:29 (Entfaellt, Treffer 0)“ (`Update-IdealAkeWms.ps1:710-713`).
6. Update an IDEAL-Test, Nachlauf, R-2:
   - Der Trockenlauf zeigt TREFFER > 0 und listet die Buchungen der 8 Arbeitsgänge.
   - Danach Backup und SCHARF.
   - Der zweite Trockenlauf meldet `TREFFER: 0`.
   - In `DeployScriptRuns` steht ein Marker `Scharf` mit dieser Id.
7. Update an IDEAL-Test, Nachlauf, `#2` (direkt danach):
   - Der Trockenlauf meldet TREFFER ≈ 637 + 8 (der genaue Wert hängt vom Datenstand am Deploy-Tag ab) und eine **leere** Meldeliste.
   - Danach SCHARF.
   - Der zweite Trockenlauf meldet `TREFFER: 0` mit leerer Meldeliste.
   - In `DeployScriptRuns` steht der Marker `Scharf` für `…#2`. Der alte Marker `Entfaellt` bleibt unverändert.
8. Nach dem Lauf liefert P-2 der Ursprungs-Spec für 1069721/1069724 nur noch den Arbeitsgang der Kopfzeile (VM bzw. M31) sowie Kürzel ohne
   Teileliste-Werkbank. P-6 liefert keine Buchungen mehr.
9. Bestandsverhalten:
   - Ein neuer Strukturlauf (Erkennung) legt die gelöschten Arbeitsgänge an Teileliste-Kürzeln **nicht** wieder an (Regel v1.99.32,
     `WorkOperationStructureDetectionService.cs:96-98,125-129`).
   - Teile Einzeln/Summiert melden die Blechteile weiter.
   - Manuell angelegte Arbeitsgänge (anderes `CreatedBy`) sind unberührt.
10. Ein zweites Update mit demselben Paket bietet keinen der beiden Einträge mehr an, weil der Marker gesetzt ist (`:537-539`).
11. Die Version steht in beiden `AppVersion.cs`, das Anwender-Changelog ist ergänzt (Betriebshinweis: Update-Nachlauf bereinigt
    BDE-Arbeitsgänge an IDEAL).

## Test-Szenarien
Neues Kapitel in `docs/TESTSZENARIEN.md`, **voraussichtlich TS-214**. `docs/TESTSZENARIEN.md` endet bei TS-209 (`:15085`). TS-210–213 sind
von Entwurfs-Specs vorgemerkt, z. B. [[2026-11-26-leitstand-kaskaden-zaehler-ohne-hauptfa-spec]] TS-210 und
[[2026-11-27-buchungsrechte-je-lagerort-spec]] TS-213. Der Dev-Lauf prüft die Nummer. Titel: **„IDEAL: BDE-Arbeitsgang-Altbestand
Teileliste – Wiederholung Einmal-Skript (#2) und Testbuchungen“**. Danach `secondbrain/tests/testszenarien-index.md` nachziehen.
- **Vorbedingungen:** IDEAL-Test (bzw. eine Kopie davon auf IDEAL-DEV mit `System`-Anpassung nur zum Proben). Paket der neuen Version,
  Marker `Entfaellt` der ersten Id vorhanden (P-4), Trockenlauf-Stand wie im Befund, DB-Backup-Ordner erreichbar.
- **Schritte:**
  1. `-NurPruefen`: Die Liste zeigt beide neuen Ids als offen, die alte als erledigt (AK 5).
  2. Update. R-2-Trockenlauf: Liste der Testbuchungen prüfen, danach Backup und SCHARF. Der zweite Trockenlauf liefert 0 (AK 6).
  3. `#2`-Trockenlauf: Zählung je Kürzel, die Meldeliste ist leer. Danach SCHARF. Der zweite Trockenlauf liefert 0 (AK 7).
  4. P-2/P-6 nachfragen (AK 8).
  5. BDE am KA-Terminal, Reiter FA, Spaltenfilter Haupt-FA 1069617: Für Sub-FAs ohne KA in der Kopfzeile (1069718–1069727, 1069729, 1069732,
     1069733) gibt es keine KA-Zeile mehr. 1069728 und 1069730 (Kopfzeile KA) bleiben.
  6. Reiter Teile Einzeln: KA-Blechteile sind meldbar.
  7. Dienst-Strukturlauf abwarten bzw. anstoßen, danach P-2 erneut: Die Arbeitsgänge sind nicht wieder angelegt (AK 9).
  8. Update erneut starten: Beide Einträge stehen als erledigt da und werden nicht angeboten (AK 10).
- **Negativfälle:**
  - Ohne Backup aus diesem Lauf ist SCHARF gesperrt und der Eintrag bleibt offen (`:577`, `:516-523`).
  - Eine andere Eingabe als `SCHARF` führt nichts aus (`:578-579`).
  - IDEAL-Prod: Der R-2-Eintrag wird nicht angezeigt (System Test). `#2` zeigt den Prod-Umfang.
  - AKE: Keiner der Einträge erscheint (Firma IDEAL).

## Etappen (nur bei epic: true)
entfällt (kein Epic)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy
*Vorläufig – der QA-Lauf bestätigt es gegen den echten Diff.*
- **Web-App:** nein (keine Codeänderung; der Versionssprung kommt nur mit dem Paket)
- **Service:** nein
- **Migration:** nein
- **Update-Paket:** Ja. Die Änderung wirkt nur über `scripts/deploy/nachlauf.psd1` und `SQL/Einmalig/` im Paket. Das Paket trägt die neue
  Version (`$PaketVersion`), deshalb sind Versionssprung und Paket nötig. Nach CLAUDE.md DoD kommt das Paket-ZIP auf beide Shares.
- **DB-Backup vor SCHARF** (vom Update-Skript erzwungen).
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023), in dieser Reihenfolge nach `:342`:
  1. `Einmalig/2026-11-28_v<neu>_DeleteTestBookingsTeilelisteKandidaten`:
     - Version `<neu>`, Typ Einmalig, Firma IDEAL, `System=@('Test')`, Phase Nachher, Ausführung Trockenlauf.
     - Text siehe Lösungsentwurf R-2.
     - Die Trockenlauf-Datei hat genau ein `DECLARE @Trockenlauf BIT = 1;` und eine Zeile `TREFFER: n`. Sie hat keine Wiederholungssperre,
       der zweite Trockenlauf liefert 0 aus den Daten.
  2. `Einmalig/2026-11-17_v1.99.32_RemoveParentWorkOperationsFromLeafTokensTeileliste#2`:
     - Version `<neu>`, Typ Einmalig, Firma IDEAL, Phase Nachher, Ausführung Trockenlauf.
     - Dieselbe Datei wie `:339` (unverändert).
     - Text siehe Lösungsentwurf R-1.
- **IDEAL-Prod (`IDEAL_AKE_WMS`):**
  - Ob Prod betroffen ist, ist offen. Der Trockenlauf von `#2` im Update zeigt den dortigen Umfang.
  - TREFFER 0 bedeutet „entfaellt“.
  - Stehen gebuchte Kandidaten auf der Prod-Meldeliste, bleiben sie stehen. Die Liste geht an Gerald, denn die Freigabe „nur Testbuchungen“
    gilt nur für IDEAL-Test.
  - Ist auf Prod schon der **erste** Eintrag offen (kein Marker), läuft er zuerst. `#2` liefert danach TREFFER 0 → „entfaellt“. Das ist
    harmlos.
- **Version:** nächste freie Version, der Dev-Lauf prüft sie (main = 1.99.89; parallele Entwürfe belegen ggf. weitere Nummern).
  `<neu>` steht für diese Version im Dateinamen und in den Manifest-Feldern.
- **Publish-Befehle:** kein Web-/Dienst-Publish nötig. Das Update-Paket wird wie üblich gebaut (`scripts/deploy`, ADR 0022).

## Offene Rueckfragen
keine. Alle Entscheidungen sind getroffen (Ursprungs-Spec Antworten 3 und 4, Gerald 2026-11-28 zu den Testbuchungen). P-6 ist ein lesender
Prüfschritt des Dev-Laufs, keine Frage.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

keine offenen Rückfragen.
