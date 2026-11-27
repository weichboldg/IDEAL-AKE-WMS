---
type: aufgabe
status: erledigt
created: 2026-11-18
spec: "[[2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck-spec]]"
---
# /goal-Lauf 2026-11-18: Beschichtungsauftrag-Ausbau (Haken, freie Positionen, Versendet, Kreditor, Druck)

Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Start bei c39126c3 (v1.99.40). Nummern beim Start: v1.99.41, SQL/161, TS-162.
Freigabe: Antworten 1–17 (R1–R5 Gerald 11:48, 6–17 wie Empfehlung 12:55).
Grenzen: nichts an Sage schreiben, keine Einstellungen auf Test/Prod, `SageFaArbeitsgaengeAnlegenAktiv` bleibt aus, Publish nur im Worktree (Paket 6), Pakete 1–5 unverändert, kein Merge, kein Push.

| Spec | Typ | SQL | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck-spec]] | Epic (E1–E6) | 161 | 1.99.41 | Testbereit | 5c042c3d, b525cfba, 4b2b490c, 5ff2c7a8, b1ce8608, cf11a777, QA-Fix 9b6b3e0d | P-1/P-2/P-3 (Mensch) |

## Plan (kompakt)
- Kein neuer Konstruktor-Parameter am Workflow: Artikel- und Kreditor-Lesen über `IBeschichtungsauftragRepository` (ADR 0001).
- `AnlegenAsync(hauptFa, AnlageAuswahl)`; `AnlegenAsync(hauptFa)` bleibt als „alle freien“ (Tests).
- E1 Schema/Status → E2 Bearbeiten bis vor Versendet + freie Positionen → E3 Vorschlag mit Haken → E4 Kreditor → E5 Druck → E6 Doku. Je Etappe ein Commit.

## Protokoll
- Start: Spec freigegeben, Vorprüfung ok (alle 17 Antworten, keine Blocker). Status InUmsetzung.
- E1 erledigt `5c042c3d`: Migration 20261118120203 + SQL/161, FreshInstall, Manifest (SQL/161, 2 Prüfungen, Info), Versendet-Übergänge mit Haken-Regel, Badge/Verlauf/Listenspalte. Tests Web 3263, Dienst 779 grün.
- E2 erledigt `b525cfba`: `BearbeitenAsync` (ein Speichern, Entwurf+Übergeben, Kopf nur Entwurf), freie Positionen (AKE Maße ×10, IDEAL 0 + Hinweis P-3), Aktion `PositionenHinzufuegen` in `Bearbeiten` aufgegangen, Partial `_BeschichtungFreiePositionen` + `beschichtung-frei.js`. Tests Web 3274, Dienst 779 grün.
- E3 erledigt `4b2b490c`: `AnlegenAsync(hauptFa, VorschlagAuswahl?)` (null = alle freien, nur Altweg/Tests), Haken-Spalte Liste/Summiert ohne `data-col-key`, `VergabeInfo.FreiJeArtnr` für „n von m frei“. Zusatzfund: ein Auftrag nur mit Positionen ohne FA erscheint nicht im Gruppenkopf der Vorschlagsliste (Aufträge kommen aus den Vergaben, die OhneFa filtern) — gemeldet, nicht gebaut. Tests Web 3280, Dienst 779 grün.
- E4 erledigt `5ff2c7a8`: `KopfDaten.CreditorId`, Prüfung gegen `GetBeschichterAsync` (aktiv + IsCoater), `KreditorVorbelegen` (IDEAL, genau ein Kurzname-Treffer), Reklamation übernimmt den Kreditor. Anschrift im Briefkopf kommt mit dem Druck in E5. Tests Web 3285, Dienst 779 grün.
- E5 erledigt `b1ce8608`: Druck je Position (`BeschichtungsauftraegeDruck.Sortiert`, auch Detail), Brief-Anschrift aus `a.Creditor` (`PrintBriefkopfModel.FuerAuftrag(..., kreditor)`), A12 über `beschichtung-bemerkung.js` (`data-bemerkung-pruefen`). P-1 im Code: kein Unterschied Druck/PDF, keine Druck-CSS-Regel auf `.bemerkung` → Befund 2 bleibt der belegte Weg; den Browser-Lauf (Edge + Chrome + PDF) macht der qa-agent auf DEV, das CSS der echten Briefkopfvorlage lässt sich nur dort prüfen. Tests Web 3287, Dienst 779 grün.
- E6 erledigt `cf11a777`: TS-162 (+ Querverweis TS-84/TS-145), Hilfe Beschichtung, Anwender-Changelog, README, beide `AppVersion.cs` 1.99.41. Brain: Changelog [[2026-11-18-v1-99-41-beschichtungsauftrag-ausbau]], feature-map, Testindex, Fallstrick 52, datenmodell/controller ergänzt. Kein ADR 0034: die Entscheidungen stehen in der Spec, Dauerwissen in Fallstrick 52. Tests Web 3287, Dienst 779 grün.
- QA (qa-agent, 2026-11-18): **Testbereit**. F1 (mittel) behoben in `9b6b3e0d`: Artikel-Auswahl der Position ohne FA ohne leere Option → `freiArtikelId`/`freiMenge` verrutschten paarweise; Wächtertest `FreiePositionen_ArtikelAuswahl_SendetJedeZeile`. Hinweise H1–H7 nur gemeldet (u. a. H2 Reklamationsauswahl zeigt Position ohne FA als „— / 0 / —“, H3 freie Position in Reklamations-Entwurf per direktem POST möglich). Tests Web 3287 (1 skip), Dienst 779. DEV AKE + IDEAL v1.99.41, Migration 161 auf beiden. P-1 in Chrome bestanden (Bemerkung in Druck und PDF), Edge offen.
- Paket 6: `publish/paket6-v1.99.41` (Commit 9b6b3e0d, publish-info.txt), Pakete 1–5 unverändert. Kein Merge, kein Push.
- Offen für den Menschen: Schranke 2 (Manual-UAT TS-162 Rest laut QA-Checkliste), P-2/P-3/P-4.
