---
type: spec
title: "FA-Struktur Arbeitsgänge als Symbole — Teil 3: Vorbereitung BDE-Auftragsanzeige (PLATZHALTER)"
slug: 2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-3-spec
status: Entwurf
created: 2026-10-06
updated: 2026-10-06
source_backlog: "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole]]"
depends_on: "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec]]"
task: ""
worktree: ""
branch: ""
affected_code: []
open_questions:
  - "Was genau fehlt heute im BDE: welche Ansicht soll die Auftraege inkl. Arbeitsgaenge zeigen (BDE-Terminal je Werkbank, BDE-Cockpit, eine neue Auftragsuebersicht)?"
  - "Fuer welche Rolle(n) ist die Anzeige gedacht (bde_user / bde_shiftlead / bde_admin, oder auch Planung/Leitstand)?"
  - "Welcher Auftragsbestand: alle offenen HauptFAs, nur die der gewaehlten Werkbank, ein Zeitfenster (Termin), nur freigegebene?"
  - "Sollen die Arbeitsgaenge dort mit Status-Farben aus Teil 2 erscheinen oder nur als Symbole aus Teil 1? (bestimmt, ob Teil 3 auch von Teil 2 abhaengt)"
  - "Nur Standort IDEAL (Arbeitsgaenge aus der Struktur) oder auch AKE (flache FAs, OSEON)?"
epic: false
etappen: []
deploy:
  web: false
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
# Flache Schluessel mit Absicht: Obsidians Property-Editor kann verschachtelte
# YAML-Objekte NICHT bearbeiten - und genau diesen Block fuellt der Mensch aus.
---

> [!warning] Platzhalter — erst spezifizieren, wenn die Rückfragen beantwortet sind
> Dieser Teil ist bewusst **nicht** ausspezifiziert (Notiz-Punkt 5: „Baustein 3 erst spezifizieren, wenn
> das beantwortet ist“). Kein Lösungsentwurf, keine Code-Festlegung. Nach den Antworten wird diese Spec im
> Rework-Lauf ausgearbeitet.

> **Umsetzungsort (Hinweis, keine Entscheidung):** nach Schranke 2 des IDEAL-Bündels, aus `main`
> (neuer Worktree). TS-Nummer und Version: nächste freie zum Umsetzungszeitpunkt.
>
> **depends_on Teil 1:** Die Anforderung lautet „Aufträge im BDE anzeigen **inklusive der notwendigen
> Arbeitsgänge**“ — die Arbeitsgang-Darstellung als Symbol aus
> [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec]] ist der wiederverwendbare Baustein.
> **Teil 2** ([[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]]) ist **nicht** als Abhängigkeit
> eingetragen: ob die BDE-Anzeige Status-Farben braucht, ist Rückfrage 4. Wird sie bejaht, wird
> `depends_on` auf Teil 2 umgestellt.

## Ziel / Nutzen (das Warum)

Vorbereitung, damit Aufträge im BDE angezeigt werden, inklusive der notwendigen Arbeitsgänge (Anforderung
Gerald, [[2026-10-06-fa-struktur-arbeitsgaenge-symbole]]). Bezug: Die Arbeitsgänge, gegen die das
BDE-Terminal bucht, legt seit v1.44.0 die Struktur-Erkennung an
([[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]]).

Kontext am Code (nur zur Einordnung der Rückfragen, keine Festlegung): Das BDE-Terminal bietet heute je
gewählter Werkbank die **offenen Arbeitsgänge** an (`BdeApiController.GetAvailableOperations`,
`BdeApiController.cs:193-273`; Basis `WorkOperationRepository.GetOpenByWorkplaceIdAsync`,
`WorkOperationRepository.cs:59-82`), im NurFA-Modus stattdessen offene FAs der Werkbank
(`BdeApiController.cs:205-218`). Eine Auftragssicht mit allen Arbeitsgängen eines Auftrags gibt es im BDE
nicht; die FA-Struktur (`/FaHierarchy`) zeigt sie, liegt aber außerhalb des BDE und hinter
`[RequirePickingOrTrackingOrLeitstandAccess]` (`FaHierarchyController.cs:23`).

## Umfang (In-Scope / Out-of-Scope)

Wird nach Beantwortung der Rückfragen festgelegt.

## Fachliche Anforderungen

Wird nach Beantwortung der Rückfragen festgelegt.

## Technischer Loesungsentwurf

Keiner (Platzhalter). Leitplanken, die bei der Ausarbeitung gelten: frontend-design-Skill im Dev-Lauf
Pflicht; Farbe nie alleiniger Bedeutungsträger, WCAG AA (Fertigungsterminals).

## Migrations-/SQL-Auswirkungen

Offen — wird nach Beantwortung der Rückfragen bewertet (OBJECT_ID-Guard, FreshInstall an zwei Stellen,
falls eine Migration entsteht).

## Audit-Feld-Auswirkungen

Offen — wird nach Beantwortung der Rückfragen bewertet.

## Rollen / Zugriffsfilter

Offen — Rückfrage 2.

## Listen-View-Pattern (ADR 0005)

Offen — gilt, falls eine neue Tabellenansicht entsteht.

## Akzeptanzkriterien

1. Wird nach Beantwortung der Rückfragen spezifiziert.

## Test-Szenarien

Wird nach Beantwortung der Rückfragen spezifiziert (Kapitel in `docs/TESTSZENARIEN.md`, nächste freie
Nummer zum Umsetzungszeitpunkt).

## Etappen (nur bei epic: true)

Entfällt (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

**Vorläufig, ohne Grundlage** — wird mit der Ausarbeitung festgelegt.

- **Web-App:** offen
- **Service:** offen
- **Migration:** offen
- **Publish-Befehle:** offen

## Offene Rueckfragen

Aus Notiz-Punkt 5 („Was genau fehlt heute im BDE — welche Ansicht, welche Rolle, welcher
Auftragsbestand?“) und den daraus folgenden Unterfragen. Die Notiz gibt hierzu keine Empfehlung.

1. **Ansicht:** Was genau fehlt heute im BDE — wo sollen die Aufträge inkl. Arbeitsgänge erscheinen
   (BDE-Terminal je Werkbank, BDE-Cockpit, eine neue Auftragsübersicht)?
2. **Rolle:** Für wen ist die Anzeige (`bde_user`, `bde_shiftlead`, `bde_admin`, oder auch
   Planung/Leitstand)?
3. **Auftragsbestand:** Welche Aufträge — alle offenen HauptFAs, nur die der gewählten Werkbank, ein
   Termin-Zeitfenster, nur freigegebene?
4. **Status:** Arbeitsgänge mit Status-Farben aus Teil 2 oder nur als Symbole aus Teil 1? (bestimmt die
   Abhängigkeit von Teil 2)
5. **Standort:** nur IDEAL oder auch AKE?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Ansicht — wo im BDE? →
2. Rolle — für wen? →
3. Auftragsbestand — welche Aufträge? →
4. Status — mit Farben aus Teil 2 oder nur Symbole aus Teil 1? →
5. Standort — nur IDEAL oder auch AKE? →
