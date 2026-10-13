---
type: testprotokoll
title: "Browser-Testprotokoll TS-91 — Spaltenfilter-Suchsyntax (v1.58.0 + v1.58.1)"
ausfuehrung: Claude Cowork (Abnahme)
system: "http://idealweb01.ideal.ideal-ake.at:88/ (nur IDEAL)"
spec:
  - "[[2026-09-23-suchsyntax-spaltenfilter-erweitern-spec]]"
created: 2026-10-13
---

# Browser-Testprotokoll TS-91 — Spaltenfilter-Suchsyntax (v1.58.0 + v1.58.1)

Grundlage: TS-91.A1–A9 und TS-91.B1–B10 in `docs/TESTSZENARIEN.md` (Z. 9818–9910) im Stand des Bündel-Commits `ead16fc4`
(Worktree `2026-08-07-ideal-teile-1-5`, v1.58.1), die 23-Punkte-Checkliste „Manuelle Test-Checkliste (Schranke 2)“ am Ende von
[[2026-09-23-suchsyntax-spaltenfilter-erweitern-spec]], die Freigabe-Antworten 1–10 und Regel 5 in der Fassung von Ruling R1
(`=` mit lesbarem Zahl-/Datumswert vergleicht in Zahl-/Datumsspalten den **Wert**; „enthält“ und `%` bleiben auf dem Zelltext).
Teil B läuft unter der Arbeitsweise ABNAHME aus [[cowork-projekt-ideal-ake-wms-anweisungen]] (Abschnitt `=== ABNAHME ===`).

**Getesteter Stand:** v1.58.1. Zwei Szenarien aus Etappe A sind durch Etappe B überholt und werden hier **nicht** in ihrer
A-Fassung geprüft: TS-91.A7 („Zahl-/Datumsspalten zählen bis Etappe B als Text, `>5` → rot“) und TS-91.A9 (Zwischenzustand:
Datumsvergleich in Leitstand/BDE-Buchungen rot, Stückliste ohne „?“). Seit v1.58.1 gilt das Gegenteil (TS-91.B9) — so steht es
in B7, B10, B12 und B14.

**Belegt gegen den Code im Bündel** (nicht aus dem Gedächtnis):
- Menüwege `Views/Shared/_Layout.cshtml` Z. 92 (Lager › Bewegungshistorie), 109 (FA-Struktur), 122–123 (Beschichtung ›
  Vorschlagsliste/Aufträge), 148/152/156 (Fertigungsaufträge › FA-Liste/FA-Vervollständigen/FA-Abarbeitungsliste), 170
  (Leitstand › Kommissionierung), 190 (Kommissionierung › Kommissionierlisten), 226/231/232 (Bestellungen › Lagerbestellungen /
  Lager: Eingehende Listen / Lager: Fehlteile), 262 (BDE › Buchungen), 296–297 (Stammdaten › Artikel/Lagerplätze); „Hilfe“
  steht in der **Fußzeile** (Z. 407).
- Spaltenüberschriften aus den `<th data-col-key>` der jeweiligen View; Zahl-/Datumsspalten aus dem Register
  `ColumnFilterTypes.Lists` (`Services/ColumnFilterSyntax.cs` Z. 31–83). **Was nicht im Register steht, ist Text.**
- Texte im Browser aus `wwwroot/js/column-filter-syntax.js`: Knopf „?“ mit `aria-label` „Hilfe zur Filtersyntax“ (Z. 213–214),
  Popover-Titel „Filter-Syntax“ (Z. 221), Popover-Zeilen (Z. 166–168) und Fußnote (Z. 179), Hinweis unter dem Feld
  `<Teil>: <Grund> — ignoriert` (Z. 83, 160) mit den Gründen „Vergleich nur in Zahl- und Datumsspalten“ (Z. 46), „Bereich nur in
  Zahl- und Datumsspalten“ (Z. 58), „Zahl nicht lesbar“ / „Datum nicht lesbar (TT.MM.JJJJ)“ (Z. 39).
- Druck-Banner „Gefilterte Ansicht — dieses Dokument zeigt nur die gefilterten …“ (`Views/FaHierarchyBeschichtung/PrintSummiert.cshtml`
  Z. 65, `Views/FaHierarchyKommissionierListen/PrintSummiert.cshtml` Z. 56).

**Spaltentypen der geprüften Listen** (Register bzw. `<th>`). Nicht jede Liste hat alle drei Typen — fehlt einer, prüft der
Schritt dort, dass ein Vergleich **rot** wird:

| Liste | Text (Beispiele) | Zahl | Datum | Filterweg |
|---|---|---|---|---|
| FA-Liste | FA Nr., Kunde, Matchcode, Artikelnummer, Werkbank | Prio | KO-Termin, Beschicht., BG-Termin, Komm., Fert.-Termin, Liefertermin | Server |
| Leitstand › Kommissionierung | FA Nr., Kunde, Matchcode, VK–VA | — | Beschicht., BG-Termin, Komm., Fert.-Termin, Liefertermin | Server |
| Stammdaten › Artikel | Artikelnummer, Bezeichnung, Matchcode, Einheit, Artikelgruppe, Kategorie | — | — („Erstellt am“ nicht filterbar) | Server |
| Lager › Bewegungshistorie | Artikel, Lagerplatz, Bewegungsart, Benutzer, Fertigungsauftrag | — (Menge nicht filterbar) | Datum/Zeit | Server |
| BDE › Buchungen | Operator, Werkbank, Typ, Ziel, Status | Gut, Ausschuss | Start, Ende | Server |
| Bestellungen › Lager: Fehlteile | Werkbank, Artikel-Nr, Bezeichnung, Notiz Lager, Notiz EK | — | — | Server |
| Stückliste (`/Picking/Bom/<Id>`) | Bezeichnung 1, Komm.-Ziel, Ebene, … | Menge | — | Client |
| FA-Struktur (Baum) | Struktur, Matchcode, Bezeichnung, Arbeitsgänge, Soll / Fert., SubFA; Auswahl: Arbeitsbereich, Komm.-Ziel, Status | — | — | Client |
| Beschichtung › Aufträge | Nummer, HauptFA, Dienstleister, RAL | Positionen | Start, Retour, Angelegt am | Server |
| Beschichtungsauftrag-Detail (Positionen) | Vater / SubFA / Pos., Artnr., Matchcode | Breite, Höhe, Tiefe, Menge | — | Client |

**Ablauf:** Teil A erledigst du **vor dem Deploy** (je Punkt ein Kreuz). Dann den Freigabe-Block ankreuzen, die Platzhalter
`{...}` in Teil B ausfüllen und den Block als Auftrag an Claude Cowork geben (eigene Aufgabe, Arbeitsweise Abnahme). Teil C
machst du danach.

> [!info] Ergebnis „bekannte Grenze“
> Die Projektanweisungen kennen nur OK, FEHLER, NICHT PRUEFBAR und PROTOKOLL PASST NICHT. Schritte mit dem Vermerk **INFO**
> prüfen eine bekannte, dokumentierte Grenze (QA Epic M-2, M-3, B-R3; Antwort 5): Trifft die beschriebene Erwartung ein, ist das
> Ergebnis **OK (INFO)** — kein Fehler. Nur eine Abweichung von der beschriebenen Erwartung ist ein FEHLER.

---

## Freigabe-Block (Gerald kreuzt an — ohne Haken startet Cowork nicht bzw. der Schritt ist NICHT PRUEFBAR)

Teil B ist **rein lesend**: Filter setzen und löschen, Spalten ein-/ausblenden, Seitengröße ändern, Druckansichten öffnen — nichts
speichern. Es gibt keinen SCHREIBEND-Schritt; `FREIGABEN` in Teil B bleibt „keine“.

- [ ] Teil A vollständig, **kein STOPP** aus A3 (P-2) oder A4 (P-3)
- [ ] Testsystem IDEAL zeigt v1.58.1, Benutzer G (Admin) ist im Browser angemeldet
- [ ] Platzhalter in Teil B ausgefüllt (`KEIN_FALL`, wo A6 nichts liefert)
- [ ] B10 BDE-Buchungen: `BdeAktiv` am Testsystem ist **an** (sonst B10 NICHT PRUEFBAR — Cowork schaltet nichts)
- [ ] B11, B16, B18 b: `LagerbestellungAktiv` ist **an** (sonst diese Schritte NICHT PRUEFBAR)
- [ ] B12 Download CSV der Stückliste erlaubt (Cowork fragt trotzdem vorher im Chat)

---

## Teil A — Gerald, VOR dem Deploy

**A1 — Stand und Einrichtung.** [ ]
Publish aus dem Bündel-Worktree (Web; Service nur für die Versionsanzeige), Testsystem IDEAL zeigt in der Fußzeile **1.58.1**.
Keine Migration, kein SQL-Skript. IDEAL-Einstellungen: `ProduktionsauftragHierarchisch = true`, `LeitstandAktiv`,
`FaHierarchyBeschichtungAktiv`, `FaHierarchyKommissionierlistenAktiv` an (Abfrage in A6).

**A2 — P-1 gespeicherte Standardfilter (IDEAL-WMS-DB und AKE-WMS-DB, je `IDEAL_AKE_WMS`, nur lesen).** [ ] IDEAL [ ] AKE

```sql
SELECT u.Id, u.Name, v.Spalte, v.Wert
FROM dbo.Users u
CROSS APPLY (VALUES
    ('DefaultFilterBeschaffung',             u.DefaultFilterBeschaffung),
    ('DefaultFilterArtikelgruppe',           u.DefaultFilterArtikelgruppe),
    ('DefaultFilterFaWorklistDescription1',  u.DefaultFilterFaWorklistDescription1),
    ('DefaultFilterBomDescription1',         u.DefaultFilterBomDescription1),
    ('DefaultFilterBomKommissionierziel',    u.DefaultFilterBomKommissionierziel)
) v(Spalte, Wert)
WHERE v.Wert IS NOT NULL AND (
       v.Wert LIKE '%[%]%'                                   -- % : kuenftig beginnt/endet
    OR v.Wert LIKE '%;%'                                     -- ; : kuenftig ODER
    OR v.Wert LIKE '%...%'                                   -- Bereich (in Textspalten ungueltig)
    OR REPLACE(v.Wert, ' ', '') LIKE '[!=<>]%'               -- Operator am Anfang (! ist ok, nur zur Sicht)
    OR REPLACE(v.Wert, ' ', '') LIKE '%[,;][=<>]%'           -- Operator am Anfang eines weiteren Teils
    OR (v.Wert LIKE '%(leer)%' AND v.Wert <> '!(leer)')      -- (leer) ausser dem normalisierten Komm.-Ziel
);
```

**Erwartung:** leer (Abfrage aus der Spec, Abschnitt „Migrations-/SQL-Auswirkungen“; alle fünf Felder sind Textspalten).
**Bei Treffern** (Antwort 7: kein Skript) — Bedeutung vorher → nachher:

| Muster im Wert | vorher | ab v1.58.x | Was tun |
|---|---|---|---|
| `%` am Anfang/Ende (`7%`) | enthält die Zeichen „7%“ (traf praktisch nichts) | beginnt mit / endet mit | mit dem Benutzer klären, ob „beginnt mit“ gewollt ist |
| `;` | enthält „;“ | ODER-Trenner | meist gewollt; sonst Wert anpassen |
| `...` | enthält „...“ | Textspalte: rot, Teil ignoriert | Wert anpassen |
| `=`, `<`, `>` am Anfang eines Teils | enthält das Zeichen | `=` genau gleich; `<`/`>` rot, ignoriert | Wert anpassen |
| `(leer)` (außer `!(leer)`) | enthält „(leer)“ | Feld ist leer | mit dem Benutzer klären |

Korrigieren: ein Admin unter Stammdaten › Benutzer › Bearbeiten oder der Benutzer selbst unter „Mein Profil“ — oder nur
informieren. **Treffer beim Testbenutzer G** vor Teil B bereinigen oder hier notieren (sie wirken in B12 und B18 c als
Standardfilter). Ergebnis IDEAL: ______ AKE: ______

**A3 — P-2 Kollation (IDEAL-WMS-DB und AKE-WMS-DB, nur lesen).** [ ] IDEAL [ ] AKE

```sql
SELECT DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS DbCollation;   -- erwartet ..._CI_...

SELECT t.name AS Tabelle, c.name AS Spalte, c.collation_name
FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id
WHERE c.collation_name IS NOT NULL AND c.collation_name NOT LIKE '%[_]CI[_]%';   -- erwartet: leer
```

**Erwartung:** `…_CI_…` und die zweite Abfrage leer. **Sonst STOPP — nicht deployen, melden:** Der SQL-Bauer setzt kein `LOWER`
auf Spalten (Regel 9, Ruling R2); Groß/Klein hängt in SQL allein an der Kollation. Eine `_CS_`-Spalte würde Groß/Klein
unterscheiden, während die Client-Listen es nicht tun. Ergebnis IDEAL: ______ AKE: ______

**A4 — P-3 Server-Kultur (im Browser, beide Testsysteme).** [ ] IDEAL [ ] AKE
Fertigungsaufträge › FA-Liste, Spalte **„Stk.“** ansehen. `1,000` ⇒ deutsch, weiter. `1.000` im Sinne von eins ⇒ **STOPP**
(Zahlvergleiche wie `>2` oder `1...3` würden falsch gelesen).

> [!warning] Abweichung von der Spec
> Die Spec nennt als Prüfstelle „Beschichtungsauftrag `Breite`“. Diese Zelle rendert **fest deutsch**
> (`BeschichtungsauftragService.FormatMass`, `Services/BeschichtungsauftragService.cs` Z. 65, `ToString("0.###", De)`) und zeigt
> `2,5` unabhängig von der Server-Kultur — sie beweist P-3 nicht. „Stk.“ der FA-Liste rendert mit der Server-Kultur
> (`Views/ProductionOrders/_ProductionOrderRow.cshtml` Z. 66, `Quantity.ToString("N3")`), ebenso „Stk.“ der FA-Abarbeitungsliste
> (`Views/FaWorklist/_FaWorklistRow.cshtml` Z. 58).

Ergebnis IDEAL: ______ AKE: ______

**A5 — Laufzeitmessung TS-91.B1 / AK 12 (SSMS, IDEAL-Testsystem-DB).** [ ]
SSMS: Strg+M („Tatsächlichen Ausführungsplan einschließen“), dann:

```sql
SET STATISTICS TIME, IO ON;

-- (a) "vorher" (Stand v1.57: LIKE ohne ESCAPE)
SELECT COUNT(*) FROM dbo.Articles WHERE ArticleNumber LIKE N'%5000%';
SELECT COUNT(*) FROM dbo.Articles WHERE Description   LIKE N'%schraube%';
SELECT COUNT(*) FROM dbo.ProductionOrders WHERE OrderNumber LIKE N'%4711%';

-- (b) "nachher" (v1.58.1: Konstante mit ESCAPE, kein LOWER)
SELECT COUNT(*) FROM dbo.Articles WHERE ArticleNumber LIKE N'%5000%' ESCAPE N'\';      -- enthaelt: gleicher Plan wie (a)
SELECT COUNT(*) FROM dbo.Articles WHERE Description   LIKE N'%schraube%' ESCAPE N'\';
SELECT COUNT(*) FROM dbo.Articles WHERE ArticleNumber LIKE N'5000%' ESCAPE N'\';       -- beginnt mit: Index Seek erwartet
SELECT COUNT(*) FROM dbo.ProductionOrders WHERE OrderNumber LIKE N'%4711%' ESCAPE N'\';
SELECT COUNT(*) FROM dbo.ProductionOrders WHERE OrderNumber LIKE N'4711%' ESCAPE N'\'; -- Index Seek erwartet

SET STATISTICS TIME, IO OFF;
```

„vorher“ ist reines SQL und nicht versionsabhängig — beide Blöcke dürfen nacheinander auf demselben System laufen (QA Epic M-4).
Präfix `5000`/`4711` bei Bedarf durch einen am Testsystem vorkommenden ersetzen (derselbe Wert in (a) und (b)).
**Erwartung:** „enthält“ (a) und (b) gleicher Plan (Scan), ähnliche Zeiten; „beginnt mit“ zeigt **Index Seek** auf
`UQ_Articles_ArticleNumber` bzw. `IX_ProductionOrders_OrderNumber` (`SQL/00_FreshInstall.sql` Z. 162 / 1121).
Ergebnis (je Abfrage CPU-Zeit, verstrichene Zeit, logische Lesevorgänge, Plan-Operator): ______
Das Ergebnis zusätzlich in TS-91.B1 („Ergebnis: ______“) eintragen.

**A6 — Testdaten (IDEAL-WMS-DB).** [ ]
Kein Treffer für einen Fall: beim Platzhalter `KEIN_FALL` eintragen; Cowork wertet den Schritt(-Teil) dann als NICHT PRUEFBAR.
`<…>` in späteren Abfragen durch den zuvor ermittelten Wert ersetzen.

```sql
-- Schalter-Stand
SELECT [Key],[Value] FROM dbo.AppSettings
WHERE [Key] IN ('ProduktionsauftragHierarchisch','LeitstandAktiv','FaHierarchyBeschichtungAktiv',
                'FaHierarchyKommissionierlistenAktiv','BdeAktiv','LagerbestellungAktiv','FaCompletionAktiv');

-- {STICHTAG_BEW} (TT.MM.JJJJ): ein Tag MIT Bewegungen, davor und danach weitere Tage mit Bewegungen
SELECT TOP 15 CAST([Timestamp] AS date) AS Tag, COUNT(*) AS Anzahl
FROM dbo.StockMovements GROUP BY CAST([Timestamp] AS date) ORDER BY Tag DESC;

-- Sollzahlen Bewegungshistorie (die Liste zeigt ALLE Zeilen, kein Standard-Zeitraum; Timestamp = Ortszeit wie angezeigt)
DECLARE @d date = '<JJJJ-MM-TT von STICHTAG_BEW>';
SELECT COUNT(*) AS Gesamt,                                                                           -- {BEW_GESAMT}
       SUM(CASE WHEN CAST([Timestamp] AS date) = @d THEN 1 ELSE 0 END) AS AmTag,                     -- {BEW_AM_TAG}
       SUM(CASE WHEN [Timestamp] >= @d THEN 1 ELSE 0 END) AS AbTag,                                  -- {BEW_AB_TAG}
       SUM(CASE WHEN [Timestamp] >= DATEADD(day, 1, CAST(@d AS datetime2)) THEN 1 ELSE 0 END) AS NachTag,   -- {BEW_NACH_TAG}
       SUM(CASE WHEN [Timestamp] <  DATEADD(day, 1, CAST(@d AS datetime2)) THEN 1 ELSE 0 END) AS BisTagInkl, -- {BEW_BIS_TAG}
       SUM(CASE WHEN [Timestamp] < @d THEN 1 ELSE 0 END) AS VorTag,                                  -- {BEW_VOR_TAG}
       SUM(CASE WHEN MovementType = 1 THEN 1 ELSE 0 END) AS AusbuchungGenau,                         -- {BEW_AUS_GENAU}
       SUM(CASE WHEN MovementType IN (1, 4) THEN 1 ELSE 0 END) AS AusbuchungEnthalten,               -- {BEW_AUS_ENTH}
       SUM(CASE WHEN MovementType NOT IN (1, 4) THEN 1 ELSE 0 END) AS NichtAusbuchung                -- {BEW_NICHT_AUS}
FROM dbo.StockMovements;
-- MovementType: 0 Einbuchung, 1 Ausbuchung, 2 Umbuchung, 3 Sage-Einbuchung, 4 Sage-Ausbuchung (Models/MovementType.cs);
-- Filtertexte "einbuchung", "ausbuchung", ... "sage-ausbuchung" (StockMovementRepository.cs Z. 591-598)

-- Sollzahlen Beschichtung > Auftraege (ohne Statusfilter zeigt die Liste alle Auftraege);
-- {STICHTAG_BA} = ein Start-Datum, das in der Liste vorkommt
DECLARE @s date = '<JJJJ-MM-TT von STICHTAG_BA>';
SELECT COUNT(*) AS Gesamt,                                                         -- {BA_GESAMT}
       SUM(CASE WHEN b.StartBeschichtung >= @s THEN 1 ELSE 0 END) AS StartAb,      -- {BA_START_AB}
       SUM(CASE WHEN b.BeschichtenRetour IS NULL THEN 1 ELSE 0 END) AS RetourLeer, -- {BA_RETOUR_LEER}
       SUM(CASE WHEN x.Pos > 2 THEN 1 ELSE 0 END) AS PositionenUeber2               -- {BA_POS_UEBER_2}
FROM dbo.Beschichtungsauftraege b
OUTER APPLY (SELECT COUNT(*) AS Pos FROM dbo.BeschichtungsauftragPositionen p WHERE p.BeschichtungsauftragId = b.Id) x;

-- {ID_BA}: Auftrag NICHT im Entwurf (Status 30/40: Detailseite ohne Mengenfelder), viele Positionen, moeglichst mit Nachkomma
SELECT TOP 5 b.Id, b.Nummer, b.Status, COUNT(*) AS Positionen,
       SUM(CASE WHEN p.Breite <> FLOOR(p.Breite) OR p.Menge <> FLOOR(p.Menge) THEN 1 ELSE 0 END) AS MitNachkomma
FROM dbo.Beschichtungsauftraege b JOIN dbo.BeschichtungsauftragPositionen p ON p.BeschichtungsauftragId = b.Id
WHERE b.Status IN (30, 40)
GROUP BY b.Id, b.Nummer, b.Status ORDER BY MitNachkomma DESC, Positionen DESC;

-- {ARTNR_UNTERSTRICH} / {ARTNR_FALSCHTREFFER}: Artikelnummer mit "_" und eine, die ein UNmaskiertes "_" faelschlich traefe
SELECT TOP 5 a.ArticleNumber AS MitUnterstrich, b.ArticleNumber AS WuerdeFalschTreffen
FROM dbo.Articles a JOIN dbo.Articles b ON b.ArticleNumber LIKE a.ArticleNumber AND b.ArticleNumber <> a.ArticleNumber
WHERE a.ArticleNumber LIKE '%[_]%' AND a.ArticleNumber NOT LIKE '%[%[]%';
-- leer => {ARTNR_FALSCHTREFFER} = KEIN_FALL und {ARTNR_UNTERSTRICH} aus:
SELECT TOP 5 ArticleNumber FROM dbo.Articles WHERE ArticleNumber LIKE '%[_]%';

-- {TEXT_PROZENT}: Bezeichnung mit "%" im INNEREN (z. B. "5%0"); einen Ausschnitt um das % herum notieren
SELECT TOP 5 ArticleNumber, Description FROM dbo.Articles WHERE Description LIKE '%_[%]_%';

-- {ARTNR_FA_UNTERSTRICH}: Artikelnummer mit "_" in der FA-Liste (Filterkarte, Antwort 8)
SELECT TOP 5 OrderNumber, ArticleNumber FROM dbo.ProductionOrders WHERE ArticleNumber LIKE '%[_]%';

-- {ID_BOM}: Id der HauptFA-Zeile einer Stueckliste mit Komm.-Zielen (wie {ID_BOM_ZIELE} im TS-81-90-Protokoll)
SELECT TOP 10 HauptFA, COUNT(*) AS Zeilen,
       SUM(CASE WHEN LTRIM(RTRIM(ISNULL(Kommissionieren,''))) <> '' THEN 1 ELSE 0 END) AS MitZiel
FROM dbo.FaHierarchyNodes GROUP BY HauptFA
HAVING SUM(CASE WHEN LTRIM(RTRIM(ISNULL(Kommissionieren,''))) <> '' THEN 1 ELSE 0 END) > 0 ORDER BY MitZiel DESC;
SELECT Id FROM dbo.ProductionOrders WHERE OrderNumber = '<HauptFA>' AND SubOrderNumber = OrderNumber;

-- {HFA_BAUM}: Struktur mit Zeilen, deren Arbeitsgaenge GENAU "KA" sind, UND Zeilen mit KA neben anderen Kuerzeln
SELECT TOP 5 HauptFA, COUNT(*) AS Zeilen,
       SUM(CASE WHEN LTRIM(RTRIM(Arbeitsschritte)) = 'KA' THEN 1 ELSE 0 END) AS NurKA,       -- {ANZ_NUR_KA}
       SUM(CASE WHEN ' ' + Arbeitsschritte + ' ' LIKE '% KA %' THEN 1 ELSE 0 END) AS MitKA   -- {ANZ_MIT_KA}
FROM dbo.FaHierarchyNodes GROUP BY HauptFA
HAVING SUM(CASE WHEN LTRIM(RTRIM(Arbeitsschritte)) = 'KA' THEN 1 ELSE 0 END) > 0
   AND SUM(CASE WHEN ' ' + Arbeitsschritte + ' ' LIKE '% KA %' THEN 1 ELSE 0 END)
     > SUM(CASE WHEN LTRIM(RTRIM(Arbeitsschritte)) = 'KA' THEN 1 ELSE 0 END)
ORDER BY Zeilen;
-- Richtwerte: der Baum leitet die Kuerzel aus Arbeitsschritte ab (Views/FaHierarchy/_FaHierarchyNode.cshtml Z. 15)

-- {ID_EINGEHEND}: abgeschlossene Lagerbestellung (Status 3: Detailseite ohne Eingabefelder) mit >= 6 Positionen
SELECT TOP 5 r.Id, COUNT(*) AS Positionen
FROM dbo.WarehouseRequisitions r JOIN dbo.WarehouseRequisitionItems i ON i.WarehouseRequisitionId = r.Id
WHERE r.Status = 3 GROUP BY r.Id HAVING COUNT(*) >= 6 ORDER BY r.Id DESC;

-- {ARTNR_F1}, {ARTNR_F2}: zwei Artikelnummern im Reiter "Offene Fehlteile" (ShortageStatus 1, Liste Status 3/5)
SELECT TOP 10 i.ArticleNumber, COUNT(*) AS Anzahl
FROM dbo.WarehouseRequisitionItems i JOIN dbo.WarehouseRequisitions r ON r.Id = i.WarehouseRequisitionId
WHERE i.ShortageStatus = 1 AND r.Status IN (3, 5) GROUP BY i.ArticleNumber ORDER BY Anzahl DESC;

-- {BDE_BUCHUNGEN}: 0 => B10 KEIN_FALL
SELECT COUNT(*) AS Buchungen FROM dbo.BdeBookings;
```

---

## Teil B — Auftrag an Claude Cowork (ab hier kopieren)

```text
ABNAHME - Browser-Testprotokoll TS-91 Spaltenfilter-Suchsyntax (v1.58.1)
Protokoll: C:\Git\IDEAL-AKE-WMS\secondbrain\tests\2026-10-13-ts-91-browser-testprotokoll.md
Spec: 2026-09-23-suchsyntax-spaltenfilter-erweitern-spec
Adresse: NUR IDEAL http://idealweb01.ideal.ideal-ake.at:88/ (Schritte B1-B20). Benutzer G (Admin), kein Benutzerwechsel.
Es gelten die Regeln der Arbeitsweise ABNAHME aus den Projektanweisungen. Du pruefst, du reparierst nichts.

FREIGABEN: keine. Dieser Lauf ist rein LESEND.
Erlaubt: navigieren, Filter setzen und loeschen, Spalten ein-/ausblenden und verschieben, Seitengroesse aendern,
Druckansichten oeffnen, Screenshots. Nicht erlaubt: alles, was speichert, bucht, freigibt, kommissioniert,
abhakt, bestellt oder Einstellungen/Profile aendert. Keine Eingabe in Mengen-, Ist- oder Notizfelder.

ZUSAETZLICH FUER DIESEN LAUF
- Filter anwenden: In Server-Listen (FA-Liste, Leitstand, Artikel, Bewegungshistorie, BDE-Buchungen, Fehlteile,
  Beschichtung -> Auftraege, Vorschlagsliste, Kommissionierlisten, Lagerplaetze, Lagerbestellungen) wirkt ein
  Spaltenfilter erst mit ENTER (Seite laedt neu). In Client-Listen (Stueckliste, FA-Struktur-Baum,
  Beschichtungsauftrag-Detail, Eingehende Liste-Detail) wirkt er beim Tippen.
- Trefferzahl: In Server-Listen die Zahl "von N" der Seitennavigation notieren; in Client-Listen die sichtbaren
  Zeilen zaehlen.
- Werte ablesen ist in diesem Protokoll ausdruecklich vorgesehen, wo ein Schritt "<...>" in spitzen Klammern
  nennt: nimm einen Wert, der sichtbar in der Spalte steht, und notiere ihn im Bericht. Platzhalter in
  geschweiften Klammern kommen von Gerald; fehlt einer, frag vor dem Start.
- "jede sichtbare Zeile erfuellt ...": erste Seite (bis 25 Zeilen) ansehen; eine Zeile, die die Bedingung NICHT
  erfuellt, ist ein FEHLER (Zeile notieren, Screenshot).
- Hinweis unter einem roten Feld: Wortlaut EXAKT abschreiben. Form: "<Teil>: <Grund> — ignoriert" (mehrere
  Teile mit " · " getrennt).
- INFO-Schritte pruefen bekannte Grenzen. Trifft die Erwartung ein: Ergebnis "OK (INFO)", kein Fehler.
- Downloads nur in B12 und nur nach Frage im Chat. Druckansichten oeffnen ja, drucken nie (Druckdialog: Escape).
- Nach jedem Schritt alle gesetzten Filter loeschen (Feld leeren + ENTER bzw. Feld leeren).

TESTDATEN (von Gerald ausgefuellt; KEIN_FALL = Schritt-Teil NICHT PRUEFBAR)
- Bewegungshistorie: Stichtag {STICHTAG_BEW}; Sollzahlen Gesamt {BEW_GESAMT}, am Tag {BEW_AM_TAG},
  ab Tag {BEW_AB_TAG}, nach dem Tag {BEW_NACH_TAG}, bis Tagesende {BEW_BIS_TAG}, vor dem Tag {BEW_VOR_TAG};
  Bewegungsart genau Ausbuchung {BEW_AUS_GENAU}, enthaelt "ausbuchung" {BEW_AUS_ENTH}, ohne {BEW_NICHT_AUS}
- Beschichtung -> Auftraege: Stichtag {STICHTAG_BA}; Gesamt {BA_GESAMT}, Start ab Stichtag {BA_START_AB},
  Retour leer {BA_RETOUR_LEER}, Positionen > 2 {BA_POS_UEBER_2}
- Beschichtungsauftrag-Detail: /Beschichtungsauftraege/Details/{ID_BA}
- Escaping: Artikelnummer mit "_" {ARTNR_UNTERSTRICH}, die ein falsches "_" faelschlich traefe {ARTNR_FALSCHTREFFER};
  Bezeichnung mit "%" innen {TEXT_PROZENT}; FA-Liste Artikelnummer mit "_" {ARTNR_FA_UNTERSTRICH}
- Stueckliste: /Picking/Bom/{ID_BOM}
- FA-Struktur: HauptFA {HFA_BAUM} (Zeilen nur "KA": ca. {ANZ_NUR_KA}; Zeilen mit KA: ca. {ANZ_MIT_KA})
- Lager: Eingehende Liste (abgeschlossen): /WarehousePicking/Details/{ID_EINGEHEND}
- Fehlteile: Artikelnummern {ARTNR_F1} und {ARTNR_F2}
- BDE-Buchungen vorhanden: {BDE_BUCHUNGEN}

VOR DEM START
Version in der Fusszeile notieren (erwartet 1.58.1). Folgende Ansichten kurz oeffnen und Spaltenauswahl (Zahnrad)
sowie Seitengroesse notieren: FA-Liste, Leitstand -> Kommissionierung, Stammdaten -> Artikel, Lager ->
Bewegungshistorie, BDE -> Buchungen, Bestellungen -> Lager: Fehlteile, Stueckliste {ID_BOM}, FA-Struktur,
Beschichtung -> Auftraege, Beschichtungsauftrag {ID_BA}, Beschichtung -> Vorschlagsliste (Summiert),
Kommissionierung -> Kommissionierlisten (Summiert), Eingehende Liste {ID_EINGEHEND}, Stammdaten -> Lagerplaetze.
Am Ende (B20) wiederherstellen.

B1 Hilfe-Knopf "?" und Popover (TS-91.A6, TS-91.B9; AK 8)
Menue Fertigungsauftraege -> FA-Liste. In der Filterzeile (Zeile unter den Spaltenkoepfen) den Knopf "?" suchen.
Nur Tastatur: mit Tab bis zum Knopf, Enter druecken; Popover mit Escape schliessen; dasselbe mit der Leertaste.
OK, wenn: der Knopf in der ERSTEN SICHTBAREN Zelle der Filterzeile steht, per Tab erreichbar ist und einen
sichtbaren Fokusrahmen hat; Enter bzw. Leertaste ein Popover mit dem Titel "Filter-Syntax" oeffnen; das Popover
eine Tabelle mit den Zeilen 7% / %7 / =KA / KA;SW / !KA;SW / (leer) / >5 <=5 / 1...5 / KA,SW zeigt und darunter
den Satz "„Oder“ immer mit ; – in Zahl- und Datumsspalten ist das Komma Dezimalzeichen (kw41;kw42). Groß/Klein
egal."; Escape schliesst. Wenn ansehbar: Knopfbeschriftung fuer Screenreader "Hilfe zur Filtersyntax" (sonst
-> Teil C). Danach kurz in Stueckliste {ID_BOM} und FA-Struktur ansehen: "?" vorhanden. Screenshot.

B2 "?" wandert mit der Spalte (TS-91.B9)
Adresse /Beschichtungsauftraege/Details/{ID_BA}, Positionstabelle. Erste Spalte: "Vater / SubFA / Pos.".
Zahnrad: "Vater / SubFA / Pos." ausblenden.
OK (Teil 1), wenn "?" in die naechste sichtbare Filterzelle springt und das Popover weiter oeffnet.
Seite neu laden: OK (Teil 2), wenn "?" weiter in der ersten sichtbaren Filterzelle steht.
Spalte wieder einblenden, dann per Zahnrad eine andere Spalte an die erste Stelle verschieben:
OK (Teil 3), wenn "?" jeweils mitwandert und die Seite fluessig bleibt (kein Haengen, kein Flackern).
Notierte Spaltenauswahl wiederherstellen. Screenshot.

B3 FA-Liste: Textspalten (TS-91.A1; AK 1, Antwort 2)
FA-Liste. Zwei FA-Nummern <FA1>, <FA2> ablesen; <P> = die ersten 4 Zeichen von <FA1>; <E> = die letzten 2
Zeichen von <FA1>. Spalte "FA Nr." nacheinander (je ENTER, "von N" notieren):
a) <P>%   -> jede sichtbare FA-Nr. BEGINNT mit <P>
b) %<E>   -> jede sichtbare FA-Nr. ENDET mit <E>
c) =<FA1> -> nur Zeilen mit FA-Nr. genau <FA1>
d) <FA1>;<FA2> -> nur diese beiden
e) !<FA1>;<FA2> -> beide fehlen, die anderen sind da
Spalte "Kunde": (leer) -> nur Zeilen ohne Kunde; !(leer) -> nur Zeilen mit Kunde.
Spalte "Matchcode": einen Matchcode <M> ablesen; =<M in GROSSbuchstaben>, dann =<M in kleinbuchstaben>.
OK, wenn a)-e) wie beschrieben, (leer)/!(leer) passen, beide Schreibweisen von <M> dieselbe Trefferzahl liefern,
die Adresszeile den Filter traegt (colf_...) und ein Seitenwechsel den Filter behaelt. Kein Feld wird rot.

B4 FA-Liste: Datumsspalten (TS-91.A2; AK 2, Antwort 3, Regel 5/R1)
Aus Spalte "Fert.-Termin" ein Datum <D> (TT.MM.JJJJ) aus der Mitte der Liste und die Kalenderwoche <K> einer
Zeile ablesen. Spalte "Fert.-Termin" nacheinander: >=<D>, <<D>, <D>...<D plus 30 Tage>, =<D>, <D> (ohne
Operator), kw<K>, kw<K>;kw<K+1>, kw<K>,kw<K+1>, den Monat MM.JJJJ von <D>, dann Kalenderknopf in der Filterzelle
-> einen Tag waehlen.
OK, wenn: >=, < und Bereich nur passende Termine zeigen (jede sichtbare Zeile); =<D> und <D> nur Termine am Tag
<D>; kw<K> und MM.JJJJ wie bisher; kw<K>;kw<K+1> = beide Wochen; kw<K>,kw<K+1> = KEINE Treffer und KEIN rotes
Feld (das Komma ist in Datumsspalten Dezimalzeichen, der Wert wird als ein Text gesucht); der Kalender setzt ein
Datum und filtert. Spalte "KO-Termin": (leer) und !(leer) trennen sauber. Kein Feld wird rot.

B5 FA-Liste: Zahlspalte Prio und Dezimalkomma (TS-91.A2; AK 3)
Spalte "Prio" nacheinander: >2, 1...3, 3...1, =1, =1,0, >abc.
OK, wenn: >2 nur Prio > 2; 1...3 und 3...1 (vertauscht) dieselben Treffer 1 bis 3; =1 und =1,0 dieselben
Treffer (Komma = Dezimalzeichen); >abc -> Feld rot, darunter exakt ">abc: Zahl nicht lesbar — ignoriert",
Liste ungefiltert (gleiches "von N" wie ohne Filter). Zeigt Prio am Testsystem keine Werte: den Werte-Teil
NICHT PRUEFBAR, >abc trotzdem pruefen.

B6 FA-Liste: ungueltig bleibt rot nach Neuladen (TS-91.A4; AK 7, Antwort 6)
Spalte "Artikelnummer": 1...5, ENTER.
OK (Teil 1), wenn das Feld rot ist, darunter exakt "1...5: Bereich nur in Zahl- und Datumsspalten — ignoriert"
steht und die Liste ungefiltert ist ("von N" wie ohne Filter).
Seite neu laden (F5): OK (Teil 2), wenn das Feld weiter rot mit demselben Text ist.
Spalte "FA Nr.": >5;<FA1>, ENTER: OK (Teil 3), wenn rot mit ">5: Vergleich nur in Zahl- und Datumsspalten —
ignoriert" UND trotzdem nur <FA1> gefiltert ist.

B7 Leitstand (TS-91.B7, TS-91.B9; M2)
Menue Leitstand -> Kommissionierung. NICHTS freigeben, zuweisen oder abhaken; keine Auswahl-Haekchen setzen.
Spalte "Fert.-Termin": >=<D>, <D>...<D plus 30 Tage>, kw<K> (Werte aus B4 oder neu ablesen).
OK (Teil 1), wenn passende Termine erscheinen und KEIN Feld rot wird (seit v1.58.1 als Datum erkannt).
Spalte "Matchcode": %<Endung eines sichtbaren Matchcodes>: OK (Teil 2), wenn nur Matchcodes mit dieser Endung.
Spalte "VK" (nur wenn die Spalte Werte zeigt): =fertig, !offen, (leer). Die Spalte filtert auf die Texte
"offen" / "in bearbeitung" / "fertig"; leer = Arbeitsgang nicht anwendbar.
OK (Teil 3), wenn =fertig nur fertige, !offen keine offenen (aber leere und fertige) Zeilen zeigt. Zeigt VK
keine Werte: Teil 3 NICHT PRUEFBAR.
Spalte "Kunde": <K1>;<K2> (zwei sichtbare Kunden): OK (Teil 4), wenn genau diese beiden.

B8 Artikel + Escaping (TS-91.B1, TS-91.A3; AK 4, AK 10, Antwort 8)
Menue Stammdaten -> Artikel. Spalte "Artikelnummer" nacheinander: <P2>% (P2 = erste 4 Zeichen einer sichtbaren
Artikelnummer), %-1, =<eine sichtbare Artikelnummer>, 1...5.
OK (Teil 1), wenn <P2>% nur Beginnende, %-1 nur auf "-1" Endende, = genau diese Artikelnummer zeigt und 1...5 rot
mit "1...5: Bereich nur in Zahl- und Datumsspalten — ignoriert" ist.
Spalte "Artikelnummer": {ARTNR_UNTERSTRICH}. OK (Teil 2 Escaping), wenn jede Trefferzeile das "_" an genau
dieser Stelle enthaelt und {ARTNR_FALSCHTREFFER} NICHT unter den Treffern ist (bei KEIN_FALL nur der erste Teil).
Spalte "Bezeichnung": {TEXT_PROZENT} (Ausschnitt mit % innen). OK (Teil 3), wenn nur Bezeichnungen mit genau
dieser Zeichenfolge inklusive "%" erscheinen. Dann 50%: OK (Teil 4), wenn nur Bezeichnungen erscheinen, die mit
"50" BEGINNEN (ein % am Ende heisst "beginnt mit" - Absicht, kein Escaping-Fehler).
Spalte "Artikelnummer": >5. OK (Teil 5), wenn rot (die Artikelliste hat keine Zahl- oder Datumsspalte).
FA-Liste, Filterkarte "Artikelnummer" (oben, Knopf "Filtern"): {ARTNR_FA_UNTERSTRICH}. OK (Teil 6), wenn jede
Trefferzeile das "_" enthaelt. Filterkarte leeren, "Filtern".

B9 Bewegungshistorie: Datum/Zeit und Bewegungsart (TS-91.B2; AK 16, Antwort 3)
Menue Lager -> Bewegungshistorie, Filterkarte leer. "von N" ohne Filter = {BEW_GESAMT}?
Spalte "Datum/Zeit" nacheinander, je "von N" mit dem Soll vergleichen:
{STICHTAG_BEW} -> {BEW_AM_TAG}; ={STICHTAG_BEW} -> {BEW_AM_TAG}; >={STICHTAG_BEW} -> {BEW_AB_TAG};
>{STICHTAG_BEW} -> {BEW_NACH_TAG} (ab Folgetag 00:00); <={STICHTAG_BEW} -> {BEW_BIS_TAG} (bis 23:59);
<{STICHTAG_BEW} -> {BEW_VOR_TAG}; Monat MM.JJJJ und Jahr JJJJ des Stichtags (wie bisher, plausibel);
<Stichtag>...<Stichtag plus 30 Tage> (plausibel); <Tag1>;<Tag2> (zwei Tage mit Bewegungen) = Summe beider Tage.
Spalte "Bewegungsart": =ausbuchung -> {BEW_AUS_GENAU} (ohne "Sage-Ausbuchung"); ausbuchung -> {BEW_AUS_ENTH};
%ausbuchung -> {BEW_AUS_ENTH}; ausbuchung% -> {BEW_AUS_GENAU}; !ausbuchung -> {BEW_NICHT_AUS}.
OK, wenn jede Zahl genau dem Soll entspricht und kein Feld rot wird.
INFO (QA B-R3): "Datum/Zeit" kw<K> ohne Operator -> keine Treffer, nicht rot (war schon vor v1.58 so). OK (INFO),
wenn so.

B10 BDE-Buchungen (TS-91.B3, TS-91.B9; AK 17) - nur wenn {BDE_BUCHUNGEN} > 0 und Menue "BDE" sichtbar
Menue BDE -> Buchungen. Nichts korrigieren, nichts stornieren.
Spalte "Start": >=<Datum aus der Liste>; "Ende": <<Datum aus der Liste>; "Gut": >0, =0; "Ausschuss": =0, 0...1.
OK (Teil 1), wenn jede sichtbare Zeile die Bedingung erfuellt und KEIN Feld rot wird.
Spalte "Operator": %<Endung eines sichtbaren Namens in kleinbuchstaben>, dann dieselbe Endung in GROSSbuchstaben.
OK (Teil 2), wenn beide dieselbe Trefferzahl liefern (Gross/Klein egal).
Spalte "Typ": >1. OK (Teil 3), wenn rot (Textspalte).

B11 Fehlteile: Negation fuer alle Teile (TS-91.B4; Antwort 5)
Menue Bestellungen -> Lager: Fehlteile, Reiter "Offene Fehlteile". Spalte "Artikel-Nr" nacheinander:
a) {ARTNR_F1};{ARTNR_F2} -> beide da, sonst nichts
b) !{ARTNR_F1};{ARTNR_F2} -> BEIDE fehlen (weder noch), alle anderen da
c) {ARTNR_F1},{ARTNR_F2} -> wie a) (Komma als ODER in Textspalten, alte Gewohnheit unveraendert)
d) <Anfang von ARTNR_F1>% und ={ARTNR_F1}
OK, wenn a)-d) wie beschrieben; "von N" bei b) = (N ohne Filter) - (N aus a).
INFO (alte Gewohnheit, Antwort 5): {ARTNR_F1},!{ARTNR_F2}. Das "!" wirkt nur am Anfang des ganzen Werts;
"!{ARTNR_F2}" wird als Text gesucht und trifft nichts. Erwartet: nur {ARTNR_F1}, kein rotes Feld (vor v1.58.1
hiess dieselbe Eingabe "F1 und nicht F2"). OK (INFO), wenn so.
Spalte "Bezeichnung": 1...5 -> rot (die Liste hat keine filterbare Zahl- oder Datumsspalte).

B12 Stueckliste (TS-91.B5, TS-91.B9; AK 14)
Adresse /Picking/Bom/{ID_BOM}. NICHTS abhaken, nichts bestellen, keine Lagerbestellung.
Ausgangslage notieren: welcher Komm.-Ziel-Filter steht (z. B. "Alle Ziele (nur kommissionier-relevant)").
OK (Teil 1), wenn "?" in der Filterzeile steht und der Komm.-Ziel-Standardfilter wie bisher wirkt.
Komm.-Ziel-Dropdown: ein Einzelziel, dann zwei Ziele, dann "Alle Ziele". OK (Teil 2), wenn jeweils nur passende
Zeilen bleiben und darueberliegende Baugruppen aufklappen. Danach Ausgangslage wiederherstellen.
Spalte "Menge" (Zahl): >=10, 1...5, =1, =<ein Komma-Wert aus der Liste, z. B. 1,5>. OK (Teil 3), wenn nur
passende Mengen bleiben und kein Feld rot wird.
Spalte "Bezeichnung 1": =<eine sichtbare Bezeichnung>, (leer), <Teil1>;<Teil2>. OK (Teil 4), wenn passend.
Spalte "Bezeichnung 1": ein Wert ohne Treffer. OK (Teil 5), wenn ein Hinweis statt einer stummen leeren Tabelle
erscheint (bei aktivem Komm.-Ziel-Filter: "Standardfilter „…“ aktiv — keine Treffer in dieser Stückliste.").
INFO (QA M-2): Spalte "Ebene": >2 -> rot ("Vergleich nur in Zahl- und Datumsspalten — ignoriert"), Liste
ungefiltert. OK (INFO), wenn so.
Optional, nur nach Ja im Chat: Menge >=10 setzen, "CSV" -> Dateiname notieren; Inhalt -> Teil C.

B13 FA-Struktur-Baum (TS-91.B6, TS-91.B9; AK 15)
Menue FA-Struktur, Filterkarte FA-Nr. {HFA_BAUM}, Struktur aufklappen ("Alle aufklappen"). Zaehler ueber dem
Baum notieren.
Spalte "Arbeitsgaenge" (Platzhalter "z. B. KA") nacheinander: ka, =ka, ka;pg, !ka.
OK (Teil 1), wenn: ka -> Zeilen mit KA (ca. {ANZ_MIT_KA}) samt Pfad; =ka -> NUR Zeilen, deren Arbeitsgaenge
GENAU "KA" sind (ca. {ANZ_NUR_KA}) - Zeilen wie "LS KA PG" sind bei =ka KEINE Treffer, weil = den ganzen
Zelltext vergleicht; ka;pg -> KA oder PG; !ka -> Zeilen ohne KA; der Zaehler zeigt "<X> von <Y> Knoten
(Spaltenfilter aktiv)"; Pfad-/Kontextzeilen wie vor v1.58.
Spalte "Struktur": <erste 4 Zeichen einer sichtbaren Artnr.>% -> nur Beginnende.
Auswahlfilter "Arbeitsbereich" und "Status": je einen Wert waehlen. OK (Teil 2), wenn genau dieser Wert (keine
Teiltreffer) - wie vor v1.58.
Spalte "Arbeitsgaenge": >5. OK (Teil 3), wenn rot mit Hinweistext.
INFO (QA M-2): Spalte "Soll / Fert.": >2 -> rot, ignoriert (Textspalte). OK (INFO), wenn so.

B14 Beschichtung -> Auftraege (Server-Liste ueber Apply; TS-91.B8; TS-91.A7 in der v1.58.1-Fassung)
Menue Beschichtung -> Auftraege, kein Statusfilter. "von N" = {BA_GESAMT}?
Spalte "Start": >={STICHTAG_BA} -> {BA_START_AB}. Spalte "Retour": (leer) -> {BA_RETOUR_LEER}.
Spalte "Positionen": >2 -> {BA_POS_UEBER_2}; dann 2...2 und =2 (gleiche Trefferzahl).
Spalte "Dienstleister": =<ein sichtbarer Dienstleister>, <DL1>;<DL2>, %<Endung>.
Spalte "Angelegt am": >=<ein Datum aus der Spalte>. Kalenderknopf in "Start", "Retour", "Angelegt am" vorhanden.
OK, wenn die Zahlen dem Soll entsprechen, die uebrigen Filter passen und kein Feld rot wird.
Spalte "Nummer": 1...5 -> rot.

B15 Beschichtungsauftrag-Detail (Client) + Druck-Banner (TS-91.B8; QA Epic I-1, Checkliste 20)
a) /Beschichtungsauftraege/Details/{ID_BA}, Positionstabelle: "Breite" >500 und =<Komma-Wert aus der Liste,
   z. B. 2,5>; "Menge" >1, 1...3; "Artnr." %<Endung>, <A1>;<A2>.
   OK (Teil 1), wenn passend und kein Feld rot; der Komma-Wert trifft genau diese Zeilen.
b) Menue Beschichtung -> Vorschlagsliste (Summiert). Spalte "Summe Sollmenge": >10, ENTER. "von N" notieren.
   "Drucken" (Druckansicht im neuen Tab; NICHT drucken, Dialog mit Escape).
   OK (Teil 2), wenn die Druckansicht das Banner "Gefilterte Ansicht — dieses Dokument zeigt nur die gefilterten
   ..." zeigt und nur Zeilen mit Summe Sollmenge > 10 enthaelt. Filter loeschen, erneut "Drucken":
   OK (Teil 3), wenn KEIN Banner. Tabs schliessen.
c) Wie b) in Kommissionierung -> Kommissionierlisten (Summiert), Spalte "Summe Sollmenge" >10.
   OK (Teil 4), wenn Banner mit Filter und kein Banner ohne Filter.

B16 Client = Server: gleiche Eingabe, gleiche Treffer (TS-91.A5; AK 6)
Adresse /WarehousePicking/Details/{ID_EINGEHEND} (abgeschlossene Liste ohne Eingabefelder). Nur in die
Filterzeile tippen.
Nacheinander in der Tabelle (Client): "Bezeichnung" =<eine sichtbare Bezeichnung>; "Bezeichnung" <Teil1>;<Teil2>;
"Bestellt" >1; "Pos" 2...5. Je Eingabe sichtbare Zeilen zaehlen, dann "Drucken" (Druckansicht im neuen Tab, filtert
auf dem Server; NICHT drucken) und dort die Positionen zaehlen.
OK (Teil 1), wenn je Eingabe Bildschirm und Druckansicht dieselben Positionen in derselben Anzahl zeigen.
Tabelle: Eingabe | Zeilen Bildschirm | Zeilen Druck.
"Bezeichnung" 1...5: OK (Teil 2), wenn rot und die Druckansicht ALLE Positionen zeigt (der Server ignoriert den
ungueltigen Teil genauso).

B17 Alte Eingaben und Komma-Regel (TS-91.A8, TS-91.B10)
FA-Liste, Spalte "Werkbank": <W1>,<W2> (zwei sichtbare Werkbaenke, mit KOMMA), dann <W1>;<W2>.
OK (Teil 1), wenn beide dieselbe Trefferzahl liefern (Komma als ODER in Textspalten unveraendert).
Spalte "Artikelnummer": !<Anfang einer sichtbaren Artikelnummer>. OK (Teil 2), wenn diese Artikel fehlen.
Beschichtungsauftrag-Detail {ID_BA}, Spalte "Menge" (Zahl): 960,886. OK (Teil 3), wenn KEIN ODER entsteht: kein
rotes Feld, nur Zellen, die "960,886" enthalten (meist keine Treffer). Erwartete Aenderung gegenueber vor v1.58.

B18 Stichprobe uebrige Listen und bekannte Grenzen (TS-91.B8; Checkliste 19, 23)
a) Stammdaten -> Lagerplaetze: "Erstellt am" <01.01.2026; "Kapazitaet" >0. OK, wenn passend und nicht rot.
b) Bestellungen -> Lagerbestellungen: "#" >100. OK, wenn passend.
   INFO (QA M-3): "Submit" (leer) -> Zeilen, die dort "—" zeigen (nicht abgeschickt), werden NICHT gefunden.
   OK (INFO), wenn so.
c) Fertigungsauftraege -> FA-Abarbeitungsliste (falls im Menue): "Stk." >1 -> eine Zeile mit Stk. 2 ist dabei
   (AK 18). Fehlt der Menuepunkt: NICHT PRUEFBAR.
d) Fertigungsauftraege -> FA-Vervollstaendigen (falls im Menue): INFO (QA M-2): "Anwendbar" >2 und
   "Vervollständigt" >2 -> rot, ignoriert. OK (INFO), wenn so.

B19 Hilfeseite (TS-91.A6, TS-91.B9; AK 9)
Fusszeile -> Hilfe, Karte "Listen-Pagination & Filter (seit v1.14.0)", Eintrag "Filter-Syntax (seit v1.58.0)".
OK, wenn dort die Syntax-Liste steht, der Satz "Seit v1.58.1 gilt das in allen Listen mit Spaltenfilter" und ein
blauer Info-Kasten "Neu: „oder“ mit Semikolon." mit dem Satz zu den Fehlteil-Listen ("!a;b heisst weder a noch
b"). Screenshot des Info-Kastens (fuer Teil C).

B20 Anzeige-Einstellungen wiederherstellen
Alle Filter loeschen, die zu Beginn notierte Spaltenauswahl und Seitengroesse aller Ansichten aus VOR DEM START
wiederherstellen, 5 Sekunden warten, neu laden und pruefen.

BERICHT
Wie in den Projektanweisungen: Chat-Tabelle, Berichtsordner
secondbrain\tests\<Datum>-ts-91-browser-testbericht\ und am Ende der Spec genau eine Verweiszeile.
Zusaetzlich im Bericht: alle abgelesenen Werte (<FA1>, <D>, <K>, ...), je Filter "von N" bzw. Zeilenzahl, den
exakten Wortlaut jedes roten Hinweises, die Tabelle aus B16 und den Dateinamen aus B12 (falls geladen).
```

---

## Teil C — Nur durch den Menschen (nach dem Lauf)

**C1 — Anwender-Ankündigung „;“ = ODER (Info-Kasten Hilfeseite).** [ ]
Fußzeile › Hilfe › Karte „Listen-Pagination & Filter (seit v1.14.0)“ › „Filter-Syntax (seit v1.58.0)“ → Info-Kasten „Neu:
„oder“ mit Semikolon.“ (`Views/Help/Index.cshtml` Z. 234–240; Screenshot aus B19). Prüfen: Wortlaut verständlich für Lager,
Fertigung und Einkauf; Fehlteil-Satz `!a;b` = weder a noch b vorhanden. Entscheiden, wer zusätzlich informiert wird (Spec,
Abschnitt Deploy: „Anwender informieren: „oder“ heißt jetzt `;` …; Fehlteile `!a;b` = weder a noch b“) — mindestens die
Fehlteil-Nutzer, weil eine gewohnte Eingabe `a,!b` dort **ohne Rot-Markierung** ihre Bedeutung ändert (B11 INFO: nur noch a).
P-1-Treffer aus A2 persönlich informieren.

> [!note] Zusatzfund (nicht Teil dieses Laufs)
> Der Absatz „Andere Listen“ derselben Hilfe-Karte (`Views/Help/Index.cshtml` Z. 243–249) nennt BDE-Buchungen als Liste, die nur
> clientseitig auf der aktuellen Seite filtert. `Views/BdeBookings/Index.cshtml` Z. 82 trägt `data-server-column-filter="true"` —
> der Satz ist veraltet. Melden, nicht hier beheben.

**C2 — Screenreader und Kontrast (TS-91.A6, AK 8).** [ ]
NVDA/Sprachausgabe: am Filterfeld wird der Kurztext vorgelesen („Filter: 7% beginnt mit, %7 endet mit, …“, `aria-describedby`,
`column-filter-syntax.js` Z. 124–125); am „?“ „Hilfe zur Filtersyntax“; bei rotem Feld zusätzlich der Hinweistext. Kontrast „?“
(#495057) und roter Hinweis (#b02a37) auf der grauen Filterzeile messen (Soll ≥ 4,5:1; QA Etappe A M-1 gerechnet 7,8:1 bzw.
6,2:1), am Fertigungsterminal ansehen.

**C3 — P-1 vorher/nachher (TS-91.B10).** [ ]
Für jeden P-1-Treffer aus A2: die betroffene Liste mit dem gespeicherten Standardfilter öffnen und mit der Erwartung aus der
Tabelle in A2 vergleichen; Korrektur bzw. Information des Benutzers notieren.

**C4 — AKE-Flachmodus (TS-91.A Vorbedingung).** [ ]
Teil B läuft nur auf IDEAL. Am AKE-Testsystem (Master `ProduktionsauftragHierarchisch` aus) eine Stichprobe in der FA-Liste:
FA Nr. `<Präfix>%`, `=<FA>`, `<FA1>;<FA2>`, Fert.-Termin `>=<Datum>`, Artikelnummer `1...5` → rot. Prio und KO-Termin gibt es dort
nicht (nur hierarchisch gerendert, `Views/ProductionOrders/Index.cshtml` Z. 87–93).

**C5 — CSV der Stückliste (falls in B12 geladen).** [ ]
Datei in Excel: genau die gefilterten Zeilen (Menge ≥ 10), auch unter zugeklappten Baugruppen.

**Aufräumen:** nichts — Teil B schreibt nicht. Anzeige-Einstellungen von G prüfen, falls der Bericht zu B20 eine Abweichung nennt.

**Bericht des Agenten** liegt als eigener Ordner unter `secondbrain\tests\`; die Spec trägt am Ende eine Verweiszeile. Jeden
FEHLER als Befund in die Spec eintragen.

---

## Übersicht der Schritte

| Schritt | TS-Nummer | Checkliste (Spec) | schreibend | Vorbedingung |
|---|---|---|---|---|
| A1 | Deploy | 4 | – | Publish v1.58.1 |
| A2 | P-1, TS-91.B10 | 1 | – | IDEAL- und AKE-DB |
| A3 | P-2 | 2 | – | IDEAL- und AKE-DB |
| A4 | P-3 | 3 | – | beide Testsysteme |
| A5 | TS-91.B1 Laufzeit, AK 12 | 12 | – | SSMS, IDEAL-Testsystem-DB |
| A6 | Testdaten | – | – | IDEAL-WMS-DB |
| B1 | TS-91.A6, B9 | 10, 21 | nein | – |
| B2 | TS-91.B9 | 21 | nein | `{ID_BA}` |
| B3 | TS-91.A1 | 5 | nein | A3 (Groß/Klein) |
| B4 | TS-91.A2 | 6 | nein | – |
| B5 | TS-91.A2 | 6 | nein | A4 |
| B6 | TS-91.A4 | 8 | nein | – |
| B7 | TS-91.B7, B9 | 18 | nein | Leitstand an |
| B8 | TS-91.B1, A3 | 7, 12 | nein | `{ARTNR_UNTERSTRICH}`, `{TEXT_PROZENT}`, `{ARTNR_FA_UNTERSTRICH}` |
| B9 | TS-91.B2 | 13 | nein | Sollzahlen `{BEW_*}` |
| B10 | TS-91.B3, B9 | 14 | nein | BdeAktiv an, `{BDE_BUCHUNGEN}` > 0 |
| B11 | TS-91.B4 | 15 | nein | LagerbestellungAktiv, `{ARTNR_F1}`, `{ARTNR_F2}` |
| B12 | TS-91.B5, B9 | 16, 23 | nein (Download optional) | `{ID_BOM}` |
| B13 | TS-91.B6, B9 | 17, 23 | nein | `{HFA_BAUM}` |
| B14 | TS-91.B8, A7 | 11, 19 | nein | Sollzahlen `{BA_*}` |
| B15 | TS-91.B8, QA I-1 | 19, 20 | nein (Druckansicht) | `{ID_BA}`, Beschichtung + Kommissionierlisten an |
| B16 | TS-91.A5 | 9 | nein (Druckansicht) | `{ID_EINGEHEND}` |
| B17 | TS-91.A8, B10 | 11, 22 | nein | `{ID_BA}` |
| B18 | TS-91.B8 | 19, 23 | nein | Lagerbestellung / FA-Vervollständigung im Menü |
| B19 | TS-91.A6, B9 | 21 | nein | – |
| B20 | Anzeige-Einstellungen | – | nein | – |
| C1 | Ankündigung „;“ | – | – | B19, A2 |
| C2 | TS-91.A6 | 10 | – | Screenreader |
| C3 | TS-91.B10 | 22 | – | A2-Treffer |
| C4 | TS-91.A Vorbedingung | – | – | AKE-Testsystem v1.58.1 |
| C5 | TS-91.B5 | 16 | – | Datei aus B12 |
