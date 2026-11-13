---
type: backlog
created: 2026-11-14
status: neu
quelle: "[[2026-11-14-wochenend-goal-lauf]]"
---
# Hilfeseite verlinkt die Anwender-Anleitungen (`docs/anwender/`)

Gerald 2026-11-13 22:47 (Antwort 1 im [[2026-11-14-wochenend-goal-lauf]]): „wäre nice“ – **ja, aber nicht im Wochenend-Lauf** (Code-Änderung +
Auslieferung der Markdown-Dateien).

## Ausgangslage

- Der Wochenend-Lauf hat Anleitungen `docs/anwender/01-…` bis `12-…` mit Bildern unter `docs/anwender/bilder/` geschrieben (Bündel-Worktree,
  Commits `494747d3`, `15833616` und der Commit für 09–12). Relative Bildpfade, keine Obsidian-Syntax → 1:1 einbindbar.
- `IdealAkeWms/Views/Help/Index.cshtml` ist Code; die Markdown-Dateien werden heute **nicht** ausgeliefert (kein Teil des Publish).

## Zu spezifizieren

1. Auslieferung: Markdown beim Build nach `wwwroot/anleitungen/` kopieren und serverseitig rendern (Markdig o. ä. – neue Abhängigkeit braucht
   ADR) **oder** beim Build in statisches HTML/PDF umwandeln **oder** nur Bilder + eine Razor-Seite je Anleitung.
2. Verweise: je Hilfeseiten-Karte „Ausführliche Anleitung mit Bildern“ (Vorschläge der Läufe C1/C2/C3: Karte „Kommissionieraufträge und
   Kommissionierbereiche“ ca. Z. 1427, „Kommissionierung“ ca. Z. 1750, je Modul analog).
3. Zugriffsschutz: Anleitungen nur nach Anmeldung (wie Hilfe).
4. Textvorschläge der Läufe für die Hilfeseite selbst (Bedarfsmeldung erfüllen nur über Einbuchung; Teilgeliefert bei Fehlteil, Abschließen bucht
   keinen Bestand; Glasbestellungen zählen in den Bestellvorschlägen) prüfen und übernehmen.
