---
type: aufgabe
status: in-arbeit
created: 2026-11-17
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Goal-Lauf Sammel-Abend 2026-11-17

Auftrag Gerald (/goal, 2026-11-17 abends), Start-Stand `c6c04f9f`. Teile strikt nacheinander. Grenzen: Publish nur unter
`<Worktree>\publish\`, nichts an Sage, keine Einstellungen auf Test/Prod, keine Datei in einen echten WiCAM-Pool, keine Einweg-Schalter,
kein Merge, kein Push.

| Teil | Spec | Version | SQL | Commits | QA | Publish |
|---|---|---|---|---|---|---|
| 1 | [[2026-11-13-blechfertigung-bf-auftraege-wicam-csv-spec]] B-1 (c) + Test-CSV | 1.99.35 | 158 (unverändert) | `0b31a17d` | Testbereit (`f0d949c6`) | Paket 1 |
| 2a | [[2026-11-16-beschichtungslogik-je-standort-spec]] Nachtrag 4 | 1.99.36 | – | `8e0a346e` | Testbereit (`f0d949c6`) | Paket 1 |
| 2b | [[2026-11-13-bde-vereinfacht-teileliste-blechfertigung-spec]] Nachtrag Kontrast | 1.99.36 | – | `e40ecfcb` | Testbereit (`f0d949c6`) | Paket 1 |
| 3 | [[2026-11-17-fa-struktur-abgleich-statt-full-replace-spec]] E1–E8 | nächste | 159 | – | offen | Paket 2 |

## Protokoll

- Teil 1: `BfCsv.Feld` (Excel-Einfassung bei `;`/`"`, vorläufig bis P-1), `;` sperrt die Freigabe nicht mehr, `BfCsv.KopfAsync` gemeinsam
  für Dienst und Web, Aktion `TestCsv` (schachtler, POST, Download, Protokolleintrag `BfAuftragCsv`, kein Statuswechsel). TS-157.21–23.
  Build 0 Fehler, Tests 715 + 3222 grün. Commit `0b31a17d`.
- Teil 2a: Einheit der Sage-Maße vor dem Bau bestätigt (lesend, `ake_TEST2026.KHKArtikel`): 87070769, 00725401, 87010986, 87038500,
  87048047 je `Laenge/Breite/Hoehe × 10 = USER_Tiefemm/Breitemm/Hoehemm`; über alle Artikel mit beiden Werten 113.473 von 113.523 gleich
  (50 Einzelabweichungen = Pflegeunterschiede, keine andere Einheit) → kein Anhalten. Umsetzung: Kopf je WA (`KopfInfosAsync`, Id 0),
  Maße × 10 in `Projektion`, kommissionierte WA ausgeblendet (nur Gesamtliste, Häkchen `kommissionierte=true` wandert über Umschalter und
  Druck mit), Sammelmeldung nur nicht kommissionierte mit Nummer, F1-Sortierung, Modultext, F2-Tests (AK 20/22/25/26 + Maße/Kopf/Häkchen),
  F3 `docs/betrieb/04-konfiguration.md`. TS-145.14–145.19. Commit `8e0a346e`.
- Teil 2b: `bde.css` dunkle Schrift (#212529) auf `.btn-success`/`.btn-secondary` nur unter `.bde-terminal`; die Modals der Teileliste und
  des Terminals liegen außerhalb des Wrappers und bekommen die Klasse mit. TS-152.12. Commit `e40ecfcb`.
- Version 1.99.36 (beide `AppVersion.cs`), Anwender-Changelog, Hilfe. Build 0 Fehler, Tests 715 + 3229 (1 übersprungen) grün.
- QA Teil 1+2 (qa-agent, Brain `f0d949c6`): Build 0 Fehler, Tests 715 + 3229 (1 übersprungen). DEV AKE + IDEAL (Zielordner geleert)
  v1.99.36, HTTP 200. TS-157.21–23, TS-145.14–19 (Regression AKE flach, 4445-relevant), TS-152.12 OK. Befunde nicht blockierend
  (R1 Test-CSV ohne Freigabeprüfung, B1 Maße 0 ohne `ArticleSageDetails`, B2 Altpositionen behalten 0). Offen für Gerald: P-1
  (WiCAM-Import mit eingefasstem Feld), Byte-Vergleich Test-CSV/Pool auf Test, Einkäufer-/Direkt-POST-Prüfung, 87070769 auf AKE-Test,
  Terminal-Modals am echten Terminal, Umgang Altaufträge mit Maß 0.
- **Paket 1** gebaut: `<Worktree>\publish\paket1-v1.99.36\` (Ordner vorher geleert) aus `8e0a346e` – IDEALAKEWMSWeb (65 Dateien),
  IDEALAKEWMSWebService (72), SQL (133 Skripte inkl. 158), deploy, `publish-info.txt` mit Commit und Specs. Bleibt unverändert liegen.
