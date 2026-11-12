---
type: changelog
version: 1.99.9
date: 2026-11-12
---
# v1.99.9 — System-Kürzel in den Sage-Kennungen (WE#/SM#)

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commits `115e8a09` (Code, SQL/144), `c47f0136` (Version, Doku, TS-116.25–116.30).
Spec [[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]], Abschnitt „Freigabe-Nachtrag 2026-11-12 14:20“. Nicht gemergt, nicht
gepusht, vor QA.

## Kernpunkte

- Neue ServiceSetting `Sync:SageSystemKuerzel` (Default leer = Produktion, `WE#<Id>#`/`SM#<id>#` unverändert). Gesetzt: `WE#<K>#<Id>#` bzw.
  `SM#<K>#<id>#`. Erlaubt `^([A-Z][A-Z0-9]{0,4})?$` (Buchstabe vorne, Serverprüfung über `SettingDefinition.Muster`).
- Die beim Senden verwendete Kennung wird gespeichert (`PurchaseOrderReceipts.SageWeKennung`, `SageBookingQueueItems.SageKennung`); die Suche vor
  dem erneuten Senden (E5, Recovery/Requeue der Lagerbuchungen) nutzt sie. Altbestand ohne Kennung = altes Format.
- Sicherungsnetz: jede WE-Kennung mit oder ohne Kürzel, die nicht exakt die eigene ist, gilt als fremd.
- Lagerbuchungen (SM#) hatten dasselbe Risiko (Lookup findet die Buchung eines anderen WMS → nie gebucht) und bekommen dasselbe Kürzel.
- Dienst liest das Kürzel ohne Safe-Fallback; ungültig oder Lesefehler = Takt bricht ab, nichts gesendet.

## Migration / SQL

Migration `20261112141208_AddSageKennungen`, **SQL/144_AddSageKennungen.sql** (additiv, nicht daten-destruktiv), FreshInstall Schema + MigrationId.
SQL/AgentJobs nicht betroffen.

## Deploy-Risiken

Web **und** Dienst gemeinsam deployen (Dienst schreibt die neuen Spalten). Nachlauf `SQL/144` (automatisch) und `v1.99.9-SystemKuerzel`:
Kürzel je Testsystem setzen (DEV, AT = AKE-Test, ATI = AKE Test-IDEAL, IT = IDEAL-Test), **Produktion leer lassen**. DEV-Absicherung setzt `DEV`.
