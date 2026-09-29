-- =====================================================================================================
-- !!! NICHT ERNEUT AUSFUEHREN !!!
-- =====================================================================================================
-- SQL/Einmalig/2026-09-28_Hotfix-1.30.1_ResetAutosaveZeroQuantityPicked.sql
--
-- EINMALIGES DATENSKRIPT — KEINE Schema-Aenderung, KEIN __EFMigrationsHistory-Eintrag, KEIN Eintrag in
-- SQL/00_FreshInstall.sql, NICHT Teil der nummerierten SQL-Reihe (bewusst eigener Ordner).
--
-- Warum nie zweimal: Ab v1.30.1 ist eine getippte 0 eine bewusste Bestaetigung ("nichts geliefert").
-- Ein zweiter Lauf wuerde genau diese Bestaetigungen still loeschen.
-- Genau EINMAL je System (AKE mit v1.30.1; IDEAL mit dem Deploy des Buendels), und die Ausfuehrung im
-- Deploy-Protokoll vermerken (System, Datum, Ergebnis der Zaehl-Abfrage).
--
-- Hintergrund (Hotfix v1.30.1, Spec 2026-09-28-ake-hotfix-stueckliste-druck-lagerbestellung-ist):
-- Der bisherige Autosave der Lagerbestellung (Details.cshtml, collectProgress) hat leere Ist-Mengen als 0
-- statt NULL gespeichert. Fuer noch nicht abgeschlossene Bestellungen (Status Submitted = 2) macht dieses
-- Skript diese nie bestaetigten Nullen wieder zu NULL, damit die neue Pflichtpruefung beim Abschliessen
-- (Ist-Menge ODER Fehlteil) nicht durch alte Autosave-Nullen umgangen wird.
--
-- NICHT angefasst:
--   * Bestellungen PartiallyDelivered / Closed / Cancelled (dort ist eine 0 bereits gebucht).
--   * Zeilen mit Fehlteil-Markierung (ShortageStatus <> 0) — durch die Markierung bestaetigt
--     (Freigabe-Nachtrag 2026-09-29).
--
-- REIHENFOLGE: 1. DB-Backup.  2. Neue Web-Version (v1.30.1) deployen.  3. DANN dieses Skript.
-- (Vor dem Web-Deploy liefe noch der alte Autosave und schriebe sofort wieder 0.)
-- =====================================================================================================
SET NOCOUNT ON;

-- 1) Vorab zaehlen — Ergebnis ins Deploy-Protokoll.
SELECT COUNT(DISTINCT wr.Id) AS BetroffeneBestellungen, COUNT(*) AS BetroffeneZeilen
FROM dbo.WarehouseRequisitionItems wri
JOIN dbo.WarehouseRequisitions wr ON wr.Id = wri.WarehouseRequisitionId
WHERE wr.Status = 2                 -- Submitted
  AND wri.QuantityPicked = 0
  AND wri.ShortageStatus = 0;       -- None (Fehlteil-Zeilen bleiben unberuehrt)

-- 2) Zuruecksetzen (Audit-Felder je Zeile, ADR 0003).
UPDATE wri
SET wri.QuantityPicked    = NULL,
    wri.ModifiedAt        = GETDATE(),
    wri.ModifiedBy        = 'Hotfix-1.30.1',
    wri.ModifiedByWindows = SYSTEM_USER
FROM dbo.WarehouseRequisitionItems wri
JOIN dbo.WarehouseRequisitions wr ON wr.Id = wri.WarehouseRequisitionId
WHERE wr.Status = 2                 -- Submitted
  AND wri.QuantityPicked = 0
  AND wri.ShortageStatus = 0;       -- None

SELECT @@ROWCOUNT AS ZeilenZurueckgesetzt;
