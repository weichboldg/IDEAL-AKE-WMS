---
type: backlog
typ: bug
created: 2026-11-20
status: in-spec
quelle: Gerald 2026-11-20 11:59 („die icons am dashboard sind weg, hier möchte ich wieder welche“)
modul: Dashboard / Startseite
---
# Bugfix: Dashboard-Kacheln wieder mit Symbolen

Seit der Umstellung auf Kacheln nach Kategorie und Berechtigung zeigt die Startseite keine Symbole mehr.

Beleg Abweichung von der Spec [[2026-11-19-dashboard-kacheln-nach-kategorie-und-berechtigung-spec]]:
- Rumpf Z. 257: „Symbole bleiben Inline-SVG wie heute (`Index.cshtml:76-79`), mit `aria-hidden="true"`“;
  `NavZiel` hat dafür `SymbolPfad` (Z. 230).
- Umsetzung/Review Z. 444: „Dashboard ohne SVG-Symbole (bewusste Vereinfachung)“, Z. 469: „Symbole entfallen“.

Wunsch: jede Kachel wieder mit Symbol, eine Quelle mit dem Menü ([[0034-navigation-menue-und-kacheln-aus-einer-quelle]]),
weiterhin keine Icon-Bibliothek (Inline-SVG), Symbol rein dekorativ, Kontrastregeln der Spec bleiben.
Prüfen: Symbole aus der Zeit vor v1.99.45 (Index.cshtml:76-79 im alten Stand) wiederverwenden, für neue Kacheln passende ergänzen.
