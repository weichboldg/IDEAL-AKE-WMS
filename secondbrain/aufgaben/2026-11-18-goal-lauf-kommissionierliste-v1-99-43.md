---
type: aufgabe
status: erledigt
created: 2026-11-18
---
# /goal-Lauf 2026-11-18: IDEAL-Kommissionierliste – Vorfilter Ziele, Abteilung, Lagerplätze, Leitstand-Gate (v1.99.43)

Spec [[2026-11-18-ideal-kommissionierliste-filter-spalten-leitstand-spec]] (Epic E1–E4, freigegeben 17:07; liegt noch in `specs/entwurf/`,
Gerald verschiebt). Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Start `b5463336` (v1.99.42). Version 1.99.43, TS-164,
keine Migration (Reserve SQL/164). Grenzen: nichts an Sage, keine Einstellungen auf Test/Prod (:88, 4445, Prod),
`SageFaArbeitsgaengeAnlegenAktiv` aus, kein Merge, kein Push. Paket 8 nach `publish\paket8-v1.99.43\` + ZIP.

| Spec | Typ | SQL | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-18-ideal-kommissionierliste-filter-spalten-leitstand-spec]] | Epic | – | 1.99.43 | Testbereit | `77c7c57e`, `cbd32566`, `7d544402`, `6c9d4a5e`, QA `3a6372a5` | – |

## Plan (kompakt, Rumpf + Freigabe-Antworten 1–5 verbindlich)
- E1: `SettingStammdaten.Kommissionierziel` + Optionen/Validator/Feld für MultiChoice aus Stammdaten; Einstellungen Vorfilter (Default K-01,K-02)
  + NurFreigegebene (Seed an); `target` mehrwertig, Gedächtnis `UserViewPreference` ViewKey `FaHierarchyKommissionier.Ziele`, „Ziele auf Standard“.
- E2: Spalte `montage-abteilung` über `VormontageService.FormatTexte` (kommagetrennt, kein Kombigerät), vier Quellen beider Ansichten + Druck.
- E3: Spalte `lagerplaetze` aus `GetStockByArticleNumbersAsync` (ohne Wagen, > 0), eine Batch-Abfrage je Seite, Filter vor Paging.
- E4: Gate über `IsReleasedForPicking` (neue Repo-Methode), Banner, PDF-Warnung, Manifest-Prüfungen, Doku, Version, TS-164.

## Protokoll
- Start: Vorprüfung ok (Freigegeben, Worktree passt, Antworten vollständig). Spec → InUmsetzung.
- E1 `77c7c57e`: Stammdaten-MultiChoice, Einstellung Vorfilter (Seed K-01,K-02), target[] + Gedächtnis + „Ziele auf Standard“.
  Abweichung: NurFreigegebene-Einstellung kommt mit dem Gate in E4. Der Einstellungswert wird nicht als Benutzerwert gemerkt
  (keine Umleitung); Druck/PDF-Links tragen die wirksamen Ziele über `ZielQuery`. Build + Tests grün (Web 3301/1, Service 790).
- E2 `cbd32566`: Spalte `montage-abteilung` „Abteilung“ (FormatTexte, kein Kombigerät) in Liste/Summiert/Druck, vier Quellen +
  ColumnMap je Anfrage (`BuildColumnMap`), Filter vor Paging. Tests grün (Web 3304/1, Service 790).
- E3 `7d544402`: Spalte `lagerplaetze` (GetStockByArticleNumbersAsync ohne Kommissionierwagen, > 0, „K-02 (12)“, „—“), eine
  Batch-Abfrage je Seite; mit `colf_lagerplaetze` eine Abfrage für die Restmenge vor dem Paging; Druck ganze Menge.
  Tests grün (Web 3308/1, Service 790).
- E4 `6c9d4a5e`: Schalter `FaHierarchyKommissionierlisteNurFreigegebene` (Seed an), `GetReleasedHauptFaOrderNumbersAsync` (eine Abfrage,
  `IsHauptFa`), Gate in allen sechs Aktionen vor den Spaltenfiltern, Banner `_KommissionierGate` (Zahl + Warnung Leitstand aus),
  PDF-Direktaufruf → WarningMessage; Manifest drei Prüfungen + Einstellung/Info; Version 1.99.43, Changelog, Hilfe, README, TS-164.
  **Abweichung:** `Pruefung/KommissionierlisteNichtFreigegeben` in Phase **Nachher** statt Vorher – Prüfungen laufen bei jedem Update, eine
  Vorher-Prüfung > 0 bricht Prod vor dem Stoppen ab (`Update-IdealAkeWms.ps1:701`); zusätzlich 0 bei Schalter aus.
  Tests grün (Web 3318/1, Service 790).
- QA (qa-agent synchron): **Testbereit**. Fix `3a6372a5` (nur TS-164.1-Text: Standard-Ziele stehen im Druck-Link, nicht in der
  Adresszeile). Beide Abweichungen bestätigt. Hinweise H1 (Manifest-Zählung vs. Banner bei führenden Nullen/verschwundenen Knoten),
  H2 (Pdf-Fehlerpfad ohne Query, vorbestehend), H3, H4. DEV AKE + IDEAL v1.99.43; Testdaten FaHierarchyOrderInfos 14456 (QA-TS164)
  bleibt. Nachweis [[2026-11-18-qa-v1-99-43-kommissionierliste-testprotokoll]].
- Paket 8: `publish/paket8-v1.99.43` + `.zip` (Commit `3a6372a5`, publish-info.txt), keine neue SQL ggü. Paket 7; Pakete 5–7 und
  übrige Dateien in `publish\` unverändert.
- Offen (Schranke 2): Manual-UAT 164.1c, 164.4 Wagenfall, 164.4c, Paging mehrseitig, Zahnrad+Druck; vor Test-Deploy Q-A/Q-B/Q-C und
  Entscheidung freigeben vs. Schalter vorübergehend aus. Spec bleibt in `entwurf/` (Gerald verschiebt). Kein Merge, kein Push.
