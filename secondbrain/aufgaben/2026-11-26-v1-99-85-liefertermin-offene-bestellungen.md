---
type: aufgabe
status: offen
created: 2026-11-26
spec: "[[2026-11-26-liefertermin-offene-bestellungen-artikelinfo-lagerbestellung-spec]]"
---
# v1.99.85 Liefertermin offener Bestellungen – Dev-Lauf

Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`.

## Stand
- [x] Code + Tests (wip feature-complete `1f11fa48`), Build 0 Fehler, Tests Web 3782/1 übersprungen, Service 848 grün
- [x] Version 1.99.85 (Web + Service), Hilfe (Artikelinfo, Lager-Workflow), Anwender-Changelog
- [x] docs/TESTSZENARIEN.md TS-206, Testindex Zeile 206
- [ ] qa-agent (synchron), danach Testbereit + Brain-Changelog + feature-map
- [ ] DoD: Fotos per Playwright gegen DEV, Paket-ZIP auf beide Shares (Größe + SHA-256)

## Geänderte Doku-Kapitel (für spätere Confluence-Übernahme)
- `docs/anwender/03-lagerbestellung-glasbestellung.md` – neuer Abschnitt 9 „Spalte Liefertermin und Artikelinfo Offene Bestellungen“
- `docs/anwender/README.md` – Versionstabelle 1.99.85
- `docs/betrieb/` – keine Änderung (keine Migration, kein neuer Key; Abhängigkeit `Sync:EinkaufBestellungenEnabled` besteht schon)

## Befunde
- BEHOBEN (Gerald 2026-11-26 „gleich implementieren“): Druck der Lager-Details: ein Spaltenfilter auf „Liefertermin“ (wie heute schon „Fräsen-Pfad“) filtert im Druck gegen leeren Text,
  weil der Druck diese Spalten nicht kennt. Gleiche Lage wie seit v1.99.72 beim Fräsen-Pfad.

## Pakete auf den Shares (je Größe + SHA-256 gegen das Original geprüft)
- Paket 24 `paket24-v1.99.85.zip` (Publish `b3a3809f`): 237720179 Bytes, SHA-256 `9D5B48915838D2D2CB4816796443EDAA188C6857F901500A417AD41C31038923`,
  AKENET01 OK, IDEALWEB01 OK. **Veraltet** (ohne Layout-Fix `fa54b418`), nicht verwenden; auf den Shares nicht gelöscht.
- Paket 25 `paket25-v1.99.85.zip` (Publish `fa54b418`): 237718633 Bytes, SHA-256 `0EA555E055B0937797D1A5B710BF06EAE96C86AD4CF61646F6F761EDC4CF7E02`,
  AKENET01 OK, IDEALWEB01 OK. **Veraltet** (ohne B-2/B-4), nicht verwenden.
- **Paket 26 `paket26-v1.99.85.zip` (Publish `f6d4fcb2`, QA 3 bestanden) – GÜLTIG:** 237720488 Bytes, SHA-256
  `08A4F849D3C38BCE671F60174D9E7931CB827E4A090BD64FA76F8686E7E30523`, AKENET01 OK, IDEALWEB01 OK.

## Stand / Haken
- [x] Umsetzung, QA 1 + Nach-QA 1–3 (B-1 Druck-Filter, Layout „überfällig“, B-2 Druck-Sortierung, B-4 Datumsvergleich), Testbereit
- [x] DEV AKE + IDEAL deployt, Fotos Kap. 03 (03-10, 03-11), TS-206.1–206.9 (B-5)
- [x] Paket auf beide Shares mit Größe + SHA-256
- [ ] Manual-UAT (Schranke 2), IDEAL `Sync:EinkaufBestellungenEnabled` einschalten (Gerald: „IDEAL lesen wir dann auch“)

## Für spätere Confluence-Übernahme geänderte Kapitel
- `docs/anwender/03-lagerbestellung-glasbestellung.md` (Stand, Abschnitt 9 mit Fotos)
- `docs/anwender/README.md` (Versionszeile 1.99.85)
- `docs/betrieb/00-index.md` (Stand v1.99.85)

