---
typ: notiz
spec: "[[2026-10-30-sage-sdata-fa-ansicht-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: FA-Struktur live aus Sage (SData, nur lesen)

Spec [[2026-10-30-sage-sdata-fa-ansicht-spec]] (Rumpf + **Freigabe-Nachtrag geht vor**), direkt im Bündel. Start `d7dc3d03`
(v1.70.0), Ziel **v1.71.0**, **TS-103**. Keine Migration. Deploy: Web (Dienst nur Version).

## Plan (kompakt)

1. **Modelle + Builder** `Services/SageSdata/SageSdataFaModels.cs`, `SageSdataFaTreeBuilder.cs` (rein, statisch):
   Root `BelID = StrukturID` (VerursacherTyp egal), Zuordnung MA-Position → Sub-FA nur eindeutig (F-4/F-5), Positionen nach
   `BelID` gruppiert, `visited` gegen Zyklen. Tests: Fixtures 835/836 + synthetisch (VK, zweistufig, Mehrdeutigkeiten, Sub-FA
   ohne Position, ohne Root, Zyklus, Termin ohne Offset).
2. **Client** `ISageSdataReadClient`/`SageSdataReadClient`: nur GET, `RealTimeData` konstant, `where` Pflicht, `or`-Blöcke à 50
   BelIDs, TLS-Schalter je Request in `HttpRequestMessage.Options`, Basic-Auth, Body-Cap 4000, Fehler → `SdataCall`
   (Status 0 bei Verbindungsfehler). Registrierung `Program.cs` mit Callback, der nur die Option liest. Tests per Stub-Handler,
   `RequestUri.AbsoluteUri`.
3. **Controller + View + Menü + Schalter**: `SageSdataFaController.Index(int? belId)`, Gate `SageSdataFaAnsichtAktiv`, Konfig-
   Prüfung (BaseUrl/Dataset/Username, BaseUrl ohne `@`), 5 Aufrufe, Abbruch nach erstem Status 0 („übersprungen“), View
   (frontend-design, keine `filterable-table`, `table-responsive`), Menüpunkt, Katalogzeile.
4. Doku: Hilfe, Changelog, README (AppSetting + „Web-App: Sage-Zugangsdaten“), TS-103, v1.71.0, TLS-Beschreibungstexte.
5. Build/Test, `wip … feature-complete`, qa-agent.

## Stand

- 2026-10-30: gestartet (Spec nach `freigegeben/`, Status InUmsetzung).
- 2026-10-30: umgesetzt, Commits `00c368d6`, `8c09ab87` (wip feature-complete). Build 0 Fehler, Web 2254 / 1 übersprungen, Dienst 328. Wartet auf qa-agent.
- 2026-10-30: **Testbereit** (qa-agent): Build 0 Fehler, Web 2254 / 1 übersprungen, Dienst 328; QA-Fix `0fe5e7c7` (Q-1 2xx ohne lesbares JSON galt als Erfolg, Q-2 Text „übersprungen“, Q-3 Auf/Zu-Dreieck), Q-4–Q-7 Hinweise. Wartet auf Schranke 2 (Manual-UAT TS-103 + Bündel-Merge).
