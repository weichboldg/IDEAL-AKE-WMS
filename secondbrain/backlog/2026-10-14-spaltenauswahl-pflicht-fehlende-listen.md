---
typ: feature
---
# Spaltenauswahl fehlt auf 18 Listen (ADR 0005 Pflicht)

**Aufgenommen 2026-10-14 (Koordination)** aus dem spec-agent-Lauf zu [[2026-10-14-spaltenauswahl-zahnrad-sammel-spec]]:
18 Listen laden die Spaltenauswahl-Skripte, haben aber **keine** Spaltenauswahl (kein Zahnrad, keine Spaltendefinition).
Laut ADR 0005 ist die Spaltenauswahl für Listen Pflicht. Bekannt u. a.: Roles, Users und vier weitere Listen, auf denen
heute der CSV-Knopf im blauen Hinweis landet (Fund spec-agent; vollständige Liste der 18 im Spec-Lauf ermitteln).

**Nicht** Teil der Sammel-Spec (die bleibt bei Platz, Gruppenkopf, Speicherfehler, Anzeigename). Reihenfolge: nach der
Sammel-Spec, weil deren zentraler Toolbar-Host den Platz für das Zahnrad liefert.

Offen: Liste der 18 mit Datei:Zeile belegen; je Liste prüfen, ob eine Spaltenauswahl fachlich sinnvoll ist oder eine
begründete Ausnahme in ADR 0005 gehört.
