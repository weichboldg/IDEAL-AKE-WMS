---
type: bug
title: "Tabellen-Sortierung greift bei gruppierten Listen nur auf die erste Gruppe (table-filter.js liest nur das erste tbody)"
status: behoben
severity: mittel
created: 2026-08-12
fixed: 2026-08-12
fixed_in: "Etappe 6 des IDEAL-Epics, Branch feature/2026-08-07-ideal-teile-1-5, Worktree-Commit 37e8752. Erreicht main erst mit dem Epic-Merge. Fix: je tbody separat sortieren; fuer Ein-tbody-Tabellen verhaltensgleich. Build+Tests gruen: Web 1139 + Service 221."
affected_code:
  - IdealAkeWms/wwwroot/js/table-filter.js (Sortier-Logik, stuetzte sich auf table.querySelector('tbody') statt querySelectorAll)
  - "betroffen (nur im Epic-Zweig, nicht in main): Views/FaHierarchyKommissionierListen/Index.cshtml, Views/FaHierarchyBeschichtung/Index.cshtml, Views/FaHierarchyVormontage/Index.cshtml (je ein tbody pro HauptFA-Gruppe)"
spec: "[[2026-08-12-listen-spaltenauswahl-spec]]"
---

## Symptom

In Listen mit **mehreren tbody-Elementen** (ein Block je Gruppe) sortierte ein Klick auf eine
Spaltenueberschrift **nur die erste Gruppe**. Alle weiteren Gruppen blieben unveraendert stehen.
Fuer den Anwender sah das nach einem kaputten Programm aus: Er klickt auf "Matchcode", der oberste
Block ordnet sich, der Rest der Seite nicht.

## Ursache

`table-filter.js` ermittelte den Sortier-Container mit `table.querySelector('tbody')` - das liefert
**nur das erste** tbody-Element. Bei einer Tabelle mit einem Block je Gruppe blieben alle uebrigen
unberuehrt.

## Einordnung

**Vorbestehender Defekt in gemeinsam genutztem JavaScript**, nicht Folge einer der neuen Specs.
Sichtbar wurde er erst durch die gruppierten IDEAL-Listen - die bestehenden AKE-Listen haben ein
einzelnes tbody und waren nicht betroffen.

## Loesung

Ueber **alle** tbody-Elemente iterieren und **innerhalb** jedes einzelnen sortieren. Damit bleibt
zugleich die gewollte Eigenschaft erhalten, dass Zeilen **nie ueber Gruppengrenzen wandern** - eine
Position gehoert immer zu ihrem HauptFA. Fuer Tabellen mit nur einem tbody ist das Verhalten
unveraendert.

## Umsetzungsweg (weicht bewusst von der urspruenglichen Planung ab)

**Geplant war** ein eigener kleiner Worktree von main, Merge, dann `sync-worktree.ps1` in den
Epic-Zweig - Begruendung: gemeinsam genutztes JS verdient einen eigenen Testumfang.

**Tatsaechlich** wurde der Fix in **Etappe 6 des IDEAL-Epics** mitgenommen. Vertretbar, weil die
Aenderung fuer Ein-tbody-Tabellen nachweislich verhaltensgleich ist und der volle Testlauf gruen war
(Web 1139 + Service 221) - das Regressionsrisiko fuer die bestehenden AKE-Listen ist damit
abgedeckt. Die Trennung waere die vorsichtigere Wahl gewesen; der Testnachweis ersetzt sie hier.

**Folge:** Der Fix erreicht main **nur mit dem Epic-Merge**. Solange der Epic offen ist, besteht der
Defekt in main weiter - dort aber folgenlos, weil es die betroffenen gruppierten Listen ebenfalls
nur im Zweig gibt.

## Merge-Vorbedingung: erledigt

Das urspruengliche Gate ("Epic wird nicht gemergt, solange dieser Bug offen ist") ist **erfuellt** -
der Fix liegt im selben Zweig. Kein offener Merkposten mehr.

**Noch zu entscheiden:** Die drei Listen setzen `supportsSortDefault: false`. Das war die defensive
Massnahme **bis zum Fix**. Da die Sortierung jetzt ueber alle Gruppen korrekt arbeitet, ist zu
klaeren, ob der Schalter auf `true` gehen soll. Nicht stillschweigend belassen - sonst bleibt eine
Einschraenkung stehen, deren Grund weggefallen ist, und in drei Monaten weiss niemand mehr, warum.

## Nachtrag: Laufzeit-Verdacht gegen diesen Fix geprueft und widerlegt (2026-09-10)

Am 20.08. entstand der Verdacht, dieser Fix habe die Sortierung **korrekt, aber langsam** gemacht:
Wer seither alle Gruppen anfasst, koennte je Zeile die ganze Tabelle durchsuchen (das dokumentierte
OSEON-Muster). Beobachtung war ein scheinbar 30 Sekunden blockierter Sortierklick
([[2026-08-20-fehlerprotokoll-anzeige-epic-ab]], B-1).

**Gemessen am Testsystem: 5–7 ms** bei 130 Zeilen in 4 Gruppen, 21 Spalten. Der Verdacht war falsch.

**Warum der Fix nicht teuer ist:** Er ist linear gebaut. Je `tbody` ein `querySelectorAll('tr')`,
der `colspan`-Test ist **zeilen**-skopiert, der Vergleicher liest nur Zellen der beiden zu
vergleichenden Zeilen. Nichts durchsucht je Zeile die gesamte Tabelle. Der OSEON-Selector-Storm
sitzt in einem anderen Codepfad (`OseonIndex.cshtml`, eigene Baum-Sortierung) und ist dort behoben.

Skalierungsmessung (Zeilen clientseitig vervielfacht): 520 → 12,5 ms · 2080 → 86 ms · 4160 → 148 ms.
Wachstum ~linear; der ueberwiegende Teil der Wandzeit ist **Layout** des 21-spaltigen
Tabellenkoerpers, nicht die Sortierung. **Nicht wegoptimieren** — hier ist nichts zu holen.

**Zur offenen Entscheidung oben (`supportsSortDefault`):** Die Sortierung arbeitet jetzt
nachweislich ueber alle Gruppen korrekt **und** schnell. Der Grund fuer die defensive `false`-
Einstellung der drei Listen ist damit messbar weggefallen.

## Test

Liste mit **mindestens zwei** HauptFA-Gruppen oeffnen, auf eine Spaltenueberschrift klicken: **jede**
Gruppe ist in sich sortiert, keine Zeile hat ihre Gruppe verlassen. Zusaetzlich eine bestehende
AKE-Liste (ein tbody) gegenpruefen - Verhalten unveraendert.

**Hinweis zur Erhebung der Regressionsliste:** „ein tbody" ist **je Tabelle** zu pruefen, nicht je
View-Datei. `BdeMasterData/Index.cshtml` enthaelt drei `<tbody>` und ist trotzdem unkritisch — es
sind drei getrennte Tabellen mit je einem.
