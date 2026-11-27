---
type: backlog
typ: bug
created: 2026-11-20
status: erledigt
quelle: Gerald 2026-11-20 16:44 („Ja“ auf Koordinations-Befund am Foto 18-02-startseite-ideal.jpg, v1.99.46)
modul: Dashboard / Startseite
bezug: "[[2026-11-20-bugfix-dashboard-kacheln-symbole-fehlen-spec]]"
---
# Dashboard-Symbole nachschärfen (Folge zu v1.99.46)

Beobachtet am Doku-Foto `docs/anwender/bilder/18-02-startseite-ideal.jpg` (Worktree, QA 2026-11-20):

- **Doppelt belegt** (verschiedene Funktionen, gleiches Symbol):
  - Umbuchung ↔ Lagerplatz umbuchen (Doppelpfeil)
  - Mehrfach-Einbuchung ↔ Lager: Eingehende Listen (Ablage-Stapel)
  - Meine Fehlteile ↔ Lager: Fehlteile (Warndreieck)
- **Leeres Blatt** (Quell-SVG unvollständig, QA-Befund Q1): Kommissionierlisten, Vorschlagsliste (Beschichtung), Buchungen (BDE).
- **Missverständlich:** Cockpit (BDE) = Einkaufswagen; Lagerplatz ausbuchen = Papierkorb (wirkt wie Löschen).
- Einstellungen = Stift statt Tabellenvorgabe (QA Q1) – mit prüfen.

Wunsch: jede Kachel eindeutig und sinnfällig; gleiche Regeln wie die Spec (Inline-SVG, eine Quelle mit dem Menü, keine Icon-Bibliothek,
aria-hidden). Selbstgezeichnete einfache Formen sind erlaubt (Antwort 2 der Spec). AKE-Foto 18-01 und Handy 18-05 mit prüfen.
Klein; als Nachzügler zum Teil-6-Lauf.
