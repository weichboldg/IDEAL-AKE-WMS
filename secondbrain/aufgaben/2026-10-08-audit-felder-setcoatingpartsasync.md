---
typ: aufgabe
status: offen
erstellt: 2026-10-08
---
# Audit-Felder in `SetCoatingPartsAsync` (Service-Name fehlt)

Fund aus dem QA-Review zu v1.47.5 ([[2026-10-08-v1-47-5-zahnrad-lagerbestellung-lackflag-kind-sub-fa]], M-4),
**vorbestehend**, nicht durch v1.47.5 eingeführt.

- `IdealAkeWms/Data/Repositories/ProductionOrderPickingStatusRepository.cs` `SetCoatingPartsAsync` (Bündel ca.
  Z. 141-168) setzt bei Änderung nur `ModifiedAt`; `ModifiedBy`/`ModifiedByWindows` bleiben leer (Kommentar
  „Sync-Job, kein User“).
- Hausregel (CLAUDE.md „Audit-Felder“, ADR 0003): Service-Syncs schreiben ihren Service-Namen.
- Warum jetzt relevant: Seit v1.47.5 kippt der Materialisierungslauf `HasCoatingParts` an Vater-Sub-FAs auf
  `false` und setzt dabei ein manuell gesetztes `IsCoatingDone` zurück — ohne Spur, wer das war.
- Vorschlag: Aufrufer-Kennung als Parameter (`"IDEALAKEWMSService"`) in `ModifiedBy`/`ModifiedByWindows`; Aufrufer
  `FaMaterializationSyncService` (Konstante `ServiceUser`). Nicht mitgebaut — eigener kleiner Fix.
