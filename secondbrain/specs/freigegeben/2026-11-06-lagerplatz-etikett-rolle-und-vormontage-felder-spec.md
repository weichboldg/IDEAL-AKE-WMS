---
type: spec
title: Lagerplatz-Etikett 93 × 55 mm (Rollendrucker) und neue Felder in den Vormontage-Listen
slug: 2026-11-06-lagerplatz-etikett-rolle-und-vormontage-felder-spec
status: Freigegeben
created: 2026-11-06
updated: 2026-11-06
source_backlog: "[[2026-11-06-lagerplatz-etikett-rolle-und-vormontage-felder]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
affected_code:
  - "IdealAkeWms/Views/StorageLocations/PrintLabels.cshtml (Format-Weiche: @page :123-126, .label :61-73, Seitenumbruch :144-145; Rollenformat 93x55, ein Etikett je Seite)"
  - "IdealAkeWms/Models/AppSettingKeys.cs (neuer Key Rollenformat, Default aus, kein Seed)"
  - "IdealAkeWms/Models/Standort/StandortSettingsCatalog.cs (neues Bool-Feld + ggf. Gruppe :28-35, :102-105)"
  - "IdealAkeWms/Views/Settings/Index.cshtml (:38 Key in Gruppe Lager, damit er nicht unter Sonstige landet)"
  - "IdealAkeWms/Views/Help/Index.cshtml (:1805 ein Satz zum Rollenformat)"
  - "IdealAkeWms/Services/VormontageService.cs (ColumnMap :79-90, AggregatColumnMap :97-103, BuildEinzelteileAsync :153-181, AggregateSummiert :217-275: Kopf-Lookup, Positions-Vorfilter vor der Aggregation, Anzeige-Listen)"
  - "IdealAkeWms/Models/ViewModels/FaHierarchyVormontageAggregatViewModel.cs (:16-32 neue Anzeige-Eigenschaften)"
  - "IdealAkeWms/Views/FaHierarchyVormontage/Index.cshtml (colCount :6, th :75-82, Zellen :132-139, column-config :154-163)"
  - "IdealAkeWms/Views/FaHierarchyVormontage/Summiert.cshtml (colCount :7, Untertitel :68, th :80-82, Zellen :97-99, column-config :113-117)"
  - "IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (FaHierarchyVormontageEinzeln :429-444, FaHierarchyVormontageSummiert :452-462)"
  - "IdealAkeWms/Services/ColumnFilterSyntax.cs (ColumnFilterTypes.Lists :54-55, fe-termin als Datum)"
  - "IdealAkeWms.Tests/Services/VormontageServiceTests.cs (neue Faelle)"
  - "IdealAkeWms.Tests/Views/KommissionierSpaltenKonsistenzTests.cs (:40-46, :76-81) bzw. neuer Drift-Waechter A/B/C ohne Druck fuer die zwei Vormontage-Views"
  - "docs/TESTSZENARIEN.md (neues Kapitel), IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs, Views/Help/Changelog.cshtml"
open_questions:
  - "1 Abteilung: ist damit der Arbeitsbereich je Position gemeint (Antwort 2026-10-06 der aufgenommenen Spec) oder zusaetzlich die FAInfos-Montage-Abteilung je HauptFA? Empfehlung: Abteilung = Arbeitsbereich, keine zusaetzliche Montage-Abt.-Spalte"
  - "2 Ein-/Ausblenden: Spaltenwahl je Benutzer (Zahnrad) oder Standort-Schalter je Feld? Empfehlung: Zahnrad je Benutzer, neue Spalten standardmaessig sichtbar"
  - "3 Summiert-Sicht: Anzeige als Werteliste + Filter vor dem Zusammenzaehlen (Schluessel bleibt Matchcode), Schluessel erweitern oder nur einzelne Felder? Empfehlung: Anzeige + Vorfilter"
  - "4 Druckprozessor: Browser-Druckansicht oder serverseitiger Druck (PrintService)? Empfehlung: Browser-Druckansicht"
  - "5 Umfang alle Vormontagelisten: nur Einzeln + Summiert + CSV-Export (automatisch) oder auch Druckansicht/PDF/Isolierfraesen-Export? Empfehlung: Einzeln + Summiert + CSV"
  - "6 Format-Einstellung: Bool-Schalter (Default aus = A4) oder Auswahlliste? Empfehlung: Bool in neuer Gruppe Druck"
  - "7 Rollendrucker und Etikett: Druckermodell/Aufloesung, Ausrichtung 93 breit x 55 hoch, Rahmen ja/nein, Standort fuer den Test? Empfehlung: quer, ohne Rahmen, Test am echten Drucker"
  - "8 Kombigeraet (mehrere Kopfzeilen je HauptFA): Kunde/FE-Termin als Liste aller Werte, FE-Termin-Filter vergleicht den fruehesten? Empfehlung: ja"
  - "9 Beschriftung und Reihenfolge der neuen Spalten (Bemerkung PN, Fertigungsinfo PN, FE-Termin, Kunde, HauptFA) - Empfehlung siehe Rueckfrage"
  - "10 P5-Ergebnis (Vorbehalt aus der aufgenommenen Spec) vor Freigabe ausfuehren und eintragen"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-06"
---

> [!info] Ersetzt eine Entwurfs-Spec (Entscheidung Gerald 2026-11-06)
> Diese Spec nimmt [[2026-10-06-vormontage-abteilung-spalte-filter-spec]] vollständig auf. Die alte Spec wird nicht mehr
> eigenständig umgesetzt. Zuordnung im Abschnitt [[#Aufgenommene Spec]].

> [!note] Umsetzungsort
> Ein Dev-Lauf, beide Teile, im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` auf
> `feature/2026-08-07-ideal-teile-1-5` (Stand v1.71.1, `IdealAkeWms/AppVersion.cs:5`). Alle `Datei:Zeile`-Angaben
> unten sind gegen diesen Stand gemessen. Empfohlene Reihenfolge im Lauf: erst Teil 1 committen, dann Teil 2.

> [!important] Freigabe-Nachtrag 2026-11-06 (geht dem Rumpf vor)
> Antworten 1–10 im Block „Freigabe-Antworten“. Kurz: „Abteilung“ = **Arbeitsbereich je Position** (a), keine Spalte
> „Montage-Abt.“; **keine weiteren Felder** als die angeforderten. Spaltenwahl je Benutzer, neue Spalten sichtbar; Summiert A
> ohne Kappung; Browser-Druckansicht; Umfang Einzeln + Summiert + CSV; Format als Bool in Gruppe „Druck“; Kombigerät als
> Liste, Datumsfilter auf frühesten FE-Termin; Labels/Reihenfolge wie Rückfrage 9.
> Rollenetikett: **fortlaufend, ein Etikett je Seite**, Annahme quer 93 × 55 mm ohne Rahmen; Druckermodell/dpi unbekannt,
> Prüfung am echten Drucker bei der Abnahme; Treiber-Hinweis (Papier 93 × 55, Ränder keine) in Hinweistext und Hilfe.
> **P5 entfällt (Gerald 2026-11-06)** – der Vorbehalt unten ist damit aufgehoben: Die Spalte zeigt das vorhandene Feld
> `Arbeitsbereich` unter dem Label „Arbeitsbereich“; dass es der Sage-Spalte „Abteilung“ entspricht, bleibt **ungeprüft**.
> Ergebnisse IDEAL-Test-DB (Gerald 2026-11-06): **P7** 72 Positionen mit VMBedarf, `BemerkungPN` 6 gefüllt (max. 3 Zeichen),
> `FertigungsInfoPN` 0 gefüllt → keine Kappung/`defaultHidden` nötig; **P9** längster Barcode 16 Zeichen (`Magazin-W1;0;0;0`).

> [!warning] Vorbehalt P5 (übernommen aus der aufgenommenen Spec) — **aufgehoben durch Freigabe-Nachtrag 2026-11-06**
> Die Spalte „Arbeitsbereich“ beruht auf Antwort 1 vom 2026-10-06 (Variante A, Knotenfeld `FaHierarchyNode.Arbeitsbereich`),
> **vorbehaltlich P5**. P5 ist noch nicht gelaufen. **Weicht P5 von den Screenshot-Werten ab** (A-BODEN ≠ `H1-03`,
> FR-4301-…-1530 ≠ `S-01`, Mittelholmblende ≠ `H2-02` oder Korpus-Gepunktet nicht leer), geht die Spec **zurück an
> Schranke 1**: Dann ist die Feldwahl für „Abteilung“/„Arbeitsbereich“ neu zu entscheiden. Teil 1 hängt nicht an P5.

## Ziel / Nutzen (das Warum)

**Teil 1.** Lagerplatz-Etiketten kommen heute nur auf A4: drei Etiketten zu 85 mm je Seite
(`Views/StorageLocations/PrintLabels.cshtml:61-73, 123-126, 144-145`). Für einen Rollendrucker mit Etiketten
93 × 55 mm braucht es ein zweites Format mit genau einem Etikett je Druckseite. Welches Format gilt, soll der
Standort einstellen. Ohne Umstellen ändert sich nichts.

**Teil 2.** Die Vormontage plant und arbeitet mit Angaben, die in den Vormontage-Listen fehlen. Sie stehen zwar im
WMS-Cache, werden dort aber nicht gezeigt: `BemerkungPN`, `FertigungsInfoPN` und `Arbeitsbereich` je Position,
`Kunde` und `FE_Termin` je HauptFA. Gerald will sie in **allen** Vormontage-Listen gleich, und jede Spalte soll
ein- und ausblendbar sein. Die aufgenommene Spec bringt die Spalte „Arbeitsbereich“ samt Filter mit.

**Standort.** Teil 1 gilt an beiden Standorten. Der Druck ist standortneutral, nur das Format wird je Standort
eingestellt. Teil 2 wirkt nur an **IDEAL**: `FaHierarchyNodes`/`FaHierarchyOrderInfos` füllt allein der IDEAL-Sync
(`IDEALAKEWMSService/Services/FaHierarchySyncService.cs:138-147`), und die Vormontage-Listen hängen am Toggle
`FaHierarchyVormontageAktiv` (Default aus, `Controllers/FaHierarchyVormontageController.cs:29-30`,
`Program.cs:448`). Darum steht im Frontmatter `standort: beide`.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Teil 1: Rollenformat 93 × 55 mm als zweite Darstellung **derselben** View `PrintLabels.cshtml`. Die View dient
  beiden Aufrufern: Sammeldruck `StorageLocationsController.PrintLabels` (`:157-171`, Knopf „Alle Etiketten drucken“
  `Views/StorageLocations/Index.cshtml:17`) und Einzeldruck `PrintLabel(id)` (`:173-186`, rendert
  `View("PrintLabels", …)` `:185`, Knopf je Zeile `Index.cshtml:127`). Weitere Aufrufer gibt es nicht. Eine Suche
  nach `PrintLabel` in `*.cs`, `*.cshtml` und `*.js` des Worktrees findet nur diese vier Stellen. Dazu kommt der
  Standort-Schalter in `StandortSettingsCatalog` und je ein Satz in Hilfe und Changelog.
- Teil 2, Sicht 1 „Einzelne Teile“ (`/FaHierarchyVormontage/Index`) und Sicht 2 „Summiert“
  (`/FaHierarchyVormontage/Summiert`): neue Spalten nach [[#Fachliche Anforderungen]], jede mit Server-Spaltenfilter
  und Spaltenwahl. Registriert werden sie an allen drei Stellen, dazu kommt ein Drift-Wächter.
- Der CSV-Export beider Sichten liefert die neuen Spalten ohne eigenen Code mit (siehe [[#Teil 2 — Vormontage-Felder]]).
- Freigabe-Nachtrag 2026-10-09 der aufgenommenen Spec: Der Untertitel der Summiert-Sicht spricht künftig vom
  „VM-Bedarf“ statt vom „Arbeitsbereich“.
- Automatisierte Tests, Testszenarien-Kapitel, Versions-Bump, Anwender- und Brain-Changelog.

**Out-of-Scope**
- Serverseitiger Druck (`Services/PrintService.cs`), siehe Rückfrage 4.
- Andere Etiketten- oder Druckformate (Lagerbestellung, Stückliste, Kommissionierliste).
- Eine Vormontage-Druckansicht oder ein PDF. Es gibt keine (`Views/FaHierarchyVormontage/` enthält nur `Index.cshtml`
  und `Summiert.cshtml`, der Controller hat nur `Index` und `Summiert`, `FaHierarchyVormontageController.cs:63, 102`).
  Das bleibt Backlog [[2026-09-14-vormontage-druckansicht]].
- Der Isolierfräsen-Export bleibt Backlog [[2026-08-06-vormontage-isolierfraesen-export]].
- Die Trennung von Kombinationsgeräten bleibt [[2026-08-06-kombinationsgeraete-montageabteilung]]. Die alte
  Antwort 3 gilt: eigener Schritt, dort nur Querverweis.
- Der Gruppenkopf von Sicht 1 bleibt, wie er ist (`Index.cshtml:99-128`): FAInfos-Abteilung, Kunde, AB, PT.
- Die Zielort-Einordnung von `Arbeitsbereich` (ADR 0014, [[fallstricke]] §15) bleibt unberührt. Es wird keine
  Werkbank abgeleitet.
- Nebenbefunde, gemeldet und nicht mitgebaut: siehe [[#Nebenbefunde (nicht Teil des Auftrags)]].

## Fachliche Anforderungen

### Teil 1 — Etikettenformat

1. Neue Standort-Einstellung **„Lagerplatz-Etiketten im Rollenformat 93 × 55 mm“** (Bool, Default aus; Rückfrage 6).
   Ist sie aus oder fehlt ihre Zeile, rendert `PrintLabels.cshtml` **dieselbe Darstellung wie heute**: A4 hoch, drei Etiketten
   je Seite, 85 mm hoch, Rahmen 2 px.
2. Ist sie an, gilt im Sammel- **und** im Einzeldruck: Druckseite 93 mm × 55 mm, **ein Etikett je Seite**, keine leere
   Seite am Ende. n Lagerplätze ergeben n Seiten.
3. Der Inhalt bleibt wie heute, nur kleiner gesetzt: Code (`.label-code`), Beschreibung falls vorhanden, Barcode
   Code128 aus `BarcodeValue ?? Code` (`StorageLocationsController.cs:165, 179`), Zone falls vorhanden. Die
   Reihenfolge bleibt. Der Sammeldruck zeigt weiter **alle** Lagerplätze inklusive inaktiver, wie heute
   (`GetAllOrderedAsync`, `:159`).
4. Bildschirmleiste „Drucken/Zurück“ und Titel bleiben (`PrintLabels.cshtml:131-135`). Sie werden nicht gedruckt
   (`:107-109`).

**Risiko Barcode-Lesbarkeit (benannt, nicht wegdiskutiert).** Das Barcode-Bild ist ein PNG mit
`max(800, Länge × 45)` × 200 px (`StorageLocationsController.cs:192-193`). Die View zeigt es mit fester Höhe 25 mm
und `max-width: 100 %` (`PrintLabels.cshtml:95-98`). Auf A4 stehen rund 190 mm Breite zur Verfügung, auf dem
Rollenetikett nach Rand nur etwa 85 mm. Damit wird das Bild schmaler, und weil die Höhe fest ist, kann es sich
verzerren. Die Modulbreite eines Code128-Strichs sinkt mit der Codelänge. Bei manuellen Codes mit höchstens 12
Zeichen ([[fallstricke]] „`StorageLocation.Code`: DB 50 Zeichen, manuell 12“) bleibt grob 0,5 mm je Modul, was
unkritisch ist. Sage-Codes dürfen bis 50 Zeichen lang sein (`Models/StorageLocation.cs:13`). Dann fällt das Modul
unter die üblichen Mindestwerte von 0,25 bis 0,3 mm bei 203 dpi, und die Ruhezone links und rechts kann fehlen.
**Belegpflicht:** P9 (längster Code) und TS-Schritt „Scan am echten Etikett“. Ist der längste Code nicht lesbar,
wird das **gemeldet und nicht still gelöst**. Mögliche Folgearbeit wäre ein eigenes, schmaleres Barcode-Bild für das
Rollenformat; das ist nicht Teil dieses Laufs.

### Teil 2 — Vormontage-Felder

Belegte Ausgangslage (Schreib- und Lesewege, nicht aus den Namen gedeutet):

| Feld | Granularität | Einziger Schreibweg | Sage-Quelle laut Repo-Doku | Heutige Leser im Web |
|---|---|---|---|---|
| `FaHierarchyNode.BemerkungPN` (`Models/FaHierarchyNode.cs:37`, `nvarchar(max)`, `Migrations/20260807105825_AddFaHierarchy.cs:32`) | je Strukturzeile | `FaHierarchySyncService.cs:220, 246` aus `f.[BemerkungPN]` (`FaHierarchySql.cs:145`) | „Position bevorzugt, Beleg als Fallback“ (`SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql:37`) | **keiner**, nur Mapping `ApplicationDbContext.cs:1031` |
| `FaHierarchyNode.FertigungsInfoPN` (`:38`, `nvarchar(max)`, Migration `:33`) | je Strukturzeile | `FaHierarchySyncService.cs:220, 247` aus `f.[FertigungsInfoPN]` (`FaHierarchySql.cs:145`) | wie oben (`…FAListe.sql:38`) | **keiner**, nur Mapping `ApplicationDbContext.cs:1032` |
| `FaHierarchyNode.Arbeitsbereich` (`:40`, `nvarchar(200)`, Migration `:35`) | je Strukturzeile | `FaHierarchySyncService.cs:221, 249` aus `f.[Arbeitsbereich]` (`FaHierarchySql.cs:146`) | „Position bevorzugt, Beleg als Fallback (positionsbezogen!)“ (`…FAListe.sql:40`); Sage `USER_OSAbteilung` steht **nur im Brain** ([[fallstricke]] §15) | Kommissionierlisten (`KommissionierListenService.cs:64`, `FaHierarchyKommissionierListen/Index.cshtml:82, 151`, `Print.cshtml:114, 130`), FA-Struktur (`FaHierarchy/_FaHierarchyNode.cshtml:14, 25, 95`) |
| `FaHierarchyOrderInfo.Kunde` (`Models/FaHierarchyOrderInfo.cs:25`) | je Kopfzeile (ABNr + Pos), mehrere je HauptFA möglich | `FaHierarchySyncService.cs:273, 289` aus `i.[Kunde]` (`FaHierarchySql.cs:158`) | „Endkunde“, Quelle `[PPSImport].[dbo].[vw_PPSData]` (`…FAInfos.sql:9, 25`) | Gruppenkopf Sicht 1 (`FaHierarchyVormontage/Index.cshtml:115`) u. a. |
| `FaHierarchyOrderInfo.FE_Termin` (`:32`, `datetime2`, Migration `:72`) | je Kopfzeile | `FaHierarchySyncService.cs:276, 295` aus `i.[FE_Termin]` (`FaHierarchySql.cs:160`) | „Wann muss die Blechfertigung fertig sein“ (`…FAInfos.sql:27`) | KW-Filter beider Sichten (`VormontageService.cs:169, 237-240`), `ArbeitsgangStatusRegel.cs:47` |
| `FaHierarchyOrderInfo.MontageAbteilung` (`:21`) | je Kopfzeile, zweiter Schlüssel bei Kombigeräten | `FaHierarchySyncService.cs:272, 286` aus `i.[Abteilung] AS [MontageAbteilung]` (`FaHierarchySql.cs:158`) | „Montage-Abteilung“ (`…FAInfos.sql:28`) | Gruppenkopf Sicht 1 (`Index.cshtml:114`), FA-Liste (`ProductionOrdersController.cs:303, 311, 321`), `FaHierarchyTreeBuilder.cs:70-73` |

Es gibt keinen weiteren Schreibweg. Die Suche nach Zuweisungen `BemerkungPN|FertigungsInfoPN|Arbeitsbereich|MontageAbteilung|FE_Termin = `
im ganzen Worktree findet als Entity-Schreiber nur `FaHierarchySyncService.cs:246-295`. Die Treffer in
`ProductionOrdersController.cs:235, 303, 321` schreiben ViewModels, keine Entity. Der Rest sind Tests. Beide Tabellen
ersetzt der Sync per Full-Refresh in einer Transaktion (`FaHierarchySyncService.cs:138-147`). Leere Sage-Werte
werden zu `null` (`Str`, `:308`). Die echte Sage-Quelle wählt die Einstellung `Sync:FaHierarchyListeViewName` bzw.
`Sync:FaHierarchyInfosViewName` (Default `[vw_IDEAL_AKE_WMS_FAListe]`/`[vw_IDEAL_AKE_WMS_FAInfos]`,
`Models/ServiceSettingDefinitions.cs:40-41`). Den CREATE-VIEW-Text gibt es im Repo nicht (`…FAListe.sql:57` TODO).
Wie gut `BemerkungPN`/`FertigungsInfoPN` an IDEAL befüllt sind, ist deshalb **unverifiziert** (P7, P10).

Heutiger Spaltenstand:
- Sicht 1: `hauptfa`, `hauptartnr`, `artnr`, `matchcode`, `sollmenge`, `fertigungmenge`, `vmbedarf`, `material`
  (`Index.cshtml:75-82`, `ColumnDefinitions.cs:435-442`). **`hauptfa` ist schon da** (gesperrt, `:435`).
- Sicht 2: `matchcode`, `sollmenge`, `fertigungmenge` (`Summiert.cshtml:80-82`, `ColumnDefinitions.cs:458-460`).
  Summiert wird nach `Matchcode` je `VMBedarf`-Reiter (`VormontageService.cs:246-257`).

Anforderungen:

5. **Neue Spalten in beiden Sichten.** Unterstellt sind die Empfehlungen zu den Rückfragen 1, 3 und 9. Die
   Spaltenliste ist in beiden Sichten dieselbe, nur `hauptfa` ist in Sicht 1 schon vorhanden:

   | Col-Key | Label (Rückfrage 9) | Wert je Position | Sicht 1 | Sicht 2 |
   |---|---|---|---|---|
   | `arbeitsbereich` | Arbeitsbereich | `n.Arbeitsbereich` | neu, direkt nach `hauptfa` | neu, direkt nach `matchcode` |
   | `hauptfa` | HauptFA | `n.HauptFA` | vorhanden | neu |
   | `kunde` | Kunde | Kopfwert(e) des HauptFA | neu | neu |
   | `fe-termin` | FE-Termin | Kopfwert(e) des HauptFA, `dd.MM.yyyy` | neu, Datumsfilter | neu, Datumsfilter |
   | `bemerkungpn` | Bemerkung PN | `n.BemerkungPN` | neu | neu |
   | `fertigungsinfopn` | Fertigungsinfo PN | `n.FertigungsInfoPN` | neu | neu |
   | ~~`montage-abteilung`~~ | ~~Montage-Abt.~~ | nur falls Rückfrage 1 = „zusätzlich“ | – | – |

6. **Kopfwerte je HauptFA** (Kunde, FE-Termin): Sie stammen aus den `FaHierarchyOrderInfo`-Zeilen desselben HauptFA,
   zugeordnet ohne Fan-out, also nie per Join gegen die Positionen (Muster `FaHierarchyListBuilder.cs:152-163`).
   Gibt es keine Kopfzeile, bleibt die Zelle leer. Bei mehreren Kopfzeilen (Kombigerät, Rückfrage 8) werden alle
   verschiedenen, nicht leeren Werte gezeigt: Text sortiert, Datum aufsteigend, getrennt durch `", "`. Die Anzeige
   wählt nie still eine Variante aus ([[glossar]] „Kombinationsgeraet“). Die Positionszahl bleibt 1:1.
7. **Sicht 1, Filter.** Für jede neue Spalte gibt es einen Server-Spaltenfilter (`colf_<key>`) mit der geltenden
   Suchsyntax ([[0017-ein-parser-fuer-die-spaltenfilter-syntax]]). Er wirkt **je Position vor der Gruppierung**
   (`FaHierarchyListBuilder.cs:136-138`): Eine Gruppe bleibt, wenn mindestens eine Position passt, und zeigt dann
   nur die passenden Positionen. `TotalCount` zählt Gruppen mit Treffer. `fe-termin` ist eine Datumsspalte.
8. **Sicht 2, Anzeige und Filter** (Rückfrage 3, Fortschreibung der Keyuser-Antwort 2026-10-07 zu `arbeitsbereich`):
   Der Gruppierschlüssel bleibt `Matchcode`. Jede neue Spalte zeigt je Aggregatzeile die verschiedenen Werte der
   beitragenden Positionen (Text sortiert, HauptFA numerisch, Datum aufsteigend, `", "`). Filter auf neuen Spalten
   wirken **je Position vor dem Zusammenzählen**. Die Summen enthalten dann nur Mengen der passenden Positionen.
   Filter auf `matchcode`/`sollmenge`/`fertigungmenge` wirken wie heute auf der Aggregat-Projektion
   (`VormontageService.cs:260`).
9. **Leere Werte** (null oder leer): Die Zelle bleibt leer und zeigt keinen Platzhalter. Der Gruppenkopf bleibt
   unverändert (`Index.cshtml:114`, „—“).
10. **Spaltenwahl** (Rückfrage 2): Jede neue Spalte lässt sich über das Zahnrad je Benutzer ein- und ausblenden und
    verschieben. Die Wahl überlebt einen Reload. Default ist sichtbar, keine Spalte ist `locked`. Wer schon
    gespeicherte Einstellungen hat, sieht die neuen Spalten an der Nachbarposition (`column-preferences.js`
    `mergeWithDefaults` `:76`, Rückfall `visible: !c.defaultHidden` `:117`).
11. **CSV-Export** beider Sichten: Sichtbare neue Spalten erscheinen mit dem angezeigten Text. Das liefert
    `csv-export.js` schon heute ohne Zusatzcode: Es exportiert sichtbare `<th>` (`:56, 128`) im Server-Mode mit
    `pageSize=0` (`:199`).
12. **Untertitel Summiert** (Freigabe-Nachtrag 2026-10-09): `Summiert.cshtml:68` lautet künftig „… summierte
    Mengensicht des gewählten **VM-Bedarfs**“. Reiter-Beschriftungen mit dem Wort „Arbeitsbereich“ gibt es nicht. Die
    Reiter zeigen den rohen `VMBedarf`-Wert (`Index.cshtml:31`, `Summiert.cshtml:33`), dort ist also nichts
    umzustellen. Code-Kommentare, die die Reiter „Arbeitsbereich“ nennen (`Index.cshtml:23`, `Summiert.cshtml:24`,
    `VormontageService.cs:107`, `FaHierarchyVormontageController.cs:16`, `FaHierarchyVormontageAggregatViewModel.cs:21`),
    nennt der Dev-Lauf in „VM-Bedarf“ um. Das ist Aufräumen ohne UI-Wirkung.
13. Zugriff, Toggle, Reiter (`VMBedarf`), KW-Filter und Paging-Einheiten ändern sich nicht.

## Technischer Loesungsentwurf

Muster: Listen-View-Pattern ([[0005-listen-view-pattern-mit-server-side-spaltenfilter]]) mit allen vier
Pflichtbestandteilen. Getter liefern den gerenderten Text ([[fallstricke]] §3 „Universal-Filter-Pattern“, §26
„Getter rendern wie die Zelle“). Feature-Toggle bzw. Standort-Wert über AppSettings
([[0011-feature-toggles-ueber-appsettings]]). Es kommt keine neue Abhängigkeit, kein neuer Repository-Aufruf und
keine Migration dazu.

### Teil 1 — Rollenformat in `PrintLabels.cshtml`

**T1.1 Einstellung.** Neuer Key in `AppSettingKeys.cs`, zum Beispiel `LagerplatzEtikettRolle93x55`. Der Name ist
Sache des Dev-Laufs, Englisch-/Deutsch-Mischung wie die Nachbarn. Kommentar: Default aus, nur exakt `"true"`
aktiviert, kein Seed, keine Migration. Ein Eintrag in `StandortSettingsCatalog.Fields`:
`StandortFieldType.Bool`, `StandortBackend.App`, Hinweistext
„Aus = A4 mit drei Etiketten je Seite (bisher). An = ein Etikett 93 × 55 mm je Seite; im Druckdialog den
Rollendrucker und das Papierformat 93 × 55 mm wählen, Ränder ‚Keine‘.“ Die Gruppe ist neu, etwa „Druck“, oder
eine bestehende (Rückfrage 6). Eine neue Gruppe gehört auch in `GroupOrder` (`StandortSettingsCatalog.cs:102-105`).
**Warum kein Seed:** Es ist ein Default-aus-Schalter. Fehlt die Zeile oder steht `"false"` darin, gilt A4
([[fallstricke]] „Default-an-Schalter … brauchen eine geseedete Zeile“: „Default-aus-Schalter … brauchen keinen
Seed“). Die Maske speichert Bool als `"true"`/`"false"` (`StandortEinstellungenController.cs:64-66`).
In `/Settings` den Key in die Gruppe „Lager“ aufnehmen (`Views/Settings/Index.cshtml:38`), damit er nach dem
ersten Speichern nicht unter den ungruppierten Keys steht (`:48-49`).

**T1.2 Lesen an einer Stelle.** Beide Aufrufer rendern dieselbe View (`StorageLocationsController.cs:170, 185`).
Am kleinsten ist es, den Wert **in der View** zu lesen:
`@inject IdealAkeWms.Data.Repositories.IAppSettingRepository AppSettings` (Muster `Views/Shared/_VaultLink.cshtml:2`,
`_Layout.cshtml:2`) und `var rolle = string.Equals(await AppSettings.GetValueAsync(Key), "true", StringComparison.OrdinalIgnoreCase);`.
So bleibt der Controller unverändert, und Einzel- wie Sammeldruck sind gleichzeitig abgedeckt.

**T1.3 Darstellung.** Bei `rolle == false` bleibt der heutige Block unverändert, damit Anforderung 1 erfüllt ist.
Bei `rolle == true` gelten zusätzliche Regeln, etwa über eine Klasse `labels-rolle` am `<body>` oder einen zweiten
`<style>`-Block. Richtwerte, die der Dev-Lauf am Drucker nachmisst:
- `@page { size: 93mm 55mm; margin: 0; }`, Container ohne Innenabstand.
- `.label { width: 93mm; height: 55mm; padding: 3mm; margin: 0; border: none; }` (Rahmen laut Rückfrage 7).
  Seitenumbruch mit `break-before: page` ab dem zweiten Etikett (`.label + .label`) statt des Modulo-3-Umbruchs
  (`:144`). So entsteht keine leere Seite am Ende. **Kein** `:last-child`-Trick ([[fallstricke]] §27).
- Schrift etwa: Code 18 pt, Beschreibung 10 pt (max. 2 Zeilen, `max-height` wie heute mit `overflow: hidden`),
  Zone 9 pt. Barcode etwa 18 bis 20 mm hoch, Breite `max-width: 100 %`. Die feste Höhe nicht mit `max-width`
  kombinieren, sondern `height: auto` mit `max-height` setzen, damit das Seitenverhältnis bleibt. Siehe Risiko oben.
- Kontrast bleibt Schwarz auf Weiß. Die Zone in `#666` ist auf Thermodruck schwach. Im Rollenformat `#000` nehmen
  (CLAUDE.md, Kontrast als Funktion).
- Der Skill `frontend-design` ist für die View Pflicht (CLAUDE.md).

**T1.4 Was nicht geändert wird.** `GenerateBarcodeBase64` (`:188-197`) und `IBarcodeService`
(`Services/BarcodeService.cs`, 600 × 160 px) bleiben, wie sie sind. Ein Tausch ändert das A4-Bild und verletzt
Anforderung 1.

### Teil 2 — Vormontage-Felder

**T2.1 Ein Formatierer für Kopfwerte.** Eine statische Hilfsmethode in `VormontageService`, die aus den
`FaHierarchyOrderInfo`-Zeilen eines HauptFA die Texte für `kunde` und `fe-termin` bildet. Sie verwirft
null/leer/Leerzeichen, nimmt verschiedene Werte, sortiert (Datum aufsteigend als `dd.MM.yyyy`, Text ordinal-ignore-case)
und verbindet mit `", "`. **Dieselbe** Methode benutzen der Getter und die Zelle, so bleibt „Getter rendern wie die
Zelle“ erfüllt. Die Zelle in Sicht 1 ruft sie mit `group.OrderInfos` auf, den der Baustein schon liefert
(`FaHierarchyListBuilder.cs:162-164`). Vorbild ist der verworfene Formatierer B1 der aufgenommenen Spec, dort für
`MontageAbteilung`. Er wird hier für Kunde/FE-Termin wiederbelebt, nicht für den Arbeitsbereich.

**T2.2 Sicht 1, Getter-Map je Request.** Der Baustein filtert Positionen **vor** der Kopf-Zuordnung (`:136-138`
vor `:152-163`). Ein Getter für Kopfwerte braucht deshalb einen Lookup `HauptFA → Kopftext`, den
`BuildEinzelteileAsync` (`VormontageService.cs:153-181`) einmal aus `allInfos` baut (`:162`). Die Map entsteht je
Request: die statischen Positions-Getter aus `ColumnMap` (`:79-90`) ergänzt um `arbeitsbereich`, `bemerkungpn` und
`fertigungsinfopn`, dazu `kunde` und `fe-termin` über den Lookup. Diese Map geht an `_builder.Build` (`:173-177`).
Der Dev-Lauf wählt einen Weg, auf dem der Drift-Wächter die Schlüsselmenge ohne DB erreicht, etwa
`BuildColumnMap(lookup)` statisch mit leerem Lookup aufrufbar.

**T2.3 Sicht 2, Vorfilter und Anzeige.** In `AggregateSummiert` (`:217-275`) passiert nach dem KW-Vorfilter
(`:234-242`) und vor dem Gruppieren (`:246`) Folgendes: Die Positionen werden mit
`ColumnFilterHelper.Apply(leaves, columnFilters, positionsMap, types)` eingeschränkt. `positionsMap` enthält genau
die neuen Keys mit denselben Gettern wie Sicht 1, ein gemeinsamer Ort. Diese Keys kommen **nicht** in
`AggregatColumnMap` (`:97-103`). `Apply` ignoriert unbekannte Keys (`ColumnFilterHelper.cs:57`), also wertet der
Nachfilter `:260` sie nicht ein zweites Mal aus. Die Anzeige bekommt neue String-Eigenschaften an
`FaHierarchyVormontageAggregatViewModel` (`:16-32`), etwa `Arbeitsbereiche`, `HauptFas`, `Kunden`, `FeTermine`,
`BemerkungenPN`, `FertigungsInfosPN`. Gefüllt werden sie im `Select` (`:248-255`) aus den Positionen der Gruppe;
Kopfwerte laufen über den Lookup aus `allInfos`, der Parameter ist schon da (`:219`). Bei `FE-Termin` gilt: alle
Kopfzeilen aller HauptFA der Gruppe, verschieden, aufsteigend.

**T2.4 Typ-Register.** `ColumnFilterTypes.Lists` bekommt `("fe-termin", D)` für `FaHierarchyVormontage/Index` und
`FaHierarchyVormontage/Summiert` (`ColumnFilterSyntax.cs:54-55`). Das passende `<th data-date-filter>` kommt dazu
([[fallstricke]] §26 „Typ an `<th>` UND im Register“). Der Vorfilter in Sicht 2 erhält dasselbe Register wie der
Nachfilter.
**Bekannte Grenze:** Die Datumsauswertung liest nur das **erste** Datum einer Zelle (`CellDate` ist an `^`
verankert, `ColumnFilterSyntax.cs:239`). Weil die Liste aufsteigend sortiert ist, vergleicht ein Datumsfilter bei
Kombigeräten mit verschiedenen FE-Terminen den **frühesten**. Gleiche Regel wie
`ArbeitsgangStatusRegel.cs:47` (früheste FE). In Hilfe und Changelog wird das genannt (Rückfrage 8).

**T2.5 Views und `ColumnDefinitions`.** In beiden Views werden an derselben Position ergänzt:
`<th data-filterable data-col-key="…">` (bei `fe-termin` zusätzlich `data-date-filter`), die Zelle und der
`#column-config`-Eintrag (`"locked": false`, kein `defaultHidden`). Neu gesetzt werden `colCount` (`Index.cshtml:6`
8 → 13, `Summiert.cshtml:7` 3 → 9; mit Montage-Abt. jeweils +1) und `ColumnDefinitions`
(`FaHierarchyVormontageEinzeln` `:429-444`, `FaHierarchyVormontageSummiert` `:452-462`) in **derselben Reihenfolge**
wie das `#column-config`. Die Skript-Reihenfolge stimmt schon (`Index.cshtml:167-168`, `Summiert.cshtml:121-122`).
Die ViewKeys sind registriert (`ColumnDefinitions.cs:579-580`). Lange Texte (`BemerkungPN`/`FertigungsInfoPN` sind
`nvarchar(max)`) umbrechen normal. Die Tabellen-Scrollregel aus v1.64.1 gilt global. Eine eigene Kürzung baut dieser
Lauf nicht (YAGNI). Zeigt P7 sehr lange Texte, entscheidet Rückfrage 2 über `defaultHidden`.

**T2.6 Drift-Wächter.** `KommissionierSpaltenKonsistenzTests.ViewPairs` (`:40-46`) verlangt eine Print-Datei
(Quelle D). Die Vormontage hat keine. Deshalb kommt ein gleichartiger Test nur mit A/B/C für `Index.cshtml` und
`Summiert.cshtml`, der die Extraktions-Helfer wiederverwendet. Dazu ein `ServiceColumnMapPairs`-Fall (`:76-81`) für
die Sicht-1-Map gegen `FaHierarchyVormontageEinzeln`. **Kein** solcher Fall für `AggregatColumnMap`: Sie trägt die
Vorfilter-Keys bewusst nicht (T2.3). Der Test würde absichtlich scheitern.

**T2.7 Changelog und Hilfe.** Der Anwender-Changelog nennt die neuen Spalten, „Arbeitsbereich“ (nicht „Abteilung“),
dass in „Summiert“ der Filter vor dem Zusammenzählen wirkt, dass bei Kombigeräten der früheste FE-Termin für den
Datumsfilter zählt, und den neuen Untertitel. Einen Vormontage-Abschnitt in `Views/Help/Index.cshtml` gibt es nicht.
Dieser Lauf legt keinen an, nur den Satz zu Teil 1 bei `:1805`.

## Migrations-/SQL-Auswirkungen

Keine. Belegt:
- Teil 1: Der neue AppSettings-Key entsteht als Zeile beim ersten Speichern der Standorteinstellungen. Muster ohne
  Seed und ohne Migration wie `HardwareScannerAktiv`, `AppSettingKeys.cs:100-105`. Ohne Zeile gilt der Default
  A4. Es gibt kein neues Schema-Objekt, kein `SQL/XX_*.sql`, und `SQL/00_FreshInstall.sql` bleibt.
- Teil 2: Alle Felder bestehen schon in `FaHierarchyNodes`/`FaHierarchyOrderInfos`
  (`Migrations/20260807105825_AddFaHierarchy.cs:32-35, 63, 72`). Der Sync bleibt unverändert, `SQL/AgentJobs/` ist
  nicht betroffen (dort kein Treffer auf `FaHierarchyNodes`, nur in `SQL/89_*`, `SQL/94_*`, `SQL/00_FreshInstall.sql`
  und `SQL/Einmalig/*`).
- Es gibt keine daten-destruktive Änderung, ein DB-Backup ist nicht nötig.

## Audit-Feld-Auswirkungen

Keine neuen Pflichten.
- `AppSettings` ist bewusst keine `AuditableEntity` ([[fallstricke]] „`AppSettings` ist **kein** `AuditableEntity`“).
  Die Standort-Maske schreibt über den bestehenden `IStandortSettingsWriter`, der Lauf ändert ihn nicht.
- `FaHierarchyNode`/`FaHierarchyOrderInfo` sind Cache-Tabellen ohne Audit-Felder (`FaHierarchyNode.cs:8-10`,
  `FaHierarchyOrderInfo.cs:10`). Teil 2 liest nur.
- Teil 1 schreibt keine `StorageLocation`.

**Rollen und Zugriff** bleiben unverändert. Es gibt keine neue Rolle und keinen neuen Filter, `RoleOverview` bleibt
unberührt.
- Etikettendruck: Class-Level `[RequireMasterDataReadAccess]` (`StorageLocationsController.cs:11`). `PrintLabels`/`PrintLabel`
  haben kein Edit-Attribut, Lesezugriff reicht (admin, masterdata_read, masterdata; [[controller]]).
- Einstellung: `[RequireAdminAccess]` (`StandortEinstellungenController.cs:20`).
- Vormontage: `[RequireVorbauAccess]` + Gate `[RequireFaHierarchyVormontageAktiv]` (`FaHierarchyVormontageController.cs:29-30`).

**Listen-View-Pattern** (ADR 0005, Pflicht für die geänderten Tabellen): Pagination bleibt (Gruppen-Paging Sicht 1,
Aggregatzeilen Sicht 2), die Filterkarte bleibt, Server-Mode bleibt (`data-server-column-filter="true"`,
`Index.cshtml:72`, `Summiert.cshtml:77`). Jede neue `<th>` bekommt `data-col-key`, jede neue Spalte steht in
`ColumnDefinitions` und `#column-config`. Datumsspalten tragen den Typ an `<th>` und im Register. Es entsteht keine
neue Tabellen-View.

## Akzeptanzkriterien

Unterstellt sind die Empfehlungen zu den Rückfragen 1 bis 9. Abweichende Antworten zieht der Rework nach.

**Teil 1**
1. Fehlt die Einstellung oder steht sie auf `false`, sind Sammeldruck und Einzeldruck unverändert: A4 hoch, drei
   Etiketten je Seite, 85 mm, Rahmen. Prüfung: Druckvorschau vorher und nachher, Seitenzahl bei n Lagerplätzen = ⌈n/3⌉.
2. Die Standorteinstellungen (nur Admin) zeigen das Bool-Feld „Lagerplatz-Etiketten im Rollenformat 93 × 55 mm“ mit
   Hinweistext. Speichern schreibt `true` bzw. `false`, und der Wert bleibt nach dem Reload erhalten.
3. Mit `true` zeigt die Druckvorschau von „Alle Etiketten drucken“ bei n Lagerplätzen genau n Seiten im Format
   93 × 55 mm, je Seite ein Etikett, ohne leere Schlussseite.
4. Mit `true` zeigt der Einzeldruck eines Lagerplatzes genau eine Seite 93 × 55 mm.
5. Jedes Rollenetikett zeigt Code, Beschreibung (falls vorhanden), Barcode und Zone (falls vorhanden) vollständig
   innerhalb der Seite. Kein Element ragt über den Rand oder auf eine zweite Seite. Eine Beschreibung über zwei
   Zeilen wird abgeschnitten, wie heute.
6. Ein Rollenetikett lässt sich mit dem Handscanner lesen, und der gelesene Wert entspricht dem Code. Das gilt für
   einen manuellen 12-stelligen Code und für den längsten Code aus P9 (Manual-UAT am echten Rollendrucker). Ist der
   längste Code nicht lesbar, wird das im Testprotokoll gemeldet und nicht still behoben.
7. Die Bildschirmleiste „Drucken/Zurück“ erscheint nicht im Ausdruck.

**Teil 2**
8. (aus 1A) Sicht 1 zeigt die Spalte „Arbeitsbereich“ direkt rechts neben `HauptFA`. Jede Positionszeile zeigt
   ihren eigenen `Arbeitsbereich`: Zwei Positionen derselben Gruppe mit `H1-03` und `H2-02` zeigen verschiedene Werte.
9. (aus 2A) Eine Position mit leerem oder fehlendem Wert in einer neuen Spalte hat eine leere Zelle, und der
   Gruppenkopf bleibt unverändert.
10. (aus 3A, erweitert) Sicht 1, Filter auf einer beliebigen neuen Spalte: Es erscheinen nur Positionen, deren
    Zelltext passt. Eine Gruppe mit mindestens einer passenden Position erscheint mit genau diesen Positionen,
    `TotalCount` zählt Gruppen mit Treffer, und eine Negation `!<Wert>` blendet nur die betroffenen Positionen aus.
11. Sicht 1 zeigt die Spalten Kunde, FE-Termin, Bemerkung PN und Fertigungsinfo PN. Kunde und FE-Termin sind in
    allen Positionen einer Gruppe gleich und entsprechen den Kopfzeilen dieses HauptFA. Bei einem Kombigerät mit
    zwei verschiedenen Kunden steht „A, B“ (sortiert), und die Positionszahl bleibt gleich.
12. Ein Datumsfilter auf FE-Termin (z. B. `>=01.12.2026`, `01.12.2026...31.12.2026`) filtert in beiden Sichten wie in
    anderen Datumsspalten. Bei einem Kombigerät mit zwei FE-Terminen vergleicht er den früheren.
13. (aus 4A, erweitert) Sicht 2 zeigt Arbeitsbereich direkt rechts neben `Matchcode`, danach HauptFA, Kunde,
    FE-Termin, Bemerkung PN und Fertigungsinfo PN. Jede Aggregatzeile zeigt die verschiedenen Werte der
    beitragenden Positionen. Je Matchcode gibt es genau eine Zeile.
14. Sicht 2, Filter auf einer neuen Spalte (z. B. `arbeitsbereich` = „H1“ oder `hauptfa` = eine Nummer): Die Summen
    enthalten nur Mengen der passenden Positionen, also Vorfilter vor der Aggregation. Matchcodes ohne passende
    Position verschwinden, und `TotalCount` zählt die Aggregatzeilen nach dem Filter. Filter auf
    Matchcode/Summen verhalten sich wie vorher.
15. (aus 5A) Ein Kombigerät beeinflusst weder Anzeige noch Filter der Spalte „Arbeitsbereich“, und es erscheint kein
    Hinweis-Banner.
16. (aus 7) Jede neue Spalte ist in beiden Sichten per Default sichtbar und über das Zahnrad ausblendbar und
    verschiebbar. Nach dem Reload bleibt die Wahl erhalten, und die Prefs-API antwortet nicht mit 400.
17. (aus 8) Für beide Views sind die Key-Mengen von `<th data-col-key>`, `#column-config` und `ColumnDefinitions`
    gleich, mit derselben Reihenfolge von `#column-config` und `ColumnDefinitions`. Sie enthalten `arbeitsbereich`
    und keinen Key `abteilung`. Die Sicht-1-Filtermap hat dieselbe Schlüsselmenge wie
    `ColumnDefinitions.FaHierarchyVormontageEinzeln`. Der Drift-Wächter und `ColumnFilterTypesDriftTests` sind grün.
18. Der CSV-Export beider Sichten enthält die sichtbaren neuen Spalten mit dem angezeigten Text, ausgeblendete fehlen.
19. Der Untertitel von Sicht 2 lautet „… summierte Mengensicht des gewählten VM-Bedarfs (…)“. Für den `VMBedarf` steht
    in der Oberfläche der Vormontage nirgends mehr „Arbeitsbereich“.
20. (aus 9) Reiter (`VMBedarf`), KW-Filter und bestehende Spaltenfilter verhalten sich unverändert, und die
    bestehenden `VormontageServiceTests` sind grün.
21. Zugriff unverändert: ohne Vorbau-Zugriff AccessDenied, Toggle aus führt zu Redirect Home. AKE mit Toggle aus ist
    unberührt.
22. (aus 10) `dotnet build` und `dotnet test` sind grün.
23. (aus 11) P5 ist vor Umsetzungsbeginn gelaufen und entspricht den Screenshot-Werten. Sonst geht die Spec zurück an
    Schranke 1 (Vorbehalt).

## Test-Szenarien

Zwei neue Kapitel in `docs/TESTSZENARIEN.md`, die TS-Nummern vergibt der Dev-Lauf (Bündel aktuell bis TS-103).
Querverweise auf TS-61 (Vormontage-Grundfunktion), TS-67 (Standorteinstellungen), TS-86 (CSV-Export) und TS-91
(Suchsyntax). Danach `secondbrain/tests/testszenarien-index.md` nachziehen. Die Version vergibt der Dev-Lauf
(Bündel v1.71.1).

**Automatisiert**
- Katalog: Das neue Feld steht in `StandortSettingsCatalog.Fields` als Bool im App-Backend (Muster
  `StandortEinstellungenControllerTests`). Die Druckdarstellung selbst ist **nicht** automatisiert prüfbar
  (CSS/Druckdialog), das bleibt Manual-UAT.
- `VormontageServiceTests`, DB-frei:
  - Sicht 1, `arbeitsbereich`: Gruppe mit `H1-03`/`H2-02`, Filter „H1“ lässt die Gruppe nur mit der `H1-03`-Position
    stehen; „!H1“ entfernt nur diese Position; `null` liefert leer, und Filter „H1“ entfernt die Position.
  - Sicht 1, Kopfwerte: HauptFA mit zwei Kopfzeilen (Kunden „B“, „A“; FE-Termine 19.11./12.11.) ergibt Getter
    „A, B“ bzw. „12.11.2026, 19.11.2026“. Datumsfilter `<=15.11.2026` trifft. HauptFA ohne Kopfzeile ergibt leer.
  - Sicht 2, Vorfilter: derselbe Matchcode an Positionen zweier HauptFA bzw. Bereiche ergibt ohne Filter eine Zeile
    mit beiden Werten, sortiert. Filter auf einen HauptFA oder Bereich senkt die Summe auf dessen Mengen. Ein
    Filter auf `sollmenge` wirkt weiter auf die Summe.
  - Formatierer: null/leer/Leerzeichen, Duplikate, Sortierung.
- Drift-Wächter A/B/C für beide Vormontage-Views und ServiceColumnMap-Fall für Sicht 1 (T2.6).

**Manuell, Teil 1** (beide Standorte möglich; Vorbedingung: Rollendrucker mit 93 × 55-Etiketten angeschlossen
(Rückfrage 7), Benutzer mit `masterdata_read` und ein Admin)
1. Einstellung aus: Sammel- und Einzeldruck wie bisher (A4, drei je Seite). Seitenzahl prüfen.
2. Admin schaltet das Rollenformat in den Standorteinstellungen ein und speichert, der Wert bleibt nach Reload.
3. Einzeldruck eines Lagerplatzes mit Beschreibung und Zone: Vorschau zeigt eine Seite 93 × 55. Druck auf dem
   Rollendrucker, der Inhalt ist vollständig.
4. Sammeldruck: Vorschau mit n Seiten, keine leere Schlussseite. Druck einiger Etiketten.
5. Scan: manueller 12-stelliger Code und der längste Code aus P9 mit dem Handscanner (Netum). Ergebnis: ______
6. Negativ: Lagerplatz ohne Beschreibung und Zone, Etikett ohne Lücken-Artefakte. Beschreibung mit 200 Zeichen wird
   abgeschnitten, nicht übergelaufen. Benutzer ohne Stammdaten-Lesezugriff erhält AccessDenied (unverändert).
7. Einstellung wieder aus, A4 wie in Schritt 1.

**Manuell, Teil 2** (IDEAL-Testsystem; Vorbedingung: Toggle `FaHierarchyVormontageAktiv = true`, Benutzer mit
`vorbau`/`admin`, produktivnahe Daten, P5 gelaufen und konform, P7/P8 bekannt)
1. Sicht 1: neue Spalten an der richtigen Position. Arbeitsbereich je Position stimmt mit der Sage-Spalte „Abteilung“
   in „Fertigungsaufträge bearbeiten“ überein (wie P5; in Sage heißt sie weiter „Abteilung“). Kunde/FE-Termin
   stimmen mit dem Gruppenkopf. Bemerkung PN/Fertigungsinfo PN stimmen mit Sage für eine Position aus P7.
2. Filter je neuer Spalte mit ENTER: URL enthält `colf_<key>=`, nur passende Positionen, Seitenzahl passt,
   Negation, Filter entfernen.
3. FE-Termin-Filter mit Datumssyntax; Kombigerät aus P8 zeigt „A, B“, und der Datumsfilter greift auf den früheren
   Termin.
4. Sicht 2: neue Spalten, je Matchcode eine Zeile; Filter Arbeitsbereich bzw. HauptFA senkt die Summen plausibel;
   Untertitel spricht vom VM-Bedarf.
5. Zahnrad: jede neue Spalte aus- und einblenden, verschieben, Reload. Benutzer mit alten Prefs sieht die neuen
   Spalten an der Nachbarposition.
6. CSV-Export beider Sichten mit und ohne ausgeblendete neue Spalten.
7. Kombination mit Reiterwechsel und KW-Filter: Ergebnisse konsistent.
8. Negativ: ohne Vorbau-Zugriff AccessDenied; Toggle aus führt zu Redirect Home.
9. Regression: Kommissionierlisten, Beschichtung, FA-Liste und FA-Struktur unverändert; AKE (Toggle aus) unberührt.

## Etappen (nur bei epic: true)

Entfällt (`epic: false`). Empfohlen sind zwei Commits im selben Lauf (Teil 1, dann Teil 2), aber kein eigener Merge.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | –      | –      | –      |

## Deploy

**Vorläufig.** Der QA-Lauf bestätigt das gegen den echten Diff.

- **Web-App:** ja (View `PrintLabels`, Standort-Katalog, Vormontage-Service/-Views, `ColumnDefinitions`, Register, Hilfe, Changelog)
- **Service:** nein (Sync unverändert; `IDEALAKEWMSService/AppVersion.cs` wird nur mitgezählt)
- **Migration:** nein
- **Vor Deploy:** P5 am IDEAL-Testsystem gelaufen und konform (Vorbehalt, nur Teil 2). Für Teil 1 ist nichts
  vorzubereiten, Default A4.
- **Nach Deploy:** Den Standort-Schalter Rollenformat setzt nur der Admin des Standorts, der den Rollendrucker
  nutzt.
- **Publish-Befehl** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Pruefabfragen fuer Gerald

Nur lesend. Nicht ausgeführt, Ergebnisse stehen aus. Alle Abfragen außer P10 laufen auf der **WMS-DB** des
jeweiligen Systems. Der Cache spiegelt die Sage-View zum Zeitpunkt `SyncedAt`. P10 läuft auf der **Sage-DB IDEAL**.

```sql
-- P0: Welche Sage-Views speisen den Cache? (Ergebnis fuer P10 notieren)
SELECT [Key], [Value] FROM dbo.ServiceSettings
WHERE [Key] IN ('Sync:FaHierarchyListeViewName', 'Sync:FaHierarchyInfosViewName');
SELECT MAX(SyncedAt) AS LetzterSync FROM dbo.FaHierarchyNodes;

-- P3 (uebernommen): Arbeitsbereich-Werte mit Komma (kollidieren mit dem ", "-Trenner der Sicht-2-Anzeige
--     und dem Komma als ODER in Textfiltern)
SELECT DISTINCT Arbeitsbereich FROM dbo.FaHierarchyNodes WHERE Arbeitsbereich LIKE '%,%';

-- P4 (uebernommen): Wertevorrat Arbeitsbereich vs. VMBedarf-Reiter
SELECT DISTINCT Arbeitsbereich FROM dbo.FaHierarchyNodes WHERE Arbeitsbereich IS NOT NULL AND Arbeitsbereich <> '' ORDER BY 1;
SELECT DISTINCT VMBedarf FROM dbo.FaHierarchyNodes WHERE VMBedarf IS NOT NULL AND VMBedarf <> '' ORDER BY 1;

-- P5 (uebernommen, VORBEHALT; praezisiert um HauptFA, Position, SyncedAt): Abgleich mit dem Sage-Screenshot
--     aus [[2026-10-06-fa-struktur-arbeitsgaenge-symbole]], Abschnitt "Vorbild aus Sage"
SELECT HauptFA, Position, Artnr, Bezeichnung1, Arbeitsbereich, Arbeitsschritte, SyncedAt
FROM dbo.FaHierarchyNodes
WHERE Artnr IN ('G-KT-A-BODEN-1320-660-V01', 'FR-4301-40-20-1,25-1530,0MM-V01',
                'G-KT-Mittelholmblende-40-760-V02', 'G-KT-Korpus-Gepunktet')
   OR Bezeichnung1 IN ('G-KT-A-BODEN-1320-660-V01', 'G-KT-Korpus-Gepunktet')
ORDER BY HauptFA, Position;
-- Erwartung laut Screenshot (Sage-Spalte "Abteilung"): A-BODEN = H1-03; FR-4301-...-1530 = S-01;
--   Mittelholmblende = H2-02; Korpus-Gepunktet = (leer). Gleiche HauptFA wie im Screenshot pruefen.

-- P7 (neu): Befuellung der Positionsfelder in der Vormontage-Basis (VMBedarf gesetzt), inkl. Laenge und Zeilenumbruechen
SELECT COUNT(*) AS Positionen,
       SUM(CASE WHEN NULLIF(LTRIM(RTRIM(BemerkungPN)), '')      IS NOT NULL THEN 1 ELSE 0 END) AS BemerkungPN_gefuellt,
       MAX(LEN(BemerkungPN))                                                                  AS BemerkungPN_maxLen,
       SUM(CASE WHEN BemerkungPN LIKE '%' + CHAR(10) + '%' THEN 1 ELSE 0 END)                 AS BemerkungPN_mitUmbruch,
       SUM(CASE WHEN NULLIF(LTRIM(RTRIM(FertigungsInfoPN)), '') IS NOT NULL THEN 1 ELSE 0 END) AS FertigungsInfoPN_gefuellt,
       MAX(LEN(FertigungsInfoPN))                                                             AS FertigungsInfoPN_maxLen,
       SUM(CASE WHEN NULLIF(LTRIM(RTRIM(Arbeitsbereich)), '')   IS NOT NULL THEN 1 ELSE 0 END) AS Arbeitsbereich_gefuellt
FROM dbo.FaHierarchyNodes
WHERE NULLIF(LTRIM(RTRIM(VMBedarf)), '') IS NOT NULL;
SELECT TOP 20 HauptFA, Position, Artnr, BemerkungPN, FertigungsInfoPN
FROM dbo.FaHierarchyNodes
WHERE NULLIF(LTRIM(RTRIM(VMBedarf)), '') IS NOT NULL
  AND (NULLIF(LTRIM(RTRIM(BemerkungPN)), '') IS NOT NULL OR NULLIF(LTRIM(RTRIM(FertigungsInfoPN)), '') IS NOT NULL)
ORDER BY HauptFA, Position;

-- P8 (neu): Kopfwerte fuer die HauptFA der Vormontage-Basis — fehlende Kopfzeile, leere Werte, Kombigeraete mit Abweichung
SELECT COUNT(DISTINCT n.HauptFA) AS HauptFA_gesamt,
       COUNT(DISTINCT CASE WHEN i.HauptFA IS NULL THEN n.HauptFA END) AS HauptFA_ohneKopf,
       COUNT(DISTINCT CASE WHEN NULLIF(LTRIM(RTRIM(i.Kunde)), '') IS NULL AND i.HauptFA IS NOT NULL THEN n.HauptFA END) AS HauptFA_KundeLeer,
       COUNT(DISTINCT CASE WHEN i.FE_Termin IS NULL AND i.HauptFA IS NOT NULL THEN n.HauptFA END) AS HauptFA_FeTerminLeer,
       COUNT(DISTINCT CASE WHEN NULLIF(LTRIM(RTRIM(i.MontageAbteilung)), '') IS NULL AND i.HauptFA IS NOT NULL THEN n.HauptFA END) AS HauptFA_MontageAbtLeer
FROM dbo.FaHierarchyNodes n
LEFT JOIN dbo.FaHierarchyOrderInfos i ON i.HauptFA = n.HauptFA
WHERE NULLIF(LTRIM(RTRIM(n.VMBedarf)), '') IS NOT NULL;
SELECT HauptFA, COUNT(*) AS Kopfzeilen, COUNT(DISTINCT Kunde) AS Kunden,
       COUNT(DISTINCT FE_Termin) AS FeTermine, COUNT(DISTINCT MontageAbteilung) AS Abteilungen
FROM dbo.FaHierarchyOrderInfos
GROUP BY HauptFA HAVING COUNT(*) > 1
ORDER BY Kopfzeilen DESC;

-- P9 (neu, Teil 1, je Standort): laengste Barcode-Werte der Lagerplaetze (Lesbarkeit Rollenetikett)
SELECT TOP 10 Code, BarcodeValue, LEN(COALESCE(BarcodeValue, Code)) AS Laenge, Source, LEN(Description) AS BeschrLaenge
FROM dbo.StorageLocations
ORDER BY LEN(COALESCE(BarcodeValue, Code)) DESC;
```

```sql
-- P10 (neu, Sage-DB IDEAL): Herkunft von BemerkungPN / FertigungsInfoPN / Arbeitsbereich in der View aus P0.
-- <FAListe-View aus P0> einsetzen, z. B. vw_IDEAL_AKE_WMS_FAListe.
EXEC sp_helptext '<FAListe-View aus P0>';
-- Im Text suchen: aus welcher Tabelle/Spalte kommen BemerkungPN, FertigungsInfoPN, Arbeitsbereich
-- (erwartet laut Repo-Doku: Position bevorzugt, Beleg als Fallback; Arbeitsbereich = USER_OSAbteilung nur laut Brain)?
SELECT COUNT(*) AS Zeilen,
       SUM(CASE WHEN NULLIF(LTRIM(RTRIM(BemerkungPN)), '')      IS NOT NULL THEN 1 ELSE 0 END) AS BemerkungPN_gefuellt,
       SUM(CASE WHEN NULLIF(LTRIM(RTRIM(FertigungsInfoPN)), '') IS NOT NULL THEN 1 ELSE 0 END) AS FertigungsInfoPN_gefuellt
FROM [<FAListe-View aus P0>]
WHERE NULLIF(LTRIM(RTRIM(VMBedarf)), '') IS NOT NULL;
-- Zahlen sollen zu P7 passen (Abweichung = Sync-Stand alt oder Existenzfilter FaHierarchySql.cs:149-151).
```

Bis P5, P7 und P10 gelaufen sind, gilt als **unverifiziert**, dass die Sage-Spalte „Abteilung“ dem Feld
`Arbeitsbereich` entspricht und dass `BemerkungPN`/`FertigungsInfoPN` an IDEAL sinnvoll befüllt sind. Liefert P3
Treffer, ist der `", "`-Trenner der Sicht-2-Anzeige nicht eindeutig. Dann ist `" / "` als Anzeigetrenner zu erwägen;
der Filter-Trenner bleibt der Kern ([[0017-ein-parser-fuer-die-spaltenfilter-syntax]]).

## Aufgenommene Spec

Quelle: [[2026-10-06-vormontage-abteilung-spalte-filter-spec]] (Entwurf, zuletzt 2026-10-07, Nachtrag 2026-10-09).
Die Datei bleibt unverändert. Sie trägt bereits den Hinweis „Aufgenommen in eine neue Spec (Gerald 2026-11-06)“.

**Zuordnung: was von dort wohin**

| Alt (Abschnitt / Nummer) | Neu in dieser Spec | Bemerkung |
|---|---|---|
| Ziel/Nutzen, Standort IDEAL | [[#Ziel / Nutzen (das Warum)]], Teil 2 | übernommen |
| Vorbehalt P5 | Callout oben, AK 23, P5 | wörtlich übernommen, P5 um HauptFA/Position/SyncedAt präzisiert |
| Fachliche Ausgangslage (Quelle, Schreib-/Leserwege, Brain-Beleg `USER_OSAbteilung`, Begriffsnähe `VMBedarf`, ADR-0014-Hinweis, kein Kopf-Join) | Tabelle in [[#Teil 2 — Vormontage-Felder]], Out-of-Scope, Anf. 12 | neu gemessen gegen v1.71.1 |
| Anforderungen 1–6 | Anf. 5 (Zeile `arbeitsbereich`), 7, 8, 9, 10, 13 | Filter je Position, Vorfilter in Sicht 2 |
| A1 (ein Map-Eintrag) | T2.2 (Positions-Getter `arbeitsbereich`) | |
| A2 (Gruppen-Paging-Folgen) | Anf. 7, AK 10 | Semantik unverändert |
| A3 (Vorfilter Sicht 2, nicht in `AggregatColumnMap`) | T2.3, Anf. 8 | auf alle neuen Spalten verallgemeinert (Rückfrage 3) |
| Schritte 4/5 (Views, `ColumnDefinitions`) | T2.5 | Position nach `hauptfa`/`matchcode` bleibt |
| Schritt 6 (Drift-Wächter) | T2.6 | um ServiceColumnMap-Fall Sicht 1 ergänzt |
| Schritt 7 (Changelog) | T2.7 | |
| Prüfabfragen P3, P4, P5 | [[#Pruefabfragen fuer Gerald]] | P1, P2, P6 betrafen nur die verworfene Variante B und entfallen |
| Migration/Audit/Rollen: keine | gleichnamige Abschnitte | bestätigt |
| AK 1A–5A, 7–11 | AK 8–10, 13–17, 20, 22, 23 | |
| Testszenarien (automatisiert + manuell 1–8) | [[#Test-Szenarien]] Teil 2 | erweitert |
| Rückfrage 3 (Kombigeräte-Backlog) | Out-of-Scope | beantwortet: eigener Schritt, nur Querverweis |
| FREIGABE-NACHTRAG 2026-10-09 (Untertitel/Reiter) | Anf. 12, AK 19 | Pfad korrigiert, siehe unten |
| Protokoll Variante B (verworfen) | nicht übernommen | bleibt dort als Audit-Trail; Formatierer-Idee B1 lebt in T2.1 für Kunde/FE-Termin wieder auf |
| Nebenbefund `SupportsSortDefault` | [[#Nebenbefunde (nicht Teil des Auftrags)]] | besteht weiter |

**Keyuser- und Gerald-Antworten der aufgenommenen Spec (wörtlich, Quelle dort im Block „Freigabe-Antworten“):**
- Antwort 1 (2026-10-06): „Variante A – Knotenfeld `Arbeitsbereich` (Gerald 2026-10-06: „in der View ist die
  Abteilung gleich dem Arbeitsbereich“), vorbehaltlich P5. Stützend: `fallstricke.md:1118` (Arbeitsbereich = Sage
  `USER_OSAbteilung`, Werte wie K-02, S-01, H4-04 – dieselben Werte wie die Sage-Spalte „Abteilung“). P5-Ergebnis:
  noch ausstehend. Weicht P5 ab, ist die Antwort neu zu entscheiden. Frage 2 entfällt damit.“
- Offen 1 (Keyuser 2026-10-07): „Wie vorgeschlagen (Keyuser 2026-10-07): Anzeige, Filter wirkt vor dem
  Zusammenzählen.“
- Offen 2 (Keyuser 2026-10-07): „Label „Arbeitsbereich“ (Keyuser 2026-10-07); Server-Mode/Registrierung, Position und
  Sichtbarkeit wie vorgeschlagen.“
- Offen 3 (Gerald 2026-10-09): „Eigener Schritt, nur Querverweis im Kombigeraete-Backlog, nicht als erledigt
  markieren (Gerald 2026-10-09).“
- FREIGABE-NACHTRAG (Gerald 2026-10-09): „Untertitel der summierten Sicht (`Views/Vormontage/Summiert.cshtml:68`) von
  „summierte Mengensicht des gewählten Arbeitsbereichs“ auf „… des gewählten VM-Bedarfs“ ändern, damit
  „Arbeitsbereich“ nur noch die neue Spalte meint. Reiter-Beschriftungen der `VMBedarf`-Reiter mitprüfen und, wo sie
  „Arbeitsbereich“ heißen, ebenso auf „VM-Bedarf“ umstellen. Geht dem Rumpf vor.“

**Überholt gegenüber der alten Spec** (dort nicht geändert, hier richtiggestellt):
- ~~Filter-Mini-Syntax „Komma = ODER, `!` = NICHT, Teilstring“ in `ColumnFilterHelper.cs:45-60, 71-93`~~ ist
  überholt. Seit v1.58 wertet `ColumnFilterSyntax` die erweiterte Syntax aus
  ([[0017-ein-parser-fuer-die-spaltenfilter-syntax]]); `ColumnFilterHelper.Apply` ignoriert unbekannte Keys bei `:57`
  (alt `:81`).
- Pfad im Nachtrag: ~~`Views/Vormontage/Summiert.cshtml:68`~~ heißt richtig `Views/FaHierarchyVormontage/Summiert.cshtml:68`.
  Der Text steht dort unverändert.
- Neu gemessene Zeilen (alt → v1.71.1): KW-Vorfilter `:233-241` → `:234-242`; Gruppierung `:245-256` → `:246-257`;
  Nachfilter `:259` → `:260`; Kommissionier-Spalte `ColumnDefinitions.cs:341` → `:345`; `FaHierarchyVormontageEinzeln`
  `:400-415` → `:429-444`; `FaHierarchyVormontageSummiert` `:423-433` → `:452-462`; ViewKeys `:481-482` → `:579-580`;
  FA-Struktur `workarea` `:459` → `:488`; Sync-Schreibweg `:212, 240` → `:221, 249`; `FaHierarchySql.cs:126` → `:146`;
  Full-Refresh `:129-138` → `:138-147`; `Str` `:299` → `:308`; Mapping `ApplicationDbContext.cs:1016` → `:1024`;
  `_FaHierarchyNode.cshtml:93` → `:95`; Drift-Test `:40-44` → `:40-46`; column-config Index `:153-164` → `:154-163`,
  Summiert `:112-118` → `:113-117`. Unverändert sind `ColumnMap :79-90`, `AggregatColumnMap :97-103`,
  `colCount :6/:7`, `th hauptfa :75`, `th matchcode :80`.

## Nebenbefunde (nicht Teil des Auftrags)

- `Services/PrintService.cs` ist registriert (`Program.cs:163`), wird aber nirgends benutzt. Eine Suche nach
  `PrintService` außerhalb von Program.cs, der Klasse selbst und eines Kommentars in
  `FaHierarchyBeschichtungController.cs:94` („kein PrintService“) findet nichts. Dazu kommt: `PrintFileAsync`
  übergibt `printerPath` gar nicht an `rundll32` (`:30`) und würde auf dem **Standarddrucker des Web-Servers**
  drucken.
- `Views/Help/Index.cshtml:562-567` beschreibt noch die Werkbank-Ableitung aus dem Arbeitsbereich, die seit v1.43.0
  zurückgebaut ist ([[fallstricke]] §15). Das [[glossar]] (Eintrag „Arbeitsbereich“) sagt dasselbe Veraltete.
- `ColumnDefinitions.FaHierarchyVormontageEinzeln` hat `SupportsSortDefault: false` (`:431`), das `#view-config`
  derselben View sagt `true` (`Index.cshtml:151`). Das war schon in der alten Spec ein Nebenbefund.
- `docs/TESTSZENARIEN.md:9259` nennt `WarehousePickingController.Print` „Lagerplatz-Etiketten-Druck“. Die View
  druckt aber eine Lagerbestellung ohne Etikett oder Barcode.

## Offene Rueckfragen

1. **„Abteilung“: welches Feld?** Gerald nennt „Abteilung“ neben HauptFA, Kunde und FE_Termin. Im Code gibt es zwei
   Kandidaten. Zum einen `FaHierarchyOrderInfo.MontageAbteilung` (FAInfos `[Abteilung]`, je HauptFA bzw. je
   Kombi-Variante, heute unbeschriftet im Gruppenkopf, `Index.cshtml:114`). Zum anderen
   `FaHierarchyNode.Arbeitsbereich` (je Position). **Verhältnis zur aufgenommenen Spec:** Dort hat Gerald am
   2026-10-06 genau diese Frage beantwortet: „in der View ist die Abteilung gleich dem Arbeitsbereich“ (Variante A,
   vorbehaltlich P5). Der Keyuser hat am 2026-10-07 das Label „Abteilung“/`abteilung` zugunsten von „Arbeitsbereich“
   verworfen. Die FAInfos-Abteilung wurde für **diese** Spalte verworfen, als Gruppenkopf blieb sie bestehen.
   Varianten:
   (a) „Abteilung“ = Arbeitsbereich. Die Spalte `arbeitsbereich` aus der aufgenommenen Spec deckt das ab, es kommt
   keine weitere Spalte dazu.
   (b) Zusätzlich eine Spalte „Montage-Abt.“ (`montage-abteilung`, Label wie FA-Liste `ColumnDefinitions.cs:36`) mit
   dem Kopfwert je HauptFA. Kombigeräte zeigen dann „A, B“. Der Aufwand ist klein, weil Lookup und Formatierer aus
   T2.1/T2.2 schon da sind.
   *Empfehlung:* (a). Grund: Die Frage ist durch Geralds eigene Antwort vom 2026-10-06 entschieden, und zwei Spalten
   mit „Abteilung“-Bedeutung riefen genau die Verwechslung hervor, die der Nachtrag vom 2026-10-09 beseitigen wollte.
   (b) nur, wenn die Vormontage die FAInfos-Abteilung als filterbare Spalte braucht. Im Gruppenkopf steht sie schon.
2. **Ein-/Ausblenden „über Settings“.** Variante 1: die vorhandene Spaltenwahl je Benutzer (Zahnrad,
   `column-preferences.js`, gespeichert über `UserViewPreferencesApiController`, ViewKeys registriert
   `ColumnDefinitions.cs:579-580`). Das kostet keinen neuen Code und ist Pflichtbestandteil von ADR 0005. Variante 2:
   ein Standort-Schalter je Feld (sechs AppSettings, Katalogeinträge, bedingtes Rendern von `<th>`/`<td>`/
   `#column-config`/`ColumnDefinitions` wie in der FA-Liste `ColumnDefinitions.cs:16-21`). Das wäre deutlich mehr
   Code, mehr Drift-Risiko und doppelt gemoppelt neben dem Zahnrad. Die Listen gibt es ohnehin nur an IDEAL, ein
   Standort-Schalter bringt also nichts.
   *Empfehlung:* Variante 1, neue Spalten **standardmäßig sichtbar**. Gründe: Gerald hat die Felder ausdrücklich
   angefordert, der Keyuser hat `arbeitsbereich` am 2026-10-07 als „Default sichtbar“ bestätigt, und jeder Werker
   kann sie mit zwei Klicks ausblenden. Ausnahme nur, wenn P7 sehr lange Texte zeigt (Richtwert > 200 Zeichen oder
   viele Umbrüche): dann `bemerkungpn`/`fertigungsinfopn` mit `defaultHidden`.
3. **Summiert-Sicht: Felder je HauptFA bzw. je Position bei einer Summe über Matchcode.**
   - **A (empfohlen): Anzeige plus Vorfilter.** Der Schlüssel bleibt `Matchcode`. Jede neue Spalte zeigt die
     verschiedenen Werte der beitragenden Positionen, Filter wirken je Position **vor** dem Zusammenzählen.
     *Folgen:* Alle Listen sind gleich, wie gewünscht. Das ist dieselbe Regel, die der Keyuser am 2026-10-07 für den
     Arbeitsbereich entschieden hat. Mit Filter („Kunde X“, „HauptFA 123“) bleibt die Summe richtig. Ohne Filter
     können Zellen lang werden: viele HauptFA je Matchcode, mehrere Bemerkungen.
   - **B: Gruppierschlüssel erweitern** (z. B. `Matchcode + HauptFA`). *Folgen:* Sicht 2 wird fast zu Sicht 1, und
     der Summierzweck geht verloren. Der Keyuser hat das analog für den Arbeitsbereich verworfen.
   - **C: Nur einzelne Felder in Summiert** (z. B. Arbeitsbereich und HauptFA), Kunde/FE-Termin/PN nur in Sicht 1.
     *Folgen:* Weniger lange Zellen, aber das widerspricht „immer alle Vormontagelisten gleich“.
   - Zusatz zu A, **nicht** in diesem Lauf: lange Listen auf drei Werte plus „+n“ kappen (Muster Lagerplatz-Limit
     v1.62, [[fallstricke]] „Volle Liste in einer begrenzten Zelle“). Erst wenn die Praxis es verlangt.
   *Empfehlung:* A ohne Kappung.
4. **„Druckprozessor“.** Variante 1 ist ein Layout in der Browser-Druckansicht `PrintLabels.cshtml`, gedruckt über
   den Druckdialog am Arbeitsplatz. Dort wird auch der Rollendrucker gewählt. Variante 2 wäre serverseitiger Druck.
   Belegt: `PrintService` hat keinen Verbraucher (nur Registrierung `Program.cs:163`). Er ignoriert den
   Druckerparameter (`PrintService.cs:30`) und würde am Standarddrucker des IIS-Servers drucken, nicht im Lager.
   Alle Druckdokumente des Projekts sind Browser-Druckansichten.
   *Empfehlung:* Variante 1, Browser-Druckansicht.
5. **Umfang „alle Vormontagelisten“.** Per Suche nach `FaHierarchyVormontage` und `Vormontage` belegt: Es gibt genau
   zwei Listen, `Index` und `Summiert` (Controller `:63, 102`, Views-Ordner, Menü `_Layout.cshtml:159-163`). Es gibt
   keine Druck-Action, kein PDF und keinen eigenen Export. Der CSV-Export hängt sich automatisch an jede
   `filterable-table` und exportiert die sichtbaren Spalten (`csv-export.js:56, 128, 199`). Er bekommt die neuen
   Spalten ohne eigenen Code. Offen sind die Backlogs Druckansicht [[2026-09-14-vormontage-druckansicht]] und
   Isolierfräsen-Export [[2026-08-06-vormontage-isolierfraesen-export]].
   *Empfehlung:* Einzeln + Summiert + CSV-Export (automatisch). Druck und Export werden in den jeweiligen Backlogs
   vermerkt: „neue Spalten mitnehmen“.
6. **Form der Format-Einstellung.** Die Standort-Maske kennt nur `String`, `Int` und `Bool`
   (`StandortSettingsCatalog.cs:3`, Rendering `Views/StandortEinstellungen/Index.cshtml:74-89`). Auswahllisten gibt es
   nur in `/Settings` (`AppSettingKeys.FesteWerte`, `:40-45`, Rendering `Views/Settings/Index.cshtml:82, 129`).
   Variante 1 ist ein Bool „Rollenformat 93 × 55“ (aus = A4). Er braucht keinen Seed und bleibt beim Speichern
   anderer Werte stabil. Variante 2 ist eine Auswahlliste (A4 / 93×55 / künftige Formate), für die die Standort-Maske
   um Dropdowns erweitert werden muss.
   *Empfehlung:* Variante 1 in einer neuen Gruppe „Druck“. Kommen weitere Formate, wird dann eine Auswahlliste daraus
   (YAGNI).
7. **Rollendrucker und Etikett (Fakten, die nur Gerald kennt).** Welches Druckermodell mit welcher Auflösung (203
   oder 300 dpi)? Sind die Etiketten 93 mm breit und 55 mm hoch, also quer, wie angenommen? Gestanzt mit Lücke oder
   Endlos? Soll das Rollenetikett ohne Rahmen gedruckt werden? Auf Stanz-Etiketten sitzt ein Rahmen selten exakt.
   An welchem Standort und mit welchem Gerät wird getestet?
   *Empfehlung:* quer 93 × 55, ohne Rahmen, Test am echten Drucker mit P9-Codes. Die Druckertreiber-Einstellung
   (Papierformat 93 × 55, Ränder „Keine“) kommt in Hinweistext und Hilfe.
8. **Kombigerät: Kopfwerte bei mehreren Kopfzeilen je HauptFA.** Kunde und FE-Termin können je Variante abweichen
   (P8). Sollen alle verschiedenen Werte erscheinen („A, B“; Datum aufsteigend)? Und genügt es, dass ein Datumsfilter
   dann den **frühesten** FE-Termin vergleicht (technische Grenze `ColumnFilterSyntax.cs:239`)?
   *Empfehlung:* ja. Das [[glossar]] verlangt, dass die Oberfläche nie still eine Variante wählt. Die früheste FE ist
   schon die Regel in `ArbeitsgangStatusRegel.cs:47`. Ein Filter „trifft, wenn irgendeine Variante passt“ bräuchte
   eine Sonderauswertung neben dem Kern. Das sollte er nicht tun ([[fallstricke]] §26 „Spaltenfilter nie selbst
   auswerten“).
9. **Beschriftung und Reihenfolge der neuen Spalten.** *Empfehlung:* Labels „Bemerkung PN“, „Fertigungsinfo PN“,
   „FE-Termin“, „Kunde“, „HauptFA“, „Arbeitsbereich“. Die Sage-Namen bleiben erkennbar, so wie Gerald sie in eckigen
   Klammern genannt hat. Reihenfolge Sicht 1: `hauptfa`, `arbeitsbereich` (Keyuser), `hauptartnr`, `artnr`,
   `matchcode`, `sollmenge`, `fertigungmenge`, `vmbedarf`, `material`, `kunde`, `fe-termin`, `bemerkungpn`,
   `fertigungsinfopn`. Sicht 2: `matchcode`, `arbeitsbereich` (Keyuser), `sollmenge`, `fertigungmenge`, `hauptfa`,
   `kunde`, `fe-termin`, `bemerkungpn`, `fertigungsinfopn`. Die neuen Spalten stehen hinten, damit die gewohnten
   Mengenspalten bleiben, wo sie sind. Jeder Benutzer kann über das Zahnrad umsortieren.
10. **P5-Ergebnis (Vorbehalt).** P5 ist seit der aufgenommenen Spec offen. Bitte vor der Freigabe ausführen und das
    Ergebnis hier eintragen. Weicht es ab, geht die Spec zurück an Schranke 1 (siehe Callout).

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. „Abteilung“ = Arbeitsbereich (a) oder zusätzlich Spalte „Montage-Abt.“ (b)? (Empfehlung: a) → **(a) Arbeitsbereich je Position, keine Spalte „Montage-Abt.“; keine weiteren Felder** (Gerald 2026-11-06).
2. Ein-/Ausblenden über die Spaltenwahl je Benutzer oder Standort-Schalter; neue Spalten Standard sichtbar? (Empfehlung: Zahnrad je Benutzer, sichtbar) → **Wie empfohlen: Zahnrad je Benutzer, sichtbar** (Gerald 2026-11-06).
3. Summiert-Sicht: A Anzeige + Vorfilter / B Schlüssel erweitern / C nur einzelne Felder? (Empfehlung: A ohne Kappung) → **Wie empfohlen: A ohne Kappung** (Gerald 2026-11-06).
4. Druckprozessor = Browser-Druckansicht oder serverseitiger Druck? (Empfehlung: Browser-Druckansicht) → **Wie empfohlen: Browser-Druckansicht** (Gerald 2026-11-06).
5. Umfang: Einzeln + Summiert + CSV automatisch, Druck/Export bleiben Backlog? (Empfehlung: ja) → **Wie empfohlen: ja** (Gerald 2026-11-06).
6. Format-Einstellung als Bool in Gruppe „Druck“ oder Auswahlliste? (Empfehlung: Bool) → **Wie empfohlen: Bool in Gruppe „Druck“** (Gerald 2026-11-06).
7. Rollendrucker: Modell/dpi, Ausrichtung 93 breit × 55 hoch, gestanzt/endlos, Rahmen ja/nein, Test-Standort? (Empfehlung: quer, ohne Rahmen) → **Fortlaufend auf der Rolle, ein Etikett je Seite; Annahme quer 93 × 55 ohne Rahmen. Modell/dpi derzeit unbekannt, Prüfung am echten Drucker bei der Abnahme** (Gerald 2026-11-06).
8. Kombigerät: alle Kopfwerte als Liste, Datumsfilter auf frühesten FE-Termin? (Empfehlung: ja) → **Wie empfohlen: ja** (Gerald 2026-11-06).
9. Labels und Reihenfolge der neuen Spalten wie vorgeschlagen? (Empfehlung: ja) → **Wie empfohlen: ja** (Gerald 2026-11-06).
10. P5-Ergebnis (Vorbehalt Arbeitsbereich = Sage-Spalte „Abteilung“) → **P5 entfällt (Gerald 2026-11-06), Zuordnung ungeprüft. P7: 72 Positionen, BemerkungPN 6 gefüllt (max. 3 Zeichen), FertigungsInfoPN 0. P9: max. 16 Zeichen.**
