---
typ: notiz
status: aktuell
datum: 2026-09-15
ersetzt: "[[2026-08-20-wiedereinstieg-ideal]]"
---
# Wiedereinstieg IDEAL-Bündel (Stand 2026-09-15)

> [!tip] TL;DR
> Alles ist **committet und Testbereit**. Ein PC-Neustart verliert **nichts** — alle offenen
> Änderungen liegen auf der Platte. Das gesamte IDEAL-Bündel wartet auf **Schranke 2**
> (Mensch: Manual-UAT + EIN Merge). **Kein Merge, kein Push offen von der Maschine.**
> **Nichts `git clean -fdx`** — das löscht die SDD-Ledger (gitignored) im Worktree.

## Wo liegt was

- **Worktree (Zweig-Inhalt, Code):** `.claude/worktrees/2026-08-07-ideal-teile-1-5`,
  Branch `feature/2026-08-07-ideal-teile-1-5`, **HEAD `716a676`**. Sauber bis auf `CLAUDE.md`
  (unstaged — der `ponytail`-Abschnitt, den du eingefügt hast; harmlos, überlebt den Neustart).

> [!note] Nachtrag 2026-09-16 — Hilfeseite gepflegt (Commit `716a676`)
> Doku-Lücke geschlossen: **PDF-Download (v1.39)** und **Matchcode am Artikel (v1.40)** standen nur
> im Changelog, nicht als konkreter Hilfe-Eintrag. Beide jetzt als `dt/dd` in `Views/Help/Index.cshtml`
> (bestehendes Bootstrap-Muster, reine Texte, kein Funktions-/Layout-Eingriff). Build Web grün, 0 Fehler.
> Status-Hinweis: reine Hilfe-Texte, **kein** re-QA-Zwang wie bei Logik-Fixes — aber der Bündel-HEAD ist
> jetzt `716a676` statt `7fa71c1`. Nichts gemerged/gepusht.
- **Hauptcheckout (Brain):** `C:\Git\IDEAL-AKE-WMS`, `main` **HEAD `c7f69d3`**, **26 Commits vor
  `origin/main`** (reine Brain-Historie v1.38 → v1.40). Uncommittet und bewusst so gelassen:
  `.claude/settings.json` + `CLAUDE.md` (Plugin-Install/ponytail), `.obsidian/graph.json` (Zoom-Rauschen).

## Was im Bündel steckt (alles Testbereit, EIN Merge)

Teile 1–8 (v1.31–1.34) · BOM-Guard · FA-Struktur-Darstellung · FA-Liste-Hierarchie (v1.35) ·
BOM-Bridge (v1.36) · Materialisierung fachliche Felder (v1.37) · **FA-Liste-Ausbau/Matchcode (v1.38)** ·
**PDF-Erzeugung (v1.39)** · **Matchcode-Artikelstamm (v1.40)**.

### Diese Session (2026-09-14/15) — zwei abgeschlossene `/dev`-Läufe

1. **v1.39.0 — PDF-Erzeugung für FaHierarchy-Druckdokumente** ([[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]],
   Testbereit). PDF je HauptFA per Headless-Edge; Kommissionierliste + Beschichtungsauftrag.
   Changelog [[2026-09-14-v1-39-0-ideal-pdf-erzeugung-fahierarchy-druck]], Umsetzung
   [[2026-08-06-pdf-erzeugung-fahierarchy-druck-umsetzung]], Test **TS-74**, Fallstricke §11.
   Betriebs-Vorbedingung: **Edge auf dem Web-Server**.
2. **v1.40.0 — Matchcode im Artikelstamm** ([[2026-09-13-matchcode-artikelstamm-spec]], Testbereit).
   `Article.Matchcode` neu; `ProductionOrder.Matchcode` (v1.38) sauber zurückgebaut (Migration
   remove→add); fünf FA-Listen aus Article-Join; hausweit suchbar; Sage-Sync liest `KHKArtikel.Matchcode`.
   Changelog [[2026-09-15-v1-40-0-matchcode-artikelstamm]], Umsetzung
   [[2026-09-13-matchcode-artikelstamm-umsetzung]], Test **TS-75**, Fallstricke §12, Nachlese
   [[2026-09-15-matchcode-nachlese]].

Detail-Ledger (gitignored, im Worktree): `.superpowers/sdd/2026-09-14-pdf-erzeugung-fahierarchy-druck/progress.md`
und `.superpowers/sdd/2026-09-15-matchcode-artikelstamm/progress.md`.

## Offen für den Menschen (Schranke 2)

- **Manual-UAT** vor dem Merge: TS-74.1–74.14 (PDF, v. a. echter Edge-Render, Temp-Aufräumen,
  Timeout, Zugriffsschutz) und TS-75.1–75.9 (Matchcode, v. a. **AK-2 Vorher/Nachher der fünf
  FA-Listen auf echten IDEAL-Daten** — zählen, wie viele Zeilen von gefüllt auf leer kippen,
  Erwartung null; **AK-14 Fehltreffer-Log**; AK-15 Typeahead ab 3 Zeichen).
- **publish.zip-Merge-Blocker** (216 MB in der Zweig-Historie) ist weiterhin vor dem Merge zu
  entscheiden — Optionen in [[2026-09-10-publish-zip-blockiert-push-und-merge]].
- **Deploy des Bündels:** Web + Service + Migration. DB-Backup; Migrationen über SQL-Skript +
  History-Insert, **nicht** per `Migrate()` ([[fallstricke]] §8). Edge auf dem Web-Server.
  Nach dem Deploy Artikel-Sync-Lauf abwarten (Matchcode-Zwei-Lauf-Muster).
- **Lose Enden (nicht von mir committet — deine Entscheidung):**
  - Zwei untracked Backlog-Notizen `secondbrain/backlog/2026-09-13-kommissionierung-nur-hauptfa.md`
    und `…-matchcode-artikelstamm-kommissionierung-hauptfa.md` (deine Quell-Notizen; die Matchcode-Spec
    verweist per `source_backlog` darauf). Einchecken oder so lassen.
  - Aus einem früheren Task: die Löschung des veralteten Entwurfs-Zwillings
    `secondbrain/specs/entwurf/2026-08-06-pdf-erzeugung-fahierarchy-druck-spec.md` (gleicher Name wie
    die freigegebene Fassung → Wikilink-Kollision) steht ggf. noch als unstaged `D`.

## Wiedereinstieg nach dem Neustart

1. Brain lesen: diese Notiz + [[feature-map.md]] (Abschnitte v1.38/1.39/1.40).
2. Nichts ist zu bauen — der Ball liegt bei dir (Schranke 2). Bei neuer Arbeit: `ponytail:ponytail`
   ist Pflicht (CLAUDE.md), im Bündel-Worktree weiterarbeiten, kein neuer Zweig.
