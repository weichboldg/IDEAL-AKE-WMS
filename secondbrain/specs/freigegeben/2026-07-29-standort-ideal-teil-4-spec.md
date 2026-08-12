---
type: spec
title: "IDEAL-Standort Teil 4 — Beschichtungsauftrag"
slug: 2026-07-29-standort-ideal-teil-4-spec
status: Freigegeben
created: 2026-08-06
updated: 2026-08-07
source_backlog: "[[2026-07-29-Standort-IDEAL]]"
depends_on: "[[2026-07-29-standort-ideal-teil-1-spec]], [[2026-07-29-standort-ideal-teil-3-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/FaHierarchyBeschichtungController.cs (neu — erweitert den in Teil 3 geschnittenen gemeinsamen FaHierarchy-Listen-/Druck-Baustein, keine Neuentwicklung von Grund auf)
  - IdealAkeWms/Services/BeschichtungsauftragService.cs (neu — Filter `Beschichtet == true`, Kopf je `HauptFA` in eigener Abfrage ohne Fan-out-Join)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/Index.cshtml (neu — vollwertige ADR-0005-Liste, Seiteneinheit Gruppe `HauptFA`)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/Print.cshtml (neu — HTML-Layout analog Views/WarehousePicking/Print.cshtml, Razor-View direkt an den Browser (`return View`), kein `PrintService` — das waere ein physischer `rundll32 mshtml.dll,PrintHTML`-Druck und im App-Code nirgends konsumiert; Druck via Browser-Druckdialog/CSS `@media print`)
  - IdealAkeWms/Filters/RequireBeschichtungsauftragAccessAttribute.cs (neu — Rolle `beschichtungsauftrag`, Read-Ebene, Class-Level)
  - IdealAkeWms/Filters/RequireFaHierarchyBeschichtungAktivAttribute.cs (neu — AppSetting-Gate, **invertierte Default-Aus-Semantik** — fehlend/nicht `"true"` ⇒ inaktiv — NICHT 1:1 `RequireLagerbestellungAktivAttribute` uebernehmen, dessen Default-Ein aus Bestandsschutz kommt)
  - IdealAkeWms/Models/AppSettingKeys.cs (neuer Key `FaHierarchyBeschichtungAktiv`, Default `false`)
  - secondbrain/codebase/controller.md (Zeile fuer `[RequireBeschichtungsauftragAccess]` + Toggle-Gate ergaenzen — Pflicht laut ADR 0006)
  - IdealAkeWms/Views/Users/RoleOverview.cshtml (Zeile fuer Rolle `beschichtungsauftrag` ergaenzen — dritte der drei Pflichtstellen)
  - README.md (AppSettings-Abschnitt: `FaHierarchyBeschichtungAktiv` dokumentieren)
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Layout-/Kopfdaten-Vorlage (Corporate Design, Pflichtfelder) fuer den Dienstleister-Ausdruck liegt weiterhin nicht vor."
  - "PDF-Erzeugung ist als eigener Querschnitts-Baustein vorgesehen, existiert aber noch nicht als Spec (aktuell nur Backlog-Notiz [[2026-08-06-pdf-erzeugung-fahierarchy-druck]], typ: feature, kein Status-Automat) — Teil 4 liefert deshalb vorerst ausschliesslich Bildschirmdruck; Zeitpunkt/Freigabe der PDF-Folge-Spec ist offen, blockiert aber nicht die Freigabe von Teil 4 selbst."
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-08-07
---

## Ziel / Nutzen (das Warum)

Druckbares Dokument, das die zu beschichtenden Teile beim Transport zum externen
Beschichtungs-Dienstleister begleitet — analog zur bestehenden AKE-Lackierteil-Logik, aber auf
IDEAL-Datenbasis (`FaHierarchyNode`/`FaHierarchyOrderInfo`, Teil 1). Teil 4 baut auf dem in Teil 3
als Referenzimplementierung geschnittenen gemeinsamen FaHierarchy-Listen-/Druck-Baustein auf
(Flag-Filter, Kopf-Join, Gruppierung/Pagination, Druck-Scaffold) und erweitert ihn um den
Beschichtet-Filter und den Dienstleister-Kopf, statt die Mechanik ein zweites Mal zu bauen.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:**
- Liste (ADR 0005, vollwertig — Pagination, Filterkarte, Server-Spaltenfilter) und Bildschirmdruck
  aller `FaHierarchyNode`-Positionen mit `Beschichtet == true` (die in Teil 1 aus dem Sage-Rohwert
  `-1` gemappte `bit`-Spalte), gruppiert nach `HauptFA`.
- Kopfdaten (`Dienstleister`, `RAL`, `Start_Beschichtung`, `Beschichten_Retour`, `ABNr`, `Pos`,
  `MontageAbteilung`) aus `FaHierarchyOrderInfo`, in **eigener Abfrage je `HauptFA`** ermittelt
  (kein Fan-out-Join gegen die Positionen). Existieren zu einem `HauptFA` mehrere FAInfos-Zeilen
  (Kombinationsgeraet), werden **alle** Kopfvarianten im Klartext aufgefuehrt, das Dokument wird
  sichtbar als mehrdeutig gekennzeichnet, und eine `ILogger`-Warnung (`HauptFA` + Zahl der
  Kopfzeilen) wird geschrieben — Web-seitige Lesefunktion, kein Hintergrund-Sync, daher `ILogger`
  statt SyncLog/Aktivitaets-Protokoll (ADR 0010 gilt nur fuer Hintergrund-Services).
- Bildschirmdruck als HTML: Razor-View (`return View`) analog `Views/WarehousePicking/Print.cshtml`,
  direkt an den Browser ausgeliefert, gedruckt ueber den Browser-Druckdialog (CSS `@media print`,
  Seitenumbruch je `HauptFA`). **Kein** `PrintService` — der ruft real `rundll32
  mshtml.dll,PrintHTML` auf einen physischen Drucker auf und ist im App-Code nirgends konsumiert;
  das ist nicht das gemeinte Referenzmuster. Druckt die aktuell gefilterte Liste.
- Neue Rolle `beschichtungsauftrag` (Read-Ebene) + Feature-Toggle `FaHierarchyBeschichtungAktiv`
  (Default `false`, **invertierte Default-Aus-Semantik**: fehlend/nicht `"true"` ⇒ inaktiv — nicht
  1:1 von `RequireLagerbestellungAktivAttribute` uebernehmen, dessen Default-Ein aus Bestandsschutz
  kommt).

**Out-of-Scope:**
- **PDF-Download.** Vorerst vollstaendig aus Teil 4 herausgeloest (siehe Kritische Pruefung
  2026-08-07, B7-1/B7-2): Die PDF-Erzeugung gehoert in einen eigenen Querschnitts-Baustein, den
  sich Teil 3/4/5 teilen sollen, und der aktuell nur als Backlog-Notiz
  [[2026-08-06-pdf-erzeugung-fahierarchy-druck]] existiert (`typ: feature`, keine Spec — der
  Status-Automat kann eine Abhaengigkeit dorthin nie aufloesen). Teil 4 liefert deshalb zunaechst
  ausschliesslich den Bildschirmdruck; der PDF-Download (ein PDF je `HauptFA`, auf Knopfdruck,
  Dateiname `Beschichtungsauftrag_<HauptFA>_<yyyyMMdd-HHmm>.pdf`) wird als eigener Folge-Merge
  nachgezogen, **sobald** diese PDF-Erzeugungs-Spec existiert, freigegeben und umgesetzt ist. Das
  ist eine weiche Sequenzierungs-Abhaengigkeit im Text, keine harte `depends_on`-Referenz.
- Trennung der Positionen nach `MontageAbteilung` bei Kombinationsgeraeten — das Datenmodell liefert
  dafuer keinen Struktur-Schluessel auf Positionsebene (`MontageAbteilung` liegt bewusst nur auf
  `FaHierarchyOrderInfo`, Teil 1). Fachliche Behandlung ist als Backlog-Nachtrag
  [[2026-08-06-kombinationsgeraete-montageabteilung]] festgehalten, konsistent zu Teil 1 und Teil 3.
- Keine automatische Zuordnung/Erkennung wie beim bestehenden AKE-`CoatingDetectionService`
  (`LackierteilKategorieName`) — IDEAL liefert das Flag bereits fertig aus Sage.
- Kein Word-/`.docx`-Template (Render-Weg ist HTML, siehe Fachliche Anforderungen).
- Serverseitige Ablage, Archivierung in enaio oder automatischer Mailversand des PDFs an den
  Dienstleister — bewusst ausgeklammert, spaeter moeglich (gilt erst recht, solange das PDF selbst
  noch nicht existiert).
- `ProductionOrders`/AKE unveraendert.

## Fachliche Anforderungen

- **Filter:** `Beschichtet == true` — die in Teil 1 aus dem Sage-Rohwert `Beschichtet = -1`
  (VB6-Boolean-Konvention) gemappte `bit`-Spalte. Der Rohwert `-1` gehoert ausschliesslich in die
  Teil-1-Import-Mapping-Logik; Teil 4 konsumiert bereits eine echte `bool`-Spalte, in der die
  gespeicherte "wahr"-Auspraegung `1` ist.
- **Gruppierung** nach `HauptFA`. Seiteneinheit der Liste ist die **Gruppe**, nicht die Zeile
  (konsistent zu Teil 2/3/5): eine Gruppe wird nie ueber Seiten getrennt, `TotalCount` zaehlt
  Gruppen. Spaltenfilter wirken auf Zeilen; faellt eine Gruppe dadurch auf null Positionen,
  verschwindet sie ganz (kein Kopf ohne Zeilen).
- **Kopf:** `ABNr`, `Pos`, `MontageAbteilung`, `HauptFA`, `Start_Beschichtung`, `Dienstleister`,
  `RAL`, `Beschichten_Retour` (alle aus `FaHierarchyOrderInfo`, separate Abfrage je `HauptFA`,
  **kein** `INNER JOIN ... ON HauptFA` gegen die Positionen — das wuerde bei Kombinationsgeraeten
  jede Positionszeile pro FAInfos-Zeile duplizieren). Bei mehreren FAInfos-Zeilen je `HauptFA`:
  alle Varianten auflisten, Dokument als mehrdeutig kennzeichnen, `ILogger`-Warnung (`HauptFA` +
  Anzahl Kopfzeilen) — kein SyncLog/Aktivitaets-Protokoll-Eintrag (Web-Lesefunktion, ADR 0010 gilt
  nur fuer Hintergrund-Services).
- **Positionstabelle:** `HauptFA`, `HauptArtnr`, `Artnr`, `Matchcode`, `Sollmenge`, `Beschichtet`,
  `Breite`, `Hoehe`, `Tiefe` (aus `FaHierarchyNode`).
- **Listen-View-Pattern (ADR 0005) — Pflicht:** Pagination (`PageSize.Resolve` + `PaginationState`
  + `_Pagination`-Partial), Filterkarte, Server-Side-Spaltenfilter (`data-server-column-filter`,
  `data-col-key` je `<th>`, `ColumnFilterHelper.ReadFromQuery`). Referenz: `ProductionOrdersController.Index`.
- **Zugriffsschutz (ADR 0006):** neue Rolle `beschichtungsauftrag`, Read-Ebene genuegt (Liste +
  Druck, keine Bearbeitung), Class-Level-Filter `[RequireBeschichtungsauftragAccess]`. Zusaetzlich
  kumulativ das Feature-Toggle-Gate `[RequireFaHierarchyBeschichtungAktiv]`
  (`AppSettingKeys.FaHierarchyBeschichtungAktiv`, Default `false`). **Invertierte Default-Semantik:**
  fehlend/nicht `"true"` ⇒ inaktiv — das Muster `RequireLagerbestellungAktivAttribute` behandelt
  fehlend/`"true"` als **aktiv** (Bestandsschutz, Default-EIN); eine 1:1-Uebernahme wuerde
  Beschichtung faelschlich per Default freischalten. Der Filter ist daher strukturell analog, aber
  mit **umgekehrter** Default-Auswertung zu implementieren.
- **Render-Weg:** HTML, kein Word/`.docx`. Der Bildschirmdruck ist eine Razor-View (`return View`)
  analog `Views/WarehousePicking/Print.cshtml`, die direkt an den Browser ausgeliefert und dort
  ueber den Browser-Druckdialog gedruckt wird — **nicht** ueber `PrintService`
  (`rundll32 mshtml.dll,PrintHTML`), der real auf einen physischen Drucker zielt und im App-Code
  nirgends konsumiert wird. Das gerenderte HTML muss trotzdem bereits **eingebettet** sein (CSS
  inline, keine externen Referenzen), damit derselbe Markup spaeter unveraendert von der separaten
  PDF-Erzeugungs-Spec wiederverwendet werden kann.
- **PDF:** **Out-of-Scope dieser Spec** (siehe Umfang). Die dort dokumentierte Ziel-Konvention (ein
  PDF je `HauptFA`-Gruppe, auf Knopfdruck, reiner Download ohne Ablage, Dateiname
  `Beschichtungsauftrag_<HauptFA>_<yyyyMMdd-HHmm>.pdf`) bleibt als Vorgabe fuer die kuenftige
  PDF-Erzeugungs-Spec/den Folge-Merge stehen, ist aber kein Bestandteil des Umfangs von Teil 4.
- **Leerfall:** Ein `HauptFA`, dessen Positionen nach dem Filter alle wegfallen, erscheint gar
  nicht (weder Gruppe noch leeres Dokument). Ist die gesamte gefilterte Menge leer, zeigt der Druck
  einen Hinweis statt eines leeren Blatts.

## Technischer Loesungsentwurf

`BeschichtungsauftragController`/`BeschichtungsauftragService` erweitern den in Teil 3 geschnittenen
gemeinsamen Baustein (Filter als Parameter, gemeinsamer Header-Join ohne Fan-out, gemeinsame
Gruppierung/Pagination nach `HauptFA`, gemeinsames Druck-Scaffold) um:

- Filter `Beschichtet == true` gegen `IFaHierarchyNodeRepository` (statt `Kommissionieren` wie in
  Teil 3).
- Kopf-Ermittlung ueber `IFaHierarchyOrderInfoRepository.GetByHauptFaAsync(hauptFa)` **je Gruppe**
  in einer eigenen Abfrage (liefert potenziell mehrere Zeilen bei Kombinationsgeraeten) — nicht
  ueber einen SQL-/LINQ-Join gegen die Positionsmenge.
- Zusammenfuehrung im Druck-ViewModel: Positionsliste + Liste der Kopfvarianten je Gruppe;
  `Kopfvarianten.Count > 1` steuert die Mehrdeutigkeits-Kennzeichnung im UI/Druck und loest die
  `ILogger`-Warnung aus.
- Bildschirmdruck: Razor-View `Views/FaHierarchyBeschichtung/Print.cshtml`, direkt per
  `return View(vm)` an den Browser ausgeliefert (kein `PrintService`), druckt die aktuell gefilterte
  Liste mit denselben Query-Parametern wie die Bildschirmliste (inkl. `colf_*`), Seitenumbruch je
  `HauptFA` per CSS `@media print`/`page-break-after`.
- PDF-Download: **nicht Teil dieser Spec** (siehe Umfang, Out-of-Scope). Sobald die separate
  PDF-Erzeugungs-Spec existiert und freigegeben ist, ruft eine Folge-Aenderung deren Dienst mit dem
  HTML-Snippet der Gruppe auf (HTML-zu-PDF, Trennung Erzeugung/Auslieferung) und liefert das
  Ergebnis als Download-Response aus, ohne serverseitige Ablage.

## Migrations-/SQL-Auswirkungen

Keine eigene Migration in Teil 4 — reine Lesefunktion auf den in Teil 1 angelegten Tabellen. Die
serverseitige PDF-Erzeugung ist nicht Teil dieser Spec (siehe Umfang) und wird, sobald der separate
Baustein [[2026-08-06-pdf-erzeugung-fahierarchy-druck]] als eigene Spec existiert, dort bewertet.

## Audit-Feld-Auswirkungen

Keine neuen Entitaeten. Der Hinweis bei mehrdeutigem Kopf (mehrere FAInfos-Zeilen je `HauptFA`)
laeuft ueber eine `ILogger`-Warnung, **nicht** ueber das Aktivitaets-Protokoll (ADR 0010) — Teil 4
ist eine Web-Lesefunktion (`deploy.service: false`, kein Hintergrund-Sync); `ISyncLogger`/
`SyncLogServices.All` ist Infrastruktur der Windows-Service-Syncs und hat fuer einen Web-Request
keinen Lauf, in den geschrieben werden koennte (konsistent zu Teil 3 und zur
Uebersichts-Querschnittsregel „Das Web verschickt keine Mails / kein SyncLog aus dem Web").

## Akzeptanzkriterien

1. Liste/Druck zeigt ausschliesslich Positionen mit `Beschichtet == true` (gemappte `bit`-Spalte);
   Positionen mit `Beschichtet == false` erscheinen nicht.
2. Kopfdaten (Dienstleister, RAL, Termine) stammen aus einer separaten Abfrage je `HauptFA` gegen
   `FaHierarchyOrderInfo` — nachweisbar kein `INNER JOIN` gegen die Positionsmenge im erzeugten SQL.
3. Existieren zu einem `HauptFA` mehrere FAInfos-Zeilen (Kombinationsgeraet), werden alle
   Kopfvarianten im Klartext aufgefuehrt, das Dokument ist sichtbar als mehrdeutig gekennzeichnet,
   und eine `ILogger`-Warnung (`HauptFA` + Anzahl Kopfzeilen) wird geschrieben — kein SyncLog/
   Aktivitaets-Protokoll-Eintrag (ADR 0010 gilt nur fuer Hintergrund-Services). (Ersetzt die
   urspruengliche, nicht implementierbare Fassung "korrekt nach Montage-Abteilung getrennt".)
4. Die Liste erfuellt ADR 0005 vollstaendig: Pagination, Filterkarte, Server-Side-Spaltenfilter je
   `<th>`; Seiteneinheit ist die Gruppe (`HauptFA`), `TotalCount` zaehlt Gruppen.
5. Zugriff nur mit Rolle `beschichtungsauftrag` (oder `admin`) **und** aktivem Toggle
   `FaHierarchyBeschichtungAktiv`; bei inaktivem Toggle Redirect/404 analog dem bestehenden
   AppSetting-Gate-Muster (`RequireLagerbestellungAktivAttribute`). **Invertierte Default-Semantik:**
   fehlend/nicht `"true"` ⇒ inaktiv (nicht 1:1 uebernommen von `RequireLagerbestellungAktivAttribute`,
   dessen Default-Ein aus Bestandsschutz kommt).
6. Ein `HauptFA` ohne beschichtete Positionen erscheint nicht (weder Gruppe noch leeres Dokument);
   ist die gesamte gefilterte Menge leer, zeigt der Druck einen Hinweis statt eines leeren Blatts.
7. AKE-Verhalten (bestehende Lackierteil-/Beschichtungslogik, `CoatingDetectionService`,
   `CoatingDateCalculator`) bleibt unveraendert.

PDF-Download ist **kein** Akzeptanzkriterium dieser Spec (Out-of-Scope, siehe Umfang) — wird
Bestandteil eines Folge-Merges, sobald die separate PDF-Erzeugungs-Spec existiert, freigegeben und
umgesetzt ist.

## Test-Szenarien

Neues Kapitel „IDEAL Teil 4 — Beschichtungsauftrag":
- Testfall mit mehreren beschichteten Positionen unter einer Struktur (Filter `Beschichtet == true`).
- Negativfall (`Beschichtet == false`) erscheint nicht in der Liste.
- Kombinationsgeraet mit mehreren FAInfos-Zeilen je `HauptFA` (falls am Testsystem vorhanden):
  alle Kopfvarianten erscheinen, Dokument ist als mehrdeutig gekennzeichnet, `ILogger`-Warnung
  vorhanden (Server-Log, nicht SyncLog).
- Leerfall: `HauptFA` ohne beschichtete Positionen erscheint nicht; komplett leere gefilterte Liste
  zeigt Hinweis statt leerem Druck.
- Zugriffsschutz: Benutzer ohne Rolle `beschichtungsauftrag`/`admin` wird abgewiesen; Toggle aus →
  Redirect/404; Toggle-Key fehlt komplett (kein Eintrag in `AppSettings`) → ebenfalls inaktiv
  (invertierte Default-Semantik, AK 5).
- Druck-Layout-Abnahme gegen die Vorlage (sobald geliefert, siehe offene Rueckfrage 4).
- PDF-Download-Abnahme: **entfaellt fuer diese Spec** (Out-of-Scope, siehe Umfang) — wird
  Bestandteil des Testkapitels, sobald der PDF-Download per Folge-Merge nachgezogen wird.
- **Vorbedingung fuer alle vorstehenden Faelle:** produktivnahe IDEAL-Daten mit beschichteten
  Positionen im Testsystem (aktuell laut Teil-3-Freigabe-Antwort 4 leer) — Schranke 2 kann ohne
  diese Daten nicht gruen werden.

## Deploy

- **Web-App:** ja.
- **Service:** nein.
- **Migration:** nein.
- **Reihenfolge/Voraussetzung:** setzt Teil 1 (Datenbasis `FaHierarchyNode`/`FaHierarchyOrderInfo`)
  und Teil 3 (gemeinsamer FaHierarchy-Listen-/Druck-Baustein als Referenzimplementierung) voraus.
  Teil 4 selbst liefert ausschliesslich Bildschirmdruck und ist davon unabhaengig deploybar; der
  PDF-Download ist Out-of-Scope dieser Spec und folgt als eigener Merge, sobald die separate
  PDF-Erzeugungs-Spec (aktuell nur Backlog-Notiz
  [[2026-08-06-pdf-erzeugung-fahierarchy-druck]]) existiert, freigegeben und umgesetzt ist.
- **Schranke-2-Vorbedingung:** Manual-UAT (Filter, Kombigeraet-Kopf, Leerfall, Druckvergleich) kann
  erst gruen werden, wenn produktivnahe IDEAL-Daten mit beschichteten Positionen im Testsystem
  liegen — das Testsystem ist laut Teil-3-Freigabe-Antwort 4 aktuell leer. Zusaetzlich haengt die
  Layout-Abnahme an der weiterhin fehlenden Dienstleister-Vorlage (offene Rueckfrage 4). Beides dem
  qa-agent explizit als Vorbedingung melden, nicht erst beim Testversuch entdecken lassen.
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  (provisorisch).

## Offene Rueckfragen

1. **GEKLAERT (Render-Weg, Kritische Pruefung 2026-08-06 B-3 + 2026-08-07 S7-1):** Render-Weg ist
   HTML, kein Word/`.docx`. Bildschirmdruck ist eine Razor-View (`return View`) analog
   `Views/WarehousePicking/Print.cshtml`, **ohne** `PrintService` — der ist real ein physischer
   `rundll32 mshtml.dll,PrintHTML`-Druck und im App-Code nirgends konsumiert, also nicht das
   gemeinte Referenzmuster.
   *(Die urspruengliche Antwort 1 unten verlangte einen Word-Vorlage-Mechanismus; das ist durch die
   Kritische Pruefung ueberholt, siehe Abschnitt „Antworten auf die Kritische Pruefung" weiter
   unten.)*
2. **GEKLAERT (Rolle/Zugriff, Kritische Pruefung 2026-08-06 + 2026-08-07 S7-2):** neue Rolle
   `beschichtungsauftrag`, Read-Ebene, Toggle `FaHierarchyBeschichtungAktiv` (Default `false`,
   **invertierte Default-Aus-Semantik**: fehlend/nicht `"true"` ⇒ inaktiv — nicht 1:1
   `RequireLagerbestellungAktivAttribute` uebernehmen, dessen Default-Ein aus Bestandsschutz kommt).
   Drei Pflichtstellen (Attribut, `controller.md`, `RoleOverview.cshtml`) in `affected_code`
   verankert.
3. **Bewusst aus dem Umfang genommen (Verhaeltnis zur AKE-Logik):** bewusst getrennt von
   `CoatingDetectionService`/`CoatingDateCalculator` (IDEAL liefert das Flag fertig aus Sage). Der
   angedachte Toggle „Beschichtungslogik OSEON oder Stueckliste" ist eine eigene, noch nicht
   getroffene Entscheidung und **nicht** Teil dieser Spec.
4. Layout-/Kopfdaten-Vorlage (Corporate Design, Pflichtfelder) fuer den Dienstleister-Ausdruck liegt
   weiterhin nicht vor.
5. **GEKLAERT (PDF-Umfang, Kritische Pruefung 2026-08-07 B7-1/B7-2):** PDF-Download ist aus Teil 4
   herausgeloest (Out-of-Scope). `depends_on` verweist nur noch auf echte Specs (Teil 1, Teil 3);
   der PDF-Baustein ist aktuell nur eine Backlog-Notiz
   ([[2026-08-06-pdf-erzeugung-fahierarchy-druck]], `typ: feature`), auf die der Status-Automat
   keine Abhaengigkeit aufloesen kann. Damit ist auch die ehemalige Rueckfrage zur
   Headless-Edge-Verfuegbarkeit fuer diese Spec gegenstandslos — sie gehoert zur kuenftigen
   PDF-Erzeugungs-Spec.
6. **Bleibt offen, kein Blocker fuer Teil 4:** Zeitpunkt/Freigabe der separaten
   PDF-Erzeugungs-Spec. Das bestimmt nur, wann der PDF-Download als eigener Folge-Merge nachgezogen
   werden kann — nicht die Freigabe von Teil 4 selbst, der ausschliesslich Bildschirmdruck umfasst.

*(Die Testbarkeit am leeren IDEAL-Testsystem ist als Vorbedingung im Deploy-Abschnitt dokumentiert,
nicht mehr als eigene Rueckfrage gefuehrt — analog Teil 3.)*

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. →layout eventuell über ein Word-Vorlagefile generisch abholen. Word file liegt in dem WEb-APP Verzeichnis in einem Unternordner "Templates", dieses word wird dann befüllt?
   *(Praezisiert durch die Kritische Pruefung/Nachbesserung: Render-Weg ist HTML statt Word, ohne
   `PrintService` fuer den Bildschirmdruck (Razor-View), siehe Abschnitt „Antworten auf die
   Kritische Pruefung" unten — das eigentliche Vorlagen-Layout selbst ist trotzdem noch offen, siehe
   Rueckfrage 4.)*
2. →neue rolle beschichtungsauftrag?
   *(Ergaenzt durch Kritische Pruefung 2026-08-07 (S7-2): Toggle-Default muss invertiert sein
   (fehlend/nicht `"true"` ⇒ inaktiv), nicht 1:1 von `RequireLagerbestellungAktivAttribute`
   uebernommen werden.)*
3. →der beschichtungsauftrag könnte in der ake auch zustande kommen, die erkennung soll getrennt bleiben. eventuell auch hier wieder ein Toggle, "Beschichtungslogik OSEON oder Stückliste"
4. →
5. → *(kein weiterer Freigabe-Bedarf — per Kritische Pruefung 2026-08-07 (B7-1/B7-2) redaktionell
   geloest: PDF-Download aus Teil 4 herausgeloest, siehe Rueckfrage 5 und Nachbesserung 2.)*
6. →

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
=> korrekt, gilt nur für Sage View - bitte die specs überarbeiten

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

=> Kombinationsgeraeten bitte vorerst out of scope und auch hier wieder vermerken und im backlog hinterlegen.

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
=> dann anstatt eines DOCS ein HTML welches gerendert und befüllt wird. Daraus sollte sich ein PDF erstellen lassen können.

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

### Nachbesserung (2026-08-06)

**B-1 (Filter-Semantik) — behoben.** Umfang, Fachliche Anforderungen und AK 1 sind auf
`Beschichtet == true` (die in Teil 1 aus dem Sage-Rohwert `-1` gemappte `bit`-Spalte) umgestellt;
der irrefuehrende „nicht `= 1`"-Hinweis ist gestrichen. Der Rohwert `-1` wird nur noch im
Zusammenhang mit Teil 1 erwaehnt.

**B-2 (Dienstleister-Kopf 1:n) — behoben, Umfang bewusst reduziert.** Konsistent zu Teil 1, Teil 3
und der Uebersicht sind Kombinationsgeraete fuer die **Positions-Trennung** out of scope — die
urspruengliche AK 3 („korrekt nach Montage-Abteilung getrennt") ist gestrichen. Der 1:n-Fall selbst
wird trotzdem behandelt, nicht ignoriert: kein Fan-out-Join, Kopfdaten in eigener Abfrage je
`HauptFA`, bei mehreren FAInfos-Zeilen alle Varianten im Klartext + Kennzeichnung als mehrdeutig +
Aktivitaets-/SyncLog-Eintrag (neue AK 3). Die fachliche Trennung nach Montage-Abteilung ist im
Backlog-Nachtrag [[2026-08-06-kombinationsgeraete-montageabteilung]] festgehalten (Datei existiert
bereits in `secondbrain/backlog/`).

**B-3 (Render-Weg) — behoben, mit offener Folgeabhaengigkeit.** Word/`.docx` entfaellt vollstaendig
(keine Infrastruktur vorhanden). Render-Weg ist HTML analog `Views/WarehousePicking/Print.cshtml` +
`WarehousePickingPrintLayout.cs`, ausgeliefert ueber den bestehenden `PrintService`. Die zusaetzlich
gewuenschte serverseitige PDF-Erzeugung (ein PDF je `HauptFA` auf Knopfdruck) ist bewusst **nicht**
Teil dieser Spec, sondern des neuen Querschnitts-Bausteins
[[2026-08-06-pdf-erzeugung-fahierarchy-druck]] (bereits in `depends_on`). **Bleibt Rueckfrage:**
diese Spec existiert zum jetzigen Zeitpunkt noch nicht, und die Sequenzierung (Teil 4 zuerst nur mit
Bildschirmdruck ausliefern, PDF-Knopf als Folge-Merge — oder Teil 4 komplett auf den Baustein warten
lassen) ist vor Freigabe zu entscheiden (offene Rueckfrage 5); ebenso ungeklaert ist die
Headless-Edge-Verfuegbarkeit auf dem Zielserver (offene Rueckfrage 6).

**S-1 (Listen-Ansicht vs. Druckdokument) — behoben.** `Index` ist verbindlich als vollwertige
ADR-0005-Liste festgeschrieben (Pagination, Filterkarte, Server-Spaltenfilter je `<th>`); Seiteneinheit
ist die Gruppe (`HauptFA`), `TotalCount` zaehlt Gruppen — konsistent zu Teil 3.

**S-2 (Rolle/Toggle nicht konsolidiert) — behoben.** Neue Rolle `beschichtungsauftrag` (Read-Ebene)
mit `RequireBeschichtungsauftragAccessAttribute`, an den drei laut CLAUDE.md/ADR 0006 Pflichtstellen
in `affected_code` verankert (Attribut, `controller.md`, `RoleOverview.cshtml`). Feature-Toggle
`FaHierarchyBeschichtungAktiv` (AppSettings, Default `false`) benannt, eigenes Gate-Attribut
`RequireFaHierarchyBeschichtungAktivAttribute` analog `RequireLagerbestellungAktivAttribute`, in
`affected_code` inkl. README verankert. Der OSEON/Stueckliste-Toggle bleibt bewusst **offene
Entscheidung**, nicht Teil dieser Spec (Rueckfrage 3/7, unveraendert offen).

**S-3 (Redundanz mit Teil 3/5) — behoben.** Laut den Ergaenzenden Querschnitts-Entscheidungen der
Uebersicht wird Teil 3 Referenzimplementierung des gemeinsamen Bausteins; Teil 4 erweitert ihn statt
ihn zu duplizieren. `depends_on` ist bereits um die Teil-3-Spec ergaenzt (siehe Frontmatter).

**S-4 (Leerfall/Testdaten) — teilweise behoben.** Der Leerfall ist jetzt definiert (AK 6: `HauptFA`
ohne beschichtete Positionen erscheint nicht, komplett leere gefilterte Liste zeigt einen Hinweis
statt eines leeren Blatts). **Bleibt Rueckfrage:** die Testdatenlage selbst — das IDEAL-Testsystem
ist laut Teil-3-Freigabe-Antwort 4 leer; als Vorbedingung im Deploy-/Test-Abschnitt vermerkt.
Schranke 2 kann erst gruen werden, wenn produktivnahe IDEAL-Daten mit beschichteten Positionen im
Testsystem liegen — zusaetzlich haengt die Layout-Abnahme an der weiterhin fehlenden
Dienstleister-Vorlage (Rueckfrage 4).

## Antworten auf die Kritische Pruefung (2026-08-06)

**Zu B-1 — Filter-Semantik: `Beschichtet == true`. [ENTSCHIEDEN]**
Der Rohwert `-1` gehoert **ausschliesslich** in die Teil-1-Import-Mapping-Logik (Sage-View-Zugriff,
VB6-Boolean-Konvention). Teil 4 konsumiert die bereits gemappte `bit`/`bool`-Spalte und filtert
daher `Beschichtet == true`. In Umfang, Fachlicher Anforderung und AK 1 entsprechend umformulieren;
den Hinweis "nicht `= 1`" **streichen** — er gilt nur fuer den Rohzugriff in Teil 1 und ist am
Konsum-Layer irrefuehrend.

**Zu B-2 — Kombinationsgeraete out of scope, aber der 1:n-Fall muss trotzdem behandelt werden.**
"Out of scope" heisst: **wir bauen keine Trennung** — nicht: der Fall kann nicht auftreten.
Die Daten enthalten ihn weiterhin, und anders als in Teil 3 braucht Teil 4 die Kopfdaten
(`Dienstleister`, `RAL`, Termine) zwingend. Deshalb verbindlich:
- **AK 3 entfaellt** ("Kombinationsgeraete werden nach Montage-Abteilung getrennt dargestellt") —
  konsistent zu Teil 1 und Teil 3.
- **Kein Fan-out-Join:** `FaHierarchyOrderInfo` NICHT per `INNER JOIN ... ON HauptFA` an die
  Positionen haengen. Kopfdaten in **eigener Abfrage** je `HauptFA` holen und im ViewModel
  zusammenfuehren.
- **Mehrere FAInfos-Zeilen zu einem `HauptFA`: NICHT stillschweigend eine auswaehlen.** Ein falsch
  gewaehlter `Dienstleister` oder `RAL` bedeutet, dass Teile physisch zum falschen Beschichter
  gehen — das ist ein fachlicher Schaden, kein Darstellungsfehler.
  Verhalten: **alle** Kopfvarianten im Klartext auffuehren (je Montage-Abteilung eine Zeile mit
  Dienstleister/RAL/Terminen), das Dokument sichtbar als **mehrdeutig kennzeichnen** und einen
  Eintrag im Aktivitaets-/SyncLog schreiben (HauptFA + Zahl der Kopfzeilen). So faellt der Fall auf
  und wird von Hand entschieden, statt falsch gedruckt zu werden.
- **Backlog-Nachtrag** (gemeinsam mit dem Eintrag aus Teil 1, B-1): fachliche Behandlung von
  Kombinationsgeraeten — Trennschluessel auf Positionsebene, getrennte Dokumente je
  Montage-Abteilung.

**Zu B-3 — Render-Weg: HTML statt Word. [ENTSCHIEDEN] — aber PDF ist eine eigene Frage.**
Der Word-/`.docx`-Weg entfaellt: gerendert wird **HTML**, befuellt ueber ein Razor-Print-Layout
analog `Views/WarehousePicking/Print.cshtml` + `WarehousePickingPrintLayout.cs`. Damit bleibt die
bestehende Infrastruktur (`PrintService`) tragend, keine neue Bibliothek.

**PDF: serverseitig erzeugt. [ENTSCHIEDEN — Variante b]**
Das Dokument wird von der Anwendung als **PDF-Datei erzeugt**, nicht nur ueber den Druckdialog
angeboten. Damit kommt eine HTML-nach-PDF-Komponente hinzu — der bestehende `PrintService`
(`rundll32 mshtml.dll,PrintHTML`) druckt nur und kann das nicht.

**Empfohlener Weg — Headless Edge/Chrome ueber Prozessaufruf.**
```
msedge.exe --headless --disable-gpu --print-to-pdf="<ziel>.pdf" --no-pdf-header-footer "<quelle>.html"
```
Begruendung: Es passt zum bestehenden Muster (`PrintService` ruft heute schon ein OS-Werkzeug auf),
braucht **kein NuGet-Paket und kein mitgeliefertes Chromium**, und der Renderer wird ueber Windows
Update gepflegt. Das gerenderte HTML ist **dasselbe** wie im Bildschirm-Druck — ein Layout, keine
zweite Wahrheit.
Zu pruefen/absichern: Ist Edge auf dem Zielserver vorhanden (bei Server Core nicht selbstverstaendlich)?
Schreibbares Temp-Verzeichnis, Prozess-Timeout, Aufraeumen der Temp-Dateien, Serialisierung bei
Parallelaufrufen. Alle Ressourcen im Print-HTML muessen **eingebettet** sein (CSS inline, Bilder als
Data-URI) — Headless-Edge folgt keinen authentifizierten Links.

**Alternativen, bewusst abgewogen:**
- *PuppeteerSharp* — mehr Kontrolle (Kopf-/Fusszeilen, Warten auf Rendering), aber laedt ein
  eigenes Chromium (~150 MB) und ist im Betrieb schwerer. Nur, falls der Prozessaufruf an seine
  Grenzen stoesst.
- *QuestPDF* — **abgelehnt.** Erzeugt hervorragende PDFs, aber das Layout wird in C# geschrieben,
  nicht in HTML. Damit gaebe es das Druck-Layout **zweimal** (HTML fuer den Bildschirmdruck, C#
  fuer die PDF) — genau die Doppelung, die wir mit der S-3-Entscheidung gerade beseitigt haben.
- *wkhtmltopdf* — abgelehnt: veralteter WebKit ohne modernes CSS, native Abhaengigkeit,
  Wartungsstand fraglich.

**Konsequenz 1 — die PDF-Erzeugung gehoert NICHT in Teil 4.** Sie ist Infrastruktur, kein Merkmal
von Beschichtungsauftraegen, und Teil 3/4/5 teilen sich laut S-3 dasselbe Druck-Geruest. Sie gehoert
daher in den **gemeinsamen Baustein** und wird als **eigene kleine Spec** gefuehrt (Arbeitstitel
"PDF-Erzeugung fuer FaHierarchy-Druckdokumente"), auf die Teil 3, 4 und 5 aufsetzen. Sonst baut
Teil 4 eine Infrastruktur, die Teil 5 danach nochmal anfassen muss.

**Konsequenz 2 — Ablage, Ausloesung, Benennung. [ENTSCHIEDEN]**
- **Wohin:** **vorerst reiner Download.** Die Datei wird temporaer erzeugt, als Download
  ausgeliefert und danach geloescht — **keine serverseitige Ablage, keine Aufbewahrungsregel,
  kein Ablagepfad.** Das haelt Teil 4 klein.
- **Wann:** **auf Knopfdruck** des Benutzers. Kein automatisches Erzeugen beim Aufbau der Liste.
- **Umfang je Datei:** **ein PDF je `HauptFA`**, nicht ein Sammeldokument ueber die ganze
  gefilterte Liste. Begruendung: Ein Beschichtungsauftrag geht an *einen* Dienstleister — ein
  Sammel-PDF mit zwanzig fremden Auftraegen waere weder mailbar noch ablegbar, und der Dateiname
  koennte keinen Auftrag benennen. Der PDF-Knopf sitzt daher **an der Gruppe**, nicht ueber der
  Liste.
  Der bestehende **Bildschirmdruck** bleibt davon unberuehrt: er druckt weiterhin die gesamte
  gefilterte Liste mit Seitenumbruch je `HauptFA`.
- **Benennung:**
  ```
  Beschichtungsauftrag_<HauptFA>_<yyyyMMdd-HHmm>.pdf
  z. B. Beschichtungsauftrag_104857_20260806-1430.pdf
  ```
  Aufbau bewusst in dieser Reihenfolge: **Dokumentart** zuerst (sortiert im Download-Ordner nach
  Art), **`HauptFA`** als zweites (der einzige Identifier, in dem die Produktion kommuniziert —
  siehe Anhang), **Zeitstempel** zuletzt als Eindeutigkeit und Versions-Unterscheidung.
  *Ein reiner Zeitstempel (`20260806-1430.pdf`) waere ungeeignet:* Im Download-Ordner oder als
  Mailanhang liesse sich nicht erkennen, um welchen Auftrag es geht, und die Sortierung erfolgte
  nach Uhrzeit statt nach Auftrag.
  Keine Klarnamen (Dienstleister, Kunde) im Dateinamen — Umlaute/Sonderzeichen machen beim
  Mailversand und auf Dateifreigaben Probleme.
- **Spaeter moeglich, jetzt NICHT im Umfang:** Ablage auf einer Freigabe, Archivierung in enaio,
  automatischer Mailversand an den Dienstleister. Bewusst ausgeklammert.

**Konsequenz 2b — Schnitt, damit der spaetere Ausbau nicht wehtut.** Weil sich das Ziel absehbar
aendert (Mailanhang/Ablage), muessen **Erzeugung** und **Auslieferung** getrennt sein: Ein Dienst
erzeugt aus dem Print-HTML einen PDF-Bytestrom; **was damit geschieht** (heute: Download-Response;
spaeter: Datei schreiben, Mail anhaengen, in enaio ablegen) ist Sache des Aufrufers. Wird beides
vermischt, kostet jede spaetere Verwendung einen Umbau.

**Konsequenz 3 — Groesse.** Teil 4 war als "reine Lesefunktion" veranschlagt. Mit serverseitiger
PDF-Erzeugung ist das nicht mehr zutreffend; die Aufwandsschaetzung und `affected_code` sind
entsprechend anzupassen — beziehungsweise faellt der Zusatzaufwand ohnehin in die eigene
PDF-Spec (Konsequenz 1).

**Zu S-1 — `Index` ist eine vollwertige Liste nach ADR 0005.** Konsistent zu Teil 3: Pagination
(`PageSize.Resolve` + `PaginationState` + `_Pagination`-Partial), Filterkarte und Server-Side-
Spaltenfilter je `<th>` sind Pflicht. **Seiteneinheit ist die Gruppe (`HauptFA`), nicht die Zeile** —
eine Gruppe wird nie ueber Seiten getrennt, `TotalCount` zaehlt Gruppen. Spaltenfilter wirken auf
Zeilen; faellt eine Gruppe dadurch auf null Positionen, verschwindet sie ganz.

**Zu S-2 — Rolle und Toggle konkret.**
- **Neue Rolle `beschichtungsauftrag`** (anders als Teil 3, das die bestehende Kommissionier-Rolle
  uebernimmt). Nach CLAUDE.md/ADR 0006 an **drei Stellen** eintragen: `RequireXxxAccess`-Attribut
  bzw. Filter, `secondbrain/codebase/controller.md`, `Views/Users/RoleOverview.cshtml`. Alle drei
  gehoeren in `affected_code`. Read-Ebene genuegt (Liste + Druck, keine Bearbeitung).
- **Feature-Toggle `FaHierarchyBeschichtungAktiv`, Default `false`** (Namenskonvention wie Teil 3),
  Key in `AppSettingKeys.cs` + README/Settings-Doku.
- **Toggle "Beschichtungslogik OSEON oder Stueckliste"** wird als **offene Entscheidung** gefuehrt,
  nicht in Teil 4 umgesetzt. Die bestehende AKE-Logik (`CoatingDetectionService`,
  `CoatingDateCalculator`) bleibt unberuehrt — IDEAL liefert das Flag fertig aus Sage.

**Zu S-3 — Teil 3 wird Referenzimplementierung; Teil 4 und 5 setzen darauf auf. [ENTSCHIEDEN]**
Teil 3/4/5 machen strukturell dasselbe (Flag-Filter auf `FaHierarchyNode`, Kopf aus
`FaHierarchyOrderInfo` je `HauptFA`, Gruppierung, Druck-ViewModel). Nach den Antworten oben ist die
Mechanik in allen dreien **identisch** — dreimal unabhaengig gebaut hiesse, das 1:n-Kopfproblem
dreimal unterschiedlich zu loesen.
Deshalb: **Teil 3 zuerst umsetzen und den Baustein bewusst wiederverwendbar schneiden** (Filter als
Parameter, gemeinsamer Header-Join, gemeinsame Gruppierung/Paginierung, gemeinsames
Druck-Scaffold). Teil 4 und Teil 5 **erweitern** diesen Baustein, statt ihn zu duplizieren.
`depends_on` von Teil 4 und Teil 5 entsprechend um die Teil-3-Spec ergaenzt.

**Zu S-4 — Leerfall und Testbarkeit.**
- **Leerfall:** Ein `HauptFA`, dessen Positionen nach dem `Beschichtet == true`-Filter alle
  wegfallen, erscheint **gar nicht** — weder als Gruppe noch als leeres Dokument. Ist die gesamte
  gefilterte Menge leer, zeigt der Druck einen Hinweis statt eines leeren Blattes (analog Teil 3).
- **Testbarkeit:** Wie in Teil 3 gilt — **Schranke 2 kann erst gruen werden, wenn produktivnahe
  IDEAL-Daten mit beschichteten Positionen im Testsystem liegen.** Zusaetzlich haengt die Abnahme
  an der noch nicht gelieferten Dienstleister-Vorlage. Beides als Vorbedingung in den Deploy-/
  Test-Abschnitt, damit der qa-agent nicht spaeter unbemerkt daran haengenbleibt.

## Kritische Pruefung (2026-08-07)

Zweiter Anwalt-des-Teufels-Durchgang, **nach** der Erweiterung der Antworten. Gegengelesen: diese
Spec komplett, Teil-1-Spec (Beschichtet-`bit`-Mapping `-1 ⇒ true`), ueberarbeitete Teil-3-Spec
(gemeinsamer Baustein, ILogger-vs-SyncLog-Begruendung, invertierte Toggle-Default-Semantik), die
Backlog-Notizen [[2026-08-06-pdf-erzeugung-fahierarchy-druck]] und
[[2026-08-06-kombinationsgeraete-montageabteilung]], der Anhang [[sage-views-ideal]] (FAInfos 1:n),
ADR 0005/0006/0010 sowie **echter main-Code**: `Services/PrintService.cs`,
`Services/WarehousePickingPrintLayout.cs`, `Controllers/WarehousePickingController.cs` (Print-Action),
`Filters/RequireLagerbestellungAktivAttribute.cs`, `Program.cs` (DI). Die drei alten Blocker (B-1
Filter, B-2 Kombigeraet, B-3 Render-Weg) sind im Rumpf **inhaltlich sauber** eingearbeitet und
widerspruchsfrei — aber die Erweiterung hat drei neue, code-belegte Bloecke hinterlassen, und die
tragende Sequenzierungs-Entscheidung ist vom Menschen gar nicht beantwortet.

### BLOCKER — vor der Freigabe zu klaeren

**B7-1 — Die tragende Freigabe-Frage (PDF-Sequenzierung) ist vom Menschen NICHT beantwortet; damit
ist In-Scope-PDF/AK 7 dieselbe leere Zusage, die die erste Pruefung an AK 3 geruegt hat.**
Die `## Freigabe-Antworten`-Zeilen 4–7 sind alle leer (`→`). Genau dort haengen die vier
Entscheidungen, die die Erweiterung neu geschaffen hat: Layout-Vorlage (4), **PDF-Sequenzierung
(5)**, Headless-Edge-Verfuegbarkeit (6), Testdaten (7). Offene Rueckfrage 5 — „kann Teil 4 mit
Bildschirmdruck allein ausgeliefert werden und der PDF-Knopf folgt als eigener Merge, oder
blockiert das die Freigabe komplett?" — ist die **Weiche fuer den Umfang von Teil 4 selbst** und
unbeantwortet. Solange sie offen ist, ist der PDF-Download In-Scope (Umfang Z. 67–71, AK 7), aber
nicht lieferbar (die Erzeugung ist ausgelagert und existiert nicht). Das ist strukturell exakt der
Fehler, den die 06er-Pruefung an AK 3 „leere Zusage" nannte — nur diesmal fuer AK 7. **Fix:** Der
Mensch muss Rueckfrage 5 verbindlich beantworten (Empfehlung: Teil 4 als reiner Bildschirmdruck
freigeben, PDF vollstaendig herausloesen — siehe B7-2), bevor Schranke 1 genommen werden kann.

**B7-2 — `depends_on` zeigt auf eine Backlog-Notiz, nicht auf eine Spec — der Status-Automat kann
diese Abhaengigkeit nie aufloesen.**
Frontmatter `depends_on` fuehrt `[[2026-08-06-pdf-erzeugung-fahierarchy-druck]]`. Diese Datei ist
`typ: feature` (Backlog-Notiz), **keine Spec** (kein `type: spec`, kein `status`). Eine
Abhaengigkeit, die selbst nie den Pfad NEU → SPEZIFIZIERT → FREIGEGEBEN durchlaeuft, kann nie
„erfuellt" werden; Teil 4 waere damit dauerhaft nicht sauber freigebbar, solange AK 7 im Umfang
haengt. Zusaetzlich haengt an derselben (noch nicht existierenden) PDF-Spec die in AK 7
zugesicherte Erzeugung **und** die ungepruefte Headless-Edge-Voraussetzung (Rueckfrage 6). **Fix:**
Entweder die PDF-Notiz zuerst zu einer eigenen Spec ausbauen, freigeben und *dann* deren Slug in
`depends_on` referenzieren (Sequenz: Teil 3 → PDF-Spec → Teil 4-PDF), **oder** — konsequenter — PDF
komplett aus Teil 4 herausloesen: AK 7 **streichen** (nicht nur „sobald vorliegt"-konditional
lassen), die PDF-Zeilen aus Umfang/Fachl. Anforderung entfernen und den PDF-Notiz-Verweis aus
`depends_on` nehmen. Ein konditionales AK, dessen Vorbedingung ausserhalb jeder freigebbaren Spec
liegt, ist kein Akzeptanzkriterium.

**B7-3 — Der Mehrdeutigkeits-Log laeuft laut Spec ueber SyncLog/Aktivitaets-Protokoll (ADR 0010) —
das widerspricht ADR 0010 selbst UND der Schwester-Spec Teil 3.**
AK 3 (Z. 165–168), Fachl. Anforderung (Z. 100–101), Umfang (Z. 62–63) und „Audit-Feld-Auswirkungen"
(Z. 155–157) schreiben bei mehreren FAInfos-Zeilen einen „Aktivitaets-/SyncLog-Eintrag" vor.
Teil 4 ist aber eine **Web-Lesefunktion** (`deploy.service: false`, kein Hintergrund-Sync). Teil 3
hat genau denselben Anomalie-Fall bewusst ueber `ILogger`/Serilog geloest und das explizit
begruendet: „Dies ist eine Web-seitige Lesefunktion (kein Hintergrund-Sync), daher `ILogger`-Warnung
statt `SyncLog`-Eintrag (ADR 0010 gilt fuer Hintergrund-Services)" (Teil 3, Fachl. Anforderung 3).
`ISyncLogger`/`SyncLogServices.All` ist die Protokoll-Infrastruktur der Windows-Service-Syncs; ein
Web-Request hat keinen Service-Lauf, in den er schreiben koennte. **Fix:** Auf `ILogger`-Warnung
umstellen (analog Teil 3, `HauptFA` + Zahl der Kopfzeilen), „SyncLog"/„Aktivitaets-Protokoll (ADR
0010)" in AK 3, Fachl. Anforderung, Umfang und Audit-Abschnitt durch `ILogger` ersetzen. Sonst baut
der Dev entweder einen nicht existierenden SyncLog-Pfad oder divergiert von Teil 3.

### SOLLTE — macht den Dev-Lauf sicher

**S7-1 — „ausgeliefert ueber den bestehenden `PrintService`" ist technisch falsch und mischt zwei
unvereinbare Mechanismen.**
Die Spec sagt an mehreren Stellen, der Bildschirmdruck werde „analog `Views/WarehousePicking/Print.cshtml`
+ `WarehousePickingPrintLayout.cs`" **und** „ausgeliefert ueber den bestehenden `PrintService`
(`rundll32 mshtml.dll,PrintHTML`)" (affected_code Z. 17, Umfang Z. 64–66, Fachl. Anforderung
Z. 112–115). Am echten Code stimmt das nicht: `WarehousePickingController.Print` liefert per
`return View(vm)` eine Razor-Seite an den **Browser** (dortiger Druck via CSS/Browser-Dialog) — es
ruft `PrintService` **nicht** auf. `IPrintService`/`PrintService` ist im gesamten App-Code
**nirgends konsumiert** (nur `Program.cs:118` registriert), nimmt `printerPath` + `filePath` und
druckt via `rundll32` auf einen **physischen Drucker** — das ist kein „Bildschirmdruck" und nicht
das zitierte Referenzmuster. Die 06er-Pruefung hatte `PrintService` = rundll32-PrintHTML korrekt
benannt; die Nachbesserung hat daraus faelschlich den Auslieferungsweg des Bildschirmdrucks gemacht.
**Fix:** Formulierung korrigieren — Bildschirmdruck ist eine Razor-View (`return View`) analog
`WarehousePicking/Print.cshtml`, **ohne** `PrintService`; den `PrintService`-Verweis in
affected_code/Umfang/Fachl. Anforderung streichen.

**S7-2 — Toggle-Default-Semantik: `RequireLagerbestellungAktivAttribute` defaultet auf AKTIV — eine
1:1-Kopie fuer ein Default-`false`-Feature schaltet Beschichtung faelschlich frei.**
Die Spec fordert `RequireFaHierarchyBeschichtungAktivAttribute` „Muster analog
`RequireLagerbestellungAktivAttribute`" mit „Default `false`" (Fachl. Anforderung Z. 108–111,
S-2-Nachbesserung). Der echte Referenzfilter behandelt aber **fehlend/`"true"` ⇒ aktiv, nur `"false"`
⇒ gesperrt** (Bestandsschutz, Default-EIN). Wird er „analog"/1:1 uebernommen, ist das neue Feature
per Default **aktiv** — das Gegenteil des geforderten Default-`false`. Teil 3 hat exakt diese Falle
erkannt und explizit die **invertierte** Semantik gefordert („fehlend/nicht `'true'` ⇒ inaktiv");
Teil 4 hat diesen Hinweis verloren. **Fix:** Wie Teil 3 die invertierte Default-Semantik explizit
fordern (fehlend/≠`"true"` ⇒ inaktiv), nicht nur „analog … Default false".

### HINWEIS

**H7-1 — `affected_code` fuehrt keinen PDF-Download-Pfad, obwohl der Knopf/Download Teil-4-Verantwortung
ist.** Ausgelagert ist nur die *Erzeugung* (der Baustein); die Download-Action, der Knopf an der
Gruppe und die Dateinamens-Konvention (AK 7) baut Teil 4 selbst. Falls PDF nach B7-1/B7-2 in Teil 4
verbleibt, gehoert die Controller-Download-Action in `affected_code`. (Entfaellt, wenn PDF gemaess
B7-2 herausgeloest wird.)

**H7-2 — Sauber und code-bestaetigt:** Filter `Beschichtet == true` ist im ganzen Rumpf konsistent
(Umfang/Fachl. Anforderung/AK 1); der Anhang fuehrt weiter `= -1`/„nicht `1`" — korrekt, weil das die
Roh-View-Ebene (Teil 1) ist, kein Bruch. Dienstleister-Kopf 1:n ist widerspruchsfrei behandelt
(kein Fan-out-Join, eigene Abfrage je `HauptFA`, alle Varianten + Mehrdeutigkeits-Kennzeichnung),
konsistent mit Anhang und Backlog-Notiz. Die drei Rollen-Pflichtstellen (Attribut, `controller.md`,
`RoleOverview.cshtml`) sind in `affected_code` vorhanden (ADR 0006). ADR 0005 ist fuer die Liste
vollstaendig verankert (AK 4). Der gemeinsame Baustein mit Teil 3 (S-3) ist konsistent referenziert.

**H7-3 — 06er-Restpunkte:** B-1/B-2/B-3 der 06er-Pruefung sind im Rumpf geloest. Offen bleiben aus
06er nur die dort schon als Rueckfrage markierten Punkte (Layout-Vorlage 4, Testdaten 7) — hier als
Teil von B7-1 (unbeantwortete Freigabe-Antworten) mitgefuehrt.

NACHBESSERUNG NOETIG: (1) Freigabe-Antworten 4–7 vom Menschen einholen, insb. die PDF-Sequenzierung
(B7-1); (2) `depends_on`/AK 7 bereinigen — PDF-Notiz zur Spec promoten oder PDF ganz herausloesen
(B7-2); (3) Mehrdeutigkeits-Log von SyncLog auf `ILogger` umstellen (B7-3, ADR 0010 + Teil-3-Konsistenz).
Zusaetzlich `PrintService`-Auslieferungsweg korrigieren (S7-1) und die invertierte Toggle-Default-Semantik
explizit fordern (S7-2).

### Nachbesserung 2 (2026-08-07)

Status je Befund der zweiten Kritischen Pruefung — Details siehe Rumpf oben (Umfang, Fachliche
Anforderungen, Loesungsentwurf, Migrations-/Audit-Abschnitt, Akzeptanzkriterien, Offene
Rueckfragen), hier nur die Kurzfassung mit Verweis:

- **B7-1 (PDF-Sequenzierung unbeantwortet) — behoben, durch Herausloesen statt durch die
  urspruenglich angeforderte Freigabe-Antwort.** Statt AK 7 als konditionale, nicht lieferbare
  Zusage stehen zu lassen, ist der PDF-Download vollstaendig aus dem Umfang von Teil 4 entfernt
  (Umfang, Out-of-Scope-Abschnitt; AK-Liste ohne PDF-Kriterium). Teil 4 liefert ausschliesslich
  Liste + Bildschirmdruck und ist damit eigenstaendig freigebbar. Der PDF-Download folgt als
  eigener Folge-Merge, sobald die separate PDF-Erzeugungs-Spec existiert, freigegeben und
  umgesetzt ist — das ist eine **weiche**, textuelle Sequenzierungs-Abhaengigkeit (Umfang,
  Deploy-Abschnitt), keine harte Frontmatter-Abhaengigkeit. Die konkrete Sequenzierungsfrage
  („Bildschirmdruck zuerst, PDF-Knopf als Folge-Merge") ist damit im Sinne der Empfehlung aus
  B7-1 entschieden; **offen bleibt nur noch**, wann die PDF-Spec selbst entsteht (Offene
  Rueckfrage 6) — das ist kein Blocker fuer die Freigabe von Teil 4.
- **B7-2 (`depends_on` zeigt auf Backlog-Notiz) — behoben.** `depends_on` im Frontmatter verweist
  nur noch auf echte Specs (Teil 1, Teil 3). Der Verweis auf
  [[2026-08-06-pdf-erzeugung-fahierarchy-druck]] bleibt als **Prosa-Referenz** in Umfang/Deploy/
  Offene Rueckfragen erhalten (Sequenzierungshinweis), ist aber keine Frontmatter-Abhaengigkeit
  mehr, die der Status-Automat aufloesen muesste. AK 7 (PDF) ist gestrichen; das ehemalige AK 8
  (AKE-Verhalten unveraendert) ist zu AK 7 nachgerueckt.
- **B7-3 (SyncLog widerspricht ADR 0010/Teil 3) — behoben.** Alle Vorkommen von „Aktivitaets-/
  SyncLog-Eintrag" bei der Mehrdeutigkeits-Kennzeichnung sind durch „`ILogger`-Warnung (`HauptFA` +
  Anzahl Kopfzeilen)" ersetzt — Umfang, Fachliche Anforderungen, Loesungsentwurf,
  Audit-Feld-Auswirkungen, AK 3, Test-Szenarien. Konsistent zu Teil 3 und zur
  Uebersichts-Querschnittsregel „Das Web verschickt keine Mails / kein SyncLog aus dem Web"
  (2026-08-07).
- **S7-1 (`PrintService`-Verweis falsch) — behoben.** Alle Stellen, die den Bildschirmdruck faelschlich
  „ausgeliefert ueber `PrintService`" nannten (affected_code, Umfang, Fachliche Anforderungen,
  Loesungsentwurf), sind korrigiert: Bildschirmdruck ist eine Razor-View (`return View(vm)`) analog
  `Views/WarehousePicking/Print.cshtml`, direkt an den Browser, gedruckt ueber den
  Browser-Druckdialog — **ohne** `PrintService` (der ist ein physischer `rundll32
  mshtml.dll,PrintHTML`-Druck und im App-Code nirgends konsumiert).
- **S7-2 (Toggle-Default-Semantik nicht invertiert) — behoben.** `affected_code`, Fachliche
  Anforderungen und AK 5 fordern jetzt explizit die **invertierte** Default-Semantik
  (fehlend/nicht `"true"` ⇒ inaktiv) fuer `RequireFaHierarchyBeschichtungAktivAttribute` — nicht
  mehr nur „analog `RequireLagerbestellungAktivAttribute`", dessen Default-Ein aus Bestandsschutz
  kommt und bei 1:1-Uebernahme das neue Feature faelschlich freischalten wuerde.
- **H7-1 (PDF-Download-Pfad fehlt in `affected_code`) — gegenstandslos.** Da PDF gemaess B7-2
  vollstaendig aus Teil 4 herausgeloest ist, entfaellt die Notwendigkeit einer
  Controller-Download-Action in dieser Spec; sie gehoert zur kuenftigen PDF-Erzeugungs-Spec bzw.
  zum Folge-Merge.
- **`open_questions` (Frontmatter) getrimmt.** Von 5 auf 2 Eintraege reduziert: Dienstleister-
  Layout/Vorlage (weiterhin offen) und Zeitpunkt/Freigabe der separaten PDF-Erzeugungs-Spec
  (weiterhin offen, kein Blocker). Die vormaligen Eintraege „Headless-Edge-Verfuegbarkeit" und
  „Testbarkeit/leeres IDEAL-Testsystem" sind keine eigenen offenen Fragen mehr: Ersteres gehoert
  zur kuenftigen PDF-Spec (nicht mehr Teil-4-relevant), Letzteres ist als Vorbedingung in den
  Deploy-Abschnitt gewandert (analog Teil 3). Der bewusst aus dem Umfang genommene
  AKE-Toggle-Punkt ist als „GEKLAERT/bewusst nicht Teil dieser Spec" markiert statt als offene
  Rueckfrage gefuehrt.
