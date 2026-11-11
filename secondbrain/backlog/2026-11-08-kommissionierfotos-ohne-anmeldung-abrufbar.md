---
typ: bug
created: 2026-11-08
quelle: "[[2026-11-08-qualitaetsmasken-fragebogen-foto-enaio-spec]]"
---
# Kommissionierfotos ohne Anmeldung abrufbar; Foto-Löschen ohne FA-Prüfung

Zusatzfund beim Spezifizieren des QM-Epics (Codelesung im Bündel-Worktree v1.78.0, **nicht zur Laufzeit geprüft**), aufgenommen in
[[2026-11-08-buendel-uebersicht]]:
- Die Login-Middleware lässt jeden Pfad mit Punkt durch (`IdealAkeWms/Program.cs:429`), danach liefert `UseStaticFiles` (`:447`) auch
  `wwwroot/Fotos` aus → Kommissionierfotos sind ohne Anmeldung abrufbar, wenn man die URL kennt.
- `PhotoController.Delete` prüft nicht, zu welchem FA das Foto gehört.

Zuerst nachstellen (DEV, privates Fenster ohne Anmeldung, Foto-URL aufrufen). Fix-Richtung: Fotos aus `wwwroot` heraus oder über einen
geschützten Controller ausliefern; Ausnahme der Middleware auf echte statische Dateien eingrenzen. Dringlichkeit: vor Lauf B (QM-Fotos).
