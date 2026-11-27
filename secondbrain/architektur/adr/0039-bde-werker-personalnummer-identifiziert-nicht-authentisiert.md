---
type: adr
id: 0039
title: BDE-Werker – Personalnummer identifiziert, authentisiert nicht (Schutzgrenze Terminal-Konto)
status: accepted
date: 2026-11-26
supersedes: ""
superseded_by: ""
---

> Entscheid Gerald 2026-11-26, Block 4 Punkt 13 (H-6 Option A) und Block 1 Punkt 2 (2A) in [[2026-11-26-besprechung-0830-kritische-pruefung]].
> Umgesetzt im Fix-Lauf v1.99.81 [[2026-11-26-fix-lauf-v1-99-81]], Bündel `feature/2026-08-07-ideal-teile-1-5`.

## Kontext und Problem

- Am BDE-Terminal wählt der Werker sich per Personalnummer (Scan oder Eingabe). Die `operatorId` reist danach im Query bzw. im POST mit.
- Authentisiert ist nur das **Web-Konto** des Geräts (Windows- oder Formular-Anmeldung, Terminal-Zuordnung `BdeTerminals.UserId`).
- Bis v1.99.80 prüfte `BdeTerminalController` an Pause, Fertig, Teilmenge, Gruppen-Abschluss und „andere beenden“ weder Terminal noch Werker:
  ein manipulierter POST konnte fremde Buchungen pausieren oder beenden.

## Entscheidung

- **Die Personalnummer identifiziert den Werker, sie authentisiert ihn nicht.** Kein PIN, keine Sitzungsbindung.
- **Schutzgrenze ist das Terminal-Konto.** Jede buchende Aktion des `BdeTerminalController` prüft an EINEM Ort:
  1. `terminalId` ist das Terminal des angemeldeten Web-Kontos (`FremdesTerminalAsync`, seit v1.99.79 F-28),
  2. der Werker existiert und ist aktiv (`WaechterAsync`),
  3. bei Aktionen auf einer bestehenden Buchung: die Buchung gehört diesem Werker (`FremdeBuchungAsync`).
- **Das Start-Terminal einer Buchung zählt bewusst nicht.** An Terminal A gestartet, an Terminal B vom selben Werker beendet bleibt erlaubt
  („Meine Buchungen“ ist werkbank- und terminalübergreifend).
- „Andere Buchungen beenden“ (`CloseOthers`) nur, wenn der Werker auf diesem Arbeitsgang selbst eine beendete Buchung hat.
- Abweisungen mit festem deutschem Text, nie `ex.Message` an den Werker (Details nur ins Log).

## Begründung

- Kleinster Schutz, der die Zusage „Buchungen nur am eigenen Terminal und vom eigenen Werker“ wahr macht, ohne erlaubte Abläufe zu sperren.
- Sitzungsbindung (Option B) hätte zwei Tabs mit zwei Werkern am selben Terminal verboten (heute erlaubt) und alle BDE-Wege umgebaut (L).
- PIN (Option C) braucht fachliche Klärung (Handschuhe, Scanner).

## Folgen

- Wer am Terminal-Konto sitzt, kann für jeden **aktiven** Werker buchen, dessen Personalnummer er kennt. Das ist das bewusste Restrisiko.
- Neue BDE-Aktionen müssen denselben Wächter nutzen (Terminal + aktiver Werker + ggf. Besitz).
- Will man mehr: eigene Spec für Sitzungsbindung oder PIN; dann wird dieser ADR superseded.
