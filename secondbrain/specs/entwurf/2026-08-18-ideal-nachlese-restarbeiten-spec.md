---
type: spec
title: "IDEAL-Nachlese: gesammelte Restarbeiten nach dem Buendel-Merge — Tracking-Register + Brain-Nachtraege"
slug: 2026-08-18-ideal-nachlese-restarbeiten-spec
status: Entwurf
created: 2026-08-18
updated: 2026-08-18
source_backlog: "[[2026-08-18-ideal-nachlese-restarbeiten]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - secondbrain/aufgaben/2026-08-07-ideal-teile-1-5.md (zwei offene Checkbox-Punkte auf erledigt setzen — Inhalt existiert bereits, siehe Fachliche Anforderungen Gruppe B)
  - secondbrain/architektur/adr/0005-listen-view-pattern-mit-server-side-spaltenfilter.md (nur Verifikation — additiver Abschnitt "Spaltenpraeferenzen (Ergaenzung 2026-08-12)" ist bereits vorhanden)
  - secondbrain/architektur/fallstricke.md (nur Verifikation — Registrierungspflicht- und Mehr-tbody-Eintraege sind bereits vorhanden)
  - secondbrain/codebase/controller.md (nur Verifikation — Teile-1-5/7-Controller und Zugriffsmatrix sind bereits eingetragen)
  - secondbrain/feature-map.md (Korrektur-Vermerk: Item 7 [Teil 6] und Item 9 [Alt-Spec-Archivierung] sind laut aktuellem Stand bereits erledigt, nicht mehr offen wie im Backlog beschrieben)
  - "bedingt, nur falls Freigabe-Antwort 1 = ja: IdealAkeWms/Views/FaHierarchyKommissionierListen/Index.cshtml, IdealAkeWms/Views/FaHierarchyBeschichtung/Index.cshtml, IdealAkeWms/Views/FaHierarchyVormontage/Index.cshtml (supportsSortDefault true|false im #view-config-Block)"
open_questions:
  - "Item 8: supportsSortDefault fuer die drei gruppierten IDEAL-Listen jetzt auf true, und wenn ja wo umgesetzt (offener Buendel-Worktree vs. nach dem Merge)?"
  - "Item 10: Sage-View-DDL-Platzhalter — wer stimmt die realen Views am IDEAL-Sage-System ab, bis wann?"
  - "Item 11: Dienstleister-Layout (Teil 4) — wer verantwortet die Beschichter-Beziehung/Corporate-Design-Vorlage?"
  - "Item 12: Kombinationsgeraete — wird der fehlende Trennschluessel auf Positionsebene nachgeliefert?"
  - "Item 13: PDF-Erzeugung — Spec jetzt in Auftrag geben (Weg ist bereits entschieden)?"
  - "Item 14: Isolierfraesen-Export — liegt die Format-Spezifikation der Zielsoftware inzwischen vor?"
  - "Item 15: Sub-FA-Barcodes — kommen Barcodes mit Sub-FA-Nummern?"
  - "Item 16: Neuer_PT_PPS — wird der Wochenbezug zusaetzlich zu KO_Termin/FE_Termin gebraucht?"
  - "Item 17 (Korrektur): View-Namen-Schema-Praefix-Hinweis ist durch Commit 28cd3f6 bereits gegenstandslos — aus der Deploy-Checkliste streichen?"
  - "Item 18: RCSI-Pruefung als formaler Punkt der Deploy-Checkliste bestaetigen?"
  - "Item 19: Feature-Toggle-Aktivierung am IDEAL-Zielsystem — wer aktiviert wann, insbesondere den Master-Einwegtuer-Schalter?"
  - "Item 1/6: Entfaellt der eigenstaendige Minimal-Guard (Item 1), da der reale 500er im UAT bereits aufgetreten ist und die Vollloesungs-Spec 'erst mergen, dann bauen' empfiehlt?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
# Flache Schluessel mit Absicht: Obsidians Property-Editor kann verschachtelte
# YAML-Objekte NICHT bearbeiten - und genau diesen Block fuellt der Mensch aus.
---

## Ziel / Nutzen (das Warum)

Die Backlog-Notiz [[2026-08-18-ideal-nachlese-restarbeiten]] ist eine **Nachlese**, keine einzelne
Anforderung: eine am 2026-08-18 aus allen acht Teil-Specs des IDEAL-Standort-Buendels, der
Uebersichts-Spec (drei QA-Runden) und den Epic-Abschluss-Checklisten zusammengetragene Liste von 19
Restarbeiten, gruppiert A–E. Zweck dieser Spec: die 19 Punkte **einmal gesammelt** in ein
nachvollziehbares Tracking-Register ueberfuehren, statt sie verstreut in fuenf verschiedenen
Dateien nachzuziehen — und dabei den **tatsaechlichen** Ist-Stand zu pruefen, nicht den in der
Backlog-Notiz behaupteten.

**Wichtiger Befund aus der Brain-Recherche fuer diese Spec:** Mehrere Punkte, die die Backlog-Notiz
als offen fuehrt, sind beim Schreiben dieser Spec bereits erledigt — vermutlich weil parallel dazu
am selben Tag weitergearbeitet wurde (Teil 6 wurde z. B. noch am 2026-08-18 fertig, nachdem die
Nachlese-Notiz vermutlich schon verfasst war). Diese Spec haelt die Korrekturen fest, damit niemand
bereits erledigte Arbeit ein zweites Mal anfasst:
- **Item 2/3 (ADR-0005-Nachtrag + fallstricke.md-Eintraege):** bereits vorhanden in
  `secondbrain/architektur/adr/0005-listen-view-pattern-mit-server-side-spaltenfilter.md` (Abschnitt
  "Spaltenpraeferenzen (Ergaenzung 2026-08-12)") und `secondbrain/architektur/fallstricke.md`
  (Abschnitte "Neuer viewKey ohne ColumnDefinitions.GetByViewKey-Registrierung" und "table-filter.js
  sortierte bis 2026-08-12 nur das erste tbody"). Die Aufgaben-Checkliste
  `secondbrain/aufgaben/2026-08-07-ideal-teile-1-5.md` fuehrt diese zwei Punkte trotzdem noch als
  offene Checkbox (`[ ]`) — das ist ein reiner Nachzieh-Fehler im Tracking, kein fehlender Inhalt.
- **Item 4 (controller.md):** die vier neuen Controller der Teile 2–5 sowie
  `HierarchieUmstellungController` (Teil 7) stehen bereits in `secondbrain/codebase/controller.md`
  samt Zugriffsmatrix.
- **Item 5 (feature-map.md + changelog):** `secondbrain/feature-map.md` fuehrt bereits Teile 1–5
  (v1.31.0), Teil 7 (v1.32.0) und Teil 8 (v1.33.0); die Changelog-Dateien
  `secondbrain/changelog/2026-08-10-v1-31-0-ideal-teile-1-5.md`,
  `2026-08-17-v1-32-0-ideal-teil-7.md` und `2026-08-18-v1-33-0-ideal-teil-8-bde.md` existieren.
- **Item 7 (Teil 6 — Standorteinstellungen-Maske):** entgegen der Backlog-Aussage "noch nicht
  umgesetzt" ist Teil 6 bereits **umgesetzt und Testbereit** (qa-agent, 2026-08-18,
  `secondbrain/feature-map.md` Abschnitt "Teil 6", Changelog
  `secondbrain/changelog/2026-08-18-v1-34-0-ideal-teil-6-standorteinstellungen.md`, Commit `c8ae47f`
  im selben Buendel-Worktree). Wartet wie der Rest des Buendels auf Schranke 2.
- **Item 9 (Alt-Spec archivieren):** `2026-07-28-ideal-anpassungen-neu-nachbilden-spec.md` liegt
  bereits in `secondbrain/specs/archiv/` mit `status: Ueberholt` — **nicht** mehr in
  `specs/entwurf/`, wie die Backlog-Notiz behauptet.
- **Item 17 (View-Namen ohne Schema-Praefix):** durch den Pre-Merge-Fix
  (`secondbrain/aufgaben/2026-08-07-ideal-teile-1-5.md`, Abschnitt "Pre-Merge-Fixes", Commit
  `28cd3f6`) ist `FaHierarchySql` inzwischen fuer **beide** Schreibweisen (bare Name und
  schema-qualifiziert) korrekt — der urspruengliche Betriebshinweis ist gegenstandslos.

Echte, hier noch zu tuende Arbeit bleibt: (a) die zwei stehen gebliebenen Checkbox-Haken in der
Aufgaben-Notiz nachziehen, (b) die Item-8-Entscheidung (`supportsSortDefault`) herbeifuehren und ggf.
umsetzen, (c) alle fachlichen Klaerungen aus Gruppe D und die Deploy-Hinweise aus Gruppe E als
Schranke-1-Fragen sauber zur Entscheidung vorlegen, statt sie in Prosa verstreut zu lassen.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. Das vollstaendige Tracking-Register aller 19 Backlog-Punkte (Tabelle unten) mit Status,
   Zuordnung zu bestehenden Specs/Bugs, und — wo waehrend dieser Recherche abweichend vom
   Backlog-Text festgestellt — einem Korrektur-Vermerk mit Beleg.
2. Nachziehen der zwei stehen gebliebenen Checkbox-Punkte in
   `secondbrain/aufgaben/2026-08-07-ideal-teile-1-5.md` (rein administrativ — der Inhalt existiert
   bereits, es fehlt nur das Abhaken mit Verweis auf die bestehende Fundstelle statt auf einen neuen
   Commit).
3. Bereitstellung der Schranke-1-Entscheidungsfragen: Item 8 (`supportsSortDefault`), alle
   Fachfragen aus Gruppe D (Items 10–16) und die Betriebs-/Deploy-Hinweise aus Gruppe E (Items
   17–19), als nummerierte offene Rueckfragen mit vorbefuellten Freigabe-Antwort-Zeilen.
4. **Bedingt, nur falls Freigabe-Antwort 1 (Item 8) = "ja":** kleine Config-Aenderung
   `supportsSortDefault: true` im `#view-config`-Block der drei gruppierten IDEAL-Listen
   (`FaHierarchyKommissionierListen`, `FaHierarchyBeschichtung`, `FaHierarchyVormontage/Index`) —
   siehe Freigabe-Antwort 1 fuer den Umsetzungsort (offener Buendel-Worktree vs. main nach dem
   Merge).

**Out-of-Scope**
- Die inhaltliche Umsetzung von Item 1 (BOM-Guard, Minimalfix) und Item 6 (vollstaendige
  hierarchiefaehige AKE-View-Ablosung) — eigene Spec [[2026-08-18-ake-view-abhaengigkeiten-hierarchisch-spec]]
  (status: Entwurf), hier nur Tracking + eine Sequenz-Rueckfrage (siehe Offene Rueckfragen, Punkt
  12).
- Das Spaltenauswahl-Feature selbst (Item 2, funktionaler Teil) — eigene Spec
  [[2026-08-12-listen-spaltenauswahl-spec]] (status: Freigegeben, bereits im Buendel-Worktree
  umgesetzt, Commit `37e8752`), hier nur Tracking.
- Die Standorteinstellungen-Maske selbst (Item 7) — bereits umgesetzt/Testbereit, hier nur Tracking
  + Korrektur des Backlog-Standes.
- Die fachlichen Klaerungen aus Gruppe D (Items 10–16) inhaltlich herbeifuehren — das ist
  Fachbereichs-/Management-Arbeit ausserhalb der Dev-Pipeline. Diese Spec formuliert die Fragen nur
  sauber und verlinkt die bestehenden Backlog-/Spec-Eintraege, sie beantwortet sie nicht.
- Die Betriebs-/Deploy-Punkte aus Gruppe E (Items 17–19) operativ umsetzen — das gehoert in die
  Deploy-Checkliste des Buendel-Merges, nicht in einen Dev-Lauf.
- Der Merge des IDEAL-Buendels selbst (Schranke 2) — bleibt Sache des Menschen, unabhaengig von
  dieser Spec.

## Fachliche Anforderungen

Register aller 19 Punkte aus [[2026-08-18-ideal-nachlese-restarbeiten]], mit Ist-Stand-Korrektur wo
waehrend dieser Recherche abweichend festgestellt:

| # | Gruppe | Kurzbeschreibung | Art | Status / gehoert zu Spec |
|---|---|---|---|---|
| 1 | A | BOM-Knopf faengt im hierarchischen Modus ab statt zu werfen (Minimal-Guard vor dem Merge) | Code-Fix | **Eigene Spec vorhanden, hier nur Tracking:** [[2026-08-18-ake-view-abhaengigkeiten-hierarchisch-spec]] (dort als Minimalfall angelegt, aber der reale 500er ist im UAT bereits aufgetreten, bevor der Guard gebaut wurde — siehe Offene Rueckfrage 12) |
| 2 | B | ADR 0005 additiver Nachtrag "Spaltenpraeferenzen" als vierter Pflichtbestandteil | Brain-Nachtrag | **Bereits erledigt.** Vorhanden in `secondbrain/architektur/adr/0005-*.md`, Abschnitt "Spaltenpraeferenzen (Ergaenzung 2026-08-12)". Fachlicher Inhalt: [[2026-08-12-listen-spaltenauswahl-spec]] |
| 3 | B | `fallstricke.md`: ColumnDefinitions-Registrierungspflicht + Mehr-tbody-Sortier-Fallstrick | Brain-Nachtrag | **Bereits erledigt.** Beide Abschnitte in `secondbrain/architektur/fallstricke.md` vorhanden |
| 4 | B | `codebase/controller.md`: neue Controller/Filter/Toggles der Teile 2–5 + HierarchieUmstellung (Teil 7) | Brain-Nachtrag | **Bereits erledigt** in `secondbrain/codebase/controller.md` |
| 5 | B | `feature-map.md` + `changelog/`: Eintraege v1.31.0/v1.32.0/v1.33.0 | Brain-Nachtrag | **Bereits erledigt** — `secondbrain/feature-map.md` + drei Changelog-Dateien vorhanden |
| 6 | C | AKE-View-Abhaengigkeiten vollstaendig (`FaHierarchyBomRepository` + Sweep) | Code-Fix (eigene Spec) | **Eigene Spec vorhanden, hier nur Tracking:** [[2026-08-18-ake-view-abhaengigkeiten-hierarchisch-spec]] (status: Entwurf) |
| 7 | C | Teil 6 — Standorteinstellungen-Maske | Code-Fix (eigene Spec) | **Korrektur:** entgegen Backlog-Text bereits **umgesetzt, Testbereit** — [[2026-08-18-v1-34-0-ideal-teil-6-standorteinstellungen]], Backlog-Ursprung [[2026-08-03-standorteinstellungen-maske]]. Hier nur Tracking |
| 8 | C | `supportsSortDefault: false` — Grund (Sortier-Defekt) ist behoben, Entscheidung noetig ob auf `true` | Fachentscheidung + kleiner Code-Fix | **Offen, Schranke-1-Frage 1 dieser Spec.** Bug-Hintergrund: [[2026-08-12-tabellen-sortierung-nur-erste-gruppe-bug]] |
| 9 | C | Alt-Spec `2026-07-28-ideal-anpassungen-neu-nachbilden-spec` archivieren | Brain-Nachtrag | **Korrektur:** bereits erledigt — Datei liegt bereits in `secondbrain/specs/archiv/` mit `status: Ueberholt` |
| 10 | D | Sage-View-DDL sind Platzhalter (`SQL/sage-views/*.sql`, nur "Struktur laut Anhang"/TODO) | Fachentscheidung | Offen, Schranke-1-Frage 2 dieser Spec |
| 11 | D | Dienstleister-Layout (Teil 4) vorlaeufig, Corporate-Design-Vorlage steht aus | Fachentscheidung | Offen, Schranke-1-Frage 3 dieser Spec |
| 12 | D | Kombinationsgeraete — fehlender Trennschluessel auf Positionsebene | Fachentscheidung | **Eigener Backlog-Eintrag vorhanden, hier nur Tracking:** [[2026-08-06-kombinationsgeraete-montageabteilung]]. Offen, Schranke-1-Frage 4 |
| 13 | D | PDF-Erzeugung fuer FaHierarchy-Druckdokumente — Weg entschieden, Spec fehlt | Fachentscheidung | **Eigener Backlog-Eintrag vorhanden, hier nur Tracking:** [[2026-08-06-pdf-erzeugung-fahierarchy-druck]]. Offen, Schranke-1-Frage 5 |
| 14 | D | Isolierfraesen-Export — wartet auf Format-Spezifikation der Zielsoftware | Fachentscheidung | **Eigener Backlog-Eintrag vorhanden, hier nur Tracking:** [[2026-08-06-vormontage-isolierfraesen-export]]. Offen, Schranke-1-Frage 6 |
| 15 | D | Sub-FA-Barcodes (Teil 8, TS-66.10) — `SubOrderNumber`-Fallback bleibt unerreichbar ohne Barcode-Aenderung | Fachentscheidung | Offen, Schranke-1-Frage 7 dieser Spec |
| 16 | D | Wochenbezug `Neuer_PT_PPS` (Teil 5) — zusaetzlicher KW-Filter neben `KO_Termin`/`FE_Termin`? | Fachentscheidung | Offen, Schranke-1-Frage 8 dieser Spec |
| 17 | E | View-Namen ohne Schema-Praefix konfigurieren | Deploy-Hinweis | **Korrektur:** durch Pre-Merge-Fix (Commit `28cd3f6`) bereits gegenstandslos — beide Schreibweisen funktionieren. Zur Bestaetigung als Schranke-1-Frage 9 |
| 18 | E | RCSI muss AN sein (TS-57.0) | Deploy-Hinweis | Offen als formaler Deploy-Checklisten-Punkt, Schranke-1-Frage 10 dieser Spec |
| 19 | E | Alle Feature-Toggles stehen default aus, muessen am IDEAL-Zielsystem aktiviert werden | Deploy-Hinweis | Offen, Schranke-1-Frage 11 dieser Spec |

**Reihenfolge laut Backlog-Notiz** (unveraendert gueltig, siehe dort Abschnitt "Reihenfolge"): A vor
dem Merge, B unmittelbar danach (jetzt zu einem grossen Teil bereits erledigt, siehe oben), C als
naechster Entwicklungsblock, D parallel und ausserhalb der Pipeline, E in die Deploy-Checkliste des
Merges.

## Technischer Loesungsentwurf

Diese Spec ist ueberwiegend ein **Brain-/Dokumentations-Auftrag**, kein Code-Pattern im
ueblichen Sinn:

- **Aufgaben-Checkliste nachziehen:** In `secondbrain/aufgaben/2026-08-07-ideal-teile-1-5.md`,
  Abschnitt "Epic-Abschluss-Checkliste", die zwei Punkte "ADR 0005 additiver Nachtrag" und
  "`secondbrain/architektur/fallstricke.md`" von `[ ]` auf `[x]` setzen, mit einem Satz-Verweis auf
  die bereits bestehende Fundstelle (Datum/Abschnitt) statt auf einen neuen Commit — der Inhalt war
  schon da, nur der Haken fehlte.
- **`feature-map.md`:** kein neuer Inhalt noetig fuer Item 5/7/9 (bereits vorhanden), aber ein
  kurzer Vermerk bei Teil 6/9, dass die Nachlese-Notiz zum Zeitpunkt ihrer Erstellung diese als
  offen gefuehrt hatte — Vermeidung von Verwirrung bei kuenftigem Lesen der Backlog-Notiz.
- **Item 8 (bedingt):** Falls Freigabe-Antwort 1 = "ja", ist die Aenderung eine reine
  Daten-Aenderung im bereits bestehenden `#view-config`-JSON-Block der drei Views (Muster siehe
  [[2026-08-12-listen-spaltenauswahl-spec]], Fachliche Anforderung 1/5) — kein neues Pattern, keine
  Migration, kein Repository-Eingriff. Die Frage, ob das noch im offenen Buendel-Worktree
  `feature/2026-08-07-ideal-teile-1-5` (Nachbesserung an Etappe 6) oder erst nach dem Merge auf
  `main` passiert, ist Teil der Freigabe-Antwort (Abwaegung identisch zur bereits getroffenen
  Sequenz-Entscheidung fuer [[2026-08-18-ake-view-abhaengigkeiten-hierarchisch-spec]]: Eingriff in
  einen zur UAT abgegebenen, `Testbereit`-markierten Zweig vs. sauberer Nachtrag auf `main`).
- **Gruppe D/E:** kein Loesungsentwurf in dieser Spec — sie werden als Fragen an Schranke 1
  weitergereicht; die eigentliche Umsetzung folgt (falls noetig) in eigenen, spaeteren Specs, sobald
  die Fachantwort vorliegt.

## Migrations-/SQL-Auswirkungen

Keine. Diese Spec aendert kein Datenmodell. Die bedingte Item-8-Aenderung ist ein reiner
JSON-Konfigurationswert in einer `.cshtml`-View, keine Schema- oder SQL-Aenderung.

## Audit-Feld-Auswirkungen

Keine — es wird keine `AuditableEntity` beruehrt.

## Akzeptanzkriterien

1. Alle 19 Backlog-Punkte sind in der Tabelle unter "Fachliche Anforderungen" mit Nummer, Gruppe,
   Kurzbeschreibung, Art und Status/Spec-Verweis erfasst — kein Punkt bleibt unbewertet.
2. Fuer die Punkte 2, 3, 4, 5 (Gruppe B) ist per Fundstellen-Zitat belegt, dass der Inhalt im
   aktuellen Hauptcheckout bereits vorhanden ist (ADR-0005-Abschnitt, `fallstricke.md`-Eintraege,
   `controller.md`-Zeilen, `feature-map.md`/Changelog-Dateien).
3. Nach Abschluss dieser Spec sind die zwei betroffenen Checkbox-Punkte in
   `secondbrain/aufgaben/2026-08-07-ideal-teile-1-5.md` auf erledigt gesetzt, mit Verweis auf die
   bereits vorhandene Fundstelle statt auf einen neuen Commit.
4. Item 7 (Teil 6) und Item 9 (Alt-Spec-Archivierung) sind im Register klar als "bereits erledigt,
   Korrektur des Backlog-Standes" markiert, jeweils mit Beleg (Datei/Commit/Status).
5. Item 17 ist als "durch Commit `28cd3f6` bereits gegenstandslos" markiert und zur Streichung aus
   der Deploy-Checkliste vorgeschlagen (Schranke-1-Frage 9).
6. Fuer Item 8 liegt nach Schranke 1 eine eindeutige Ja/Nein-Entscheidung samt Umsetzungsort
   (offener Buendel-Worktree vs. main nach dem Merge) vor; wird sie mit "ja" beantwortet, ist die
   Aenderung in den drei genannten Views vor dem naechsten Merge/Deploy des jeweiligen Zweigs
   nachvollziehbar dokumentiert.
7. Alle Punkte aus Gruppe D (10–16) und E (17–19) sind als nummerierte offene Rueckfrage mit
   vorbefuelltem Freigabe-Antworten-Platzhalter erfasst (siehe unten), in derselben Reihenfolge wie
   im Fachliche-Anforderungen-Register referenziert.
8. Item 1/6 traegt eine explizite Rueckfrage zur Sequenzierung (entfaellt der Minimal-Guard, da der
   reale Fehler im UAT bereits aufgetreten ist?), statt stillschweigend beide Varianten offen zu
   lassen.

## Test-Szenarien

Diese Spec selbst ist reine Dokumentations-/Tracking-Arbeit und erzeugt **kein** neues
Testszenarien-Kapitel in `docs/TESTSZENARIEN.md`. Die fachlichen Funktionen, auf die sie verweist,
sind bereits an anderer Stelle abgedeckt (TS-57–TS-67, siehe
`secondbrain/tests/testszenarien-index.md`).

**Bedingt, nur falls Item 8 mit "ja" beantwortet wird:** Der ausfuehrende Dev-Lauf ergaenzt in den
bestehenden Kapiteln TS-59/TS-60/TS-61 (analog zum bereits dort etablierten Muster aus
[[2026-08-12-listen-spaltenauswahl-spec]]) einen kurzen Regressionsschritt: Zahnrad-Dialog der drei
gruppierten Listen zeigt jetzt die Option "Standard-Sortierung speichern"; eine gesetzte
Default-Sortierung wirkt beim Laden auf **jede** HauptFA-Gruppe, keine Zeile wandert ueber
Gruppengrenzen.

## Etappen (nur bei epic: true)

Nicht zutreffend — `epic: false`. Diese Spec ist ein einmaliges, kleines Tracking-/Brain-Update ohne
mehrstufigen Worktree-Ablauf.

## Deploy

- **Web-App:** bedingt ja — nur falls Freigabe-Antwort 1 (Item 8) zur Aktivierung von
  `supportsSortDefault: true` fuehrt (kleine View-Konfigurationsaenderung). Ohne diese Entscheidung
  ist diese Spec reine Brain-Pflege ohne Deploy-Bedarf.
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:** nur relevant im bedingten Fall, dann Standard-Web-Publish (siehe
  Publish-Vorlage im Spec-Template); Ort haengt von der Antwort auf die Sequenzfrage in
  Freigabe-Antwort 1 ab (im offenen Buendel-Worktree vs. nach dem Merge direkt auf `main`).

## Offene Rueckfragen

1. **Item 8 — `supportsSortDefault`:** Der Sortier-Defekt, der die Einschraenkung ausgeloest hat,
   ist behoben (siehe [[2026-08-12-tabellen-sortierung-nur-erste-gruppe-bug]]). Soll
   `supportsSortDefault` fuer `FaHierarchyKommissionierListen`, `FaHierarchyBeschichtung` und
   `FaHierarchyVormontageEinzeln` jetzt auf `true` gesetzt werden? Falls ja: Umsetzung noch im
   offenen Buendel-Worktree `feature/2026-08-07-ideal-teile-1-5` (Nachbesserung Etappe 6, reisst den
   `Testbereit`-Status wieder auf) oder als eigener, kleiner Nachtrag nach dem Merge auf `main`?
2. **Item 10 — Sage-View-DDL:** `SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql` und
   `..._FAInfos.sql` sind Platzhalter ("Struktur laut Anhang"/TODO) und legen am Zielsystem nichts
   an. Wer stimmt die realen View-Definitionen am IDEAL-Sage-System ab, und bis wann muss das vor
   dem ersten Produktivlauf stehen?
3. **Item 11 — Dienstleister-Layout (Teil 4):** Das Druckdokument fuer den Beschichter nutzt ein
   vorlaeufiges Layout; die Corporate-Design-Vorlage steht aus. Wer verantwortet die
   Beschichter-Beziehung und damit die finale Layout-Abstimmung vor Produktivgang?
4. **Item 12 — Kombinationsgeraete** ([[2026-08-06-kombinationsgeraete-montageabteilung]]): Wird der
   fehlende Trennschluessel auf Positionsebene nachgeliefert — und falls ja, von welcher Seite
   (Sage/PPS-Anpassung oder WMS-seitige Zusatzlogik)?
5. **Item 13 — PDF-Erzeugung** ([[2026-08-06-pdf-erzeugung-fahierarchy-druck]]): Der Weg (Headless
   Edge, ein PDF je HauptFA, Download) ist entschieden, die Spec fehlt noch. Soll sie jetzt in
   Auftrag gegeben werden, oder wartet sie auf einen der drei fachlich noch offenen Druck-Vorhaben
   (Item 11/12/14)?
6. **Item 14 — Isolierfraesen-Export**
   ([[2026-08-06-vormontage-isolierfraesen-export]]): Liegt die Format-Spezifikation der
   Zielsoftware inzwischen vor? Ohne sie bleibt der Punkt unspezifizierbar.
7. **Item 15 — Sub-FA-Barcodes:** Der `SubOrderNumber`-Fallback im BDE-Scan (Teil 8, TS-66.10) ist
   als Vorruestung gebaut, aber ohne Sub-FA-tragende Barcodes unerreichbar. Ist geplant, dass
   Barcodes kuenftig Sub-FA-Nummern tragen — und falls ja, in welchem Zeitrahmen?
8. **Item 16 — `Neuer_PT_PPS` (Teil 5):** Die KW-Filter der Vormontage-/Kommissionier-Summiert-Sicht
   liegen auf `KO_Termin`/`FE_Termin`. Wird `Neuer_PT_PPS` zusaetzlich als Filterkriterium benoetigt?
9. **Item 17 (Korrektur, zur Bestaetigung):** Der Betriebshinweis "View-Namen ohne Schema-Praefix
   konfigurieren" ist durch den Pre-Merge-Fix (Commit `28cd3f6`, `FaHierarchySql` mit
   `ValidateAndQuote` statt hartem `dbo.`-Praefix) bereits gegenstandslos — beide Schreibweisen
   funktionieren. Bestehen Einwaende, diesen Punkt aus der Deploy-Checkliste zu streichen?
10. **Item 18 — RCSI-Pruefung (TS-57.0):** Der Full-Refresh-Weg (DELETE + Neubefuellung in einer
    Transaktion) setzt RCSI voraus; ein Fallback wurde bewusst nicht gebaut. Soll dieser Punkt als
    fixer, nicht uebergehbarer Schritt in die formale Deploy-Checkliste vor dem ersten
    IDEAL-Produktivlauf aufgenommen werden?
11. **Item 19 — Feature-Toggle-Aktivierung:** Alle IDEAL-Feature-Toggles stehen default aus,
    inklusive des Masters `ProduktionsauftragHierarchisch` (Einwegtuer). Wer aktiviert sie am
    IDEAL-Zielsystem, in welcher Reihenfolge, und wer bestaetigt insbesondere die
    Master-Aktivierung als bewussten, nicht rueckgaengig machbaren Schritt?
12. **Item 1/6 — Minimal-Guard vs. Sequenzfrage der Vollloesung:** Der reale HTTP-500-Fehler des
    BOM-Knopfs ist am 2026-08-18 bereits im UAT aufgetreten — **bevor** der in Item 1 vorgesehene
    Minimal-Guard gebaut wurde. Die Vollloesungs-Spec
    [[2026-08-18-ake-view-abhaengigkeiten-hierarchisch-spec]] empfiehlt in ihrer eigenen
    Freigabe-Antwort 3 "erst mergen, dann bauen" fuer die **gesamte** Loesung. Entfaellt der
    eigenstaendige Minimal-Guard damit ersatzlos (weil der Fehler schon aufgetreten und dokumentiert
    ist und die Vollloesung ohnehin ansteht), oder soll er trotzdem als schneller Zwischenschritt vor
    dem Merge nachgezogen werden, falls die UAT durch den 500er weiter behindert wird?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. →
2. →
3. →
4. →
5. →
6. →
7. →
8. →
9. →
10. →
11. →
12. →
