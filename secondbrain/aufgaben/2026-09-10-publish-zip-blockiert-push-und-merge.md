---
typ: notiz
---
# publish.zip in der Branch-History blockiert Push UND Schranke-2-Merge

> [!danger] Vor dem Bündel-Merge zu entscheiden — nicht im Merge-Fenster
> Solange der Blob in der History steht, lässt sich der Bündel-Branch nicht veröffentlichen **und**
> ein `--no-ff`-Merge nach `main` macht `main` unpushbar. Das ist kein Push-Problem, sondern ein
> **Merge-Blocker** für [[2026-08-07-ideal-teile-1-5]].

## Befund (2026-09-10)

`publish.zip` (**216,35 MB**) liegt als Blob in der History des Branches
`feature/2026-08-07-ideal-teile-1-5`:

| | |
|---|---|
| Hinzugefügt in | `549c5db` (9. Commit nach `origin/main`) |
| Wieder entfernt in | `91d8d3e` (+ `.gitignore`-Eintrag) |
| Commits nach `549c5db` | **72** |
| Commits im Branch gesamt (vs. `origin/main`) | 80 |

**Das Entfernen im späteren Commit genügt nicht.** Git speichert den Blob in jedem Commit zwischen
`549c5db` und `91d8d3e`; ein Push überträgt ihn. Der `.gitignore`-Eintrag (Zeile 31, `publish.zip`)
verhindert nur **künftiges** Einchecken.

**GitHub lehnt Dateien über 100 MB grundsätzlich ab** — der Push scheitert also, er wird nicht bloß
langsam. Bezahlt wird vorher trotzdem die Übertragung von ~216 MB.

## Warum das auch den Merge trifft

Ein echter `--no-ff`-Merge (die bisherige Praxis, vgl. `3b127f2` bei der Glas-Bestellung) nimmt die
80 Branch-Commits **mitsamt Blob** in die `main`-History auf. Ab dann ist `main` selbst nicht mehr
pushbar — und das fällt genau dann auf, wenn das Bündel landen soll.

`main` ist **heute sauber**: größter Blob in den unveröffentlichten Commits 0,09 MB.

## Optionen und ihre Kosten

1. **History umschreiben** (`git filter-repo`, nicht installiert — per pip nachziehbar, Python 3.14
   vorhanden): Blob aus den 72 Commits entfernen. **Kosten: alle Branch-Hashes ändern sich, und
   79 von 80 Branch-Commits sind im Brain namentlich zitiert** (Changelogs, QA-Nachweise,
   Nachlese-Notizen). *Mitigation:* `filter-repo` schreibt eine commit-map (alt→neu), mit der die
   Brain-Zitate mechanisch nachgezogen werden können. Vorher den Branch als Tag sichern.
2. **Squash-Merge bei Schranke 2:** Der Blob kommt nie in die `main`-History. Kosten: die granulare
   Commit-Historie verschwindet aus git (im Brain bleibt sie beschrieben) und widerspricht der
   bisherigen `--no-ff`-Praxis.
3. **Nichts tun:** Branch bleibt dauerhaft lokal und unveröffentlicht — ein verlorener
   Rechner nimmt 80 Commits Arbeit mit. Als Dauerzustand keine Option.

## Entscheidung 2026-09-10

**Nur `main` gepusht** (85 Commits, reine Brain-Dateien). Der Bündel-Branch bleibt vorerst lokal,
die Blob-Frage wird bewusst **separat und vor** Schranke 2 entschieden — ohne Zeitdruck.

## Bezug

[[2026-09-08-ideal-code-review-nachlese]] (dort als **B1** erstmals gemeldet: „User löscht selbst" —
die Arbeitsverzeichnis-Löschung ist erfolgt, der History-Anteil blieb offen) ·
[[2026-08-18-fa-liste-hierarchie-anzeige-spec]] · [[2026-08-20-materialisierung-fachliche-felder-spec]]
