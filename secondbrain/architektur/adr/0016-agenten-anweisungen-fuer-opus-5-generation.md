---
type: adr
status: Akzeptiert
date: 2026-09-29
---
# 0016 - Agenten-Anweisungen fuer die Opus-5-Modellgeneration

## Kontext

Die Anweisungen in `CLAUDE.md`, `.claude/agents/` und `.claude/commands/` stammen aus dem Juli 2026
und waren auf fruehere Modelle abgestimmt. Im September 2026 traten drei wiederkehrende Probleme auf:

1. **Das Code-Review des `qa-agent` ging zweimal ohne Ergebnis verloren.** Der Agent hat das Review
   an einen weiteren Unteragenten bzw. Hintergrundprozess abgegeben. Laut Claude-Code-Dokumentation
   wartet ein Subagent im nicht-interaktiven Modus nicht auf eigene Hintergrund-Unteragenten; deren
   Ergebnis erreicht ihn dann nicht.
2. **Specs waren wiederholt an denselben Stellen falsch:** Bedeutungen aus Namen abgeleitet
   (`USER_OSAbteilung` als OSEON gelesen, `KHKPpsRessourcenPositionen` als IDEAL-Quelle angenommen),
   Negativaussagen nur an Lesewegen geprueft ("ein stilles 0-Buchen gibt es nicht" - es gab es).
3. **Entscheidungen blieben im Antwortblock** und erreichten den Rumpf nicht; `/dev` erklaerte zudem
   die Freigabe-Antworten zum "verbindlichen Auftrag", waehrend in der Praxis der Rumpf galt. Der
   Vorrang musste in jeden Befehl von Hand geschrieben werden.

Anthropics Leitlinie zur Opus-5-Generation (Prompting-Guide und Migrationsleitfaden auf
platform.claude.com) sagt dazu: Das Modell prueft seine Arbeit von sich aus, uebernommene
Verifikations- und Selbstpruef-Anweisungen ("include a final verification step", "use a subagent to
verify") fuehren zu Mehrfachpruefung; es delegiert bereitwilliger an Subagenten, daher sollen
Delegationsfaelle begrenzt werden; bei engen Aufgaben soll der Umfang ausdruecklich begrenzt werden,
weil das Modell Aufgaben sonst eigenmaechtig ausweitet; aggressive Formulierungen ("CRITICAL: You MUST")
fuehren zu Uebertriggern und sollen durch normale Anweisungen ersetzt werden.

## Entscheidung

- **Das unabhaengige Code-Review macht der `qa-agent` selbst, synchron, in seinem Lauf.** Keine Abgabe
  an Unteragenten, Hintergrundaufgaben oder separate `claude`-Prozesse. Ein Review ohne festgehaltene
  Befunde in der Spec gilt als nicht durchgefuehrt. Der `qa-agent` hat den Code nicht geschrieben - er
  ist die unabhaengige Instanz, keine Selbstpruefung.
- **Allgemeine Selbstpruef-Anweisungen entfallen** (`verification-before-completion` als Pflichtschritt,
  "Code-Review" im Entwickler-Lauf). Die **Beweispflicht bleibt**: Build- und Testausgaben, Beweisart je
  Akzeptanzkriterium, manuelle Checkliste - das sind Ergebnisse fuer den Menschen.
- **`qa-agent` prueft zusaetzlich:** Beweisart je Akzeptanzkriterium (EF InMemory beweist keine
  Unique-Indizes, kein raw SQL, keine echte Modellbindung, kein Browser-JS, keine Sage-Reads) und jede
  Aenderung an Bestandstests mit Einordnung.
- **Belegen statt deuten** wird Hausregel (`CLAUDE.md`, `spec-agent`, `/review` Pruefpunkt 2b).
- **Vorrang im Auftrag:** Rumpf der Spec ist der Auftrag, ein FREIGABE-NACHTRAG geht vor, Antwort- und
  Pruefabschnitte sind Begruendung (`/dev`, `/epic-stage`).
- **Ueberarbeitungsmodus** des `spec-agent` und von `/spec` festgeschrieben - vorher sechsmal improvisiert.
- **Delegation begrenzt:** Subagenten nur fuer grosse, unabhaengige Teilaufgaben, nie zum Gegenpruefen
  der eigenen Arbeit.
- **Modellwahl:** `spec-agent` und `qa-agent` von `sonnet` auf `inherit` - sie laufen auf dem Modell der
  Sitzung. Spec und unabhaengiges Review sind die Schritte mit der groessten Hebelwirkung; ihre Fehler
  haben in dieser Serie die meisten Korrekturrunden gekostet. `task-scout` bleibt auf `haiku`.
- **Ton:** in den geaenderten Abschnitten normale statt aggressiver Formulierungen, mit Begruendung.
  Harte Grenzen (kein Merge, kein Push, Schreibziele) bleiben unveraendert bestehen.

## Folgen

- Hoeherer Kontingentverbrauch durch `inherit`, wenn die Sitzung auf Opus laeuft. Zurueck auf `sonnet`,
  falls das spuerbar stoert oder ein Subagent mit einem Kontext- oder Kontingentfehler abbricht.
- **Worktrees tragen eigene Kopien** von `CLAUDE.md` und `.claude/`. Laut Claude-Code-Dokumentation gilt
  bei gleichnamigen Agenten die Definition, die dem Arbeitsverzeichnis am naechsten liegt. Eine Sitzung,
  die in einem Worktree startet, nutzt dessen alte Fassung, bis der Zweig mit `main` abgeglichen ist.
- Die bestehenden Abschnitte mit Grossschreibung ("NIE", "IMMER", "PFLICHT") ausserhalb der geaenderten
  Stellen bleiben vorerst; sie tragen harte Grenzen mit Begruendung. Bei Uebertriggern dort nachziehen.

## Quellen

- Claude Code, Subagenten: https://code.claude.com/docs/en/sub-agents
- Prompting Claude Opus 5 und Migrationsleitfaden Opus 5, platform.claude.com
