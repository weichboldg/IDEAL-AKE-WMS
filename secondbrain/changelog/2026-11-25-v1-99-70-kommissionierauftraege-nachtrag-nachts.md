---
type: changelog
version: 1.99.70
date: 2026-11-25
---
# v1.99.70 — Kommissionier-Nachtrag nachts, Knopf nur für Admins, Marker „Stückliste kleiner“

Spec [[2026-11-25-kommissionierauftraege-nachtrag-nachts-spec]]. Commit `fa1b754b` (Worktree `feature/2026-08-07-ideal-teile-1-5`).
**SQL/168** (Migration `20261125161741_AddPickingStatusStuecklisteKleiner`), Web **und** Dienst (Version in beiden `AppVersion.cs`). TS-191
(Hinweise an TS-120.2/120.6/120.13). Kein ADR. Nicht gemergt.

## Kernpunkte

- Neuer Dienst-Lauf „Kommissionier-Nachtrag“ (`IDEALAKEWMSService/Services/KommissionierNachtragService.cs`, SyncLog-Service
  `KommissionierNachtrag`): einmal je Tag ab `Sync:KommissionierNachtragUhrzeit` (Default 2), Gate `Sync:KommissionierNachtragEnabled`
  (Default an), Termin über den Startmerker „Run gestartet“ (`LetzteBestellungPlaner.Faellig`), verpasster Termin wird nachgeholt, DryRun nie.
  Je freigegebenem, offenem HauptFA eigener Scope, `PickingOrderWorkflow.ErzeugeOffeneAsync`; Fehler eines FA bricht den Lauf nicht ab.
  Counts `fa_geprueft`, `fa_mit_nachtrag`, `auftraege_angelegt`, `zeilen_angelegt`, `ohne_rest`, `altbestand_pruefen`, `ohne_stueckliste`,
  `fehler`, `stueckliste_kleiner`. Audit `CreatedBy` = `KommissionierNachtrag` über `DienstBenutzer`.
- Gemeinsame Registrierung `AddKommissionierauftraege` (`IdealAkeWms/Services/PickingOrderRegistrierung.cs`) für Web, Dienst und `BomDiResolutionTests`.
- Leitstand: beide Knöpfe „Fehlende Kommissionieraufträge …“ nur Admin (View + `[RequireAdminAccess]` auf `AuftraegeNachziehen`);
  „Altbestand geprüft“ legt bei freigegebenem, offenem FA sofort die fehlenden Aufträge an.
- Marker `ProductionOrderPickingStatus.StuecklisteKleiner` (NVARCHAR(1000), nur bei Änderung geschrieben) – eine Regel
  `PickingOrderRestmengen.Ueberdeckt`; Badge „⚠ Stückliste kleiner“ im Leitstand, live berechneter Hinweis oben in `Picking/Bom`. Offene Zeilen
  werden nie gekürzt.

## Deploy-Risiken

- Schema SQL/168 vor dem Start von Web und Dienst (beide lesen die Spalte über EF). Additiv, nullable, nicht daten-destruktiv.
- Erster Dienststart nach dem Update läuft sofort einmal (kein Startmerker) und legt ggf. Aufträge für alle bisher offenen Nachträge an
  (Manifest-Info `v1.99.70-KommNachtrag-Lauf`). Leitstand-Benutzer ohne `admin` verlieren den Knopf (Info `v1.99.70-KommNachtrag-Knopf`).
