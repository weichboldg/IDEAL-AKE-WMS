# Backlog

Hier legst DU (oder ein Kollege) formlose Anforderungen als .md ab.
Dateiname: YYYY-MM-DD-kurzer-slug.md. Inhalt: Freitext genuegt -
der Spec-Agent macht daraus eine vollstaendige Spec in specs/entwurf/.

**Ankunft hier = Kette startet.** Der Watcher beobachtet diesen Ordner; sobald
eine Datei hier landet, beginnt der Spec-Lauf. Wenn du eine Anforderung erst
reifen lassen willst, ohne dass etwas losläuft, nutze den Denkraum davor:
`../ideen/` (siehe ideen/README.md). Von dort verschiebst du sie hierher, wenn
sie reif ist - das ist die bewusste Geste, die die Pipeline anstoesst.

Eine Backlog-Datei gilt als "verarbeitet", sobald eine Spec mit
source_backlog-Verweis auf sie existiert.

## Bugs melden

Einen Bug meldest du hier genauso - als formlose Notiz. Setze im Frontmatter
`typ: bug`, dann legt der Spec-Agent zusaetzlich einen Eintrag im Register
`bugs/` an (Symptom/Reproduktion/Severity) und verlinkt beides. Beispiel:

```
---
typ: bug
---
Beim Speichern eines FA ohne Werkbank kommt eine 500er-Fehlerseite
statt einer Validierungsmeldung. Erwartet: freundlicher Hinweis.
```

So bleibt `bugs/` das Register aller bekannten Probleme, waehrend die Backlog-
Notiz die Pipeline antreibt. Ein Bug, den du nur dokumentieren (aber nicht
sofort fixen) willst, legst du direkt in `bugs/` ab - der wird dann nicht
automatisch bearbeitet.

## Grosse Anforderungen zerlegen lassen

Eine grosse Anforderung, die mehrere unabhaengige Bausteine umfasst, soll NICHT
als ein einziger, lange offener Feature-Branch umgesetzt werden (grosser Merge
= grosses Risiko). Stattdessen: In der Backlog-Notiz explizit den Split
anfordern - der Spec-Agent zerlegt dann in mehrere Teil-Specs mit Reihenfolge
und Abhaengigkeiten, jede laeuft einzeln durch die Pipeline und wird einzeln
gemergt.

Setze dafuer im Frontmatter `split: true` und beschreibe, WIE zerlegt werden
soll (fachliche Bausteine, gewuenschte Reihenfolge). Beispiel:

```
---
typ: feature
split: true
---
Grosse IDEAL-Anpassung. Bitte in sinnvolle, einzeln mergbare Teilbloecke
zerlegen, Reihenfolge beachten (Fundament zuerst):
1. konfigurierbare Sage-Views
2. Belegnummer
3. FA-Hierarchie-Inversion (Sub-FA)
4. BOM-Artikelmatchcode
```

Der Spec-Agent erzeugt dann pro Baustein eine eigene Spec (Slug
`...-teil-1-spec`, `-teil-2-spec` ...), jede mit `depends_on`-Verweis auf die
vorige, und eine kurze Uebersichts-Notiz, die alle verlinkt. Du gibst jede
Teil-Spec einzeln frei - so bleibt der Abstand zu main immer klein.

## Grosses Paket am Stueck (Epic in EINEM Worktree)

Manchmal laesst sich ein Paket NICHT sinnvoll teilen (z. B. eine
Datenmodell-Umstellung, die alles gleichzeitig beruehren muss) oder du willst
es bewusst am Stueck finalisieren. Dann `epic: true` statt `split: true`.

Unterschied zu split:
- **split** = mehrere kleine Specs, mehrere Branches, mehrere Merges. Bevorzugt,
  weil der Abstand zu main klein bleibt.
- **epic** = EINE Spec, EIN langlebiger Worktree, mehrere Etappen (je ein
  Commit), am Ende EIN Merge. Fuer unteilbare oder bewusst am Stueck gebaute
  Pakete.

```
---
typ: feature
epic: true
---
Grosse Datenmodell-Umstellung, unteilbar. Etappen:
1. Entities + Migration
2. Repositories/Services anpassen
3. Controller/Views
4. Tests + Testszenarien
```

Betrieb eines Epic (bewusst mit etwas mehr Handarbeit, weil der lange Branch
das Risiko traegt):
- Der Worktree wird EINMAL angelegt und bleibt offen bis zum Schluss.
- Umsetzung in Etappen - je Etappe ein Dev-Lauf und ein Commit. So bleibt jeder
  Lauf im Turn-Limit und der naechste dockt am letzten Commit an.
- WICHTIG: den Branch regelmaessig (taeglich / nach jedem groesseren main-Merge)
  auf Stand halten: `pwsh -File scripts\sync-worktree.ps1 -Slug <slug>`. Das
  holt main in den Epic-Branch und haelt den Merge am Ende klein und sicher.
- QA + Schranke 2 (Merge) erst, wenn ALLE Etappen fertig und gruen sind.

Faustregel: Im Zweifel **split**. **epic** nur, wenn wirklich unteilbar oder
du es explizit willst.

## Anhaenge (Schnittstellendoku, Screenshots, PDFs)

Braucht eine Anforderung zusaetzliche Dateien als Input, lege sie in einen
Unterordner PRO Backlog unter `anhaenge/<slug>/` und verweise im Frontmatter:

```
---
typ: feature
anhaenge:
  - anhaenge/2026-07-oseon-import/oseon-schnittstelle-v2.pdf
  - anhaenge/2026-07-oseon-import/zielmaske.png
---
Die Sub-FA-Nummer soll aus OSEON kommen. Schnittstelle siehe
[[anhaenge/2026-07-oseon-import/oseon-schnittstelle-v2.pdf]],
Zielmaske ![[anhaenge/2026-07-oseon-import/zielmaske.png]]
```

WICHTIG: Anforderungen MIT Anhaengen (besonders PDFs/Bilder) spezifizierst du
am besten INTERAKTIV (`claude` starten, Anhaenge mitgeben), nicht ueber den
Hintergrund-Watcher - der headless-Lauf liest Bilder/PDFs unzuverlaessig. Der
Watcher erkennt `anhaenge:` und meldet solche Notizen als "interaktiv noetig",
statt blind eine lueckenhafte Spec zu bauen.
