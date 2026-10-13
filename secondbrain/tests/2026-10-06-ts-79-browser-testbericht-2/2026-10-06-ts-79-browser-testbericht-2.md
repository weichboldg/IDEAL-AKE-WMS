---
type: testbericht
title: "Browser-Testbericht TS-79, Wiederholung B12 — Komm.-Ziel-Auswahl übersteht F5 (v1.47.2)"
protokoll: "[[2026-10-06-ts-79-browser-testprotokoll]]"
spec: "[[2026-09-25-kommissionierung-nur-hauptfa-spec]]"
vorlauf: "[[2026-10-06-ts-79-browser-testbericht]]"
system: "http://idealweb01.ideal.ideal-ake.at:88/ (Fußzeile: v1.47.2)"
ausfuehrung: Claude Cowork (Abnahme)
datum: 2026-10-06
---

# Browser-Testbericht TS-79, Wiederholung B12 — 2026-10-06

Wiederholung nur von Schritt B12 nach dem Fix für B-ABN-1 (Worktree-Commit `1df7665c`). Die Testfälle
stammen aus TS-79.6 a–c (`docs/TESTSZENARIEN.md`).

**Ergebnis: 1 OK, 0 FEHLER, 0 nicht prüfbar, 0 Protokoll passt nicht.** Zusätzlich: Fall c ist
nicht prüfbar (siehe unten), weil er einen Profil-Standardfilter braucht.

## Rahmen
- **Benutzer:** `admin`, von Gerald angemeldet.
- **Stückliste:** `/Picking/Bom/24`, HauptFA 1035235, 166 Positionen, davon 73 mit Komm.-Ziel.
- **Startzustand:** Komm.-Ziel-Auswahl leer, `sessionStorage` `tableFilters:Bom` leer, 12 Zeilen
  sichtbar (Baum zugeklappt).
- **Ausführung:** Die Auswahl erfolgte per Maus im Select2-Dropdown. Neu geladen wurde mit der Taste
  F5; die Ladeart war jeweils `reload`.

## Ergebnisse

| Schritt | Ergebnis | Beobachtung | Screenshot |
|---|---|---|---|
| B12 Neu laden behält die Auswahl (TS-79.6) | OK | **a)** „Alle Ziele“ gewählt: 73 Zeilen, Badge „Gefiltert: Alle Ziele (nur kommissionier-relevant)“, `sessionStorage` = `{"kommissionieren":"!(leer)"}`. Nach F5: Dropdown zeigt „Alle Ziele“, Badge da, 73 Zeilen, alle mit Ziel, Äste aufgeklappt, keine Meldung. **b)** KOM gewählt (ersetzt „Alle Ziele“): 54 Zeilen, nur KOM, Badge „Gefiltert: Komm.-Ziel = KOM“. Nach F5: gleich (KOM, 54 Zeilen, Badge). **Leeren:** Kreuz im Badge → Auswahl leer, Eintrag im `sessionStorage` entfernt. Nach F5: kein Filter, 12 Zeilen, kein Badge. | ja, [[B12a-alle-ziele-nach-f5.jpg]], [[B12b-kom-nach-f5.jpg]] |

## Nicht geprüft
- **TS-79.6 c (Profil-Standard gegen Sitzung):** Der Fall braucht einen im Profil gespeicherten
  Standardfilter für das Komm.-Ziel. Das Profil darf Cowork nicht ändern; der Fall bleibt für Teil C.

## Hinweis
Der Chrome-Tab lief wieder im Hintergrund (`visibilityState = hidden`). Den ersten Klick auf das
Dropdown musste ich wiederholen, alle weiteren Klicks kamen an.
