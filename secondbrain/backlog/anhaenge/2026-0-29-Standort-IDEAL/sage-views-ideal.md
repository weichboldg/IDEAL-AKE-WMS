# IDEAL: SAGE-Views für Kommissionierung, Vormontage, Beschichtung

> **Kontext für KI-Assistenten** — Ergänzung zur `CLAUDE.md`.
> Beschreibt die zwei SAGE-Views, die bei der IDEAL-Installation die Datenbasis
> für die neuen Feature-Bereiche liefern (Vormontage, Beschichtungsauftrag,
> Kommissionierlisten). Standort AKE ist nicht betroffen — dort bleibt die
> bisherige Datenquelle (`vw_AKE_Kommissionierung_WAListe`) aktiv.

---

## Standort-Kontext

Das WMS läuft in einem **gemeinsamen Codestamm** für die Standorte **AKE** und
**IDEAL**. Beide nutzen SAGE100 als ERP, aber unterschiedlich stark ausgeprägt:

- **AKE**: bestehende Datenbasis, unveraendert (`vw_AKE_Kommissionierung_WAListe`).
- **IDEAL**: zwei neue Views + drei neue Features (Vormontage, Beschichtung,
  Kommissionierlisten). Zusaetzlich existiert bei der IDEAL eine
  **PPS-Abteilung**, deren Termindaten in einer separaten View bereitstehen.

Beide Standort-Datenquellen liefern in dieselben Repositories/Services. Der
Aufloesungspfad wird pro Feature durch einen Standort-Switch entschieden (siehe
"Vorgeschlagene Integration" unten).

---

## Views-Uebersicht

| View | Zweck | Standort |
|---|---|---|
| `dbo.[vw_IDEAL-AKE_Kommissionierung_FAListe]` | Stueckliste je Struktur (Haupt-FA + alle Sub-FAs + Blaetter) | IDEAL |
| `dbo.[vw_IDEAL-AKE_Kommissionierung_FAInfos]` | PPS-Termine, Kunde, Beschichtung je Auftrag | IDEAL |

**Verknuepfungsschluessel:** `HauptFA` (in FAListe = `KHKPpsFaBelege.StrukturID`,
in FAInfos = `PPSData.FA_Nr`). Ein Datensatz aus FAInfos referenziert genau eine
Struktur in FAListe.

**Datenbank / Server:** dieselbe Instanz wie das WMS
(`AKESQL20.ake.at`, DB `IDEAL_TEST_2026_05_03` bzw. produktiv analog).
Kein Linked Server noetig, Cross-DB-Joins auf demselben Server sind performant.

---

## View 1: `vw_IDEAL-AKE_Kommissionierung_FAListe`

**Zweck:** flache Liste aller Stuecklistenpositionen einer Struktur.
Ausgangspunkt ist `KHKPpsFaBelegePositionen` (mit `RessourceTyp='MA'`), nicht
`KHKPpsFaBelege` — das vermeidet Duplikate durch verwaiste Belege.

**Granularitaet:** eine Zeile = eine Position bzw. der Hauptauftrag selbst
(Wurzel).

### Spalten

| Spalte | Typ | Quelle | Beschreibung |
|---|---|---|---|
| `HauptFA` | int | `KHKPpsFaBelege.StrukturID` | Fertigungsauftragsnummer des kompletten Geraets. **Der einzige FA-Bezeichner, der in der Produktion kommuniziert wird** — auch Barcodes, Rueckmeldungen etc. laufen ueber diesen Wert. |
| `HauptArtnr` | nvarchar | `KHKPpsFaBelege.Artikelnummer` (Wurzel) | Artikelnummer des Geraets |
| `VaterFA` | int NULL | `KHKPpsFaBelegePositionen.BelID` | BelID des uebergeordneten Sub-FA (in dessen Stueckliste diese Position steht). **NULL bei der Wurzel** (Hauptauftrag selbst). |
| `Position` | int NULL | `KHKPpsFaBelegePositionen.Position` | Position innerhalb der Vater-Stueckliste. NULL bei der Wurzel. |
| `SubFA` | int | `KHKPpsFaBelegePositionen.BelIDNachfolger` | Eigene BelID des Nachfolgers (Baugruppe mit eigenem FA). **`0` bedeutet Blatt** (Kaufteil / Endmaterial ohne eigenen FA). |
| `Artnr` | nvarchar | `KHKPpsFaBelegePositionen.RessourceNummer` | Artikelnummer des Bauteils / der Baugruppe |
| `Matchcode` | nvarchar | Position bevorzugt, `KHKArtikel` als Fallback | **In der Produktion das primaere Identifikationsfeld** (Barcode, Kommissionier-Scan). `Bezeichnung1/2` werden dort kaum verwendet. |
| `Bezeichnung1`, `Bezeichnung2` | nvarchar | Sub-FA-Beleg bevorzugt, Stammdaten als Fallback | Zusatzinfos, v.a. bei Zukaufteilen aussagekraeftig |
| `Sollmenge` | decimal | `KHKPpsFaBelegePositionen.Menge` | Benoetigte Menge (Kommissionier-Bedarf) |
| `Fertigungmenge` | decimal | `KHKPpsFaBelegePositionen.Menge` | Zu fertigende Menge (Produktions-Bedarf) |
| `Beschaffungsartikel` | nvarchar (`Ja`/`Nein`) | `KHKArtikel.IstBestellartikel` | Zukauf? |
| `Artikelgruppe` | nvarchar | `KHKArtikel.Artikelgruppe` + `KHKArtikelgruppen.Bezeichnung` | Format: `"CODE - Bezeichnung"` (**siehe Fallstrick unten** — analog zum bestehenden Article-Matching bei der AKE) |
| `Hauptlagerplatz` | nvarchar | `KHKLagerplaetze.Kurzbezeichnung` | Standardlagerplatz aus dem Artikelstamm |
| `BemerkungPN` | nvarchar | Position bevorzugt, Beleg als Fallback | Bemerkungen aus dem Blechfertigungsprogramm (PN = "Positionsnotiz") |
| `FertigungsInfoPN` | nvarchar | Position bevorzugt, Beleg als Fallback | Zusaetzliche Fertigungsinfos aus dem Blechfertigungsprogramm |
| `Kommissionieren` | nvarchar | Position bevorzugt, Beleg als Fallback | Kommissionier-Ziel (leer = **nicht** zu kommissionieren) — **Kern-Filter der Kommissionierlisten** |
| `Arbeitsbereich` | nvarchar | Position bevorzugt, Beleg als Fallback | Bearbeitungsort. **Nicht zu verwechseln mit `[Montage-Abteilung]` aus FAInfos** — Arbeitsbereich ist auf Bauteil-Ebene, Montage-Abteilung auf Auftragsebene. |
| `Arbeitsschritte` | nvarchar | Position bevorzugt, Beleg als Fallback | **Leerzeichen-getrennte** Liste der Schritte (nicht Komma!) |
| `Artikeltyp` | nvarchar | Position bevorzugt, Beleg als Fallback | Zukaufteil / Baugruppe / ... |
| `Beschichtet` | smallint (`-1`/`0`) | Position bevorzugt, Beleg als Fallback | `-1` = wird beschichtet — **Filter fuer Beschichtungsauftrag** |
| `Material` | nvarchar | Position bevorzugt, Beleg als Fallback | Rohmaterial |
| `Breite`, `Hoehe`, `Tiefe` | decimal | Position bevorzugt, Beleg als Fallback | Bauteilmasse |
| `EKBedarf` | Bit/Ja-Nein | Position bevorzugt, Beleg als Fallback | Muss der Einkauf bestellen? |
| `VMBedarf` | nvarchar | Position bevorzugt, Beleg als Fallback | Vormontage-Bereich — **Filter fuer Vormontage-Listen** |

### Baum-Beziehung

Die View ist **flach**, aber ueber `HauptFA` / `VaterFA` / `SubFA` laesst sich
der Baum client- oder server-seitig aufbauen:

- **Wurzel** (Hauptauftrag): `VaterFA IS NULL`, `Position IS NULL`, `SubFA = HauptFA`
- **Baugruppe** (Sub-FA mit eigener Stueckliste): `SubFA <> 0`, es gibt weitere Zeilen mit `VaterFA = SubFA`
- **Blatt** (Kaufteil / Endmaterial): `SubFA = 0`, keine weiteren Zeilen darunter

Beispielabfrage (alle Zeilen einer Struktur, hierarchisch sortiert):
```sql
SELECT * FROM dbo.[vw_IDEAL-AKE_Kommissionierung_FAListe]
WHERE HauptFA = 1042458
ORDER BY VaterFA, Position;
```

---

## View 2: `vw_IDEAL-AKE_Kommissionierung_FAInfos`

**Zweck:** PPS-Auftragsdaten (Kunde, Termine, Beschichtung, Priorisierung).
Quelle ist die Cross-DB-Sicht `[PPSImport].[dbo].[vw_PPSData]` — die Daten
werden aus der PPS-Anwendung der IDEAL importiert.

**Granularitaet:** eine Zeile = ein Auftrag (Kombination aus `ABNr` + `Pos`).
Kombinationsgeraete koennen **denselben `HauptFA` mit unterschiedlichem
`[Montage-Abteilung]`** haben — die Abteilung ist dann der zusaetzliche
Identifier.

### Spalten

| Spalte | Beschreibung |
|---|---|
| `ABNr` | Sage-Auftragsbestaetigungsnummer |
| `Pos` | Position innerhalb der AB |
| `Kunde` | Endkunde |
| `KO_Termin` | Konstruktions-Termin |
| `FE_Termin` | Wann muss die **Blechfertigung** fertig sein |
| `[Montage-Abteilung]` | In welcher Abteilung das Geraet zusammengebaut wird. **Zweiter Identifier bei Kombinationsgeraeten** (gleicher HauptFA, verschiedene Abteilungen). |
| `HauptFA` | **Verknuepfungs-Schluessel zur FAListe-View** |
| `Status` | PPS-Status; leer/`-` bevor die Konstruktion beginnt (siehe Datenverfuegbarkeits-Regel unten) |
| `Start_Beschichtung` | Wann Beschichtung starten |
| `Dienstleister` | Wer beschichtet |
| `Montagestunden` | Kalkulierte Montagestunden |
| `Prio` | Prioritaet |
| `RAL` | Beschichtungsfarbe |
| `Beschichten_Retour` | Rueckliefer-Datum vom Beschichter |
| `Neuer_PT_PPS` | Aktueller PPS-Fertigstellungstermin |
| `Verladetermin_Vsl` | Voraussichtlicher Verladetermin |
| `Bemerkung_Uhrzeit` | Spezielle Uhrzeit bei der Verladung |

### Datenverfuegbarkeits-Regel

Die View filtert bereits im WHERE:

```sql
WHERE FA_Nr IS NOT NULL AND FA_Nr <> '' AND FA_Nr <> '-'
  AND [Status] IS NOT NULL AND [Status] <> '' AND [Status] <> '-'
```

**Bedeutung:** Ein Auftrag erscheint erst dann in FAInfos, wenn
- eine FA-Nummer eingetragen wurde **und**
- der PPS-Status gefuellt ist (also mindestens `Konstruktion`).

**Konsequenz fuer FAListe-Anzeige:** Auch wenn eine Stueckliste physisch schon
in Sage existiert, soll sie **im WMS erst angezeigt werden, wenn ein
FAInfos-Eintrag zum HauptFA existiert**. Das stellt sicher, dass die PPS-Daten
konsistent sind, bevor Werker mit dem Auftrag arbeiten.

**Empfohlene Umsetzung im Repository:**
```sql
-- FAListe JOIN FAInfos (INNER JOIN, kein LEFT JOIN)
SELECT f.*
FROM dbo.[vw_IDEAL-AKE_Kommissionierung_FAListe] f
INNER JOIN dbo.[vw_IDEAL-AKE_Kommissionierung_FAInfos] i
    ON i.HauptFA = f.HauptFA
WHERE f.HauptFA = @HauptFA;
```

---

## Feature-Anforderungen IDEAL

### A. Vormontage-Listen

**Zweck:** Jeder Vormontage-Arbeitsbereich (aus Feld `VMBedarf`) bekommt eine
Liste seiner in der kommenden Woche vorzubereitenden Teile.

**Benoetigte Spalten:**
FAListe: `HauptFA`, `HauptArtnr`, `Artnr`, `Matchcode`, `Sollmenge`,
`Fertigungmenge`, `BemerkungPN`, `VMBedarf`
FAInfos: `FE_Termin`, `[Montage-Abteilung]`, `Prio`, `Neuer_PT_PPS`,
`Verladetermin_Vsl`

**Drei Sichten:**
1. Einzelne Teile (flache Liste)
2. Summierte Liste (aggregiert nach Artikel)
3. Export in Zwischenablage — Format kompatibel zum **Isolierfraesen-Software-Import**

**Referenz-Bild:** siehe `Konzept_Übertrag_MDE_System.docx` Vormontage-Screenshot
(Excel-Ansicht mit gruppierten Reitern pro Arbeitsbereich).

### B. Beschichtungsauftrag

**Zweck:** Druckbares Dokument, das die Teile beim Transport zum
Beschichtungs-Dienstleister begleitet.

**Filter:** `Beschichtet = -1`.

**Benoetigte Spalten:**
FAListe: `HauptFA`, `HauptArtnr`, `Artnr`, `Matchcode`, `Sollmenge`,
`Beschichtet`, `Breite`, `Hoehe`, `Tiefe`
FAInfos: `ABNr`, `Pos`, `[Montage-Abteilung]`, `HauptFA`, `Start_Beschichtung`,
`Dienstleister`, `RAL`, `Beschichten_Retour`

**Format:** vorbereiteter Ausdruck mit Dienstleister-Kopfdaten,
Farbausfuehrung, Kontaktdaten, Liefer-/Retourdatum, Teile-Tabelle.

### C. Kommissionierlisten

**Zweck:** Filterbare Kommissionierlisten je Kommissionier-Ziel (Feld
`Kommissionieren`).

**Filter:** `Kommissionieren IS NOT NULL AND Kommissionieren <> ''` (also
"alle Teile, bei denen ein Kommissionier-Ziel eingetragen ist").
Zusaetzlich Filter nach dem konkreten Ziel-Wert.

**Benoetigte Spalten:**
FAListe: `HauptFA`, `HauptArtnr`, `SubFA`, `Artnr`, `Matchcode`, `Sollmenge`,
`Hauptlagerplatz`, `Kommissionieren`, `Arbeitsbereich`, `Artikeltyp`,
`Beschichtet`, `Material`
FAInfos: `ABNr`, `HauptFA`

**Format:** je ein Ausdruck pro Auftrag mit Barcode `HauptFA` + Auftragskopf,
danach Positionstabelle.

---

## Bekannte Fallstricke

- **`HauptFA` ist der einzige Produktions-Identifier.** Das Feld ist immer die
  `StrukturID` des Haupt-Fertigungsauftrags. `SubFA` (die BelIDs der Sub-FAs)
  wird in Barcodes und Kommunikation **nicht** verwendet — auch wenn Werker
  einen Sub-FA scannen, muessen wir intern auf HauptFA aufloesen.
- **Kombinationsgeraete mit gleichem HauptFA:** `[Montage-Abteilung]` aus
  FAInfos ist bei diesen Faellen der weitere Identifier. FAListe kann sie
  nicht unterscheiden — Aufsplittung passiert erst beim Join mit FAInfos.
- **`Arbeitsbereich` vs. `[Montage-Abteilung]`:** unterschiedliche Ebenen:
  `Arbeitsbereich` (FAListe, aus `USER_OSAbteilung`) ist **positionsbezogen**;
  `[Montage-Abteilung]` (FAInfos) ist **auftragsbezogen**. Nie verwechseln.
- **`SubFA = 0` markiert ein Blatt** (Kaufteil oder Endmaterial ohne eigenen
  FA). Diese Zeilen haben typischerweise keinen Sub-FA-Beleg — der `sub`-Join
  in der View greift dann bewusst nicht.
- **Matchcode-Fallback-Reihenfolge:** Position bevorzugt, dann Stammdaten.
  Der Sub-FA-Beleg kann fuer denselben Artikel einen abweichenden Matchcode
  fuehren; im FA-Kontext ist der Position-/Beleg-Matchcode "wahrer" als der
  Stammdaten-Matchcode. Beispiel: Artikel `50001934` hat Stammdaten-Matchcode
  `G-KT-Korpus-...`, aber als Zuschnitt-Position im FA den Matchcode
  `FR-4301-...`. Fuer Fertigungssicht der zweite.
- **`Artikelgruppe`-Format `"CODE - Bezeichnung"`:** Bei Vergleichen mit der
  `Articles`-Tabelle **nur den Code verwenden**. Pattern analog zum
  bestehenden BOM-Matching (vgl. Fallstrick "Artikelgruppe BOM vs Articles"
  in der Haupt-CLAUDE.md): `articleGroup.Split(new[] { " - " }, 2, StringSplitOptions.None)[0].Trim()`.
- **`Arbeitsschritte` sind LEERZEICHEN-getrennt, nicht Komma.** Beim Split
  drauf achten.
- **`Beschichtet = -1`** ist der Aktiv-Wert (Sage-Boolean-Konvention), nicht
  `1`. Filter entsprechend: `WHERE Beschichtet = -1`.
- **Datenverfuegbarkeits-Regel PPS-Status:** Ohne FAInfos-Eintrag **keine**
  FAListe-Anzeige (INNER JOIN, siehe oben). Sonst arbeitet die Produktion mit
  unvollstaendigen PPS-Daten.
- **Duplikate ohne Sollstarttermin:** Historisch gab es in
  `KHKPpsFaBelege` Duplikat-Belege ohne `Sollstarttermin`. Das Problem hat
  sich in den aktuellen Daten erledigt; die View filtert die Duplikate
  jetzt implizit ueber die `KHKPpsFaBelegePositionen`-Hierarchie
  (`BelIDNachfolger`). Falls in Zukunft wieder Duplikate auftauchen,
  ist `Sollstarttermin IS NOT NULL` der Wiederherstellungs-Filter.
- **Cross-DB-Zugriff FAInfos → PPSImport:** Die Kette ist
  `IDEAL_TEST_2026_05_03.dbo.vw_IDEAL-AKE_Kommissionierung_FAInfos`
  → `PPSImport.dbo.vw_PPSData`. Selber Server, daher performant. Wenn die
  PPS-DB umzieht oder umbenannt wird, muss der View-Text angepasst werden.

---

## Vorgeschlagene Integration ins WMS

Analog zum bestehenden `BomRepository` / `CachedBomRepository`-Muster
(Repository Pattern + MemoryCache-Decorator, siehe `CLAUDE.md` Abschnitt
"Architektur"):

- **`IFaListeRepository`** — liest FAListe + FAInfos, joint intern nach
  Datenverfuegbarkeits-Regel, liefert Domain-Objekte (`FaListEntry`,
  `FaInfoEntry`)
- **`IFaListeStandortResolver`** — waehlt je Standort (AKE / IDEAL) die
  richtige Datenquelle. Fuer AKE bleibt die bestehende Kette
  (`vw_AKE_Kommissionierung_WAListe`), fuer IDEAL die neuen Views.
- **`CachedFaListeRepository`** — 5 min `IMemoryCache`, analog zum bestehenden
  BOM-Cache
- **Feature-Services:** eigene Services fuer die drei Anwendungsfaelle
  (`VormontageService`, `BeschichtungsauftragService`, `KommissionierListenService`),
  die auf `IFaListeRepository` aufsetzen und die spezifischen Filter/Aggregationen
  anwenden.

**Feature-Toggles** (AppSettings, analog zu `TeileverfolgungAktiv`,
`LeitstandAktiv` etc.):
- `IdealVormontageAktiv` (Default `false`)
- `IdealBeschichtungAktiv` (Default `false`)
- `IdealKommissionierlistenAktiv` (Default `false`)

Diese Features sind IDEAL-spezifisch; ein Standort-Flag oder Set von
Feature-Toggles pro Standort ist sauberer als eine harte Standort-Weiche.

---

## Zugehoerige Dateien im SAGE-Server

Die View-Definitionen leben nicht im WMS-Repo, sondern direkt in der
Sage-Datenbank. Aktueller Stand als DDL:

- `SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql` (TODO: ins Repo aufnehmen)
- `SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAInfos.sql` (TODO: ins Repo aufnehmen)

**Empfehlung:** Beide Views in `SQL/sage-views/` unter Versionskontrolle
nehmen (nicht als Migration — die sind nicht Teil der WMS-DB, aber die
DDL sollte trotzdem im Repo liegen, damit Anpassungen dokumentiert sind).
