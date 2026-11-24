---
type: backlog
typ: feature
status: offen
created: 2026-11-24
quelle: Gerald, Chat 2026-11-24 („Sage Schnittstelle – Grund/Abteilung bei Ausbuchung, Benutzer bei jeder Buchung mitgeben“)
modul: Sage-Übergabe (Lagerbuchung, Sage-WE)
bezug: "[[2026-11-23-sage-buchungen-gesamtsicht-spec]]"
---
# Sage-Übergabe: Abteilung/Grund bei Ausbuchung, Benutzer bei jeder Buchung

Wortlaut:
- *Bei Ausbuchung Grund/Abteilung eingeben und an Sage übergeben (Dropdown der Werkbänke, wenn leer nichts) → Abteilung/Grund,
  derzeit in Abteilung in `[DPSMDELagerbuchung]` – können wir über SData zugreifen?*
- *Wer hat die Buchung übergeben/ausgelöst (Benutzer mitgeben), in Referenz mitgeben – bei jeder Buchung, die ins Sage übertragen wird.*

## Befund 2026-11-24 (Bündel-Worktree, v1.99.61)

**Was das WMS heute sendet**
- Lagerbuchung (`SageLagerbuchungPayloadBuilder.cs:70-74`, Request `SageLagerbuchungPayload.cs:10-28`): Kopf `Memo` =
  `IdealAkeWms Lagerbuchung SM#<id>#` (Korrelation), `Standardtext`; Zeile Bewegungsart, Artikel, Plätze, Menge, leere Serien/Chargen.
  **Kein** `Bearbeiter`, **keine** Zeilen-`Referenz`, **kein** Zeilen-`Memo`.
- Sage-WE (`SageWareneingangPayload.cs:13-45`): Kopf `Memo` = `WE#<id>#`, je Position `Artikelmemo` = `WE#<id># [LS …] Platz …`.
  Kein Benutzer.
- Der Benutzer steht im WMS an jeder Bewegung (`AuditableEntity.CreatedBy`/`CreatedByWindows`) – er wird nur nicht mitgeschickt.

**Was der SData-Vertrag `LagerbuchungService` zulässt** ([[sage-sdata-commonwawiservices]] §LagerbuchungService)
- Kopf: `Memo`, `Standardtext`, **`Bearbeiter`**; je Zeile zusätzlich **`Referenz`** und **`Memo`** (optional).
- Ein Feld **„Abteilung“ gibt es im Vertrag nicht.** Das Datenmodell dahinter (`strLagerplatzbuchungen.Sage.Wawi`, [[sage-sdata-masterdata]])
  kennt `Referenz`, `Memo`, `Job_Benutzer` – ebenfalls keine Abteilung.
- `DPSMDELagerbuchung` kommt im WMS-Code und im Brain **nicht** vor. Name deutet auf eine DPS-Partnertabelle der MDE-Lösung – Bedeutung
  nicht aus dem Namen ableiten, siehe Prüfabfragen.

**Werkbänke im WMS:** Stammdaten `ProductionWorkplace` (`Models/ProductionWorkplace.cs`) – Quelle für das Dropdown prüfen.

## Prüfabfragen (Mensch, Sage-Testsystem – nicht annehmen)

- P-1 Spalten und Füllung von `DPSMDELagerbuchung` (Sage-DB AKE-Test):
  `SELECT TOP 20 * FROM dbo.DPSMDELagerbuchung ORDER BY 1 DESC;` + `sp_columns DPSMDELagerbuchung` – wer schreibt sie (DPS-MDE-App?),
  über welchen Schlüssel hängt sie an der Lagerbuchung (`KHKLagerplatzbuchungen.BuchungsID`?), welche Werte stehen in `Abteilung`?
- P-2 Landet ein per SData gesendetes Zeilen-`Referenz`/`Memo` bzw. Kopf-`Bearbeiter` in `KHKLagerplatzbuchungen`/Lagerjob, und füllt der
  `LagerbuchungService` irgendetwas in `DPSMDELagerbuchung`? (Schreibtest auf dem Testsystem mit gesetzten Feldern.)
- P-3 Muss `Bearbeiter` ein gültiger Sage-Benutzer sein? (Beim FA-Arbeitsgang ist er eine feste Einstellung, `SageFaArbeitsgaenge.BearbeiterFehlt`.)
- P-4 Feldlängen `Referenz` und `Memo` in Sage.
- Falls Abteilung nur in `DPSMDELagerbuchung` geht: Kann DPS den `LagerbuchungService` um ein Feld `Abteilung` erweitern (wie andere
  DPS-Services), oder gibt es einen USER_-Feld-Weg?

## Vorschlag für die Spec

1. **Benutzer:** In jede übertragene Buchung den WMS-Benutzer (Kürzel bzw. Windows-Name) an Zeilen-`Referenz` (Lagerbuchung) bzw. ans
   Positions-`Artikelmemo` (Sage-WE) anhängen; die Korrelation `SM#…#`/`WE#…#` im Kopf-Memo bleibt unverändert (Memo-Lookup sucht mit
   `%marker%`, `SageBookingCorrelation.cs:28`). `Bearbeiter` nur, wenn P-3 ihn als freien Text zulässt.
2. **Abteilung/Grund bei Ausbuchung:** optionales Auswahlfeld (Werkbänke aus `ProductionWorkplace`, leer = nichts senden) in den
   Ausbuchungs-Masken, gespeichert an der `StockMovement` (neue nullable Spalte → SQL), übertragen in das Feld, das P-1/P-2 ergeben
   (`Referenz`/`Memo` oder DPS-Erweiterung). Wenn Sage es nicht aufnehmen kann: im WMS trotzdem speichern und melden.
3. Anzeige von Benutzer und Abteilung in der Gesamtsicht „Sage-Buchungen“ ([[2026-11-23-sage-buchungen-gesamtsicht-spec]]).

## Entscheidung Gerald 2026-11-24

- Vorschlag 1 **ja**: Benutzer zumindest in die **Referenz**. Die **Abteilung kommt vorerst ebenfalls in die Referenz** (nicht in
  `DPSMDELagerbuchung`; P-1 ist damit für den ersten Schritt nicht nötig).
- Format kurz, Beispiel Gerald: `WMS: WE#…# Gerald.Weichbold Abteilung:C1` (Kennung der Buchung, Benutzer, Abteilung nur wenn gewählt).
- Offen bleiben für die Spec: P-2 (landet `Referenz` sichtbar in Sage?), P-4 (Feldlänge `Referenz`), wo die Referenz beim Sage-WE hinkommt
  (StandardBelegService hat kein `Referenz`-Feld je Position wie der LagerbuchungService – belegen).
