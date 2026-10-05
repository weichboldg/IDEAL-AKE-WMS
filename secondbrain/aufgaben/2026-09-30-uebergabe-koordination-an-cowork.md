---
type: aufgabe
title: "Übergabe der Koordination an Cowork — Lagebild 2026-09-30"
created: 2026-09-30
status: offen
---

# Übergabe der Koordination an Cowork — Lagebild 2026-09-30

Diese Notiz übergibt die Koordinationsarbeit aus einem langen claude.ai-Chat (August bis September 2026)
an ein Cowork-Projekt. **Sie ist ein Ausgangspunkt, keine Wahrheit:** Alles, was hier als Stand steht,
ist am Dateisystem zu prüfen, bevor darauf gebaut wird. Wo ich etwas selbst nicht nachprüfen konnte,
steht „ungeprüft".

> **Projekt:** Koordination und Abnahme laufen in **einem** Cowork-Projekt „IDEAL-AKE WMS" mit zwei
> Arbeitsweisen — Anweisungen in [[cowork-projekt-ideal-ake-wms-anweisungen]]. Wo unten vom Projekt
> „IDEAL-WMS Abnahme" die Rede ist, ist dieses gemeint.

## Die Rolle

Die Koordination arbeitet **zwischen** Gerald (Mensch, Schranken 1 und 2) und der Claude-Code-Pipeline
(`/spec`, `/review`, `/dev`, `/epic-stage`, `/merge`; Agenten `spec-agent`, `qa-agent`, `task-scout`).
Sie schreibt **keinen Anwendungscode** — das tut `/dev` in Claude Code. Ihre Aufgaben:
- Backlog-Notizen schreiben, Rückfragen in Specs mit begründeten Empfehlungen beantworten
- fertige Befehle für Claude Code formulieren (`/spec …`, `/review …`, `/dev …`) mit den Schwerpunkten
- Specs nachziehen, wenn die Änderung klein und eindeutig ist; größere Überarbeitungen an den
  `spec-agent` geben
- Prüf- und Testberichte bewerten, Befunde einordnen, Brain pflegen (Notizen, ADRs, Fallstricke)

## Wo was liegt

- Repository: `C:\Git\IDEAL-AKE-WMS` — Regeln in `CLAUDE.md`, Brain in `secondbrain\`
- Specs: `secondbrain\specs\entwurf\` und `secondbrain\specs\freigegeben\`
- Backlog: `secondbrain\backlog\` · Tests und Protokolle: `secondbrain\tests\`
- Architektur: `secondbrain\architektur\adr\` (zuletzt 0015 Einmal-Skripte, 0016 Agenten-Anweisungen)
- Fallstricke: `secondbrain\codebase\fallstricke.md` · Services/Schalter: `secondbrain\codebase\services.md`
- Worktrees: `.claude\worktrees\` — Bündel `2026-08-07-ideal-teile-1-5`, Hotfix
  `2026-09-28-ake-hotfix-stueckliste-druck-lagerbestellung-ist`
- Testsysteme: IDEAL `http://idealweb01.ideal.ideal-ake.at:88/`, AKE `https://akenet01.ake.at:4444/`

## Stand der Arbeit

**IDEAL-Bündel** (Zweig `feature/2026-08-07-ideal-teile-1-5`): v1.31 bis v1.47, alle Specs nach letztem
Wissen **Testbereit**, auf `idealweb01` deployt, **nicht gemergt**. Zuletzt: ADR-0014-Rückbau,
BDE-Arbeitsgänge aus der Struktur (Werkbank-Sync aus Sage), FA-Struktur-Arbeitsgänge-Modal,
Kommissionierung nur am HauptFA, Kommissionierliste Summierung/Druck (v1.47).
*Ungeprüft:* die Status aller einzelnen Specs — die Feature-Map ließ sich zuletzt nicht lesen.

**AKE-Hotfix v1.30.1** (eigener Worktree aus `main`): Stücklisten-Druck per POST statt langer URL;
Lagerbestellung — leeres IST wird nicht mehr still als 0 gespeichert, Bestätigungspflicht beim
Abschließen. Testbereit, nicht gemergt. Danach: Merge in `main`, dann **Vorwärts-Merge `main` → Bündel**
(nicht doppelt implementieren). Einmal-Skript liegt in `SQL\Einmalig\` und wird genau einmal je System
ausgeführt.

**Abnahme TS-80 (Kommissionierliste v1.47):** erster Browser-Lauf am 2026-09-30 über das Cowork-Projekt
„IDEAL-WMS Abnahme", Bericht in `secondbrain\tests\2026-09-30-ts-80-browser-testbericht\`. Kein Fehler
der Anwendung; der gemeldete B1-Fehler war ein Protokollfehler (Menü-Einstiege je Rolle, nie beide).
Offen: Testdaten Fall A–C und eine KW mit Daten (Teil A des Protokolls, SQL durch Gerald), eine
Lagerbestellung des Testbenutzers, PDF-Download mit Freigabe, und **„Drucken reagiert erst beim zweiten
Klick"** von Hand prüfen — das ist die ernsteste Beobachtung des Laufs.

**Agenten-Anweisungen** wurden am 2026-09-29 überarbeitet (ADR 0016: Review synchron im `qa-agent`,
Belegpflicht, Vorrang Rumpf vor Antworten, Überarbeitungsmodus, Modell `inherit`).
*Ungeprüft:* ob diese Änderungen eingecheckt sind. Worktrees nutzen ihre eigene, ältere Fassung, bis sie
mit `main` abgeglichen sind.

## Offene Entscheidungen bei Gerald

- **Dezimalmengen in Lagerbestellungen:** Kommen Kommamengen vor (Meter, kg)? Die Eingabe nimmt nur
  ganze Zahlen; Kommawerte werden seit dem Hotfix abgelehnt statt still auf falsche Zeilen verschoben.
  Falls nötig: Folgepunkt Bindung auf Dezimalzahlen. Und: Könnte die frühere Verschiebung schon falsche
  Buchungen erzeugt haben?
- **Geordneter Abnahmeplan für das ganze Bündel** — mehrfach angeboten, noch nicht erstellt. Reihenfolge
  zählt: ADR-0014-Rückbau vor Werkbank-Sync; Werkbank-Sync erst DryRun, dann scharf; Werkbänke für BDE
  freischalten vor der Arbeitsgang-Erkennung; SQL-Bereinigung alter Sub-FA-Freigaben vor dem
  Warteschlangen-Test; AKE-Regression auf `akenet01` (globale CSS-Regel für `[hidden]`).

## Offene Arbeit ohne Spec (Backlog)

- IDEAL, fachlich: `leitstand-subfa-readonly-stueckliste` (Inhalt unbekannt, zuerst lesen),
  `picking-warteschlange-gruppierung-rueckbau`, `invariante-haupt-fertig-sub-erkennen`,
  `replikation-uebrige-views`, dem Namen nach auch `kombinationsgeraete-montageabteilung`,
  `vormontage-isolierfraesen-export`, `vormontage-druckansicht`
- Vier Nachlesen mit über 40 geparkten Review-Befunden: `bom-bridge`, `ideal-code-review`,
  `materialisierung`, `matchcode` — nach dem Merge sichten
- Übergreifend: `suchsyntax-spaltenfilter-erweitern` (nach dem Merge, Epic),
  `spaltenpraeferenz-speicherfehler-still`, `gruppenkopf-verschwindet-bei-spaltenumsortierung`,
  `js-escaping-locationtransfer`, `druck-briefkopf-logo-adresse` (wartet auf Corporate Design)
- **Playwright** für automatisierte Oberflächentests — Gerald plant das als nächsten Schritt
- Aufräumen: in `specs\entwurf\` sind `ake-view-abhaengigkeiten-hierarchisch` und vermutlich
  `ideal-nachlese-restarbeiten` überholt; drei alte Backlog-Einträge vor dem Abhaken öffnen

## Arbeitsregeln, die sich bewährt haben

- **Belegen statt deuten.** Nie die Bedeutung eines Felds aus seinem Namen ableiten; Negativaussagen über
  alle Wege prüfen, Schreibwege eingeschlossen. Fast jede Korrekturrunde im September ging darauf zurück.
- **Vor einer Statusaussage nachsehen**, nicht aus dem Verlauf rekonstruieren.
- Der **Rumpf** einer Spec ist der Auftrag; ein Freigabe-Nachtrag geht vor; Antworten sind Begründung.
  Entscheidungen, die nur im Antwortblock stehen, erreichen den Dev-Lauf nicht.
- Kein Merge, kein Push durch Agenten; Schranken 1 und 2 gehören Gerald.
- Wiederkehrende Fehlerklassen (siehe Fallstricke): Werte roh in `<script>`, `hidden` gegen `d-*`,
  stille Fallbacks auf 0, Positionsbindung paralleler Arrays, Prüfung nur im Browser, eine Regel an
  mehreren Stellen umgesetzt, EF InMemory beweist keine Unique-Indizes.
- Bei Antworten an Gerald: Deutsch, Empfehlung mit Begründung statt Optionslisten ohne Haltung, eigene
  Fehler offen benennen, Befehle für Claude Code als fertigen Block zum Kopieren.
