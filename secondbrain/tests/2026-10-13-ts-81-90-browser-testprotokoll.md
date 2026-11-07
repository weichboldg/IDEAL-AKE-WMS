---
type: testprotokoll
title: "Browser-Testprotokoll TS-81 bis TS-90 — IDEAL-Bündel Beschichtung, Briefkopf, CSV, FA-Struktur-Arbeitsgänge, Arbeitsgang-Stammdaten, Menü (v1.48.0–v1.57.0)"
ausfuehrung: Claude Cowork (Abnahme)
system: "http://idealweb01.ideal.ideal-ake.at:88/ (Standard); AKE-Abschnitt: https://akenet01.ake.at:4444/"
spec:
  - "[[2026-10-06-beschichtungsauftrag-ausbau-teil-1-spec]]"
  - "[[2026-10-06-beschichtungsauftrag-ausbau-teil-2-spec]]"
  - "[[2026-10-06-beschichtungsauftrag-ausbau-teil-3-spec]]"
  - "[[2026-10-06-beschichtungsauftrag-ausbau-teil-4-spec]]"
  - "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec]]"
  - "[[2026-10-06-csv-export-alle-tabellen-spec]]"
  - "[[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]]"
  - "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]]"
  - "[[2026-10-13-menue-beschichtung-und-hilfe-kurzanleitungen-spec]]"
  - "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-3-spec]]"
created: 2026-10-13
---

# Browser-Testprotokoll TS-81 bis TS-90 — IDEAL-Bündel v1.48.0–v1.57.0

Grundlage: TS-81 bis TS-90 in `docs/TESTSZENARIEN.md` im Stand des Bündel-Commits `d4f326ef`
(Worktree `2026-08-07-ideal-teile-1-5`) und die manuellen Checklisten der Specs im Frontmatter. Teil B läuft unter
der Arbeitsweise ABNAHME aus [[cowork-projekt-abnahme-anweisungen]] — seit 2026-09-30 zusammengeführt in
[[cowork-projekt-ideal-ake-wms-anweisungen]] (Abschnitt `=== ABNAHME ===`, dort gilt der aktuelle Wortlaut).
**Nicht enthalten:** Suchsyntax der Spaltenfilter (TS-91, v1.58.x) — eigenes Protokoll nach Etappe B.
**Getesteter Stand:** Deploy des Bündels ab `d4108f1a` (v1.59.0), Server-Vorlage `Branding\briefkopf.html` = neues Muster mit
Fußzeile (TS-92, Nachzug Koordination 2026-10-14: B42–B44, B46, B54). Davor: `ead16fc4` (v1.58.1). Die Schritte gelten unverändert; einzige Anpassung durch
die Suchsyntax: B40 Teil 2 filtert mit `=` (Koordination 2026-10-13).

Menüwege und Knopftexte sind gegen die Views im Bündel-Worktree belegt (`Views/Shared/_Layout.cshtml` Z. 113–126
Menü „Beschichtung“ mit „Vorschlagsliste“/„Aufträge“; Z. 308–321 Stammdaten › „Arbeitsgänge“ nur Admin;
`Views/Beschichtungsauftraege/Details.cshtml`, `_VorschlagAuftraege.cshtml`, `Views/OperationConfigs/Index.cshtml`,
`Views/BdeTerminal/Index.cshtml`, `wwwroot/js/bde-terminal.js` Z. 313). „Hilfe“ steht in der **Fußzeile**, nicht in der
Menüleiste.

**Ablauf:** Teil A erledigst du vorab. Dann im Freigabe-Block ankreuzen, was Cowork ändern darf, die Platzhalter
`{...}` in Teil B ausfüllen und den Block als Auftrag an Claude Cowork geben (eigene Aufgabe, Arbeitsweise Abnahme).
Teil C machst du danach.

> [!warning] Mehrere Benutzer, ein Browser
> Cowork meldet sich nie selbst an. Teil B ist deshalb in Blöcke je Benutzer geteilt; an jeder Marke
> **BENUTZERWECHSEL** hält Cowork an, du meldest den nächsten Benutzer an und schreibst „weiter“.

---

## Freigabe-Block (Gerald kreuzt an — nur angekreuzte Schritte darf Cowork ausführen)

Ohne Haken ist der Schritt für Cowork **NICHT PRÜFBAR**. Die angekreuzten Nummern trägst du in Teil B unter
`FREIGABEN` ein (Cowork liest nur den kopierten Block, nicht diese Häkchen).

IDEAL (`http://idealweb01.ideal.ideal-ake.at:88/`):

- [ ] **B22** Arbeitsgang-Stammdaten: Testkürzel `{KUERZEL_TEST}` anlegen, ändern, Negativfälle absenden
- [ ] **B23** Einstellung `FaHierarchyBeschichtungAktiv` aus- und wieder einschalten
- [ ] **B24** Einstellung `BdeAktiv` einschalten (nur wenn `{BDE_AKTIV_START}` = false)
- [ ] **B29** BDE-Terminal: Werker B startet „Produktion starten“ auf `{AG_ZUKUNFT}`
- [ ] **B30** BDE-Terminal: Werker A klickt „Rüsten starten“ auf denselben AG (Kollisions-Fenster erwartet), ggf. „Produktion starten“
- [ ] **B31** BDE-Terminal: Werker B pausiert (Gutmenge 0)
- [ ] **B32** BDE-Terminal: Werker B startet `{AG_UEBERFAELLIG}` und pausiert danach (Gutmenge 0)
- [ ] **B33** BDE-Terminal: Werker A startet `{AG_ABSCHLUSS}` und schließt ihn mit „Beenden (mit Mengen)“ ab (Endmeldung)
- [ ] **B34** Arbeitsgang-Stammdaten: Eintrag `{KUERZEL_OHNE_EINTRAG}` anlegen
- [ ] **B47** Beschichtungsauftrag anlegen („Aus Vorschlagsliste erstellen“, HauptFA `{HFA_NEU}`)
- [ ] **B48** denselben Anlege-Knopf aus einem zweiten, vorher geladenen Tab erneut absenden
- [ ] **B49** Entwurf bearbeiten und speichern (inkl. Negativfall Menge `abc`)
- [ ] **B50** Position entfernen + zweiten Auftrag aus der freien Position anlegen
- [ ] **B51** Position entfernen und über „Positionen hinzufügen“ wieder hinzufügen
- [ ] **B52** Dienstleister leeren, speichern, „Übergeben“ versuchen, Dienstleister wieder eintragen
- [ ] **B53** Status auf „Übergeben“ setzen, Bemerkung speichern
- [ ] **B55** Status auf „Erledigt“ setzen, Reklamation anlegen
- [ ] **B56** im Reklamations-Entwurf Ursprungspositionen hinzufügen
- [ ] **B57** Reklamation übergeben/erledigen, zweite und Ketten-Reklamation anlegen
- [ ] **B58** Entwurf stornieren
- [ ] **B59** Konflikt: Entwurf in Tab A speichern, in Tab B Status wechseln
- [ ] **B68** Aufräumen: alle verbliebenen Test-**Entwürfe** stornieren
- [ ] **B69** Aufräumen: Stammdaten-Testeinträge `{KUERZEL_TEST}` und `{KUERZEL_OHNE_EINTRAG}` löschen
- [ ] **B70** Aufräumen: `BdeAktiv` auf `{BDE_AKTIV_START}` zurücksetzen

AKE (`https://akenet01.ake.at:4444/`):

- [ ] **K6** Einstellung `BdeAktiv` am AKE-Testsystem einschalten (nur wenn `{AKE_BDE_AKTIV_START}` = false)
- [ ] **K8** Aufräumen: `BdeAktiv` am AKE-Testsystem auf `{AKE_BDE_AKTIV_START}` zurücksetzen

> [!important] Nicht aufräumbar
> Aufträge im Status *Übergeben*/*Erledigt* lassen sich nicht stornieren (Statusautomat: Storno nur aus *Entwurf*,
> `Services/BeschichtungsauftragWorkflow.cs` Z. 55–57) und es gibt keine Löschfunktion. Nach B47–B59 bleiben
> mindestens der Ursprungsauftrag und eine Reklamation dauerhaft stehen, die Nummern `BA-2026-…` sind verbraucht.
> Die BDE-Endmeldung aus B33 und die pausierten Buchungen aus B31/B32 bleiben ebenfalls stehen (siehe Teil C).

---

## Teil A — Vorbereitung (Mensch, vor dem Start)

**A1 — Stand der Testsysteme.** Auf beiden Testsystemen läuft das Bündel (Fußzeile: Version **≥ 1.57.0**; ist bereits
1.58.x installiert, ist das in Ordnung — TS-91 wird hier nicht geprüft). `SQL/94_AddBeschichtungsauftrag.sql` und
`SQL/95_RenameOperationConfigsAndAddAudit.sql` sind gelaufen (vor `SQL/95` P1 „vor dem Deploy“, siehe A2).

**A2 — Prüfabfragen (Ergebnisse hier notieren).**

TS-81.1 — Mengeneinheit (Sage-DB IDEAL):

```sql
EXEC sp_helptext 'vw_IDEAL-AKE_Kommissionierung_FAListe';   -- welche Tabelle liefert Sollmenge?
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = '<Positionstabelle>' AND COLUMN_NAME LIKE '%einheit%';
-- kein Treffer => Summierung je Artnr einheitensicher; Treffer => melden, TS-81.3 (B36) unter Vorbehalt
```

Ergebnis: ______ (gleicher Befund wie TS-80.1, falls dort schon geprüft)

TS-82.1 — Nebenbefund „Dienstleister? · RAL 0“ (Sage-DB IDEAL und IDEAL-WMS-DB), für den betroffenen HauptFA:

```sql
-- Sage
SELECT [HauptFA],[Abteilung],[Dienstleister],[RAL],[Start_Beschichtung],[Beschichten_Retour]
FROM [vw_IDEAL-AKE_Kommissionierung_FAInfos] WHERE [HauptFA] = <HauptFA>;
-- WMS
SELECT HauptFA, MontageAbteilung, Dienstleister, RAL FROM dbo.FaHierarchyOrderInfos WHERE HauptFA = <HauptFA>;
-- gleich => Datenluecke in Sage; verschieden => Sync-Befund, melden
```

Ergebnis: ______

TS-83.1 — Schlüssel-Eindeutigkeit (Sage-DB IDEAL und IDEAL-WMS-DB):

```sql
-- Sage
SELECT [HauptFA],[VaterFA],[SubFA],[Position],COUNT(*)
FROM [vw_IDEAL-AKE_Kommissionierung_FAListe] WHERE [Beschichtet] <> 0
GROUP BY [HauptFA],[VaterFA],[SubFA],[Position] HAVING COUNT(*) > 1;
-- WMS
SELECT HauptFA,VaterFA,SubFA,Position,COUNT(*) FROM dbo.FaHierarchyNodes WHERE Beschichtet = 1
GROUP BY HauptFA,VaterFA,SubFA,Position HAVING COUNT(*) > 1;
-- leer => Herkunftsschluessel traegt die Doppelvergabe-Pruefung; Treffer => melden
```

Ergebnis: ______

TS-83.2 — View-Filter (Sage-DB IDEAL):

```sql
EXEC sp_helptext 'vw_IDEAL-AKE_Kommissionierung_FAInfos';
-- nach welchem Kriterium faellt ein HauptFA aus der View (und damit aus dem Cache)?
```

Ergebnis: ______

P-K1 — Feldgleichheit Sage ↔ WMS (IDEAL-WMS-DB; trägt TS-85.2 / B3):

```sql
SELECT Artnr, Bezeichnung1, Arbeitsbereich, Arbeitsschritte
FROM dbo.FaHierarchyNodes
WHERE Artnr IN ('G-KT-A-BODEN-1320-660-V01', 'FR-4301-40-20-1,25-1530,0MM-V01',
                'G-KT-Mittelholmblende-40-760-V02', 'G-KT-Korpus-Gepunktet')
   OR Bezeichnung1 IN ('G-KT-A-BODEN-1320-660-V01', 'G-KT-Korpus-Gepunktet');
-- Vorbild: A-BODEN = H1-03 / "LS KA PG"; FR-4301-...-1530 = S-01 / "ZS";
--          Mittelholmblende = H2-02 / "VM"; Korpus-Gepunktet = (leer) / "BE"
```

Ergebnis: ______

P-K2 b — Kürzel ohne/mit mehreren Werkbänken (IDEAL-WMS-DB; liefert `{KUERZEL_OHNE_WB}`):

```sql
SELECT t.value AS Kuerzel, COUNT(*) AS Knotenzeilen, COUNT(DISTINCT wp.Id) AS Werkbaenke
FROM dbo.FaHierarchyNodes n
CROSS APPLY STRING_SPLIT(n.Arbeitsschritte, ' ') t
LEFT JOIN dbo.ProductionWorkplaces wp ON LTRIM(RTRIM(wp.ArbeitsschrittCode)) = t.value
WHERE t.value <> ''
GROUP BY t.value ORDER BY Werkbaenke, Kuerzel;
```

P1–P5 Arbeitsgang-Stammdaten — **auf der AKE- und der IDEAL-WMS-DB**. Ist `SQL/95` noch nicht gelaufen, **muss**
vorher P1 alt laufen (vollständiger Block „vor dem Deploy“ in [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]]):

```sql
-- P1 VOR dem Deploy (alter Name): fehlt 20260330142035_AddOseonOperationConfig => Deploy anhalten (R10)
SELECT COUNT(*) AS Anzahl FROM dbo.OseonOperationConfigs;
SELECT i.name, i.is_unique, i.is_unique_constraint FROM sys.indexes i
WHERE i.object_id = OBJECT_ID(N'dbo.OseonOperationConfigs') AND i.name IS NOT NULL;
SELECT MigrationId FROM dbo.__EFMigrationsHistory WHERE MigrationId LIKE '%AddOseonOperationConfig';
```

Nach dem Deploy (Stand der Testsysteme):

```sql
-- P1 Bestand + Audit + Migration
SELECT COUNT(*) AS Anzahl FROM dbo.OperationConfigs;
SELECT Id, OperationName, DisplayName, DueDateOffsetDays, IsExternalSystemRelevant,
       CreatedAt, CreatedBy, ModifiedAt, ModifiedBy
FROM dbo.OperationConfigs ORDER BY OperationName;
SELECT MigrationId FROM dbo.__EFMigrationsHistory WHERE MigrationId LIKE '%RenameOperationConfigsAndAddAudit';

-- P2 Collation + Dubletten nach Normalisierung
SELECT DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS DbCollation, c.collation_name AS SpaltenCollation
FROM sys.columns c WHERE c.object_id = OBJECT_ID(N'dbo.OperationConfigs') AND c.name = N'OperationName';
SELECT UPPER(LTRIM(RTRIM(OperationName))) AS Norm, COUNT(*) AS Anzahl
FROM dbo.OperationConfigs GROUP BY UPPER(LTRIM(RTRIM(OperationName))) HAVING COUNT(*) > 1;

-- P3 Kollision Stammdaten-Eintrag <-> WMS-Kuerzel / Werkbank
SELECT c.OperationName, c.DueDateOffsetDays, wo.Anzahl
FROM dbo.OperationConfigs c
JOIN (SELECT OperationNumber, COUNT(*) AS Anzahl FROM dbo.WorkOperations GROUP BY OperationNumber) wo
  ON UPPER(LTRIM(RTRIM(wo.OperationNumber))) = UPPER(LTRIM(RTRIM(c.OperationName)));
SELECT c.OperationName, wp.Name AS Werkbank
FROM dbo.OperationConfigs c
JOIN dbo.ProductionWorkplaces wp ON UPPER(LTRIM(RTRIM(wp.ArbeitsschrittCode))) = UPPER(LTRIM(RTRIM(c.OperationName)));

-- P4 WMS-Kuerzel (Quelle der Liste "Nicht konfigurierte Kuerzel"), inkl. '01'
SELECT OperationNumber, COUNT(*) AS Anzahl, MIN(CreatedBy) AS AngelegtVon
FROM dbo.WorkOperations GROUP BY OperationNumber ORDER BY Anzahl DESC;

-- P5 BdeAktiv
SELECT [Key], [Value] FROM dbo.AppSettings WHERE [Key] = N'BdeAktiv';
```

Ergebnis AKE: ______ Ergebnis IDEAL: ______

P-T1 — AKE-WMS-DB (TS-90.7/90.9), inklusive OSEON-Kollision und Befund M1:

```sql
SELECT wo.OperationNumber, COUNT(*) AS Anzahl, MIN(wo.CreatedBy) AS AngelegtVon,
       SUM(CASE WHEN wo.IsReported = 0 THEN 1 ELSE 0 END) AS Offen
FROM dbo.WorkOperations wo GROUP BY wo.OperationNumber ORDER BY Anzahl DESC;
SELECT [Key], [Value] FROM dbo.AppSettings WHERE [Key] IN ('BdeAktiv', 'BdeNurFaMeldung');
-- nur '01' bzw. BdeNurFaMeldung = true => an der AKE greift nur der NurFA-Zweig (TS-90.9)

-- Kollision OSEON-AG-Namen <-> AKE-BDE-Kuerzel: leer => kein Thema; Treffer => VOR der AKE-Abnahme melden
SELECT c.OperationName, c.DueDateOffsetDays, wo.Anzahl
FROM dbo.OperationConfigs c
JOIN (SELECT OperationNumber, COUNT(*) AS Anzahl FROM dbo.WorkOperations GROUP BY OperationNumber) wo
  ON UPPER(LTRIM(RTRIM(wo.OperationNumber))) = UPPER(LTRIM(RTRIM(c.OperationName)));

-- M1: erwartet 0, sonst melden (FE_Termin ginge an der AKE vor ProductionDate)
SELECT COUNT(*) AS FaHierarchyOrderInfos FROM dbo.FaHierarchyOrderInfos;
```

Ergebnis: ______

P-T2 — IDEAL-WMS-DB (TS-90.6):

```sql
SELECT COUNT(*) AS Auftraege, SUM(CASE WHEN ProductionDate IS NULL THEN 1 ELSE 0 END) AS OhneProductionDate
FROM dbo.ProductionOrders WHERE IsDone = 0 AND IsCancelled = 0;
```

Ergebnis: ______

Server-Kultur (TS-86, [[2026-10-06-csv-export-alle-tabellen-spec]] Abschnitt Deploy) — **beide Testsysteme**, im
Browser: Menü Fertigungsaufträge → FA-Liste, Spalte „Stk.“. `1,000` ⇒ deutsch, weiter. `1.000` im Sinne von eins ⇒
**anhalten**, Rückfrage in die CSV-Spec, TS-86 nicht abnehmen. Ergebnis IDEAL: ______ AKE: ______

**A3 — Testdaten (IDEAL-WMS-DB).** Kein Treffer für einen Fall: beim Platzhalter `KEIN_FALL` eintragen; Cowork wertet
den Schritt dann als NICHT PRÜFBAR. `{…}` in den Abfragen durch den zuvor ermittelten Wert ersetzen.

```sql
-- Schalter-Stand (IDEAL; dieselbe Abfrage auf der AKE-DB fuer {AKE_BDE_AKTIV_START}, {AKE_NURFA})
SELECT [Key],[Value] FROM dbo.AppSettings
WHERE [Key] IN ('ProduktionsauftragHierarchisch','FaHierarchyBeschichtungAktiv','FaHierarchyKommissionierlistenAktiv',
                'LeitstandAktiv','BestellungenAktiv','BdeAktiv','BdeNurFaMeldung','BdeMehrfachBuchungProArbeitsgang',
                'SageRueckmeldungAktiv');

-- {HFA_MEHRFACH}: gleiche beschichtete Artnr mehrfach UND >= 2 verschiedene beschichtete Artnr
SELECT TOP 10 HauptFA, COUNT(DISTINCT Artnr) AS ArtnrAnzahl, COUNT(*) AS Positionen
FROM dbo.FaHierarchyNodes WHERE Beschichtet = 1
GROUP BY HauptFA HAVING COUNT(DISTINCT Artnr) >= 2 AND COUNT(*) > COUNT(DISTINCT Artnr)
ORDER BY Positionen;
-- {ARTNR_MEHRFACH}, {ANZAHL_MEHRFACH} (= Spalte "Positionen"), {SUMME_MEHRFACH} (= "Summe Sollmenge")
SELECT Artnr, COUNT(*) AS Anzahl, SUM(Sollmenge) AS Summe FROM dbo.FaHierarchyNodes
WHERE Beschichtet = 1 AND HauptFA = {HFA_MEHRFACH} GROUP BY Artnr ORDER BY Anzahl DESC;

-- {ARTNR_ZWEI_HFA} in {HFA_X} und {HFA_Y}
SELECT TOP 10 Artnr, COUNT(DISTINCT HauptFA) AS HauptFAs, MIN(HauptFA) AS HFA_X, MAX(HauptFA) AS HFA_Y
FROM dbo.FaHierarchyNodes WHERE Beschichtet = 1 GROUP BY Artnr HAVING COUNT(DISTINCT HauptFA) > 1;

-- {HFA_MASSE} / {ARTNR_MASSE}: Masse uneinheitlich (leer => KEIN_FALL)
SELECT HauptFA, Artnr FROM dbo.FaHierarchyNodes WHERE Beschichtet = 1
GROUP BY HauptFA, Artnr HAVING COUNT(DISTINCT CONCAT(Breite,'|',Hoehe,'|',Tiefe)) > 1;

-- {HFA_SUBEBENE} / {ARTNR_SUBEBENE}: beschichtete Position mit SubFA <> 0 (TS-81.6)
SELECT TOP 5 HauptFA, VaterFA, SubFA, Artnr, Sollmenge FROM dbo.FaHierarchyNodes
WHERE Beschichtet = 1 AND SubFA <> 0;

-- {HFA_KOMBI}: mehrere Kopfzeilen, mit beschichteten Positionen
SELECT i.HauptFA, COUNT(*) AS Kopfzeilen FROM dbo.FaHierarchyOrderInfos i
WHERE EXISTS (SELECT 1 FROM dbo.FaHierarchyNodes n WHERE n.HauptFA = i.HauptFA AND n.Beschichtet = 1)
GROUP BY i.HauptFA HAVING COUNT(*) > 1;

-- {HFA_OHNE_KOPF}: beschichtete Positionen, keine Kopfzeile
SELECT DISTINCT TOP 5 n.HauptFA FROM dbo.FaHierarchyNodes n
WHERE n.Beschichtet = 1 AND NOT EXISTS (SELECT 1 FROM dbo.FaHierarchyOrderInfos i WHERE i.HauptFA = n.HauptFA);

-- {HFA_OHNE_DL}: genau eine Kopfzeile ohne Dienstleister, mit beschichteten Positionen
SELECT TOP 5 i.HauptFA FROM dbo.FaHierarchyOrderInfos i
WHERE EXISTS (SELECT 1 FROM dbo.FaHierarchyNodes n WHERE n.HauptFA = i.HauptFA AND n.Beschichtet = 1)
GROUP BY i.HauptFA HAVING COUNT(*) = 1 AND MAX(ISNULL(LTRIM(RTRIM(i.Dienstleister)),'')) = '';

-- {HFA_VIELE}: viele beschichtete Positionen (mehrseitig ab ca. 35)
SELECT TOP 5 HauptFA, COUNT(*) AS Positionen FROM dbo.FaHierarchyNodes WHERE Beschichtet = 1
GROUP BY HauptFA ORDER BY COUNT(*) DESC;

-- {HFA_NEU}, {ANZAHL_NEU}: 4-15 beschichtete Positionen, >= 2 Artnr, eine Kopfzeile MIT Dienstleister,
-- noch kein Auftrag (Status 90 = Storniert zaehlt nicht)
SELECT TOP 10 n.HauptFA, COUNT(*) AS Positionen, COUNT(DISTINCT n.Artnr) AS ArtnrAnzahl
FROM dbo.FaHierarchyNodes n
JOIN (SELECT HauptFA FROM dbo.FaHierarchyOrderInfos GROUP BY HauptFA
      HAVING COUNT(*) = 1 AND MAX(ISNULL(LTRIM(RTRIM(Dienstleister)),'')) <> '') i ON i.HauptFA = n.HauptFA
WHERE n.Beschichtet = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.Beschichtungsauftraege b WHERE b.HauptFA = n.HauptFA AND b.Status <> 90)
GROUP BY n.HauptFA HAVING COUNT(*) BETWEEN 4 AND 15 AND COUNT(DISTINCT n.Artnr) >= 2
ORDER BY Positionen;
-- {DL_SAGE_NEU}
SELECT Dienstleister FROM dbo.FaHierarchyOrderInfos WHERE HauptFA = {HFA_NEU};

-- {HFA_KTZ}: Struktur der P-K1-Stichprobe (TS-85.2)
SELECT TOP 1 HauptFA FROM dbo.FaHierarchyNodes WHERE Artnr = 'G-KT-A-BODEN-1320-660-V01';

-- {HFA_KUERZEL_OHNE_WB}: Struktur, die {KUERZEL_OHNE_WB} (aus P-K2 b, Werkbaenke = 0) traegt
SELECT TOP 3 HauptFA, Artnr, Arbeitsschritte FROM dbo.FaHierarchyNodes
WHERE ' ' + Arbeitsschritte + ' ' LIKE '% {KUERZEL_OHNE_WB} %';

-- {HFA_8_KUERZEL}: Zeile mit >= 8 Kuerzeln (setzt einfache Leerzeichen voraus)
SELECT TOP 5 HauptFA, Artnr, Arbeitsschritte FROM dbo.FaHierarchyNodes
WHERE LEN(LTRIM(RTRIM(Arbeitsschritte))) - LEN(REPLACE(LTRIM(RTRIM(Arbeitsschritte)),' ','')) >= 7;

-- {HFA_NICHT_MAT}: Struktur ohne materialisierten Auftrag (leer => KEIN_FALL)
SELECT DISTINCT TOP 5 n.HauptFA FROM dbo.FaHierarchyNodes n
WHERE NOT EXISTS (SELECT 1 FROM dbo.ProductionOrders p WHERE p.OrderNumber = CAST(n.HauptFA AS nvarchar(20)));

-- {ID_BOM_ZIELE}: Id der HauptFA-Zeile einer Stueckliste mit Komm.-Zielen (wie ID_HFA_ZIELE im TS-79-Protokoll)
SELECT TOP 10 HauptFA,
       SUM(CASE WHEN LTRIM(RTRIM(ISNULL(Kommissionieren,''))) <> '' THEN 1 ELSE 0 END) AS MitZiel
FROM dbo.FaHierarchyNodes GROUP BY HauptFA
HAVING SUM(CASE WHEN LTRIM(RTRIM(ISNULL(Kommissionieren,''))) <> '' THEN 1 ELSE 0 END) > 0 ORDER BY MitZiel DESC;
SELECT Id FROM dbo.ProductionOrders WHERE OrderNumber = '<HauptFA>' AND SubOrderNumber = OrderNumber;

-- {ANZAHL_BEWEGUNGEN} (TS-86.10 braucht > 5000; sonst KEIN_FALL)
SELECT COUNT(*) FROM dbo.StockMovements;

-- BDE: Werkbaenke mit Kuerzel ({WERKBANK_BDE}: BdeAktiv = 1 mit offenen AG; {KUERZEL_NICHT_BDE}: Kuerzel einer
--      Werkbank mit BdeAktiv = 0, z. B. PG; {WERKBANK_LEER}: BdeAktiv = 1 ohne offene AG)
SELECT wp.Id, wp.Name, wp.ArbeitsschrittCode, wp.BdeAktiv,
       (SELECT COUNT(*) FROM dbo.WorkOperations wo WHERE wo.ProductionWorkplaceId = wp.Id AND wo.IsReported = 0) AS OffeneAG
FROM dbo.ProductionWorkplaces wp WHERE wp.ArbeitsschrittCode IS NOT NULL ORDER BY wp.BdeAktiv DESC, wp.Name;

-- BDE: offene Arbeitsgaenge an {WERKBANK_BDE} mit Termin (FE_Termin, sonst ProductionDate) -> {AG_ZUKUNFT},
--      {AG_UEBERFAELLIG}, {AG_ABSCHLUSS}, {FA_OHNE_TERMIN}, {HFA_BDE}, ggf. {HFA_KOMBI_BDE} (Kopfzeilen > 1)
SELECT po.Id, po.OrderNumber, po.SubOrderNumber, wo.OperationNumber, wo.Sequence,
       oi.FE_Termin, oi.Kopfzeilen, po.ProductionDate
FROM dbo.WorkOperations wo
JOIN dbo.ProductionOrders po ON po.Id = wo.ProductionOrderId
JOIN dbo.ProductionWorkplaces wp ON wp.Id = wo.ProductionWorkplaceId
OUTER APPLY (SELECT MIN(i.FE_Termin) AS FE_Termin, COUNT(*) AS Kopfzeilen FROM dbo.FaHierarchyOrderInfos i
             WHERE CAST(i.HauptFA AS nvarchar(20)) = po.OrderNumber) oi
WHERE wo.IsReported = 0 AND wp.Name = '{WERKBANK_BDE}' AND po.IsDone = 0 AND po.IsCancelled = 0
ORDER BY COALESCE(oi.FE_Termin, po.ProductionDate), po.OrderNumber, wo.Sequence;

-- BDE: Werker A/B, Terminal des Browser-Benutzers G, Offsets, Feiertage
SELECT PersonnelNumber, FirstName, LastName FROM dbo.BdeOperators WHERE IsActive = 1;
SELECT t.Id, u.Name AS Benutzer, wp.Name AS Werkbank FROM dbo.BdeTerminals t
JOIN dbo.Users u ON u.Id = t.UserId JOIN dbo.ProductionWorkplaces wp ON wp.Id = t.DefaultProductionWorkplaceId;
SELECT OperationName, DueDateOffsetDays FROM dbo.OperationConfigs ORDER BY OperationName;
SELECT TOP 10 [Date], Description FROM dbo.Holidays WHERE [Date] >= DATEADD(day, -30, GETDATE()) ORDER BY [Date];

-- Rollen der Testbenutzer
SELECT r.[Key], u.Name FROM dbo.Roles r
LEFT JOIN dbo.UserRoles ur ON ur.RoleId = r.Id LEFT JOIN dbo.Users u ON u.Id = ur.UserId
WHERE r.[Key] IN ('admin','beschichtungsauftrag','beschichtungsauftrag_schreiben','leitstand','bde_user',
                  'bde_shiftlead','masterdata','stock','stock_read','picking')
ORDER BY r.[Key], u.Name;
```

Auswahl für die BDE-Platzhalter (Format „FA/Sub-FA + Kürzel“): `{AG_ZUKUNFT}` = Termin in der Zukunft,
`{AG_UEBERFAELLIG}` = Termin vor heute, `{AG_ABSCHLUSS}` = beliebiger offener AG (am besten der **letzte** offene AG
seines Auftrags, dann prüft B33 auch „Auftrag verschwindet“), alle drei möglichst in der Struktur `{HFA_BDE}`;
`{FA_OHNE_TERMIN}` = Auftrag ohne FE_Termin und ohne ProductionDate (sonst `KEIN_FALL`). `{KUERZEL_OFFSET}` /
`{OFFSET_WERT}` = Kürzel mit Offset ≠ 0 (z. B. `KA`), `{KUERZEL_OHNE_EINTRAG}` = Kürzel **ohne** Stammdaten-Eintrag
(z. B. `SW`). `{KUERZEL_TEST}` = ein Kürzel, das es weder in `OperationConfigs` noch in `WorkOperations` gibt (z. B.
`ZZ`) — **bewusste Abweichung vom TS-87.3-Text** (dort `KA`), damit der `KA`-Eintrag für TS-88/90 stehen bleibt.
`{MEHRFACH_PRO_AG}` = Wert von `BdeMehrfachBuchungProArbeitsgang`.

AKE-Testdaten (AKE-WMS-DB, dieselben BDE-Abfragen): `{AKE_WERKBANK_BDE}`, `{AKE_PNR}`. `{OSEON_VERGLEICH}` = deine
Notizen zu Ampeln/Soll-Terminen/Reporting **vor** dem Deploy (TS-87.7); fehlen sie, `KEIN_FALL`.

**A4 — Einrichtung.**

- Rolle `beschichtungsauftrag_schreiben` vorhanden (Stammdaten → Rollen; neu mit v1.51.0, im Deploy anzulegen) und
  Benutzer S zugewiesen; `/Users/RoleOverview` listet sie.
- IDEAL-Einstellungen: `ProduktionsauftragHierarchisch = true`; `FaHierarchyBeschichtungAktiv`,
  `FaHierarchyKommissionierlistenAktiv`, `LeitstandAktiv` an. Edge auf dem Webserver (PDF).
- Briefkopf: Ordner `Branding\` **neben** dem Web-Publish-Ordner mit `briefkopf.html`,
  `IDEAL_AKE_Logo_rgb_klein.jpg` (aus dem Repo `Branding/`) und den Nimbus-Schriftdateien (Dateinamen in `@font-face`
  angepasst); App-Pool-Identität hat Leserecht.
- Stammdaten → Standorteinstellungen → Firmendaten: „Briefkopf-Vorlage (Pfad)“ = voller Pfad zu `briefkopf.html`;
  Firmenname und Firmenanschrift („In der Schörihub 28, 4810 Gmunden“) gepflegt.
- BDE (IDEAL): Werker A und B als BDE-Operatoren mit Personalnummer `{PNR_A}`/`{PNR_B}`; für Benutzer G ein
  BDE-Terminal (Standard-Werkbank `{WERKBANK_BDE}`); `{WERKBANK_BDE}` BDE-aktiv, die Werkbank von
  `{KUERZEL_NICHT_BDE}` nicht; `{KUERZEL_OFFSET}` mit Offset ≠ 0 gepflegt, `{KUERZEL_OHNE_EINTRAG}` ohne Eintrag;
  Feiertage gepflegt. AKE entsprechend ein Terminal für G an `{AKE_WERKBANK_BDE}`.
- **Rückmeldung an Fremdsysteme klären:** B33 erzeugt eine echte Endmeldung. Prüfe vorher `SageRueckmeldungAktiv`
  und wohin der Service des Testsystems zurückmeldet — schreibt er in ein produktives Sage, B33 nicht freigeben.

**A5 — Testbenutzer** (du meldest sie an den BENUTZERWECHSEL-Marken an; Cowork meldet sich nie an):

| Kürzel | Rollen | Wofür |
|---|---|---|
| **G** | `admin` (dein Konto) | Block 1 und 5: FA-Struktur, CSV, Arbeitsgang-Stammdaten, Einstellungen, BDE-Terminal (Terminal zugeordnet); AKE-Abschnitt |
| **S** | nur `beschichtungsauftrag_schreiben` | Block 2: Vorschlagsliste, Briefkopf, Aufträge anlegen/bearbeiten |
| **L** | nur `beschichtungsauftrag` | Block 3: Lesen ohne Schreib-Knöpfe, `/FaHierarchy` verweigert |
| **N** | `leitstand` + `bde_shiftlead` + `masterdata`; **ohne** `beschichtungsauftrag*`, `admin`, `stock`, `stock_read`, `picking` | Block 4: Beschichtung gesperrt, BDE-Stammdaten ohne Reiter „Arbeitsgänge“, Bestände gesperrt |
| **Werker A / B** | BDE-Operatoren (keine Web-Benutzer) | B27–B33: am Terminal von G per Personalnummer gescannt |

Kein Testbenutzer hat einen gespeicherten Standard-Filter (Profil), der die Listen vorab einschränkt.

---

## Teil B — Auftrag an Claude Cowork (ab hier kopieren)

```text
ABNAHME - Browser-Testprotokoll TS-81 bis TS-90 (IDEAL-Buendel v1.48.0-v1.57.0)
Protokoll: C:\Git\IDEAL-AKE-WMS\secondbrain\tests\2026-10-13-ts-81-90-browser-testprotokoll.md
Specs: siehe Frontmatter des Protokolls (10 Specs)
Adressen: IDEAL http://idealweb01.ideal.ideal-ake.at:88/ (Bloecke 1-5, Schritte B1-B71)
          AKE   https://akenet01.ake.at:4444/            (NUR im Abschnitt "AKE", Schritte K1-K9)
Es gelten die Regeln der Arbeitsweise ABNAHME aus den Projektanweisungen. Du pruefst, du reparierst nichts.

FREIGABEN (von Gerald eingetragen; nur diese SCHREIBEND-Schritte darfst du ausfuehren):
{FREIGABEN}
Ein Schritt "SCHREIBEND - nur mit Freigabe Gerald", dessen Nummer hier NICHT steht, ist NICHT PRUEFBAR.
Haengt ein Schritt von einem nicht freigegebenen oder nicht gelaufenen Schritt ab, ist er ebenfalls
NICHT PRUEFBAR.

ZUSAETZLICH FUER DIESEN LAUF
- BENUTZERWECHSEL: An dieser Marke anhalten, im Chat "Bitte Benutzer <X> anmelden" schreiben und erst nach
  Geralds "weiter" fortfahren. Du meldest dich nie selbst an.
- Downloads (PDF, CSV): vor JEDEM Download im Chat fragen. Ohne Ja ist dieser Schritt-Teil NICHT PRUEFBAR.
  Nach dem Download den Dateinamen notieren; den Inhalt nur pruefen, wenn du die Datei ohne Zusatzprogramm
  ansehen kannst - sonst "Inhalt -> Teil C" vermerken.
- Druck: "Drucken" oeffnet eine Druckansicht (neuer Tab). Oeffnet sich ein Druckdialog: Inhalt bzw. Vorschau
  ansehen und mit Escape schliessen. Nie drucken.
- Bestaetigungsdialoge (z. B. "... stornieren?") nur im freigegebenen Schritt annehmen, der sie nennt.
- Im Leitstand, in der Kommissionierung und in Stuecklisten nichts freigeben, zuweisen, abhaken oder buchen.
- Am BDE-Terminal ausserhalb von B29-B33 keinen Auftrags- oder Arbeitsgang-Knopf antippen (ein Antippen kann
  sofort eine Buchung starten). Personalnummer eingeben + "Scannen", "Wechseln" und die Werkbank-Auswahl oben
  sind erlaubt.
- Einstellungen, Standorteinstellungen, Benutzer, Rollen, Profil: nur ansehen, ausser in B23/B24/B70/K6/K8.

TESTDATEN (von Gerald ausgefuellt; KEIN_FALL = Schritt bzw. Schritt-Teil NICHT PRUEFBAR)
IDEAL, Vorschlagsliste/Briefkopf:
- HauptFA mit Mehrfach-Artnr: {HFA_MEHRFACH}, Artnr {ARTNR_MEHRFACH} (erwartet: Positionen {ANZAHL_MEHRFACH},
  Summe Sollmenge {SUMME_MEHRFACH})
- Artnr in zwei HauptFA: {ARTNR_ZWEI_HFA} in {HFA_X} und {HFA_Y}
- Masse uneinheitlich: HauptFA {HFA_MASSE}, Artnr {ARTNR_MASSE}
- Beschichtete Position auf Sub-FA-Ebene: HauptFA {HFA_SUBEBENE}, Artnr {ARTNR_SUBEBENE}
- Kombigeraet: {HFA_KOMBI}; ohne Kopfzeile: {HFA_OHNE_KOPF}; ohne Dienstleister: {HFA_OHNE_DL}
- Viele Positionen (mehrseitig): {HFA_VIELE}
- Neuer Auftrag: HauptFA {HFA_NEU} (Positionen {ANZAHL_NEU}, Dienstleister in Sage {DL_SAGE_NEU})
IDEAL, FA-Struktur/CSV:
- Struktur der Stichprobe: {HFA_KTZ}; Kuerzel ohne Werkbank: {KUERZEL_OHNE_WB} in {HFA_KUERZEL_OHNE_WB}
- Zeile mit >= 8 Kuerzeln: {HFA_8_KUERZEL}; Struktur ohne Auftrag: {HFA_NICHT_MAT}
- Stueckliste mit Komm.-Zielen: /Picking/Bom/{ID_BOM_ZIELE}; Anzahl Lagerbewegungen: {ANZAHL_BEWEGUNGEN}
IDEAL, Arbeitsgaenge/BDE:
- BdeAktiv zu Beginn: {BDE_AKTIV_START}; BdeMehrfachBuchungProArbeitsgang: {MEHRFACH_PRO_AG}
- Testkuerzel: {KUERZEL_TEST}; Kuerzel mit Offset: {KUERZEL_OFFSET} (Offset {OFFSET_WERT});
  Kuerzel ohne Eintrag: {KUERZEL_OHNE_EINTRAG}; Kuerzel an nicht BDE-aktiver Werkbank: {KUERZEL_NICHT_BDE}
- Werkbank BDE-aktiv: {WERKBANK_BDE}; Werkbank ohne offene AG: {WERKBANK_LEER}
- Werker A: {PNR_A}; Werker B: {PNR_B}
- Struktur mit BDE-Arbeitsgaengen: {HFA_BDE}; Kombigeraet mit BDE-AG: {HFA_KOMBI_BDE}
- AG mit kuenftigem Termin: {AG_ZUKUNFT}; AG ueberfaellig: {AG_UEBERFAELLIG}; AG zum Abschliessen: {AG_ABSCHLUSS}
  (je "FA/Sub-FA + Kuerzel"); Auftrag ohne Termin: {FA_OHNE_TERMIN}
AKE:
- BdeAktiv zu Beginn: {AKE_BDE_AKTIV_START}; NurFA (BdeNurFaMeldung): {AKE_NURFA}
- Werkbank BDE: {AKE_WERKBANK_BDE}; Werker: {AKE_PNR}; OSEON-Vergleichswerte: {OSEON_VERGLEICH}

VOR DEM START
Version in der Fusszeile notieren. Folgende Ansichten kurz oeffnen und Spaltenauswahl sowie Seitengroesse
notieren: FA-Liste, FA-Struktur, Vorschlagsliste (Summiert und "Liste"), Auftraege, BDE Stammdaten ->
Arbeitsgaenge, Bewegungshistorie. Am Ende (B71) wiederherstellen. Am AKE-System dasselbe vor K1 (K9).

=============================== IDEAL ===============================

BLOCK 1 - Benutzer G (Admin)
BENUTZERWECHSEL: "Bitte Benutzer G anmelden." (entfaellt, wenn G schon angemeldet ist)

B1 Menue "Beschichtung" (TS-89.1, TS-89.2 Teil)
Menueleiste ansehen.
OK, wenn es genau einen Menuepunkt "Beschichtung" zwischen "FA-Struktur" und "Vormontage" gibt und KEINE
Eintraege "Beschichtungsauftrag"/"Beschichtungsauftraege" mehr in der Leiste. Aufklappen: genau zwei
Eintraege "Vorschlagsliste" und "Auftraege", keine Trennlinie. Dann Menue FA-Struktur und Menue
Fertigungsauftraege -> FA-Liste oeffnen: "Beschichtung" ist dort NICHT markiert. Screenshot.

B2 FA-Struktur: Arbeitsgang-Symbole sichtbar (TS-85.1)
Menue FA-Struktur, Struktur {HFA_KTZ} suchen (Spaltenfilter) und aufklappen.
OK, wenn die Spalte "Arbeitsgaenge" rechts neben "Arbeitsbereich" steht und je Zeile die Kuerzel als gleich
aussehende Umriss-Kaestchen in Feld-Reihenfolge zeigt; Mauszeiger auf ein Kuerzel zeigt "<Kuerzel> -
<Werkbank-Name>". Bei {BDE_AKTIV_START} = false: keine Farbe, kein Status-Zeichen. Bei true: Farben nur
notieren (die Status prueft B26). Screenshot.

B3 FA-Struktur: Abgleich der Stichprobe (TS-85.2)
Gleiche Struktur.
OK, wenn: A-BODEN "LS KA PG", FR-4301-...-1530 "ZS", Mittelholmblende "VM", Korpus-Gepunktet "BE"; die
Sub-FA-Zeile Korpus-1320-660-760 nur "PG" (nicht die Kuerzel ihrer Kinder); die Wurzel ohne Kuerzel zeigt
einen Strich. Abweichungen je Zeile notieren. (Der Abgleich mit der Sage-Maske ist Teil C.)

B4 FA-Struktur: Kuerzel ohne Werkbank (TS-85.3)
Struktur {HFA_KUERZEL_OHNE_WB}, Zeile mit {KUERZEL_OHNE_WB}.
OK, wenn das Kuerzel wie jedes andere erscheint, der Tooltip NUR das Kuerzel nennt (kein Name) und keine
Fehlermeldung erscheint.

B5 FA-Struktur: Tastatur (TS-85.4, nur Tastatur-Teil)
In einer aufgeklappten Zeile mit Tab durch die Kuerzel gehen.
OK, wenn jedes Kuerzel einen sichtbaren blauen Fokusrahmen bekommt. Screenshot.

B6 FA-Struktur: viele Kuerzel (TS-85.5)
Struktur {HFA_8_KUERZEL}, Zeile mit >= 8 Kuerzeln; Browserfenster schmaler ziehen, falls moeglich.
OK, wenn die Kuerzel in der Zelle umbrechen und die Zeile keine waagrechte Scrollleiste bekommt.

B7 FA-Struktur: Filter und Spaltenauswahl (TS-85.7)
Filterzeile "Arbeitsgaenge" (Platzhalter "z. B. KA"): ka eingeben.
OK (Teil 1), wenn nur Zeilen mit KA samt ihrem Pfad bleiben. Filter loeschen.
Zahnrad: Spalte "Arbeitsgaenge" aus- und wieder einblenden, dann verschieben, neu laden.
OK (Teil 2), wenn die Einstellung nach dem Neuladen erhalten ist. Danach die notierte Spaltenauswahl
wiederherstellen.

B8 FA-Struktur: Modal und Regression (TS-85.8, TS-85.11)
An der Kopfzeile von {HFA_KTZ} den Knopf "Arbeitsgaenge" klicken.
OK, wenn das Fenster Zaehlzeile, Strich-Zeilen und je Arbeitsgang eine Zeile mit Kuerzel/Werkbank/Sage-Nr.
zeigt (wie TS-78). Fenster schliessen. "Alle auf"/"Alle zu" und das Hervorheben-Suchfeld funktionieren wie
gewohnt; Spalten Arbeitsbereich, Komm.-Ziel, Status unveraendert.

B9 FA-Struktur ohne materialisierten Auftrag (TS-85.10 Teil 2)
Struktur {HFA_NICHT_MAT}.
OK, wenn die Kuerzel sichtbar sind und kein Fehler erscheint.

B10 CSV-Knopf: wo er steht und wo nicht (TS-86.1 Negativ, TS-86.3, TS-86.4, TS-86.12, TS-86.14 - nur Knopf)
Nacheinander oeffnen und nur pruefen, ob ein Knopf "CSV" neben dem Zahnrad steht (nicht klicken):
FA-Liste, Leitstand -> Kommissionierung, FA-Vervollstaendigen, FA-Abarbeitungsliste, Kommissionierung ->
Kommissionierung, Kommissionierlisten (Liste und Summiert), Vormontage (beide Ansichten), Beschichtung ->
Vorschlagsliste (Summiert und "Liste"), Beschichtung -> Auftraege, Stammdaten -> Artikel, Lagerplaetze,
Benutzer, Artikelgruppen-Zuordnung (falls im Menue).
OK, wenn ueberall ein CSV-Knopf steht und es auf FA-Struktur, Teileverfolgung -> Rueckmeldungen (Baum),
OSEON Auftraege (falls im Menue) und in Druckansichten KEINEN gibt. Tabelle Ansicht | Knopf ja/nein.

B11 CSV FA-Liste: Grundfall und Spaltenauswahl sofort (TS-86.1, TS-86.2)
FA-Liste, Spaltenfilter "Werkbank" auf einen Wert mit mehreren Seiten, Seitengroesse 25; "von N" notieren.
Per Zahnrad eine Spalte ausblenden und eine andere nach vorn ziehen, OHNE neu zu laden. Dann "CSV"
(Download - vorher fragen).
OK, wenn der Knopf waehrend des Abrufs deaktiviert ist (Kreisel) und eine Datei
"<Ansicht>_JJJJ-MM-TT_HHMM.csv" entsteht; Zeilenzahl = N, Spalten = sichtbare Spalten in
Bildschirm-Reihenfolge ohne Aktionsspalte (sofern ansehbar, sonst Inhalt -> Teil C). Filter loeschen,
Spaltenauswahl und Seitengroesse wiederherstellen.

B12 CSV Stueckliste im Client-Mode (TS-86.12)
Adresse /Picking/Bom/{ID_BOM_ZIELE}. Eine Baugruppe zuklappen, unter der Positionen mit Ziel liegen; im
Komm.-Ziel-Dropdown ein Ziel waehlen. "CSV" (Download - vorher fragen).
OK, wenn der Bildschirm danach unveraendert ist (Klappzustand, Filter) und die Datei auch die Treffer unter
der zugeklappten Baugruppe enthaelt (sofern ansehbar). Filter wieder entfernen. Nichts abhaken.

B13 CSV-Kappung Bewegungshistorie (TS-86.10) - nur wenn {ANZAHL_BEWEGUNGEN} > 5000
Menue Lager -> Bewegungshistorie, keine Filter, Datum absteigend sortieren, "CSV" (Download - vorher fragen).
OK, wenn neben dem Knopf die gelbe Meldung "Export auf 5.000 Treffer begrenzt, bitte Filter eingrenzen.
Sortierung gilt nur innerhalb der exportierten 5.000." erscheint und trotzdem eine Datei entsteht.
Gegenprobe mit einem Filter, der < 5000 Treffer laesst: keine Meldung. Filter loeschen.

B14 Bildschirm-Sortierung unveraendert (TS-86.15, Regressionsteil)
FA-Liste: auf eine Datums-, eine Zahl- und eine Textspalte klicken; Leitstand: Gruppen bleiben beisammen;
Kombigeraet-Variantentabelle (falls vorhanden) bleibt gefuellt.
OK, wenn die Sortierung wie gewohnt wirkt. (Reihenfolge im CSV -> Teil C.)

B15 Kommissionierliste ohne Briefkopf (TS-82.13)
Menue Kommissionierung -> Kommissionierlisten, "Drucken" (Summiert und Liste).
OK, wenn die Druckansicht KEINEN Briefkopf (Logo, Absenderzeile) zeigt. Tabs schliessen.

B16 Standorteinstellungen: Feld Briefkopf (TS-82.12)
Menue Stammdaten -> Standorteinstellungen, Bereich Firmendaten ansehen (NICHTS speichern).
OK, wenn es "Briefkopf-Vorlage (Pfad)" mit Hinweistext gibt und "Anschrift / Adresse" ein einzeiliges Feld
ist. Den eingetragenen Pfad notieren.

B17 Hilfe: Kuerzel und CSV (TS-89.11)
Fusszeile -> Hilfe. Abschnitte zu den Arbeitsgang-Kuerzeln in der FA-Struktur und zum CSV-Export suchen.
OK, wenn beide mit nummerierten Schritten beginnen, die Schritte zu B2/B7/B8/B11 passen (Knopf- und
Menuetexte) und die Status-Punkte aus v1.55.0 genau einmal stehen.

B18 Menue mit Tastatur (TS-89.8, Tastatur-Teil)
Nur Tastatur: Tab bis "Beschichtung", Enter oeffnet, Pfeiltasten waehlen, Esc schliesst.
OK, wenn das so geht und der markierte Menuepunkt aussieht wie "Bestellungen" im markierten Zustand.
(Hamburger-/Mobil-Menue -> Teil C.)

B19 Arbeitsgang-Stammdaten bei BDE aus (TS-87.1) - nur wenn {BDE_AKTIV_START} = false
Menue Stammdaten -> Arbeitsgaenge.
OK (Teil 1), wenn die Seite "BDE Stammdaten" heisst und genau den Reiter "Arbeitsgaenge" hat.
Adresse /BdeMasterData: OK (Teil 2), wenn die Startseite mit "BDE ist nicht aktiviert." erscheint.

B20 Arbeitsgang-Stammdaten: nicht konfiguriert, alte Adresse, Begriffe (TS-87.4, TS-87.5, TS-87.6)
Stammdaten -> Arbeitsgaenge.
OK (Teil 1), wenn die Karte "Nicht konfigurierte Kuerzel" die IDEAL-Kuerzel mit "WMS-Arbeitsgang" (bzw. "OSEON
+ WMS-Arbeitsgang") nennt, "01" nie vorkommt und das Anlege-Dropdown dieselben Kuerzel anbietet; Liste notieren.
Adresse /Settings/OperationConfig: OK (Teil 2), wenn sie auf diese Seite weiterleitet; in Stammdaten ->
Einstellungen fuehrt der Knopf "Arbeitsgaenge (BDE-Stammdaten)" ebenfalls hierher.
OK (Teil 3), wenn Spalte/Schalter "Fremdsystem-relevant", Feld "Offset (Arbeitstage) zum Bezugstermin" und
eine Legende mit Stanztermin (OSEON), FE-Termin (IDEAL), Fertigungstermin (AKE-BDE) samt dem Satz stehen,
dass Fremdsystem-relevant nur fuer OSEON wirkt.

B21 Arbeitsgang-Stammdaten: Liste (TS-87.8, TS-87.10 Teil)
Spaltenfilter Kuerzel, Anzeigename, Offset (+2), Relevanz (nein) nacheinander; Seitengroesse "Alle";
Seitenwechsel; "CSV" (Download - vorher fragen). Dann ein Filter ohne Treffer.
OK, wenn die Filter greifen, Seitengroesse/Seitenwechsel funktionieren, die CSV alle Spalten mit
Ueberschrift hat (sofern ansehbar) und der Filter ohne Treffer eine Hinweiszeile statt eines Fehlers zeigt.
Filter loeschen, Seitengroesse wiederherstellen.

B22 SCHREIBEND - nur mit Freigabe Gerald. Arbeitsgang pflegen mit Testkuerzel (TS-87.3)
Bereich "Neuen Arbeitsgang anlegen": Kuerzel {KUERZEL_TEST}, Anzeigename "Test Abnahme", Offset +3,
Fremdsystem-relevant aus -> "Arbeitsgang anlegen".
OK (Teil 1), wenn eine gruene Meldung kommt und die Zeile in der Liste erscheint. Anzeigename in der Zeile
aendern, Speichern-Symbol der Zeile: gruene Meldung.
Negativ, jeweils "Arbeitsgang anlegen": leeres Kuerzel; {KUERZEL_TEST} in Kleinbuchstaben; {KUERZEL_TEST}
mit Leerzeichen davor und danach.
OK (Teil 2), wenn jeweils eine gelbe Meldung kommt und kein zusaetzlicher Eintrag entsteht.
Offset 0 und danach -1 in der Testzeile speichern: OK (Teil 3), wenn beides gespeichert wird.
NICHT loeschen (macht B69). Hinweis: Der TS-Text nennt KA - bewusst ersetzt, damit der KA-Eintrag fuer
B26-B33 bleibt.

B23 SCHREIBEND - nur mit Freigabe Gerald. Schalter Beschichtung aus und wieder an (TS-89.6, TS-81.13, TS-84.14)
Stammdaten -> Einstellungen, Gruppe IDEAL, FaHierarchyBeschichtungAktiv ausschalten, "Einstellungen
speichern".
OK (Teil 1), wenn danach in der Leiste kein "Beschichtung" steht und die Adressen
/FaHierarchyBeschichtung/Summiert, /FaHierarchyBeschichtung/PrintSummiert und /Beschichtungsauftraege
gesperrt sind (angezeigte Meldung notieren).
Sofort danach wieder einschalten und speichern. OK (Teil 2), wenn "Beschichtung" zurueck ist.

B24 SCHREIBEND - nur mit Freigabe Gerald. BdeAktiv einschalten (Vorbedingung fuer B25-B34 und B66)
Nur wenn {BDE_AKTIV_START} = false: Stammdaten -> Einstellungen, Gruppe BDE, BdeAktiv einschalten,
"Einstellungen speichern". Sonst nichts tun und "bereits an" notieren.
OK, wenn danach das Menue "BDE" erscheint.

B25 BDE-Stammdaten als Admin (TS-87.2 Admin-Teil, TS-86.14 Teil)
Menue BDE -> Stammdaten; dann Stammdaten -> Arbeitsgaenge.
OK, wenn die Reiter Operatoren, Aktivitaeten, Terminals, Arbeitsgaenge erscheinen, der Wechsel in beide
Richtungen geht, der aktive Reiter markiert ist und jeder Reiter einen CSV-Knopf hat (nicht klicken).

B26 FA-Struktur mit Status je Kuerzel - Ausgangslage (TS-88.3, TS-88.5, TS-88.6, TS-88.7, TS-88.8)
Menue FA-Struktur, Struktur {HFA_BDE} aufklappen; mit Tab auf die Kuerzel fuer die Ansage-Texte.
OK, wenn: offene Kuerzel mit Soll in der Zukunft grau mit leerem Kreis "<Kuerzel> - <Werkbank> - offen";
mit Soll vor heute rot mit Warndreieck "... - ueberfaellig (Soll TT.MM.JJJJ)"; {KUERZEL_NICHT_BDE} neutral
ohne Zeichen; ueber dem Baum der gelbe Hinweis "Fuer n Arbeitsgang-Kuerzel ist kein Termin-Offset
konfiguriert (Offset 0 angenommen): ..." nennt {KUERZEL_OHNE_EINTRAG}.
Struktur {HFA_KUERZEL_OHNE_WB}: {KUERZEL_OHNE_WB} neutral, Tab-Text "... - kein BDE-Arbeitsgang".
Kombigeraet {HFA_KOMBI_BDE}: Soll-Text bezieht sich auf den fruehesten FE-Termin, Kopfzeile ohne
Einzeltermin. Screenshot.

B27 BDE-Terminal: Bloecke und Sortierung (TS-90.1, TS-90.2, TS-90.6)
Menue BDE -> Terminal, Werkbank oben {WERKBANK_BDE}. Personalnummer {PNR_A} eingeben, "Scannen".
OK, wenn unter "2. Arbeitsgang waehlen" Bloecke je Auftrag stehen (Kopf: Auftrag "/ Sub-FA", Bezeichnung,
Termin bzw. "ohne Termin"), Reihenfolge: vergangener Termin, kuenftiger, ohne Termin ({FA_OHNE_TERMIN} am
Ende); je Block die AG in Sequenz-Reihenfolge; darunter "Ungeplante Taetigkeiten". {AG_UEBERFAELLIG}: Knopf
rot mit Warndreieck; {AG_ZUKUNFT}: grau mit leerem Kreis. Kuerzel {KUERZEL_OHNE_EINTRAG}: Soll = Termin, kein
Hinweis am Terminal. Termin des Blocks von {HFA_BDE} = FE-Termin aus B26 (Kombigeraet: fruehester). KEINEN
Knopf antippen. Screenshot.

B28 BDE-Terminal: Werkbank ohne offene AG (TS-90.11 Teil)
Oben Werkbank {WERKBANK_LEER} waehlen.
OK, wenn "Keine offenen Arbeitsgaenge an dieser Werkbank." erscheint. Zurueck auf {WERKBANK_BDE}.

B29 SCHREIBEND - nur mit Freigabe Gerald. Werker B startet (TS-90.3, TS-88.2)
"Wechseln", {PNR_B} scannen, Knopf {AG_ZUKUNFT} antippen, "Produktion starten".
OK (Teil 1), wenn der AG bei B unter "Aktuelle Buchungen" steht und nicht in der Liste.
"Wechseln", {PNR_A} scannen. OK (Teil 2), wenn {AG_ZUKUNFT} bei A blau mit Sanduhr "... - laeuft" zeigt.
In einem zweiten Tab /FaHierarchy, {HFA_BDE}: OK (Teil 3), wenn das Kuerzel dort ebenfalls blau "laeuft" zeigt.

B30 SCHREIBEND - nur mit Freigabe Gerald. Werker A trifft auf laufenden AG (TS-90.3)
Als A den Knopf {AG_ZUKUNFT} antippen, "Ruesten starten".
OK, wenn das Fenster "Arbeitsgang bereits in Arbeit" (... durch B ...) erscheint; mit OK schliessen.
Nur wenn {MEHRFACH_PRO_AG} = false: zusaetzlich "Produktion starten" -> OK, wenn ebenfalls das Fenster
erscheint. Bei true NICHT klicken (wuerde eine zweite Buchung starten), notieren.

B31 SCHREIBEND - nur mit Freigabe Gerald. Werker B pausiert (TS-90.3, TS-88.2)
"Wechseln", {PNR_B}; unter "Aktuelle Buchungen" "Pause", Gutmenge 0, Ausschuss 0, "Bestaetigen".
"Wechseln", {PNR_A}.
OK, wenn {AG_ZUKUNFT} bei A wieder grau "offen" ist und in /FaHierarchy (neu laden) ebenso.

B32 SCHREIBEND - nur mit Freigabe Gerald. Laeuft und ueberfaellig (TS-90.4, TS-88.3)
"Wechseln", {PNR_B}, Knopf {AG_UEBERFAELLIG}, "Produktion starten". "Wechseln", {PNR_A}.
OK, wenn der Knopf bei A Sanduhr UND Warndreieck mit rotem Rand zeigt ("... - laeuft, ueberfaellig (Soll
...)") und /FaHierarchy (neu laden) dasselbe zeigt. Screenshot. Danach als B pausieren (Gutmenge 0,
"Bestaetigen") und wieder auf A wechseln.

B33 SCHREIBEND - nur mit Freigabe Gerald. Abschluss (TS-90.5, TS-88.1)
Als A: Knopf {AG_ABSCHLUSS}, "Produktion starten", dann "Beenden (mit Mengen)", Gutmenge = Sollmenge (Hinweis
im Fenster), "Bestaetigen".
OK, wenn der AG aus der Liste verschwindet (war es der letzte AG des Auftrags, verschwindet der ganze Block)
und /FaHierarchy (neu laden) das Kuerzel gruen mit Haken "... - erledigt" zeigt.

B34 SCHREIBEND - nur mit Freigabe Gerald. Offset-Hinweis verschwindet (TS-88.6)
Stammdaten -> Arbeitsgaenge: Kuerzel {KUERZEL_OHNE_EINTRAG}, Offset 0, "Arbeitsgang anlegen".
OK, wenn danach in /FaHierarchy ({HFA_BDE}) der gelbe Offset-Hinweis {KUERZEL_OHNE_EINTRAG} nicht mehr nennt
(bzw. ganz fehlt). NICHT loeschen (macht B69).

BLOCK 2 - Benutzer S (nur beschichtungsauftrag_schreiben)
BENUTZERWECHSEL: "Bitte Benutzer S anmelden."

B35 Navigation, Umschalter, Seitentitel (TS-81.2, TS-89.9)
Menue Beschichtung -> Vorschlagsliste.
OK, wenn Adresse /FaHierarchyBeschichtung/Summiert, Ueberschrift und Browser-Tab "Vorschlagsliste
Beschichtung (IDEAL)", Umschalter "Liste | Summiert" mit "Summiert" aktiv, Menue "Beschichtung" markiert.
"Liste" -> /FaHierarchyBeschichtung, "Liste" aktiv, gleiche Ueberschrift; "Summiert" -> zurueck.

B36 Summenzeile (TS-81.3, TS-81.4)
Summiert, Spaltenfilter HauptFA {HFA_MEHRFACH}.
OK (Teil 1), wenn je Artnr genau eine Zeile steht; fuer {ARTNR_MEHRFACH} "Positionen" = {ANZAHL_MEHRFACH} und
"Summe Sollmenge" = {SUMME_MEHRFACH}; Breite/Hoehe/Tiefe angezeigt; verschiedene Artnr = verschiedene Zeilen.
HauptFA-Filter loeschen, Spaltenfilter Artnr {ARTNR_ZWEI_HFA}: OK (Teil 2), wenn {HFA_X} und {HFA_Y} je eine
eigene Gruppe haben, nie zusammengefasst. Screenshot. Filter loeschen.

B37 Masse uneinheitlich (TS-81.5)
Summiert, HauptFA {HFA_MASSE}.
OK, wenn {ARTNR_MASSE} eine Zeile ist (nicht aufgeteilt) und in der Matchcode-Spalte der gelbe Hinweis
"Masse uneinheitlich" als lesbarer Text steht. "Drucken": derselbe Hinweis in der Druckansicht. Tab schliessen.

B38 Alle Ebenen (TS-81.6)
Summiert, HauptFA {HFA_SUBEBENE}.
OK, wenn {ARTNR_SUBEBENE} in der Summe enthalten ist; in der "Liste" erscheinen nur beschichtete Positionen.

B39 Kopf je HauptFA, Kombigeraet, Empfaenger (TS-81.7, TS-82.11)
Summiert, nacheinander HauptFA {HFA_KOMBI}, {HFA_OHNE_KOPF}, {HFA_OHNE_DL}.
OK (Teil 1), wenn der Gruppenkopf Dienstleister/RAL/Start/Retour/AB/Pos/Montage-Abt. wie die Liste zeigt;
{HFA_KOMBI} zeigt alle Kopfvarianten und das Badge "mehrdeutig (mehrere Kopfzeilen)"; {HFA_OHNE_KOPF} zeigt
"Keine Auftrags-Kopfdaten zu diesem HauptFA.".
Je "Drucken": OK (Teil 2), wenn der Empfaenger bei {HFA_OHNE_DL} "Empfaenger fehlt - in Sage nachpflegen"
lautet und bei {HFA_KOMBI} "mehrdeutig - siehe Kopfzeilen", darunter die gelbe Mehrdeutigkeits-Box.

B40 Listen-Muster (TS-81.8)
Summiert, Seitengroesse 25.
OK (Teil 1), wenn die Paginierung HauptFA-Gruppen zaehlt und keine Gruppe ueber zwei Seiten reisst.
Spaltenfilter "Summe Sollmenge" auf "=<sichtbarer Summenwert>" (seit v1.58.1 Zahlspalte, Komma = Dezimalkomma): OK (Teil 2), wenn nur
Zeilen mit dieser Summe bleiben. Zusaetzlich Filter "Artnr." setzen, "Liste" klicken: OK (Teil 3), wenn der
Artnr-Filter bleibt und der Filter auf "Summe Sollmenge" NICHT mitgenommen wird. Filter loeschen,
Seitengroesse zurueck.

B41 Zahnrad (TS-81.9)
Summiert, Zahnrad rechts direkt ueber der Tabelle.
OK, wenn die Spaltenauswahl aufgeht, "Tiefe" sich ausblenden laesst und HauptFA sowie Matchcode nicht
ausblendbar sind. "Tiefe" fuer B42 ausgeblendet lassen.

B42 Druck Summiert mit Briefkopf (TS-81.10, TS-82.2)
Summiert, HauptFA-Filter {HFA_MEHRFACH}, "Drucken".
OK, wenn je HauptFA ein Ausdruck: oben Logo RECHTS (Logozone frei), KEINE Absenderzeile und keine hellblaue
Linie mehr (seit v1.59.0, Vorlage mit Fusszeile); unten auf JEDER Seite die Fusszeile: links "IDEAL Kaeltetechnik
GmbH" (IDEAL-Blau) mit Anschrift/Telefon/office@ideal-ake.at, Mitte "AKE Ausseer Kaelte- und Edelstahltechnik
GmbH" (AKE-Blau) mit Anschrift/Telefon/office@ideal-ake.at, rechts "www.ideal-ake.at", darunter Balken
dunkelblau/hellblau mit schraegem Uebergang, innerhalb der Seitenraender; kein Positionstext unter der Fusszeile.
Darueber: Empfaenger (Dienstleister, fett), "Beschichtungsauftrag (summiert) - HauptFA ...", Datum; darunter
Kopftabelle, Barcode, Hinweise "Summierte Ansicht" und "Gefilterte Ansicht", dieselben Zeilen wie am
Bildschirm ohne Paging, Spalte "Tiefe" fehlt. Am Bildschirm KEIN gelber Hinweis "Briefkopf: ...".
Screenshot. Tab schliessen, "Tiefe" wieder einblenden.

B43 PDF Summiert (TS-81.11, TS-82.2)
PDF-Knopf an der Gruppe {HFA_MEHRFACH} (Download - vorher fragen).
OK, wenn eine Datei "Beschichtungsauftrag_{HFA_MEHRFACH}_<yyyyMMdd-HHmm>.pdf" entsteht und keine gelbe
Umleitungsmeldung erscheint; Inhalt wie B42, Logo sichtbar, Fusszeile auf jeder Seite, dazu "Seite X von Y"
(PDF = genau ein Dokument, TS-92) (sofern ansehbar, sonst "Inhalt -> Teil C"). Fehlt der PDF-Knopf: notieren.

B44 Mehrseitig: Briefkopf nur auf Seite 1 (TS-82.3)
"Liste", HauptFA {HFA_VIELE}, "Drucken"; erscheint ein Druckdialog, die Vorschau durchblaettern, dann Escape.
OK, wenn der Briefkopf nur auf Seite 1 steht, Folgeseiten mit Positionen beginnen, die Fusszeile auf JEDER
Seite steht und keine Position unter ihr verschwindet; "Seite X von Y" erscheint (genau ein HauptFA im Druck,
TS-92). Bei einem Sammeldruck mit mehreren HauptFA gibt es KEINE Seitenzaehlung. Ohne Druckdialog: Seitenumbruch NICHT PRUEFBAR, Inhalt notieren.

B45 Leerfall (TS-81.12)
Summiert, Spaltenfilter Artnr XYZ-NICHTDA.
OK, wenn "Keine beschichteten Positionen fuer die aktuelle Filterung gefunden." steht und "Drucken" einen
Hinweis "nichts zu drucken" statt eines leeren Blatts bringt. Filter loeschen.

B46 Regression Einzelliste (TS-81.14)
"Liste", HauptFA {HFA_MEHRFACH}.
OK, wenn Spalten und Gruppenkopf wie die Liste aus TS-60 aussehen und "Drucken" den Briefkopf wie in B42
zeigt, inkl. Fusszeile ("Beschichtungsauftrag - HauptFA ..." ohne "(summiert)"). Tab schliessen, Filter loeschen.

B47 SCHREIBEND - nur mit Freigabe Gerald. Auftrag anlegen (TS-84.1)
Vorbereitung fuer B48: Summiert, HauptFA-Filter {HFA_NEU}, dieselbe Seite in einem ZWEITEN Tab oeffnen und
dort stehen lassen. Im ersten Tab im Gruppenkopf "Aus Vorschlagsliste erstellen".
OK, wenn das Detail des neuen Auftrags kommt: gruene Meldung mit Nummer BA-JJJJ-NNNNN und Positionszahl
({ANZAHL_NEU}), Status Entwurf, Dienstleister = {DL_SAGE_NEU}. Nummer als BA_1 notieren. Zurueck zur
Vorschlagsliste: Gruppenkopf "Auftraege: BA_1" als Link, KEIN Knopf mehr. In der "Liste" traegt jede
Position in der Spalte Beschichtet "in BA_1"; Spaltenfilter Beschichtet = Ja findet sie weiterhin. Screenshot.

B48 SCHREIBEND - nur mit Freigabe Gerald. Doppelvergabe aus altem Tab (TS-84.3)
Im zweiten Tab (vor B47 geladen) "Aus Vorschlagsliste erstellen".
OK, wenn die Vorschlagsliste mit gelber Sammelmeldung "Alle ... Positionen ... bereits vergeben" samt
Positionen und BA_1 kommt und KEIN neuer Auftrag entsteht (Beschichtung -> Auftraege). Zweiten Tab schliessen.

B49 SCHREIBEND - nur mit Freigabe Gerald. Entwurf bearbeiten (TS-84.4)
BA_1: Dienstleister auf "Test-DL Abnahme" ueberschreiben, RAL aendern, Start leeren, Retour setzen, eine
Menge 2,5 und eine 3.25, "Entwurf speichern".
OK (Teil 1), wenn "Entwurf gespeichert", Mengen 2,5 und 3,25, Start leer und unter dem Dienstleister
"Sage (PPS-View): ..." steht. Dann eine Menge "abc", "Entwurf speichern": OK (Teil 2), wenn gelbe Meldung und
nichts gespeichert (neu laden: Werte wie vorher).

B50 SCHREIBEND - nur mit Freigabe Gerald. Teilvergabe (TS-84.2)
BA_1: bei einer Position "Entfernen" anhaken, "Entwurf speichern". Vorschlagsliste, HauptFA {HFA_NEU}.
OK (Teil 1), wenn der Gruppenkopf BA_1 UND "(1 Positionen frei)" plus Knopf zeigt.
"Aus Vorschlagsliste erstellen": OK (Teil 2), wenn ein zweiter Auftrag (BA_2) nur mit dieser Position
entsteht und eine gelbe Meldung "... Positionen bereits in BA_1 vergeben, nicht uebernommen" erscheint.

B51 SCHREIBEND - nur mit Freigabe Gerald. Positionen hinzufuegen (TS-84.5)
BA_1: eine weitere Position entfernen und speichern.
OK (Teil 1), wenn sie unter "Positionen hinzufuegen" erscheint (nicht angehakt). Ohne Haken "Ausgewaehlte
hinzufuegen": gelbe Meldung. Anhaken, "Ausgewaehlte hinzufuegen": OK (Teil 2), wenn sie mit Sollmenge zurueck
ist und eine gruene Meldung mit Anzahl kommt; danach steht dort "Keine freien beschichteten Positionen fuer
diesen HauptFA". BA_2 oeffnen: OK (Teil 3), wenn dort keine Positionen von BA_1 angeboten werden.

B52 SCHREIBEND - nur mit Freigabe Gerald. Dienstleister-Pflicht und Druck-Sperre im Entwurf (TS-84.6, TS-84.7)
BA_1, Id aus der Adresszeile: Adressen /Beschichtungsauftraege/Print/<Id> und /Beschichtungsauftraege/Pdf/<Id>
aufrufen.
OK (Teil 1), wenn jeweils zurueck auf das Detail mit "Im Entwurf nicht druckbar - erst uebergeben.".
Dienstleister leeren, "Entwurf speichern": OK (Teil 2), wenn gespeichert wird und der gelbe Hinweis
"Dienstleister fehlt - zum Uebergeben erforderlich." steht. "Auf "Uebergeben" setzen": OK (Teil 3), wenn
gelbe Meldung "Dienstleister fehlt ...", Status bleibt Entwurf, kein Druckknopf. Dienstleister wieder
"Test-DL Abnahme", speichern.

B53 SCHREIBEND - nur mit Freigabe Gerald. Uebergeben (TS-84.7)
BA_1: "Auf "Uebergeben" setzen".
OK, wenn der Statusverlauf "Entwurf angelegt" und "Uebergeben" mit Zeit und Benutzer zeigt, "Erledigt"
"ausstehend"; Kopf und Positionen nur noch Text, kein "Positionen hinzufuegen"; die Bemerkung bleibt
speicherbar ("Abnahme-Test" eintragen, "Bemerkung speichern"). Screenshot.

B54 Druck und PDF des Auftrags (TS-84.8)
BA_1: per Zahnrad "Tiefe" ausblenden, "Drucken".
OK, wenn: Briefkopf (Logo rechts) und Fusszeile wie B42 mit "Seite X von Y", Empfaenger "Test-DL Abnahme" (nicht der Sage-Wert), "Beschichtungsauftrag BA_1 -
HauptFA {HFA_NEU}", Barcode, Kopf mit RAL/Start/Retour/Bemerkung, Positionen je Artnr summiert mit Massen und
Spalte Positionen, "Tiefe" fehlt. Tab schliessen. "PDF" (Download - vorher fragen): Dateiname
"Beschichtungsauftrag_BA_1_JJJJMMTT-HHMM.pdf". "Tiefe" wieder einblenden.

B55 SCHREIBEND - nur mit Freigabe Gerald. Erledigt und Reklamation (TS-84.9)
BA_1: "Auf "Erledigt" setzen". Dann "Reklamation".
OK (Teil 1), wenn die Auswahl ohne Haken kommt; ohne Haken "Reklamation anlegen" -> gelbe Meldung, kein Auftrag.
Zwei Teile anhaken, "Reklamation anlegen": OK (Teil 2), wenn ein neuer Entwurf (BA_R1) mit genau diesen Teilen
entsteht, Dienstleister "Test-DL Abnahme" und RAL/Start/Retour wie BA_1, Kopf aenderbar; BA_1 zeigt
"Reklamiert -> BA_R1", BA_R1 "Reklamation zu BA_1", beide verlinkt; Beschichtung -> Auftraege zeigt beides in
den Spalten Ursprungsauftrag/Reklamiert. Negativ: BA_2 (Entwurf) hat keinen Knopf "Reklamation"; Adresse
/Beschichtungsauftraege/Reklamation/<Id BA_2> -> gelbe Meldung, zurueck auf das Detail.

B56 SCHREIBEND - nur mit Freigabe Gerald. Positionen im Reklamations-Entwurf (TS-84.10)
BA_R1 oeffnen.
OK, wenn der Bereich "Positionen des Ursprungs BA_1 hinzufuegen" genau die uebrigen Positionen von BA_1
(Menge/Masse des Ursprungs) und keine freien HauptFA-Positionen zeigt. Eine hinzufuegen -> verschwindet aus
der Auswahl; alle hinzufuegen -> "Alle Positionen des Ursprungs sind bereits enthalten".
(Gegenueber dem TS-Text "eine von drei" gleichwertig mit zwei Startpositionen.)

B57 SCHREIBEND - nur mit Freigabe Gerald. Mehrfach- und Ketten-Reklamation (TS-84.11)
BA_R1 auf "Uebergeben" setzen. BA_1 -> "Reklamation", eine Position, die auch in BA_R1 ist -> anlegen.
OK (Teil 1), wenn das gelingt (BA_R2) und BA_1 beide Reklamationen nennt.
BA_R1 auf "Erledigt" setzen, dort "Reklamation", eine Position -> anlegen: OK (Teil 2), wenn BA_R3 auf BA_R1
verweist. Vorschlagsliste {HFA_NEU}: OK (Teil 3), wenn der Gruppenkopf Ursprung und Reklamationen nennt und
der Anlege-Knopf diese Positionen nicht uebernimmt (nicht klicken).

B58 SCHREIBEND - nur mit Freigabe Gerald. Storno (TS-84.12)
BA_2 (Entwurf): "Stornieren", Rueckfrage bestaetigen.
OK, wenn Status Storniert, Statusverlauf mit Storno, Zeit und Benutzer, kein Druck; in der Vorschlagsliste
{HFA_NEU} ist die Position wieder frei (Knopf erscheint). NICHT erneut anlegen.

B59 SCHREIBEND - nur mit Freigabe Gerald. Konflikt in zwei Tabs (TS-84.13, nacheinander-Teil)
BA_R2 (Entwurf) in Tab A und Tab B oeffnen. In A den RAL aendern, "Entwurf speichern". In B (nicht neu
laden) "Auf "Uebergeben" setzen".
OK, wenn B "... wurde inzwischen von jemand anderem geaendert - bitte neu laden" meldet und sich nichts
aendert (B neu laden: Status Entwurf). Keine Fehlerseite. Tab B schliessen.

B60 Liste der Auftraege (TS-84.15, TS-86.4, TS-86.17 f)
Beschichtung -> Auftraege.
OK, wenn Statusfilter (Filterkarte, "Filtern"/"Zuruecksetzen"), Spaltenfilter je Spalte inkl. Start/Retour
(Text 11.2026) und Status (uebergeben) wirken, neueste zuerst; Zahnrad blendet eine Spalte aus und merkt sie
nach Neuladen; Pagination erst bei > 25 Auftraegen. "CSV" in der Liste und im Detail von BA_R3 (Entwurf)
(Download - vorher fragen): im Detail-CSV keine Spalte "Entfernen", Menge = Wert des Eingabefelds (sofern
ansehbar). Spaltenauswahl wiederherstellen.

B61 Active-Markierung und Schreibknoepfe (TS-89.2, TS-89.3)
Nacheinander: Vorschlagsliste, "Liste", Auftraege, Detail BA_1, dort "Reklamation" (Reklamationsseite nur
ansehen, dann "Abbrechen" - nichts anlegen).
OK, wenn "Beschichtung" jedes Mal markiert ist, S dasselbe Dropdown wie G sieht und auf den Seiten die
Schreibknoepfe hat.

B62 Hilfe Beschichtung (TS-89.10)
Fusszeile -> Hilfe, Karte "Beschichtung (IDEAL)".
OK, wenn sie direkt nach "Fertigungsauftraege" steht mit Kurzanleitung, Vorschlagsliste, Status, Reklamation,
Druck und PDF, Wenn etwas nicht klappt, Einrichtung (Admin); die Knopf- und Menuetexte der Kurzanleitung zu
B47-B57 passen; Strg+F "Menuepunkt Beschichtungsauftrag" kein Treffer; die alten Abschnitte
v1.48/v1.49/v1.51 nicht mehr in "Fertigungsauftraege"; Rollen-Schluessel und Schalter-Name nur in
"Einrichtung (Admin)".

BLOCK 3 - Benutzer L (nur beschichtungsauftrag)
BENUTZERWECHSEL: "Bitte Benutzer L anmelden."

B63 Lesen ohne Schreibrecht (TS-89.1, TS-89.4, TS-84.14, TS-84.16)
Menue Beschichtung.
OK, wenn das Dropdown mit "Vorschlagsliste" und "Auftraege" da ist; die Vorschlagsliste {HFA_NEU} ohne "Aus
Vorschlagsliste erstellen"; Detail BA_1 und BA_R3 mit "Drucken"/"PDF" (BA_1), aber ohne Eingabefelder,
Status- und Reklamations-Knoepfe; die Druckansicht oeffnet (Tab schliessen). Screenshot.

B64 FA-Struktur verweigert (TS-85.10 Teil 1)
Adresse /FaHierarchy.
OK, wenn der Zugriff verweigert wird.

BLOCK 4 - Benutzer N (leitstand, bde_shiftlead, masterdata; keine Beschichtungsrolle)
BENUTZERWECHSEL: "Bitte Benutzer N anmelden."

B65 Beschichtung gesperrt (TS-89.5, TS-81.13, TS-84.14)
Menueleiste ansehen; Adressen /FaHierarchyBeschichtung/Summiert, /FaHierarchyBeschichtung/PrintSummiert,
/Beschichtungsauftraege.
OK, wenn kein "Beschichtung" in der Leiste steht und alle drei Adressen den Zugriff verweigern.
(/FaHierarchyBeschichtung/PdfSummiert/{HFA_MEHRFACH} nur nach Rueckfrage aufrufen - waere er nicht gesperrt,
startete ein Download.)

B66 BDE-Stammdaten ohne Arbeitsgaenge (TS-87.2 Nicht-Admin) - braucht BdeAktiv = true (B24)
Menue BDE -> Stammdaten; Menue Stammdaten aufklappen; Adresse /OperationConfigs.
OK, wenn genau drei Reiter (Operatoren, Aktivitaeten, Terminals) erscheinen, im Stammdaten-Menue kein
"Arbeitsgaenge" steht und /OperationConfigs den Zugriff verweigert.

B67 Rechte und CSV (TS-86.8)
Adresse /StockOverview. Dann Fertigungsauftraege -> FA-Liste.
OK, wenn /StockOverview verweigert wird und die FA-Liste einen CSV-Knopf zeigt. Sichtbare Spalten notieren
(Vergleich mit dem CSV -> Teil C).

BLOCK 5 - Benutzer G, Aufraeumen
BENUTZERWECHSEL: "Bitte Benutzer G anmelden."

B68 SCHREIBEND - nur mit Freigabe Gerald. Test-Entwuerfe stornieren
Beschichtung -> Auftraege, Statusfilter Entwurf, Spaltenfilter HauptFA {HFA_NEU}. Jeden Entwurf dieses Laufs
(BA_R2, BA_R3, weitere) oeffnen, "Stornieren", Rueckfrage bestaetigen.
OK, wenn keine Entwuerfe dieses Laufs mehr offen sind. Nummern und Endstatus ALLER Testauftraege (auch BA_1
und BA_R1, die nicht stornierbar sind) in den Bericht.

B69 SCHREIBEND - nur mit Freigabe Gerald. Stammdaten-Testeintraege loeschen (TS-87.3 Loeschen)
Stammdaten -> Arbeitsgaenge: Zeile {KUERZEL_TEST} -> Loeschen-Symbol, Rueckfrage bestaetigen; ebenso
{KUERZEL_OHNE_EINTRAG} (nur der in B34 angelegte Eintrag).
OK, wenn jeweils eine gruene Meldung kommt und die Zeile weg ist; {KUERZEL_OHNE_EINTRAG} steht wieder unter
"Nicht konfigurierte Kuerzel".

B70 SCHREIBEND - nur mit Freigabe Gerald. BdeAktiv zuruecksetzen
Nur wenn B24 eingeschaltet hat: Stammdaten -> Einstellungen, BdeAktiv aus, "Einstellungen speichern".
OK, wenn das Menue "BDE" verschwindet. Lief B19 noch nicht (BDE war zu Beginn an), bleibt B19 NICHT PRUEFBAR.

B71 Anzeige-Einstellungen wiederherstellen
Alle Filter loeschen, die zu Beginn notierte Spaltenauswahl und Seitengroesse wiederherstellen,
5 Sekunden warten, neu laden und pruefen. Aenderungen, die unter S, L oder N gemacht wurden, im Bericht
nennen (die stellt Gerald zurueck).

=============================== AKE ===============================
Ab hier NUR https://akenet01.ake.at:4444/. Benutzer: G (Admin) am AKE-Testsystem.
BENUTZERWECHSEL: "Bitte am AKE-Testsystem als G anmelden." Anzeige-Einstellungen von FA-Liste und
Arbeitsgaenge notieren.

K1 Kein Menue "Beschichtung" (TS-89.7)
OK, wenn die Leiste kein "Beschichtung" zeigt und sonst wie gewohnt aussieht (Lager, Fertigungsauftraege,
Kommissionierung, Bestellungen ...). Screenshot.

K2 FA-Liste: Regression und CSV (TS-86.16, TS-86.1 AKE)
Fertigungsauftraege -> FA-Liste: Darstellung, Spaltenfilter, Sortierung, Zahnrad (am alten Platz) und
Druckansicht ansehen; Spaltenfilter "Werkbank", Seitengroesse 25, "von N" notieren; "CSV" neben dem Zahnrad
(Download - vorher fragen).
OK, wenn alles wie vor dem Buendel wirkt, der CSV-Knopf neben dem Zahnrad steht und die Datei N Zeilen und die
sichtbaren Spalten ohne Aktionsspalte hat (sofern ansehbar). Teileverfolgung -> OSEON Auftraege und
Rueckmeldungen (Baum): kein CSV-Knopf. Filter und Seitengroesse zuruecksetzen.

K3 Arbeitsgang-Stammdaten an der AKE (TS-87.1 bzw. TS-87.2, TS-87.4, TS-87.6)
Stammdaten -> Arbeitsgaenge.
OK, wenn: bei {AKE_BDE_AKTIV_START} = false genau der Reiter "Arbeitsgaenge", sonst vier Reiter; die
OSEON-Eintraege aus dem Bestand in der Liste stehen; "Nicht konfigurierte Kuerzel" OSEON-Namen mit "OSEON"
(bzw. "OSEON + WMS-Arbeitsgang") nennt, nie "01" - oder "Alle bekannten Kuerzel sind konfiguriert."; Begriffe
und Legende wie B20 Teil 3. Nichts aendern.

K4 Alte Adresse, Reporting-Banner, Tracking-Schalter (TS-87.5, TS-87.6)
Adresse /Settings/OperationConfig -> neue Seite. Stammdaten -> Einstellungen: Knopf "Arbeitsgaenge
(BDE-Stammdaten)" fuehrt dorthin. Reporting -> OSEON AG-Uebersicht: der Link im Banner fuehrt dorthin.
Teileverfolgung -> OSEON Auftraege: der Schalter heisst "Fremdsystem-Relevanz".
OK, wenn alles zutrifft. Links nur anklicken, Schalter NICHT umlegen.

K5 OSEON unveraendert (TS-87.7)
Teileverfolgung -> OSEON Auftraege und Reporting -> OSEON AG-Uebersicht: Ampeln, Soll-Termine, Zeilen und
Banner mit {OSEON_VERGLEICH} vergleichen.
OK, wenn gleich. Bei KEIN_FALL: Beobachtungen notieren, Ergebnis NICHT PRUEFBAR.

K6 SCHREIBEND - nur mit Freigabe Gerald. BdeAktiv an der AKE einschalten
Nur wenn {AKE_BDE_AKTIV_START} = false: Stammdaten -> Einstellungen, Gruppe BDE, BdeAktiv ein, "Einstellungen
speichern". Sonst "bereits an" notieren.
OK, wenn das Menue "BDE" erscheint.

K7 BDE-Terminal an der AKE (TS-90.7 bzw. TS-90.9)
BDE -> Terminal, Werkbank {AKE_WERKBANK_BDE}, {AKE_PNR} eingeben, "Scannen". KEINEN Auftrags- oder AG-Knopf
antippen (im NurFA-Modus startet ein Antippen sofort die Produktion).
Bei {AKE_NURFA} = true: OK, wenn die Produktionsauftraege wie vor v1.57.0 erscheinen (Knoepfe "FA -
Bezeichnung", nach ProductionDate), ohne Bloecke und ohne Zeichen.
Bei {AKE_NURFA} = false: OK, wenn Bloecke je Auftrag nach ProductionDate sortiert sind (vergangen, kuenftig,
ohne Termin) und ueberfaellige rot mit Dreieck sind. Screenshot.

K8 SCHREIBEND - nur mit Freigabe Gerald. BdeAktiv an der AKE zuruecksetzen
Nur wenn K6 eingeschaltet hat: BdeAktiv aus, "Einstellungen speichern". OK, wenn das Menue "BDE" verschwindet.

K9 Anzeige-Einstellungen am AKE-System wiederherstellen (wie B71).

BERICHT
Wie in den Projektanweisungen: Chat-Tabelle, Berichtsordner
secondbrain\tests\<Datum>-ts-81-90-browser-testbericht\ und am Ende jeder Spec aus dem Frontmatter, zu der
Schritte gelaufen sind, genau eine Verweiszeile. Zusaetzlich im Bericht: angelegte Auftragsnummern mit
Endstatus, BDE-Aktionen (Werker, AG, Uhrzeit) und alle "Inhalt -> Teil C"-Vermerke.
```

---

## Teil C — Nur durch den Menschen (nach dem Lauf)

**Was der Browser nicht kann**

- **Excel / Hex-Ansicht (TS-86):** CSV-Dateien aus B11–B13, B21, B60, K2: beginnt mit `EF BB BF`, Trenner `;`, CRLF,
  keine `sep=`-Zeile, Name `<Ansicht>_JJJJ-MM-TT_HHMM.csv` (TS-86.5); Doppelklick in Excel: Umlaute (ä ö ü ß „–“),
  Spalten getrennt, `1.234,500` als Zahl (Summenformel rechnet), Datum ohne KW als Datum. Inhalt: Zeilenzahl = „von N“,
  Spalten = sichtbare (TS-86.1/86.2); IDEAL-Listen erste Spalte „HauptFA“, keine Gruppenkopfzeilen, keine Fremdzeilen
  aus der Variantentabelle, `hauptfa` per Zahnrad aus → fehlt (TS-86.3/86.4); Zellwerte Leitstand (TS-86.13);
  Sortierung über alle Treffer, je Gruppe sortiert (TS-86.15); TS-86.17 a–e; B67-Spalten = CSV-Spalten (TS-86.8).
- **TS-86.6 / 86.7 CSV-Injection und negative Mengen:** Testdatensatz mit Bemerkung `=HYPERLINK("http://x";"klick")`,
  `@SUMME(A1:A2)`, `+43 664 1234567`, `-x` anlegen (ändert Daten) → im CSV mit `'` davor, Excel zeigt Text; `-5,000`
  ohne `'`, als Zahl.
- **SQL-Profiler:** TS-85.9 (Seitengröße „Alle“: genau eine Abfrage auf `ProductionWorkplaces`, keine zusätzliche auf
  `WorkOperations`), TS-88.11 (eine `WorkOperations`-Abfrage + `BdeBookingQuantities` + `BdeBookings`), TS-90.10 (≤ 3
  Zusatzabfragen bei ≥ 100 offenen AG; Ladezeit, Umbruch).
- **Zwei Browser gleichzeitig:** TS-83.6 / TS-84.13 — gleichzeitig „Aus Vorschlagsliste erstellen“ für denselben
  HauptFA → nur einer übernimmt die Positionen; dieselbe freie Position gleichzeitig in zwei Entwürfe → Klartext
  („vergeben“ bzw. „bitte erneut versuchen“), keine Fehlerseite.
- **Screenreader (NVDA/Sprachausgabe):** TS-85.4 („KA – <Name>“), TS-88.10, TS-90.8 (Kürzel, Name, Status).
- **Touch-Terminal / Fertigung:** TS-85.4 (Antippen ohne Fehler), TS-85.6 (Kontrast Text ≥ 4,5:1, Rahmen ≥ 3:1, auch in
  Hover- und Suchtreffer-Zeilen), TS-88.10 (Graustufen-Screenshot: Haken/Sanduhr/Dreieck/Kreis unterscheidbar;
  Spaltenfilter `ka`: Treffer amber, Pfadzeilen kursiv mit grauem Streifen; Kontrastwerte), TS-90.8 (Knöpfe ≥ 60 px,
  Kontrast, Graustufen, auch beim Antippen).
- **Mobil-Menü / schmales Fenster:** TS-89.5 (Benutzer N: kein „Beschichtung“ auch im Mobil-Menü), TS-89.8
  (Hamburger → „Beschichtung“ aufklappbar, beide Einträge erreichbar).

**Briefkopf mit Dateizugriff am Webserver (TS-82)** — jeweils danach die Vorlage wiederherstellen:
TS-82.2 PDF → Eigenschaften → Schriftarten (Nimbus Sans Novus eingebettet); TS-82.4 Telefonnummer/Logo ändern, nächster
Druck ohne Neustart; TS-82.5 (a) Pfad leeren (Standorteinstellungen), (b) Datei umbenennen, (c) Pfad auf
`appsettings.json` → Ersatz-Kopf, gelber Hinweis nur am Bildschirm, Log-Warnung; TS-82.6 Logo fehlt; TS-82.7 Schriften
fehlen; TS-82.8 `<script>`/`onerror`/`javascript:` in der Vorlage (kein Dialog, Quelltext der Druckseite sauber);
TS-82.9 fremde Verweise (Netzwerk-Reiter ohne Anfrage an ideal-ake.at, alle `src=`/`url(` mit `data:`); TS-82.10
Größengrenzen (256 KB / 1 MB); TS-82.15 fehlerhaftes Markup (a)–(d); TS-82.14 Lineal am Ausdruck (Logo 17,5 mm,
Logozone 37,125 mm, Farben laut CD). TS-81.5 Log-Warnung „Masse uneinheitlich“ und TS-81.7 Log-Warnung Kombigerät im
Web-Log; TS-81.11 Rest: ohne Edge kein PDF-Knopf, PDF-Fehler → gelbe Meldung.

**SQL und Datenbank**

- TS-83.3 `SQL/94` zweimal auf einer DB-Kopie (zweiter Lauf ohne Fehler, eine History-Zeile), TS-83.4 FreshInstall auf
  leerer DB, TS-83.5 doppelte Nummer → UNIQUE greift (Testzeile `BA-2026-99999` danach löschen).
- TS-87.9 Migration je DB-Form (EF-, Skript-, FreshInstall-Kopie; P6 gleich; Negativfall „existieren beide“ → ABBRUCH,
  keine History-Zeile).
- TS-88.9 zweite `WorkOperation` `ka` neben `KA` per SQL → ein Symbol, genau eine Warnung je Seitenaufruf im Log.
- TS-88.12 Zuordnung Blatt/Sub-FA/Default-AG und (d) „alle Stammdaten-Einträge fehlen“ — braucht gezielte Buchungen
  bzw. eine leere Tabelle am Testsystem.

**Manipulation und Schalter, die Cowork nicht bedient**

- TS-84.14 / TS-87.2 / TS-87.10: nachgebautes POST (Entwicklerwerkzeug) als L bzw. N → AccessDenied; ungültige Id → 404.
- TS-87.3 Rest: Offset-Feld leeren, `required` per Entwicklerwerkzeug entfernen, anlegen **und** ändern → gelbe Meldung
  „Offset (Arbeitstage) fehlt – nichts gespeichert.“; Audit-Abfrage **vor** B69:
  `SELECT CreatedBy, CreatedByWindows, ModifiedBy, ModifiedAt FROM dbo.OperationConfigs WHERE OperationName = '{KUERZEL_TEST}';`
  zeigt den Benutzer; Bestandszeile `CreatedBy = 'system'`, `ModifiedAt = NULL`.
- TS-89.6 für S und L (Schalter aus → auch dort kein Menü); TS-86.9 `FaHierarchyKommissionierlistenAktiv = false` →
  weder Liste noch Export; TS-86.11 Sitzung abgelaufen → rote Meldung, keine Datei.
- TS-88.4 Endmeldung aus B33 stornieren bzw. `IsFinal` per Korrektur zurücknehmen (BDE → Buchungen) → wieder
  offen/überfällig.
- TS-90.9 an IDEAL: `BdeNurFaMeldung` an → Liste wie vor v1.57.0. TS-90.11 Rest: Benutzer ohne BDE-Rolle →
  `/api/bde/available-operations/<id>` verweigert; Werkbank nicht BDE-aktiv → leer; Auftragstext `<b>x</b>` → als Text.
- TS-85.2 Abgleich mit Sage „Fertigungsaufträge bearbeiten“ (Sage-Maske neben der FA-Struktur, Struktur `{HFA_KTZ}`).

**Aufräumen, was Cowork nicht aufräumen kann**

- Nicht stornierbare Testaufträge (*Übergeben*/*Erledigt*, mindestens BA_1 und BA_R1) — entscheiden, ob sie auf dem
  Testsystem bleiben; es gibt keine Löschfunktion.
- Pausierte Buchungen aus B31/B32 und die Endmeldung aus B33 (BDE → Buchungen); Rückmeldung an Sage kontrollieren.
- Anzeige-Einstellungen der Benutzer S, L, N, falls Cowork sie geändert hat (siehe Bericht zu B71).

**Bericht des Agenten** liegt als eigener Ordner unter `secondbrain\tests\`; die Specs tragen am Ende je eine
Verweiszeile. Jeden FEHLER als Befund in die passende Spec eintragen.

---

## Übersicht der Schritte

| Schritt | TS-Nummer | System | schreibend | Vorbedingung |
|---|---|---|---|---|
| B1 | TS-89.1, 89.2 | IDEAL | nein | G |
| B2 | TS-85.1 | IDEAL | nein | G, `{HFA_KTZ}` |
| B3 | TS-85.2 | IDEAL | nein | P-K1 notiert |
| B4 | TS-85.3 | IDEAL | nein | P-K2 b, `{KUERZEL_OHNE_WB}` |
| B5 | TS-85.4 | IDEAL | nein | – |
| B6 | TS-85.5 | IDEAL | nein | `{HFA_8_KUERZEL}` |
| B7 | TS-85.7 | IDEAL | nein | Spaltenauswahl notiert |
| B8 | TS-85.8, 85.11 | IDEAL | nein | `{HFA_KTZ}` |
| B9 | TS-85.10 | IDEAL | nein | `{HFA_NICHT_MAT}` |
| B10 | TS-86.1, 86.3, 86.4, 86.12, 86.14 | IDEAL | nein | Server-Kultur geprüft |
| B11 | TS-86.1, 86.2 | IDEAL | nein (Download) | Rückfrage Download |
| B12 | TS-86.12 | IDEAL | nein (Download) | `{ID_BOM_ZIELE}` |
| B13 | TS-86.10 | IDEAL | nein (Download) | `{ANZAHL_BEWEGUNGEN}` > 5000 |
| B14 | TS-86.15 | IDEAL | nein | – |
| B15 | TS-82.13 | IDEAL | nein | Kommissionierlisten an |
| B16 | TS-82.12 | IDEAL | nein | G (Admin) |
| B17 | TS-89.11 | IDEAL | nein | – |
| B18 | TS-89.8 | IDEAL | nein | – |
| B19 | TS-87.1 | IDEAL | nein | `{BDE_AKTIV_START}` = false |
| B20 | TS-87.4, 87.5, 87.6 | IDEAL | nein | `SQL/95` gelaufen, P1–P5 |
| B21 | TS-87.8, 87.10 | IDEAL | nein (Download) | – |
| B22 | TS-87.3 | IDEAL | **ja** | Freigabe, `{KUERZEL_TEST}` |
| B23 | TS-89.6, 81.13, 84.14 | IDEAL | **ja** | Freigabe |
| B24 | Vorbed. TS-87.2/88/90 | IDEAL | **ja** | Freigabe, `{BDE_AKTIV_START}` |
| B25 | TS-87.2, 86.14 | IDEAL | nein | B24 |
| B26 | TS-88.3, 88.5–88.8 | IDEAL | nein | B24, `{HFA_BDE}`, Offsets gepflegt |
| B27 | TS-90.1, 90.2, 90.6 | IDEAL | nein | B24, Terminal für G, `{PNR_A}`, P-T2 |
| B28 | TS-90.11 | IDEAL | nein | `{WERKBANK_LEER}` |
| B29 | TS-90.3, 88.2 | IDEAL | **ja** | Freigabe, B24, `{PNR_B}`, `{AG_ZUKUNFT}` |
| B30 | TS-90.3 | IDEAL | **ja** | Freigabe, B29, `{MEHRFACH_PRO_AG}` |
| B31 | TS-90.3, 88.2 | IDEAL | **ja** | Freigabe, B29 |
| B32 | TS-90.4, 88.3 | IDEAL | **ja** | Freigabe, `{AG_UEBERFAELLIG}` |
| B33 | TS-90.5, 88.1 | IDEAL | **ja** | Freigabe, `{AG_ABSCHLUSS}`, Rückmeldung geklärt (A4) |
| B34 | TS-88.6 | IDEAL | **ja** | Freigabe, nach B27 |
| B35 | TS-81.2, 89.9 | IDEAL | nein | S |
| B36 | TS-81.3, 81.4 | IDEAL | nein | TS-81.1 notiert, `{HFA_MEHRFACH}`, `{ARTNR_ZWEI_HFA}` |
| B37 | TS-81.5 | IDEAL | nein | `{HFA_MASSE}` |
| B38 | TS-81.6 | IDEAL | nein | `{HFA_SUBEBENE}` |
| B39 | TS-81.7, 82.11 | IDEAL | nein | `{HFA_KOMBI}`, `{HFA_OHNE_KOPF}`, `{HFA_OHNE_DL}`, TS-82.1 notiert |
| B40 | TS-81.8 | IDEAL | nein | – |
| B41 | TS-81.9 | IDEAL | nein | – |
| B42 | TS-81.10, 82.2 | IDEAL | nein | Briefkopf eingerichtet (A4) |
| B43 | TS-81.11, 82.2 | IDEAL | nein (Download) | Edge am Server, Rückfrage |
| B44 | TS-82.3 | IDEAL | nein | `{HFA_VIELE}` |
| B45 | TS-81.12 | IDEAL | nein | – |
| B46 | TS-81.14 | IDEAL | nein | – |
| B47 | TS-84.1 | IDEAL | **ja** | Freigabe, `{HFA_NEU}`, Rolle S, TS-83.1 notiert |
| B48 | TS-84.3 | IDEAL | **ja** | Freigabe, B47 |
| B49 | TS-84.4 | IDEAL | **ja** | Freigabe, B47 |
| B50 | TS-84.2 | IDEAL | **ja** | Freigabe, B47 |
| B51 | TS-84.5 | IDEAL | **ja** | Freigabe, B50 |
| B52 | TS-84.6, 84.7 | IDEAL | **ja** | Freigabe, B49 |
| B53 | TS-84.7 | IDEAL | **ja** | Freigabe, B52 |
| B54 | TS-84.8 | IDEAL | nein (Download) | B53 |
| B55 | TS-84.9 | IDEAL | **ja** | Freigabe, B53 |
| B56 | TS-84.10 | IDEAL | **ja** | Freigabe, B55 |
| B57 | TS-84.11 | IDEAL | **ja** | Freigabe, B55 |
| B58 | TS-84.12 | IDEAL | **ja** | Freigabe, B50 |
| B59 | TS-84.13 | IDEAL | **ja** | Freigabe, B57 |
| B60 | TS-84.15, 86.4, 86.17 f | IDEAL | nein (Download) | B57 |
| B61 | TS-89.2, 89.3 | IDEAL | nein | B55 |
| B62 | TS-89.10 | IDEAL | nein | – |
| B63 | TS-89.1, 89.4, 84.14, 84.16 | IDEAL | nein | L, B47/B57 |
| B64 | TS-85.10 | IDEAL | nein | L |
| B65 | TS-89.5, 81.13, 84.14 | IDEAL | nein | N |
| B66 | TS-87.2 | IDEAL | nein | N, B24 |
| B67 | TS-86.8 | IDEAL | nein | N |
| B68 | Aufräumen TS-84 | IDEAL | **ja** | Freigabe, G |
| B69 | Aufräumen TS-87.3, 88.6 | IDEAL | **ja** | Freigabe, B22/B34 |
| B70 | Aufräumen BdeAktiv | IDEAL | **ja** | Freigabe, B24 |
| B71 | Anzeige-Einstellungen | IDEAL | nein | – |
| K1 | TS-89.7 | AKE | nein | G am AKE |
| K2 | TS-86.16, 86.1 | AKE | nein (Download) | Server-Kultur AKE |
| K3 | TS-87.1/87.2, 87.4, 87.6 | AKE | nein | P1–P5 AKE |
| K4 | TS-87.5, 87.6 | AKE | nein | – |
| K5 | TS-87.7 | AKE | nein | `{OSEON_VERGLEICH}` |
| K6 | Vorbed. TS-90.7 | AKE | **ja** | Freigabe, `{AKE_BDE_AKTIV_START}` |
| K7 | TS-90.7 / 90.9 | AKE | nein | P-T1 inkl. Kollision + M1, `{AKE_PNR}`, Terminal für G |
| K8 | Aufräumen BdeAktiv | AKE | **ja** | Freigabe, K6 |
| K9 | Anzeige-Einstellungen | AKE | nein | – |

