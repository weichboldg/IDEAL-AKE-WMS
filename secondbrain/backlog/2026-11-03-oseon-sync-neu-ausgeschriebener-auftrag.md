---
typ: bug
---
# OSEON-Tracking-Sync bricht ab, wenn OSEON einen Auftrag neu ausschreibt (doppelter Schlüssel)

**Aufgenommen 2026-11-03.** Gerald meldet den Fehler vom AKE-System:

```
SqlException 2601: Eine Zeile mit doppeltem Schlüssel kann in das dbo.OseonProductionOrders-Objekt mit dem eindeutigen
IX_OseonProductionOrders_OseonOrderNumber-Index nicht eingefügt werden. Der doppelte Schlüsselwert ist (1617675-06).
at OseonSyncService.BulkMergeOrdersAsync … OseonSyncService.cs:line 377
```

Fachlicher Auslöser (Gerald): OSEON schreibt gelegentlich einen Auftrag **neu aus** — gleiche Auftragsnummer, neuer
Datensatz. Geralds Vorschlag: den bestehenden Eintrag löschen.

## Befund (am Code belegt, `main` v1.30.1)

- Die Identität ist an zwei Stellen verschieden definiert:
  - Der MERGE der Aufträge vergleicht über die **OSEON-interne ID**: `ON target.[OseonId] = source.[OseonId]`
    (`IDEALAKEWMSService/Services/OseonSyncService.cs`, `BulkMergeOrdersAsync`, MERGE-Statement).
  - Eindeutig ist in der Tabelle aber die **Auftragsnummer**:
    `IX_OseonProductionOrders_OseonOrderNumber`, `unique: true`
    (`IdealAkeWms/Migrations/20260317140041_AddOseonTracking.cs`). Auf `OseonId` liegt nur ein normaler Index.
- **Folge beim Neuausschreiben:** Die neue OSEON-ID findet keinen Treffer → `WHEN NOT MATCHED THEN INSERT` mit derselben
  Auftragsnummer → Fehler 2601. Der MERGE ist **ein** Statement, damit scheitert der ganze Lauf; auch die Arbeitsgänge
  (`BulkMergeOperationsAsync`) werden nicht mehr abgeglichen.
- **Der Sync hängt danach dauerhaft** (Ableitung aus dem Code, im Aktivitäts-Protokoll zu bestätigen):
  - Der Delta-Filter ist `pa.DateOfLastChange > MAX(LastChangedInOseon) − 5 min`.
  - Weil nichts geschrieben wird, bleibt das Maximum stehen.
  - Der neu ausgeschriebene Auftrag liegt deshalb in jedem Lauf wieder im Fenster, und jeder Lauf scheitert erneut.
  - Ergebnis: Die OSEON-Teileverfolgung friert am Stand vor dem ersten Fehler ein.
- **Zweiter Weg zum selben Fehler — „gelegentlich“:** Liefert OSEON zur selben Auftragsnummer **beide** Datensätze (alt und
  neu, z. B. weil der alte noch in den letzten 3 Monaten endet), dann gilt:
  - Der Sync gruppiert nach Auftragsnummer und nimmt `group.First()`.
  - Die OSEON-Abfrage hat kein `ORDER BY`, die gewählte ID ist also zufällig.
  - Erwischt ein Lauf die alte ID, läuft er durch; erwischt er die neue, scheitert er.
- Die Arbeitsgänge hängen schon heute an der **Auftragsnummer**: `BulkMergeOperationsAsync` verbindet
  `opo.[OseonOrderNumber] = t.[OseonOrderNumber]`. Nur der Auftrags-MERGE benutzt die OSEON-ID.
- `OseonWorkOperations` → `OseonProductionOrders` ist `ON DELETE CASCADE` (gleiche Migration). Ein gelöschter Auftrag nimmt
  seine Arbeitsgänge mit. *Ungeprüft:* ob weitere Tabellen auf `OseonProductionOrders.Id` oder `OseonWorkOperations.Id`
  verweisen.
- *Ungeprüft:* ob das Bündel (`feature/2026-08-07-ideal-teile-1-5`) denselben MERGE enthält. Davon ist auszugehen; vor der
  Spec nachsehen.

## Empfehlung der Koordination: nicht löschen, sondern über die Auftragsnummer abgleichen

1. **MERGE über die Auftragsnummer** (`ON target.[OseonOrderNumber] = source.[OseonOrderNumber]`), die OSEON-ID wird
   mitgeschrieben (`target.[OseonId] = source.[OseonId]`).
   - Damit stimmt der Abgleich mit dem eindeutigen Index und mit dem MERGE der Arbeitsgänge überein.
   - Der WMS-Datensatz behält seine `Id`, und die Arbeitsgänge bleiben daran hängen.
   - Nichts wird kaskadiert gelöscht.
2. **Deterministische Wahl**, wenn OSEON mehrere Datensätze je Auftragsnummer liefert: den jüngsten nehmen (höchste
   `DateOfLastChange`, bei Gleichstand höchste `ID`). Kein zufälliges `First()` mehr.
3. **Melden statt still behandeln** (CLAUDE.md, Hausregel):
   - Count `neu_ausgeschrieben`: Treffer, deren OSEON-ID sich geändert hat. Je Treffer eine Detailzeile mit Auftragsnummer,
     alter und neuer ID.
   - Count `mehrfach_in_oseon` für Punkt 2.
4. **Offen für die Spec:** Was passiert mit Arbeitsgängen des alten Auftrags, deren Positionsnummer der neue nicht mehr hat?
   Heute löscht der Sync nie Arbeitsgänge, die stehengebliebenen würden die Ampel weiter beeinflussen.
   - Empfehlung: nur im Fall „neu ausgeschrieben“ die nicht mehr gelieferten Positionen **dieses** Auftrags entfernen und
     zählen.
   - Voraussetzung: Die OSEON-Abfrage liefert für einen geänderten Auftrag alle seine Arbeitsgänge. Das ist am Join
     `aga.AuftragsNr = pa.AuftragsNr` abzulesen, an echten Daten aber zu prüfen.

Warum nicht Geralds Vorschlag „bestehenden Eintrag löschen“ als Regel:
- Löschen kaskadiert auf die Arbeitsgänge.
- Möglicherweise verweisen weitere Tabellen auf den Datensatz (ungeprüft).
- Das Löschen müsste vor dem MERGE in einem eigenen Schritt laufen.

Der Abgleich über die Auftragsnummer erreicht dasselbe Ergebnis ohne Datenverlust und mit weniger Code.

## Sofortmaßnahme (Gerald, vor einem Fix) — nur nach den Prüfabfragen

**P1 OSEON-DB — welche Datensätze gibt es zu dieser Nummer?**
```sql
SELECT ID, AuftragsNr, KundenAuftragsNr, Status, EndTerminSoll, DateOfLastChange
FROM ProduktionsAuftrag WHERE AuftragsNr = '1617675-06' ORDER BY ID;
```
**P2 OSEON-DB — wie oft kommt das vor?**
```sql
SELECT AuftragsNr, COUNT(*) AS Anzahl, MIN(ID) AS MinId, MAX(ID) AS MaxId
FROM ProduktionsAuftrag WHERE KundenAuftragsNr IS NOT NULL
GROUP BY AuftragsNr HAVING COUNT(*) > 1 ORDER BY Anzahl DESC;
```
**P3 WMS-DB — was steht bei uns?**
```sql
SELECT o.Id, o.OseonId, o.OseonOrderNumber, o.OseonStatus, o.LastChangedInOseon,
       (SELECT COUNT(*) FROM dbo.OseonWorkOperations w WHERE w.OseonProductionOrderId = o.Id) AS Arbeitsgaenge
FROM dbo.OseonProductionOrders o WHERE o.OseonOrderNumber = '1617675-06';
```
**P4 WMS-DB — seit wann hängt der Sync?** `/SyncLog`, Filter OseonTracking, Stufe Error: erster Eintrag mit „doppeltem
Schlüssel“.

**Fall A — P1 liefert genau einen Datensatz (nur die neue ID):** Der WMS-Datensatz bekommt die neue ID, danach läuft der
nächste Sync durch.
```sql
BEGIN TRAN;
UPDATE dbo.OseonProductionOrders
SET OseonId = <neue ID aus P1>, ModifiedAt = GETUTCDATE(), ModifiedBy = 'Einmalig-OSEON-Rekey', ModifiedByWindows = SYSTEM_USER
WHERE OseonOrderNumber = '1617675-06' AND OseonId = <alte ID aus P3>;
-- erwartet: (1 Zeile betroffen) — sonst ROLLBACK
COMMIT;
```

**Fall B — P1 liefert alte und neue ID:** Ein Umschlüsseln hilft nur, bis ein Lauf wieder die andere ID zieht. Dann kommt
der Fehler zurück („gelegentlich“). Dauerhaft hilft nur der Code-Fix; bis dahin melden.

Ein Löschen des WMS-Datensatzes statt des Umschlüsselns geht in Fall A auch. Es entfernt aber per Kaskade alle Arbeitsgänge;
der nächste Lauf legt sie aus OSEON neu an. Vorher sicherstellen, dass nichts anderes auf den Datensatz verweist.

## Verlauf

- 2026-11-03: Löschskript für den alten WMS-Eintrag geliefert (Id 141768, OseonId 1617691, Status 90, 3 Arbeitsgänge).
  Ob und mit welchem Ergebnis es lief, ist nicht belegt.
- 2026-11-05: Derselbe Fehler kommt **wieder mit 1617675-06**. Damit ist Fall B wahrscheinlich: OSEON liefert im Delta-Fenster
  mehr als eine ID zu dieser Nummer, oder der Auftrag wurde erneut ausgeschrieben. P1 nachfahren. Löschen hilft dann jeweils nur
  bis zum nächsten Lauf. Gerald wünscht ein Löschskript mit Schleife und LIKE-Muster; geliefert als
  `AKE-Oseon-Auftrag-entfernen.sql` (Probelauf als Vorgabe, Reißleine 20 Treffer). Der Code-Fix bleibt die eigentliche Lösung.
- 2026-11-05: Gerald löscht mit `1617675-%` alle Positionen des Kundenauftrags (Screenshot der Meldungen). Die -06 stand vorher
  mit der **neuen** OseonId 1664103 (WMS-Id 460581); die Löschung vom 03.11. hatte also gewirkt. Positionen gemischt: teils
  alte IDs (1617683–1617702), teils neue (1664101–1664146). Der Fehler kam danach erneut.
- 2026-11-07: Fehler besteht weiter. Zwei mögliche Ursachen, beide ungeprüft:
  (a) Fall B: OSEON hat zur -06 mehrere IDs im Delta-Fenster; `group.First()` ohne `ORDER BY` wählt je Lauf eine andere →
      ein Lauf legt ID A an, der nächste will ID B einfügen. Löschen hilft nur einen Lauf lang.
  (b) Zwei OSEON-Zeilen, die `GroupBy` (ordinal, getrimmt) als verschieden, die DB-Kollation des eindeutigen Index als gleich
      ansieht (unsichtbares Zeichen) — dann scheitert schon der erste Lauf nach dem Löschen.
  Diagnose-Abfrage mit `varbinary` an Gerald; Ergebnis offen. Empfehlung: Code-Fix jetzt (MERGE über Auftragsnummer,
  Deduplizierung in SQL nach der DB-Kollation, deckt a und b). Keine weiteren Löschungen.
- 2026-11-07 08:49: Diagnose OSEON (Screenshot Gerald), mit den Filtern des Syncs: **genau eine** Zeile, ID 1664103,
  `1617675-06`, Rohbytes sauber (UTF-16, kein verstecktes Zeichen), Status 90, EndTerminSoll 2026-08-21,
  DateOfLastChange 2026-11-02 11:59:32. Damit sind (a) und (b) für die -06 widerlegt. Der Konflikt muss mit einer
  **WMS-Zeile** entstehen, die `1617675-06` mit einer anderen OseonId trägt. WMS-Abfrage angefordert.
- 2026-11-07 08:51: WMS-Abfrage (Gerald): nur -03 (1617683), -04 (1617687), -07 (1617694), Status 20, angelegt
  2026-11-05 08:02:10 UTC und geändert 2026-11-06 13:08:33 UTC von `IDEALAKEWMSService`. **Keine -06, keine OseonId 1664103.**
  Folgerung: Der Sync dieser WMS-DB läuft seit 05.11. ~09:02 Ortszeit wieder (MERGE erfolgreich). Die -06 (OSEON-Änderung
  02.11.) liegt außerhalb des Delta-Fensters und kann hier keinen Konflikt mehr auslösen. Der gemeldete Fehler stammt
  vermutlich aus einer **anderen Instanz** mit eigener WMS-DB gegen dieselbe OSEON-DB (Testsystem akenet01:4444/4445?) oder ist
  ein alter Protokolleintrag. Ungeprüft, Rückfrage an Gerald. Folge der Löschung: -01, -02, -05, -06, -08 bis -16 fehlen in der
  Teileverfolgung, bis sie in OSEON geändert werden.
- 2026-11-07 08:55: **Bestätigt** — die Fehlermail kommt vom Testsystem: Absender `wms-testideal@ake.at`, Maschine AKENET01,
  Version 1.30.0, 07.11.2026 08:03:26. Die AKE-Produktion ist nicht betroffen. Für den Test geliefert:
  `AKE-Oseon-Auftrag-umschluesseln.sql` (Schritt 1 OSEON erzeugt die aktuellen IDs je Nummer, Schritt 2 schlüsselt in der
  Test-WMS-DB um; Probelauf als Vorgabe, Schutz gegen ID an fremder Nummer). Umschlüsseln statt Löschen, Arbeitsgänge bleiben.

## Umsetzung

- Ort: AKE-Produktion läuft auf `main` (v1.30.1) → **AKE-Hotfix aus `main`** (eigener Worktree), danach Vorwärts-Merge ins
  Bündel, wie bei v1.30.1. Ob das Bündel vorher gemergt wird, entscheidet Gerald.
- Raw SQL, nicht InMemory-testbar: Entscheidung (welche ID, neu ausgeschrieben ja/nein) als reiner Helper mit Unit-Tests;
  der MERGE selbst ist Manual-UAT (Muster `services.md`, „Reine Entscheidungs-Helper“).
- Testszenario: Neuausschreibung simulieren (WMS-Datensatz auf eine falsche `OseonId` setzen) → nächster Lauf
  „neu_ausgeschrieben = 1“, kein Fehler, Arbeitsgänge unverändert angehängt.
