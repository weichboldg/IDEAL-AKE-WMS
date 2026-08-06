---
type: spec
title: "IDEAL-Standort Teil 4 — Beschichtungsauftrag"
slug: 2026-07-29-standort-ideal-teil-4-spec
status: Entwurf
created: 2026-08-06
updated: 2026-08-06
source_backlog: "[[2026-07-29-Standort-IDEAL]]"
depends_on: "[[2026-07-29-standort-ideal-teil-1-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/FaHierarchyBeschichtungController.cs (neu, Name provisorisch)
  - IdealAkeWms/Services/BeschichtungsauftragService.cs (neu)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/Index.cshtml (neu)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/Print.cshtml (neu)
  - IdealAkeWms/Models/AppSettingKeys.cs
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Layout-/Kopfdaten-Vorlage fuer den Dienstleister-Ausdruck (Corporate Design, Pflichtfelder) noch nicht vorgelegt"
  - "Rollen/Zugriff: bestehende Rolle wiederverwenden oder neue IDEAL-Rolle?"
  - "Verhaeltnis zum bestehenden AKE-Konzept Lackierteil/Beschichtung (HasCoatingParts/IsCoatingDone, CoatingDateCalculator) — bewusst getrennt (IDEAL-eigene Domain) oder soll spaeter zusammengefuehrt werden?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Druckbares Dokument, das die zu beschichtenden Teile beim Transport zum externen
Beschichtungs-Dienstleister begleitet — analog zur bestehenden AKE-Lackierteil-Logik, aber auf
IDEAL-Datenbasis (`FaHierarchyNode`/`FaHierarchyOrderInfo`, Teil 1).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** Filter `Beschichtet = -1` (Sage-Boolean, bereits in Teil 1 als `bit` gemappt),
Druckdokument mit Dienstleister-Kopfdaten, Farbausfuehrung (`RAL`), Kontaktdaten
(`Dienstleister`), Liefer-/Retourdatum (`Start_Beschichtung`/`Beschichten_Retour`), Teile-Tabelle
(Masse `Breite`/`Hoehe`/`Tiefe`).

**Out-of-Scope:** keine automatische Zuordnung/Erkennung wie beim bestehenden AKE-
`CoatingDetectionService` (`LackierteilKategorieName`) — IDEAL liefert das Flag bereits fertig aus
Sage; `ProductionOrders`/AKE unveraendert.

## Fachliche Anforderungen

- Filter exakt `Beschichtet = -1` (nicht `= 1` — Sage-VB6-Konvention, bereits in Teil 1 korrekt
  gemappt auf `bit`, hier nur konsumiert).
- Kopf: `ABNr`, `Pos`, `MontageAbteilung`, `HauptFA`, `Start_Beschichtung`, `Dienstleister`,
  `RAL`, `Beschichten_Retour` (alle aus `FaHierarchyOrderInfo`).
- Positionstabelle: `HauptFA`, `HauptArtnr`, `Artnr`, `Matchcode`, `Sollmenge`, `Beschichtet`,
  `Breite`, `Hoehe`, `Tiefe` (aus `FaHierarchyNode`).

## Technischer Loesungsentwurf

`BeschichtungsauftragService` liest ueber die Teil-1-Repositories, filtert/aggregiert, liefert ein
Druck-ViewModel. Layout orientiert sich am bestehenden `PrintService`-Muster.

## Migrations-/SQL-Auswirkungen

Keine — reine Lesefunktion.

## Audit-Feld-Auswirkungen

Keine neuen Entitaeten.

## Akzeptanzkriterien

1. Liste/Druck zeigt ausschliesslich Positionen mit `Beschichtet = -1`.
2. Kopfdaten (Dienstleister, RAL, Termine) stimmen mit `FaHierarchyOrderInfo` ueberein.
3. Kombinationsgeraete werden korrekt nach Montage-Abteilung getrennt dargestellt.
4. AKE-Verhalten (bestehende Lackierteil-/Beschichtungslogik) unveraendert.

## Test-Szenarien

Neues Kapitel „IDEAL Teil 4 — Beschichtungsauftrag": Testfall mit mehreren beschichteten
Positionen unter einer Struktur; Druck-Layout-Abnahme gegen die Vorlage (sobald geliefert);
Negativfall (`Beschichtet = 0`) erscheint nicht in der Liste.

## Deploy

- **Web-App:** ja.
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  (provisorisch).

## Offene Rueckfragen

1. Layout-/Kopfdaten-Vorlage fuer den Dienstleister-Ausdruck fehlt noch (Notiz referenziert nur
   ein Konzept-Dokument, kein finales Layout).
2. Rollen/Zugriff fuer diese Liste.
3. Bewusste Trennung von der bestehenden AKE-Lackierteil-Logik — spaetere Zusammenfuehrung
   gewuenscht oder dauerhaft getrennte Domains?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. →layout eventuell über ein Word-Vorlagefile generisch abholen. Word file liegt in dem WEb-APP Verzeichnis in einem Unternordner "Templates", dieses word wird dann befüllt?
2. →neue rolle beschichtungsauftrag?
3. →der beschichtungsauftrag könnte in der ake auch zustande kommen, die erkennung soll getrennt bleiben. eventuell auch hier wieder ein Toggle, "Beschichtungslogik OSEON oder Stückliste"

## Kritische Pruefung (2026-08-06)

Anwalt-des-Teufels-Durchsicht VOR der Freigabe. Gegengelesen: diese Spec, Teil-1-Spec (Datenmodell
+ Sage-Boolean-Mapping), Teil-3/Teil-5-Spec (Geschwister-Teile), die Uebersicht, der Anhang
[[sage-views-ideal]] sowie echter Code (`Services/PrintService.cs`,
`Services/WarehousePickingPrintLayout.cs`, `Views/WarehousePicking/Print.cshtml`,
`Services/CoatingDateCalculator.cs`, `Services/CoatingDetectionService.cs`). Die Teil-Spec ist
kompakt und die Feld-→Tabelle-Zuordnung im Kopf ist grundsaetzlich richtig — aber es gibt zwei
Modell-Bloecke und einen Render-Block, die den Dev-Lauf fehlleiten wuerden, plus die nicht
konsolidierten Schranke-1-Antworten.

### BLOCKER — vor der Freigabe zu klaeren

**B-1 — Filter-Semantik `Beschichtet = -1` ist am Konsum-Layer FALSCH formuliert (genau der teure Fehler).**
Teil 1 mappt den Sage-Rohwert `Beschichtet` (smallint `-1`/`0`) beim Import **in eine echte
`bit`-Spalte** (`Beschichtet = -1 ⇒ true`; Teil-1 Anforderung 7 + Datenmodell-Tabelle). Der
Konsument in Teil 4 liest damit einen **`bool`**, nicht mehr den Rohwert. Diese Spec sagt aber an
drei Stellen woertlich „Filter exakt `Beschichtet = -1`" (Umfang Z. 44, Fachl. Anforderung Z. 55
inkl. **„nicht `= 1`"**, AK 1 Z. 77). Auf der gemappten `bit`/`bool`-Spalte ist der korrekte
Filter `Beschichtet == true` — die `bit`-Spalte fuehrt in der DB den Wert **`1`**, nicht `-1`. Die
Warnung „nicht `= 1`" ist am Konsum-Layer damit **invertiert**: ein Entwickler, der wortgetreu
`WHERE Beschichtet = -1` bzw. LINQ `node.Beschichtet == -1` schreibt, bekommt Compile-Fehler bzw.
Falsch-/Nulltreffer (LINQ auf `bool == -1` kompiliert nicht; roh gegen `bit` ist `-1` nicht der
gespeicherte Wert). Der Rohwert `-1` gehoert **ausschliesslich** in die Teil-1-Import-Mapping-Logik,
nicht in Teil 4. **Fix:** In Umfang, Fachl. Anforderung und AK 1 auf „`Beschichtet = true` (die in
Teil 1 aus dem Sage-Rohwert `-1` gemappte `bit`-Spalte)" umformulieren und den „nicht `= 1`"-Hinweis
streichen (er gilt nur fuer den Sage-View-Rohzugriff in Teil 1).

**B-2 — Dienstleister-Kopf ist bei Kombinationsgeraeten aus der Teil-1-Projektion NICHT ableitbar; AK 3 ist so nicht implementierbar.**
Der einzige Join-Schluessel zwischen `FaHierarchyNode` (Positionen) und `FaHierarchyOrderInfo`
(Kopf: `Dienstleister`, `RAL`, `Start_Beschichtung`, `Beschichten_Retour`, `ABNr`, `Pos`,
`MontageAbteilung`) ist `HauptFA`. Diese Beziehung ist laut Anhang **1:n**: FAInfos-Granularitaet
ist „eine Zeile = ein Auftrag (`ABNr` + `Pos`)", und Kombinationsgeraete teilen sich **denselben
`HauptFA` mit unterschiedlichem `MontageAbteilung`". Gleichzeitig tragen die
`FaHierarchyNode`-Positionen laut Teil 1 (Anforderung 3, Z. 102–103) **selbst keine
Montage-Abteilung** — die ist auftragsbezogen und wurde bewusst NICHT als Struktur-Schluessel auf
den Node gelegt. Damit laesst sich eine Position **nicht** einer bestimmten FAInfos-Zeile
(= einem bestimmten Dienstleister/RAL/Termin) zuordnen. AK 3 („Kombinationsgeraete werden korrekt
nach Montage-Abteilung getrennt dargestellt") ist mit der vorhandenen Projektion **nicht erfuellbar**.
Offene Teilfragen, die die Spec nicht beantwortet: Welche FAInfos-Zeile liefert den Kopf, wenn zu
einem `HauptFA` **mehrere** existieren? Was bei **unterschiedlichem `Dienstleister`/`RAL`** in
derselben Gruppe (mehrere Dokumente? ein Dokument mit mehreren Koepfen? Fehler)? **Fix:** Vor der
Freigabe entscheiden, wie Kopf-Zeile und Positionen bei 1:n `HauptFA`→FAInfos verknuepft werden —
und ob dafuer ein zusaetzlicher Struktur-Schluessel noetig ist (haengt an der noch offenen
B3/B4-Entscheidung aus der Ideen-Notiz). Solange das offen ist, ist AK 3 eine leere Zusage.

**B-3 — Render-Weg widerspruechlich und ohne Infrastruktur (nicht dev-fertig).**
Der „Technische Loesungsentwurf" sagt „Layout orientiert sich am bestehenden `PrintService`-Muster".
`PrintService` ist real ein `rundll32 mshtml.dll,PrintHTML`-Aufruf auf eine **HTML**-Datei
(bestehendes Muster: `Views/WarehousePicking/Print.cshtml` + `WarehousePickingPrintLayout.cs`) —
also HTML-Druck. Die **Schranke-1-Antwort 1** verlangt dagegen einen **Word-Vorlage-Mechanismus**
(`.docx` aus einem `Templates/`-Ordner befuellen). Beides ist unvereinbar, und fuer den Word-Weg
existiert **keinerlei Infrastruktur** (Grep: keine `OpenXml`/`DocumentFormat`/`.docx`-Verarbeitung
im Repo). Der Word-Weg bedeutet neue Bibliothek + Template-Verwaltung + Feld-Befuellung (deutlich
groesser als „reine Lesefunktion"). **Fix:** Render-Weg verbindlich entscheiden (HTML-Print-Partial
analog WarehousePicking **oder** Word-Template mit benannter Library/Feldliste) — davon haengen
Groesse, `affected_code` und Testbarkeit direkt ab.

### SOLLTE — macht den Dev-Lauf sicher

**S-1 — Listen-Ansicht vs. reines Druckdokument nicht abgegrenzt → ADR 0005 offen.** `affected_code`
listet **`Index.cshtml` UND `Print.cshtml`**. Ist `Index` eine filter-/paginierbare Liste der
beschichteten Positionen, greift ADR 0005 zwingend (Pagination `PageSize.Resolve` +
`PaginationState`, Filterkarte, **Server-Spaltenfilter** je `<th>`) — davon steht **nichts** in
Umfang/AK. Ist `Index` nur eine Auswahl-/Startseite fuer den Druck (klein, vorgefiltert), gehoert
das explizit so gesagt (Client-Mode-Ausnahme). Entscheiden und in den AK verankern; sonst baut der
Dev-Lauf entweder eine ADR-0005-verletzende Liste oder eine, die spaeter nachgeruestet werden muss.

**S-2 — Schranke-1-Antworten sind NICHT in die Spec konsolidiert (Rolle, Toggle, OSEON/Stueckliste).**
Alle drei Freigabe-Antworten fuehren neuen Scope ein, der weder in `In-Scope`, `affected_code` noch
in den AK auftaucht:
- *Antwort 2 (neue Rolle „beschichtungsauftrag")*: Eine neue Rolle heisst laut CLAUDE.md **drei
  Stellen** — `RequireXxxAccess`-Attribut/Filter, `secondbrain/codebase/controller.md`,
  `Views/Users/RoleOverview.cshtml`. Keine davon steht in `affected_code`; ein Access-Filter wird
  im Body gar nicht benannt (Pruefpunkt „Rolle" NICHT spezifiziert). ADR 0006.
- *Feature-Toggle*: `affected_code` nennt `AppSettingKeys.cs`, aber Key-Name **und** Default fehlen
  (Anhang schlug `IdealBeschichtungAktiv` vor → nach Namenskonvention `FaHierarchyBeschichtungAktiv`,
  Default `false`). Benennen.
- *Antwort 3 (Toggle „Beschichtungslogik OSEON oder Stueckliste")*: als bewusste Entscheidung/offene
  Frage aufnehmen — bleibt sonst in der Prosa haengen.

**S-3 — Redundanz mit Teil 3 und Teil 5: derselbe Baustein wird dreimal gebaut.** Teil 3
(Kommissionier), Teil 4 (Beschichtung) und Teil 5 (Vormontage) machen strukturell **dasselbe**:
`FaHierarchyNode` nach einem Flag filtern (`Kommissionieren` / `Beschichtet` / `VMBedarf`), Kopf aus
`FaHierarchyOrderInfo` ueber `HauptFA` joinen, nach `HauptFA` (+ `MontageAbteilung`) gruppieren, ein
Druck-ViewModel erzeugen. Trotzdem definiert jede Spec einen eigenen Service + eigene
`Index.cshtml`/`Print.cshtml` **ohne gemeinsamen Baustein**. Folge: Das ungeloeste
Kombi-/`MontageAbteilung`-Join-Problem (B-2) wird **dreimal unabhaengig** geloest — mit
Divergenzrisiko. Empfehlung: einen gemeinsamen `FaHierarchy`-Listen/Druck-Baustein (Filter + Header-
Join + Gruppierung `HauptFA`+`MontageAbteilung` + Druck-Scaffold) extrahieren **oder** einen der drei
Teile als Referenz zuerst umsetzen und die anderen darauf aufsetzen. Querschnitts-Entscheidung fuer
die Freigabe von 3/4/5.

**S-4 — Leerfall und Test-Datenlage.** (a) Der **Leerfall** (ein `HauptFA` ohne einzige beschichtete
Position) ist nirgends definiert — leeres Dokument, uebersprungen, Hinweis? Der Negativfall in den
Test-Szenarien deckt nur „`Beschichtet = 0` erscheint nicht", nicht den Leer-Druck. (b) Laut
Teil-3-Freigabe-Antwort 4 ist das **IDEAL-Testsystem leer** — es gibt dort voraussichtlich **keine
beschichteten Testdaten**, an denen AK 1–3 abgenommen werden koennten; die Abnahme haengt zusaetzlich
an der „Vorlage, sobald geliefert". Test-Checkliste ist damit derzeit **nicht abarbeitbar** — Weg zur
Testbarkeit (Testdaten anlegen / Vorlage beschaffen) benennen.

### HINWEIS

**H-1 — Feld-→Tabelle-Zuordnung im Kern korrekt.** Kopf-Felder (`Dienstleister`, `RAL`,
`Start_Beschichtung`, `Beschichten_Retour`, `ABNr`, `Pos`, `MontageAbteilung`) liegen tatsaechlich
auf dem auftragsbezogenen `FaHierarchyOrderInfo`, die Positionsmasse (`Breite`/`Hoehe`/`Tiefe`,
`Matchcode`, `Sollmenge`, `Beschichtet`) auf `FaHierarchyNode` — das stimmt. Es fehlt „nur" die
**1:n-Aufloesung** (B-2), nicht die Tabellenwahl.

**H-2 — Trennung von der bestehenden AKE-Coating-Logik ist sinnvoll.** `CoatingDetectionService`
(`LackierteilKategorieName`) und `CoatingDateCalculator` bleiben laut Freigabe-Antwort 3 bewusst
getrennt (IDEAL liefert das Flag fertig aus Sage). Der dort angedachte Toggle „OSEON oder
Stueckliste" ist eine echte, noch offene Entscheidung (siehe S-2).

**H-3 — Die Sage-VB6-Boolean-Konvention selbst ist in Teil 1 korrekt.** `-1 ⇒ true` ist dort richtig
gemappt; der einzige Bruch liegt in der **Formulierung** dieser Teil-4-Spec (B-1), nicht in der
Datenbasis.

NACHBESSERUNG NOETIG: (1) Filter-Semantik auf die gemappte `bit`-Spalte korrigieren (B-1), (2) die
1:n-Aufloesung `HauptFA`→FAInfos / Kombi-Dienstleisterkopf entscheiden, sonst ist AK 3 nicht
implementierbar (B-2), und (3) den Render-Weg (HTML-Print vs. Word-Template ohne Infrastruktur)
verbindlich festlegen (B-3). Zusaetzlich Rolle/Toggle konsolidieren und den gemeinsamen 3/4/5-Baustein
klaeren.

