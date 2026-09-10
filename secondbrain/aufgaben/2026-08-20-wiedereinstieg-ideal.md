---
typ: notiz
---
# Wiedereinstieg IDEAL nach dem Urlaub (Stand 20.08.2026)

> [!warning] UEBERHOLT (Stand 2026-09-10) — nicht als Anleitung verwenden
> Die Etappen **C, D und E** sind umgesetzt, der Epic
> [[2026-08-18-fa-liste-hierarchie-anzeige-spec]] ist komplett (v1.35.0). Danach kamen ausserdem
> die BOM-Schnittstellen-Bridge und die Materialisierung der fachlichen Felder (v1.37.0) hinzu —
> beide ebenfalls `Testbereit`.
> **Diese Notiz beschreibt einen Zwischenstand vom 20.08.2026 und bleibt als solcher erhalten.**
> Der aktuelle Stand steht in [[feature-map]] und im jeweils neuesten Eintrag in
> `secondbrain/changelog/`.
>
> Weiterhin gueltig ist nur der Abschnitt *Offene Entscheidungen* — soweit dort Punkte stehen, die
> nicht inzwischen in [[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]]
> entschieden wurden — und die **vier externen Klaerungen**, die nach wie vor bei Menschen liegen.

## Wo wir stehen geblieben sind

**Laufender Epic:** [[2026-08-18-fa-liste-hierarchie-anzeige-spec]] — FA-Liste und verwandte
Ansichten hierarchiefaehig (die „dritte Fehlerklasse": Anzeige kennt die Hierarchie nicht).

| Etappe | Stand |
|---|---|
| A — Fundament + ProductionOrders als Referenz-View | **erledigt** |
| B — fuenf weitere Views | **erledigt** (FaCompletion, PickingLeitstand, Picking, FaWorklist, Tracking/Index) |
| C — Kaskade auf der PickingLeitstand-Gruppenkopfzeile | **offen — hier weitermachen** |
| D — Z4-Zaehl-Sweep + Z3-Sync-Meldung + Z1-Regressionstest | offen |
| E — Testszenarien + Brain + qa-agent → Testbereit | offen |

Web-Suite 1234 gruen. Kein Merge, kein Push.

**Der naechste Aufruf:**
```
/epic-stage secondbrain/specs/freigegeben/2026-08-18-fa-liste-hierarchie-anzeige-spec.md

Etappe C. Bestehender Worktree laut Frontmatter
(.claude/worktrees/2026-08-07-ideal-teile-1-5), kein neuer, kein Merge.
```

## Was NOCH NICHT am Testsystem angesehen wurde

Die fuenf Views aus Etappe B sind gebaut und gruen, aber **nicht** vom Menschen gesichtet. Beim
Wiedereinstieg zuerst mit Master an ansehen — vor Etappe C, denn die Kaskade setzt auf der
Leitstand-Gruppenkopfzeile auf:
- Spaltenausrichtung ueber alle Gruppen hinweg
- Suche nach einer Sub-FA in einer **zugeklappten** Gruppe (klappt sie auf?)
- Sortieren per Spaltenkopf — wird nur die erste Gruppe sortiert? (offener Punkt fuer D)
- **Leitstand:** Bulk-Select ueber mehrere Gruppen; werden Zeilen in zugeklappten Gruppen
  mitausgewaehlt? `BulkRelease` gibt Auftraege frei — eine stille Teilauswahl waere schlimm
- **Tracking/Index:** dreistufiger Baum mit zweistufigem Fortschritt, Ruecckmelden je Arbeitsgang
- **Immer:** Flachmodus (Master aus) unveraendert

## Der grosse Kontext

**Alles liegt ungemergt in EINEM Zweig** `feature/2026-08-07-ideal-teile-1-5`: Teile 1–8
(v1.31–v1.34), BOM-Guard, FA-Struktur-Darstellung, Listen-Spaltenauswahl und dieser Epic.
Bewusste Entscheidung — die UAT deckt spaeter alles in einem Durchgang ab. Der Umfang der Abnahme
waechst allerdings mit jedem Block.

Das IDEAL-Testsystem ist befuellt (533 Struktur-Knoten, 130 materialisierte Auftraege), der
Master-Schalter ist umgelegt.

## Offene Entscheidungen, die auf dich warten

**Aus [[2026-08-20-materialisierung-fachliche-felder]]** — die leeren Spalten in den FA-Listen
(Kunde, Werkbank, Beschichtet, Termine). Ursache: Die Materialisierung schreibt nur sieben Felder.
Die Arbeit gehoert NACH diesem Epic, die Entscheidungen koennen aber jederzeit fallen:
1. **Werkbank-Stammdaten:** unbekannte Arbeitsbereiche automatisch anlegen — oder nur melden?
2. **Werkbank-Datenhoheit:** Sage fuehrend bedeutet stilles Ueberschreiben manueller Zuweisungen.
   Zu bestaetigen, dass fuer IDEAL niemand manuell zuweist. `Workplace` faellt dann aus der
   app-verwalteten Liste — Klassenkommentar im `FaMaterializationSyncService` mitziehen.
3. Welche weiteren `FaHierarchyNode`-Felder uebernommen werden.
4. Kunde und Termine nur in der Gruppen-Kopfzeile — oder doch je Zeile?

**Vier externe Klaerungen** (nicht durch Agenten loesbar, laufen parallel):
- Existieren die beiden IDEAL-Views produktiv? → Sage-Betreuung. **Der wichtigste Punkt** — ohne
  sie ist am Produktivsystem alles blind, egal wie gruen die Abnahme ausfaellt.
- Kommen Barcodes mit Sub-FA-Nummern? → Produktion. Sonst waehlt der Werker dauerhaft aus einer
  Liste (funktioniert, ist aber eine Bedienentscheidung).
- Wer prueft den Beschichtungsauftrag vor dem Versand? → Beschichter-Verantwortlicher.
- Braucht die Vormontage `Neuer_PT_PPS` zusaetzlich als Filter? → Fachbereich.

## Weitere Backlog-Kandidaten aus dieser Runde

- **BDE-Cockpit hierarchiefaehig darstellen** — ausgeklammert; zeigt im hierarchischen Modus ~130
  statt ~30 Karten (bekannte Einschraenkung, dokumentiert)
- **OseonIndex-QR-Scan** — filtert auf HauptFA-Koernung; ein Sub-FA-Scan koennte danebengreifen
- [[2026-08-18-ake-view-abhaengigkeiten-hierarchisch-spec]] — die BOM-Volloesung samt
  `vw_AKE_`-Sweep (Entwurf; der Guard haelt die Stellung)
- [[2026-08-06-pdf-erzeugung-fahierarchy-druck]], [[2026-08-06-kombinationsgeraete-montageabteilung]],
  [[2026-08-06-vormontage-isolierfraesen-export]]

## Merksatz aus dieser Runde

Zweimal hat der erste echte Datenlauf eine Annahme gekippt, die als sicher galt: die
`SubFA = 0`-Regel bei den Kommissionierlisten (das Pflicht-Banner hat es sichtbar gemacht) und der
BOM-Knopf, der im hierarchischen Modus in einen HTTP 500 lief. Beide Male war das Muster
„Annahme + sichtbare Meldung statt stillem Filter" die Rettung.
Daraus: **Ein Dokument, das sagt „X ist offen", altert genauso wie eines, das sagt „X ist
erledigt".** Vor einer Restarbeiten-Liste gehoert der Blick in die Dateien, nicht in den Bericht
ueber die Dateien.
