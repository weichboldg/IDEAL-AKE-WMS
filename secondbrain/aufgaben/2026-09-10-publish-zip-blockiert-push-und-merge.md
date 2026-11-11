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
| Hinzugefügt in | `950d872` (9. Commit nach `origin/main`) |
| Wieder entfernt in | `fec39be` (+ `.gitignore`-Eintrag) |
| Commits nach `950d872` | **72** |
| Commits im Branch gesamt (vs. `origin/main`) | 80 |

**Das Entfernen im späteren Commit genügt nicht.** Git speichert den Blob in jedem Commit zwischen
`950d872` und `fec39be`; ein Push überträgt ihn. Der `.gitignore`-Eintrag (Zeile 31, `publish.zip`)
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

## Erledigt 2026-10-05

**History umgeschrieben (Option 1, Entscheidung Gerald).** `publish.zip` (Blob `2207d6d5`, 226 856 247 Bytes
≈ 216 MB, eingeführt mit `549c5db9`, entfernt mit `91d8d3ef`) ist aus allen Commits des Zweigs
`feature/2026-08-07-ideal-teile-1-5` entfernt. Nur dieser Zweig wurde umgeschrieben; `main` und alle anderen
Zweige sind unverändert.

- **Befehl** (Hauptcheckout, `git-filter-repo` a40bce548d2c per `pip`):
  `python -m git_filter_repo --refs feature/2026-08-07-ideal-teile-1-5 --path publish.zip --invert-paths --preserve-commit-hashes --force`
- **Warum `--preserve-commit-hashes`:** Ein erster Lauf ohne diese Option schrieb auch Hash-Zitate in
  Commit-*Nachrichten* um — darunter in `f3e9465c` (ein Commit, der bereits in `main` liegt). Dadurch bekamen
  168 mit `main` geteilte Commits auf dem Zweig neue IDs, `f89c0124`/`f9e2a7a6` waren keine Vorfahren mehr.
  Verworfen: Zweig per `update-ref` auf den Backup-Tag zurückgesetzt, Lauf-1-Daten liegen unter
  `.git/filter-repo-lauf1/`, dann Lauf 2 mit der Option.
- **Backup-Tag (lokal, NIE pushen — enthält den Blob):** `backup/ideal-teile-1-5-vor-filter-repo` → `6c253c60`
- **Alte Zweigspitze:** `6c253c60e82fdebc0b202c47aca4f5cb0a3c35a3`
- **Neue Zweigspitze:** `b877e05ae3036528d144cfd0c2a25bf116b462c6`
- **Nachweise:**
    - `git log --full-history -- publish.zip` auf dem Zweig: 0 Commits; Blob `2207d6d5` vom Zweig nicht mehr
      erreichbar.
    - Baum der neuen Spitze == Baum von `6c253c60` (`4436054a…`).
    - `main` unverändert `76c743a2`; `merge-base(neu, main) == f9e2a7a6`; `f89c0124` Vorfahr der neuen Spitze.
    - Geänderte Commit-Hashes: **163** = genau die Nachfahren von `549c5db9` auf dem Zweig;
      `main..Zweig` weiterhin 170 Commits.
    - Größter Blob nur im Zweig: 487 679 Bytes (`docs/TESTSZENARIEN.md`) — weit unter 100 MB.
    - Worktree sauber; `dotnet build` 0 Fehler; Tests Web 1449/1450 (1 übersprungen), Service 268/268.
- **Brain-Hash-Zitate:** **652 Zitate** in **62 Dateien** auf die neuen Hashes umgestellt (144 verschiedene
  Kurzhashes, gleiche Länge, nur ganze Wörter; 0 mehrdeutig). Zuordnung alt → neu:
  [[2026-10-05-filter-repo-commit-map]].
- **Offen — Push mit Force (Gerald):** Auf `origin` liegt der Zweig bereits, aber auf `6d81a4a3`, der Spitze des
  **verworfenen Lauf 1** (während der Rückfrage gepusht; Reflog der Tracking-Ref: „update by push“). Dieser Stand
  ist frei von `publish.zip` (gleicher Baum `4436054a…`), trägt aber die 168 duplizierten `main`-Commits. Die
  richtige Spitze `b877e05a` ist kein Nachfahre davon, der Push braucht deshalb:
  `git push --force-with-lease=feature/2026-08-07-ideal-teile-1-5:6d81a4a3b0127c5ce8c56f6b910c51a0a189e21b origin feature/2026-08-07-ideal-teile-1-5`
  Hat jemand den Lauf-1-Stand bereits geholt, muss er seinen lokalen Zweig danach auf `origin` zurücksetzen.
