---
type: testprotokoll
title: "QA v1.99.43 IDEAL-Kommissionierliste (TS-164) auf DEV"
created: 2026-11-18
spec: "[[2026-11-18-ideal-kommissionierliste-filter-spalten-leitstand-spec]]"
version: "1.99.43"
standort: IDEAL
---

# QA v1.99.43 – IDEAL-Kommissionierliste (TS-164) auf DEV

Spec [[2026-11-18-ideal-kommissionierliste-filter-spalten-leitstand-spec]], Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`,
Diff `b5463336..6c9d4a5e` + QA-Commit `3a6372a5` (nur TS-Text).

## Build und Tests (Worktree, HEAD 6c9d4a5e)

- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 30 Warnungen (vorbestehend).
- `dotnet test`: IdealAkeWms.Tests 3318 bestanden / 1 übersprungen / 0 Fehler (3319); IDEALAKEWMSService.Tests 790 / 0 / 0.

## DEV-Deploy

| Standort | Befehl | Ergebnis |
|---|---|---|
| IDEAL | `dev-deploy.ps1 -Standort IDEAL -Quelle <worktree>` | Exit 0, „HTTP 200, Fusszeile v1.99.43“, Web + Dienst, DEV-Absicherung angewandt |
| AKE | `dev-deploy.ps1 -Standort AKE -Quelle <worktree>` | Exit 0, „HTTP 200, Fusszeile v1.99.43“ (vorher v1.99.42, Bündelstand) |

## Ausgangslage DEV IDEAL (sqlcmd)

`LeitstandAktiv=true`, `FaHierarchyKommissionierlistenAktiv=true`, Seeds `FaHierarchyKommissionierlisteVorfilterZiele=K-01,K-02`,
`FaHierarchyKommissionierlisteNurFreigegebene=true`. Vier HauptFA mit Kommissionier-Positionen: 1059463, 1064052, 1065036 freigegeben
(Ziele EM/ISO/KOM/LÖT/PG), 1060414 **nicht** freigegeben (einziger HauptFA mit K-01/K-02). Kein Kommissionierwagen auf DEV
(`StorageLocations.IsPickingTransport=1`: 0). Kein Kombigerät → Testzeile angelegt: `FaHierarchyOrderInfos` Id 14456
(HauptFA 1059463, Abteilung FKV, Kunde `QA-TS164`) — bleibt für die Abnahme stehen.

Durchführung: Browser-Tab (claude-in-chrome) für den Erstaufruf; Screenshots lieferte die Erweiterung nicht (Renderer-Timeout),
daher die Schritte per `curl --ntlm` als angemeldeter Windows-Benutzer (Gerald Weichbold, UserId 8) gegen http://localhost:8082
und Auswertung des HTML, Daten per `sqlcmd -S . -E -C`.

## TS-164 Schritte

| Nr | Durchgeführt | Ergebnis | Bewertung |
|---|---|---|---|
| 164.1 | `/Summiert` ohne Gedächtnis | K-01, K-02 angehakt (`checked`), Banner „1 HauptFA … ist noch nicht freigegeben“, Liste leer (1060414 nicht freigegeben). Adresszeile ohne `target`, Drucken-Link `PrintSummiert?target=K-01&target=K-02`, Index-Druck `Print?target=K-01&target=K-02` | OK (TS-Text „URL trägt target“ war falsch → korrigiert in `3a6372a5`) |
| 164.1a | `?target=&target=KOM&target=PG` | 79 Zeilen; `UserViewPreferences` UserId 8 `{"ziele":["KOM","PG"]}`; danach `/Summiert` → 302 `Summiert?target=KOM&target=PG`, `/Index` → 302 `?target=KOM&target=PG` | OK |
| 164.1b | `?target=` (nichts angehakt) | 126 Zeilen (alle), gemerkt `{"ziele":["*"]}` | OK |
| 164.1c | zweiter Benutzer | — | NICHT MÖGLICH (nur ein Windows-Konto); Gedächtnis ist je `UserId` geschlüsselt, Unit-Test deckt ab |
| 164.1d | POST `ZieleZuruecksetzen` (Antiforgery) | 302 auf Summiert, Gedächtnis-Zeilen 0, danach Einstellung | OK |
| 164.1e | `Print?target=KOM&target=PG`, `PrintSummiert`, PDF | Kopf „Ziel: KOM, PG“; PDF 200 `application/pdf` | OK |
| 164.1f | `?target=XYZ&target=KOM&colf_matchcode=BLENDE` | „XYZ (nicht vorhanden)“ angehakt, 15 Zeilen KOM, Umschalt-/Druck-/PDF-Links tragen `target=XYZ&target=KOM&colf_matchcode=BLENDE` | OK |
| 164.2 | `/Settings` | Häkchenliste aus FA-Struktur (EM … x), Warntext zum Gate sichtbar; Standard auf `KOM` gespeichert → Benutzer ohne Gedächtnis sieht nur KOM (72 Zeilen); zurück auf `K-01,K-02` | OK |
| 164.2a | POST `settings[...]=K-01,XYZ`; danach leer | „XYZ … ist nicht erlaubt – erlaubt: EM, H1-03 …“, DB unverändert `K-01,K-02`; leer gespeichert `[]` (erlaubt), danach zurück auf `K-01,K-02` | OK |
| 164.3 | Spalte Abteilung Summiert/Liste | 1059463 „FKV, WKS“ (kein „Kombigerät“), 1064052 TK, 1065036 GN | OK |
| 164.3a | `colf_montage-abteilung=WKS` bzw. `FKV` | je nur 1059463 (26 Zeilen), `data-total-count="1"` | OK |
| 164.3b | Druck/PrintSummiert | `<th>Abteilung</th>` und „FKV, WKS“ im Druck; Ausblenden per Zahnrad | OK / Ausblenden Manual-UAT |
| 164.3c | Vormontage Summiert | Seite 200, „Kombigerät“-Hinweis weiter vorhanden | OK |
| 164.4 | Spalte Lagerplätze | z. B. 50000338 „H4-04 (1), KELLER (3), BL (2), WEL-W1 (2), V-01 (1)“ (Hauptplatz zuerst, dann Menge absteigend); ohne Bestand „—“. Platzcodes auf DEV heißen `…;0;0;0` (DEV-Daten) | OK |
| 164.4 Wagen | — | NICHT MÖGLICH (kein Wagen auf DEV); Ausschluss über bestehenden Overload `mitKommissionierwagen:false` (`StockMovementRepository.cs:395-409`) |
| 164.4a | `colf_lagerplaetze=KELLER` Liste + Summiert | je 2 HauptFA (1059463, 1065036), Zähler „1–2 von 2“; Blättern über mehrere Seiten mit 4 HauptFA nicht darstellbar | OK / Mehrseiten Manual-UAT |
| 164.4b | Print ungepaged | „KELLER (3)“ in beiden Gruppen | OK |
| 164.4c | Abfragezahl | — | Manual-UAT (Profiler); Unit-Test „eine Batch-Abfrage je Seite“ grün |
| 164.5 | Gate an | 1060414 fehlt in Liste/Summiert/Print, Banner n=1 | OK |
| 164.5a | `PickingLeitstand/ToggleRelease id=284 release=true` (echter POST) | Flag 1, Summiert K-01/K-02 zeigt 30 Zeilen 1060414, Banner „Alle … freigegeben“. Nebeneffekt laut Spec: Kommissionierauftrag auf DEV angelegt | OK |
| 164.5b | Rücknahme `release=false` | Flag 0, Liste leer, Banner n=1 | OK |
| 164.5c | `Pdf/1060414`, `PdfSummiert/1060414` | 302 auf Liste bzw. Summiert mit Query, Meldung „HauptFA 1060414 ist im Leitstand nicht zur Kommissionierung freigegeben.“ | OK |
| 164.5d | `LeitstandAktiv=false` über `/Settings/SaveSettings` | gelbe Warnung „Leitstand ist ausgeschaltet – …“; zurück auf `true` | OK (Hinweis: direktes SQL-UPDATE wirkt wegen Settings-Cache nicht) |
| 164.5e | Schalter `NurFreigegebene=false` | kein Banner, 1060414 sichtbar (30 Zeilen), PDF 200; zurück auf `true` | OK |
| 164.5f | `Update-IdealAkeWms.ps1 -Firma IDEAL -System DEV -NurPruefen` (Scratch-Paket mit Worktree-`deploy/`+`SQL/`, nicht `publish\`) | `KommissionierlisteNichtFreigegeben` Abweichung 1 „von Hand“, `LeitstandAus` OK, `VorfilterZiele` OK, kein Abbruch, „nichts gestoppt, nichts geschrieben“ | OK |
| 164.6 | AKE-DEV | `/Settings` 200 mit beiden IDEAL-Einstellungen, keine Fehlermeldung; Kommissionierlisten → 302 Start (Modul aus); Leitstand 200. IDEAL-Menü verlinkt `/FaHierarchyKommissionierListen/Summiert` | OK |

Endzustand DEV IDEAL: Einstellungen wie Ausgangslage, Gedächtnis von UserId 8 gelöscht, 1060414 nicht freigegeben, Testzeile 14456 bleibt.
