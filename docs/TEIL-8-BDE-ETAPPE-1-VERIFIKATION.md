# Teil 8 (Sub-FA-BDE) — Etappe 1: Verifikation & Nahtstellen-Karte

Branch-lokales Entwickler-Memo. Vollstaendiges Urteil + Begruendung im Brain
(`secondbrain/aufgaben/2026-07-29-standort-ideal-teil-8.md`). Diese Datei ist die knappe
Nahtstellen-Karte fuer Etappe 2–4; sie aendert keinen Produktivcode.

## Ergebnis
Teil 7 hat die Datenschicht mehrdeutigkeitsfaehig gemacht; Teil 8 stellt die BDE-Aufrufer um und baut
die Auswahl-UI. Es gibt **genau einen** eindeutigkeitsannehmenden Scan-Seam (Normal-Modus) plus den
NurFA-Substring-Fehlbucher. OSEON ist nicht betroffen → **Etappe 5 entfaellt**.

## Nahtstellen (Datei:Zeile)

| Was | Ort | Etappe |
|-----|-----|--------|
| Mengenwertiger Lookup (Teil-7-Liefergegenstand, vorhanden) | `Data/Repositories/WorkOperationRepository.cs:101` `GetAllByFaAndOperationAsync` | — |
| Einzel-Lookup mit Mehrfachtreffer-Log (Terminal unveraendert) | `Data/Repositories/WorkOperationRepository.cs:84` `GetByFaAndOperationAsync` | — |
| **Normal-Modus-Scan-Seam** → auf mengenwertig umstellen | `Controllers/BdeApiController.cs:48` `GetWorkOperation` (Split `,` serverseitig!) | 2/3 |
| **NurFA last-wins-Substring ohne `break`** → Auswahlweg | `wwwroot/js/bde-terminal.js:168-170` (Button-Label `"{OrderNumber} — {Description1}"`) | 4 |
| NurFA-Fallback-API (`opNumber=01` hart, Einzel-Lookup) | `wwwroot/js/bde-terminal.js:176` | 4 |
| Auswahl-UI-Fundament (werkbank-gescopt, Id-basiert) | `Controllers/BdeApiController.cs:158` `GetAvailableOperations` | 3 |
| Buchung per Id (bereits eindeutig) | `Controllers/BdeTerminalController.cs:63` `StartProductionForOrder`; JS `bde-terminal.js:404-412` | 3 |
| Teileverfolgung: nur Filter (`Contains`), unkritisch | `Controllers/TrackingController.cs:63,157` | 4 (bestaetigen) |
| OSEON: kein eindeutigkeitsannehmender WMS-Schreibpfad | `IDEALAKEWMSService/Services/OseonSyncService.cs:506` set-based UPDATE-JOIN | 5 entfaellt |

## Verbindliche Entscheidungen (Schranke 1, 2026-08-12)
- **Granularitaet (KP2-3):** Auswahleinheit ist IMMER der Sub-FA (ProductionOrder), in beiden Modi.
  Normal: eindeutig, wenn genau eine `WorkOperation` zu (`OrderNumber`,`OperationNumber`); sonst
  Auswahl ueber die Sub-FAs. NurFA: eindeutig, wenn genau ein `ProductionOrder` zur `OrderNumber`.
- **Scope (KP2-6):** werkbank-gescopt, deckungsgleich mit `GetAvailableOperations`. Randfaelle:
  0 Treffer im Scope trotz existierendem HauptFA → eigene Meldung; 1 Treffer im Scope trotz global
  mehrerer → Direktbuchung.
- Fallback `SubOrderNumber` ist unerreichbare Vorruestung; Kombigeraete positionsweise (bekannte
  Grenze, kein Fehler).
