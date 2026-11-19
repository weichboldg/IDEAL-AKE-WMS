---
typ: backlog
status: in-spec
created: 2026-11-13
herkunft: "QA-Hinweis H-1 zu [[2026-11-13-updater-zweiter-trockenlauf-altbestand-spec]] (v1.99.16); Gerald 2026-11-13 16:27: Folgeaufgabe"
standort: beide
---
# Update-Skript: Warntext „ohne Zeile TREFFER“ bei Exit ≠ 0

Der Warntext nennt „ohne Zeile TREFFER“ auch dann, wenn die Zeile da war und nur der Exit-Code nicht 0 ist (so verlangt von der Spec v1.99.16).
Irreführend, kein Risiko. Text je Fall trennen (Exit ≠ 0 / keine TREFFER-Zeile). Klein, beim nächsten Update-Skript-Lauf mitnehmen.
Entscheidung H-2 (v1.99.16): DEV-Zeilen in `ziele.psd1` bleiben auch auf den Servern (Auswahl DEV → Abbruch ohne Änderung).
