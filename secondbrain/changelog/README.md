# Changelog / Aenderungshistorie

Je Version ein Eintrag (`YYYY-MM-DD-vX-Y-Z-slug.md`), kompakt: Datum + Kernpunkte + Migrationen +
Deploy-Risiken. Der Merge-Nachlauf (nach Schranke 2) schreibt hier; die anwendersichtbare
Release-Seite wird weiterhin im Code gepflegt.

**Erst-Befuellung 2026-07-27:** 39 Eintraege v1.0.0 – v1.26.0, aufgeloest aus `PROJECT_STATUS.md`
(jetzt Stub; Archiv: `../../docs/PROJECT_STATUS-backup-2026-07.md`).

## Zwei Changelogs — Zustaendigkeit

| Ort | Zielgruppe | Inhalt |
|---|---|---|
| `../../IdealAkeWms/Views/Help/Changelog.cshtml` | **Anwender** (in der App) | Was sich fuer die Benutzung aendert. Bleibt gepflegt — bei jedem Release ergaenzen. |
| dieser Ordner | **Entwicklung / Agenten** | Zusaetzlich: Migrationsnummern, Deploy-Risiken, Fallstrick-Verweise, Merge-Commits |

Die Versionsliste der App-View war bei der Nacherfassung **vollstaendiger** als PROJECT_STATUS
(dort fehlten v1.5.0, v1.6.0, v1.8.5, v1.8.6, v1.11.0, v1.12.0) — sie gilt bei Konflikten als
maßgeblich fuer Nummer und Datum. Eine bekannte Abweichung ist in
[[2026-04-09-v1-6-0-kommissionierer-zuweisung]] vermerkt.

## Verwandt

- [[feature-map]] — was existiert und in welchem Status
- [[2026-07-deploy-v1-25-0]] — offener Deploy
- Release-Pflicht: `AppVersion.cs` (Web **und** Service) + App-Changelog + Eintrag hier.
  Checkliste in `../../CLAUDE.md`.
