---
type: aufgabe
status: erledigt
created: 2026-11-26
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-26: FA-Liste nur HauptFA + FA-Struktur neu (Variante D)

Auftrag Gerald 2026-11-26 (`/goal` + `/goal-run`, zwei Specs, Reihenfolge laut
[[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-uebersicht]]: Teil 1 vor Teil 2).
Start-Stand Worktree: v1.99.81 (cf750e19, Tag `backup/buendel-v1.99.81-vor-fa-liste-hauptfa`), höchstes TS 202,
letztes Paket 21. Main-Tag `backup/main-vor-fa-liste-hauptfa-2026-11-26` (d653b939).

| Spec | Typ | SQL-Block | Version | TS | Status | Commits | Blocker |
|---|---|---|---|---|---|---|---|
| [[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-1-spec]] | Spec | – (keine Migration) | 1.99.82 | 203 | Testbereit | 557a9062 | |
| [[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-2-spec]] | Spec | – (keine Migration) | 1.99.83 + Nachtrag 1.99.84 | 204 | Testbereit | ce04f887, 1ae5f420 (QA-Fix F1–F3), d6e2183f + 2bde92f9 (v1.99.84) | |

## Vorprüfung
- Beide: status Freigegeben (Gerald 2026-11-26, 77b18ac0), worktree/branch leer, Freigabe-Antworten vollständig
  (Teil 1: 1–6, Teil 2: 1–10 + freigabe_entscheidung D), Kritische Prüfung ohne BLOCKER (d29f0af8, 9440ac4b).
- Worktree sauber bei Start.
- Kopplung: `nurHauptFa`/`CountGruppenOhneHauptFaAsync` aus der (unbeantworteten) Leitstand-Spec
  [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-2-spec]] — dieser Lauf führt ihn ein.

## Protokoll
- 2026-11-26 Start, beide Specs auf InUmsetzung.
- 2026-11-26 Teil 1 feature-complete im Worktree (wip 557a9062): Build 0 Fehler, Tests Web 3770 / Service 848 gruen; TS-203 + Index; naechster Schritt qa-agent Teil 1.
- 2026-11-26 qa-agent Teil 1: Build 0 Fehler, Tests 3770/1 skip + 848 gruen, DEV IDEAL + AKE v1.99.82 (Exit 0), TS-203 auf DEV OK bis auf NICHT MOEGLICH (Daten/Rechte), Publish worktree\publish 1.99.82; Befunde Q-1..Q-9 ohne Blocker, offen Q-8 Brain-Nachzug am Laufende. Status Testbereit.
- 2026-11-26 Brain Teil 1 nachgezogen (a2e4a58a: Changelog v1.99.82, feature-map, Fallstrick Matchcode = Q-8 Teil Brain).
- 2026-11-26 Teil 2 feature-complete im Worktree (wip ce04f887): Build 0 Fehler, Tests Web 3774 / Service 848 gruen; TS-204 + Index; naechster Schritt qa-agent Teil 2.
- 2026-11-26 qa-agent Teil 2: NICHT Testbereit (c3d3f432), F1 Kopfdaten-Tabelle Kombigeraet 4963 px breit; F2 toter Stil, F3 Texte.
- 2026-11-26 Reparaturversuch 1 (1ae5f420): `w-auto` + `.fa-head-wrap` sticky, `.fa-structure-header` entfernt, Icon-Titel/Einzahl. Build 0 Fehler, Tests 3774/1 skip + 848.
- 2026-11-26 Nach-QA Teil 2: **Testbereit** (a65d1ea4), F1 nachgemessen 638 px, alle Koepfe ohne Querscrollen; DEV IDEAL + AKE v1.99.83; Publish 1.99.83 (9cde9c6f). Hinweise D1 (A-8.10 Abfragen) + D2 (Fotos) uebernommen (d5cbdca9).
- 2026-11-26 Doku (9cde9c6f, d5cbdca9): neu `docs/anwender/20-fa-liste-und-fa-struktur.md` mit 3 Fotos (Playwright/Edge gegen DEV IDEAL :8082, 1440 px, Faktor 2, nur angesehen, nichts gebucht), README Kapitelzeile/Stand/Versionen 1.99.82+83; `docs/betrieb/00-index.md` Stand + Versionen, `08-diagnose-abfragen.md` A-8.10.
- 2026-11-26 Brain: Changelog [[2026-11-26-v1-99-83-fa-struktur-variante-d]], feature-map v1.99.83.
- 2026-11-26 Gerald nach Vorschau-Screenshots: „HauptFA“-Zeichen überflüssig, graue Linie weg, Bezeichnung ausblenden,
  Arbeitsgänge nach Matchcode — „Passt gut. Bitte umsetzen“. Nachtrag v1.99.84 in Teil-2-Spec (9c7adcde), Umsetzung d6e2183f
  (Build 0 Fehler, Tests 3775/1 skip + 848).
- 2026-11-26 Nach-QA Nachtrag v1.99.84: **Testbereit** (027aae62), B1 Manifest-Text behoben (2bde92f9); DEV IDEAL + AKE v1.99.84;
  TS-204.8 auf DEV OK (inkl. Negativfall alte Präferenz), Sichtprüfungen per Playwright nachgeholt (Sticky deckend, ohne Linie).
- 2026-11-26 Fotos 20-01..20-03 neu (7268dcb6), Kapitel 20/README/00-index/TS auf 1.99.84 (d6e2183f); Brain: Changelog
  [[2026-11-26-v1-99-84-fa-liste-fa-struktur-nachbesserung]], feature-map v1.99.84.

## Fuer spaetere Confluence-Uebernahme (geaenderte Kapitel)

- Anwender: Kapitel 20 „FA-Liste und FA-Struktur (IDEAL)“ (neu), README (Uebersicht, Stand, Versionstabelle).
- Betrieb: 00-index (Stand, Versionstabelle 1.99.82/1.99.83), 08 Diagnose-Abfragen A-8.10 (neu).
- v1.99.84: Anwender Kapitel 20 (Spalten-Absatz, 3 Fotos neu), README (Stand, Versionszeile); Betrieb 00-index (Stand, Versionszeile).

## Paket 22

- `publish\paket22-v1.99.83.zip` (enthaelt Teil 1 + Teil 2): 237699410 Bytes, SHA-256 `05BA04FB1AA63F8899EF01BF09697F33314B3FDBD3B754A5FFD4FE6CDCFB7628`.
- `\\AKENET01.ake.at\Installsourcen\paket22-v1.99.83.zip`: Groesse und SHA-256 identisch – OK.
- `\\IDEALWEB01.ideal.ideal-ake.at\InstallSourcen\paket22-v1.99.83.zip`: Groesse und SHA-256 identisch – OK.
- Fotos 20-02/20-03 auf Wunsch Gerald neu, Struktur aufgeklappt (1824dba0; Doku liegt nicht im Paket).
- Gerald 2026-11-26: FA-Struktur „gefaellt mir immer noch nicht so“ — erledigt durch Nachtrag v1.99.84 (Paket 23).

## Paket 23

- `publish\paket23-v1.99.84.zip` (Publish 2bde92f9): 237704459 Bytes, SHA-256 `5BC1FE062ED0DDB06D59AB00D834160EE5A330590DFB486172F5ABD52EED4707`.
- `\\AKENET01.ake.at\Installsourcen\paket23-v1.99.84.zip`: Groesse und SHA-256 identisch – OK.
- `\\IDEALWEB01.ideal.ideal-ake.at\InstallSourcen\paket23-v1.99.84.zip`: Groesse und SHA-256 identisch – OK.

## Offen fuer Gerald (Schranke 2)

- Manual-UAT TS-203 + TS-204 (Kontrast, echtes Blaettern mit `mitTeilen`, Einzahl „1 Teil“, rekursive Filtersuche), dann EIN Merge.
- Manual-UAT TS-204.8 (v1.99.84). Teil 1: Q-2 erledigt (Zeichen entfernt, v1.99.84); Q-4 Auftraege mit erledigtem HauptFA fallen ohne Banner weg (fachlich bestaetigen); Q-5 „1 Auftraege“ ohne Einzahl; Q-9 Antwortzeit auf dem Testsystem (Ziel < 3 s).
