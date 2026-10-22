---
type: changelog
version: 1.67.0
date: 2026-10-22
---
# v1.67.0 — Vault- und enaio-Links konfigurierbar

Spec [[2026-10-21-vault-links-per-schalter-spec]]. Basis `845e38c9` (v1.66.2), Commits `b1cb87e1`, `9fe7553f`, `4bfa4576` (qa).
**Keine Migration**, TS-100.

## Kernpunkte

- **Neue AppSettings:** `VaultLinksAktiv` / `EnaioLinksAktiv` (Default an, nur `false` sperrt) und `VaultLinkVorlage` (`{Artikelnummer}`)
  / `EnaioLinkVorlage` (`{ObjektId}`). Gepflegt in den Standorteinstellungen, Gruppen „Vault (Zeichnungen)“ und „enaio (Dokumente)“.
- **Laufzeit-Seed** in `Program.cs` mit den bisherigen AKE-Adressen. Er ist Pflicht, denn ohne Zeile würde die Maske den Schalter beim
  nächsten Speichern auf `false` setzen ([[fallstricke]] §3).
- **Eine Regel:** `Helpers/LinkVorlage.Build`. Die Nummer wird überall URL-kodiert; ist sie leer, gibt es keinen Link. Zehn
  Vault-Stellen laufen über `_VaultLink`, enaio über `_EnaioDmsBadges`. Der Knopf „Stückliste öffnen“ bleibt immer.
- **enaio-Sync:** Quelle aus `Sync:EnaioDmsViewName` (Default `[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]`, SQL zeichengleich).
  Geprüft wird mit `ValidateAndQuote(…, maxSegments: 3)`; ein ungültiger Name führt zu keinem SQL, sondern zu Fehler und Fehlermail.
  `Sync:EnaioDmsEnabled` ist unverändert.

## Deploy-Risiken

- **Web und Dienst:** Der Dienst wird einmal neu gestartet (Binärtausch); spätere Änderungen am View-Namen wirken ohne Neustart.
- **IDEAL:** Nach dem Einspielen „Vault-Links anzeigen“ ausschalten. Über enaio an IDEAL entscheidet Gerald beim Deploy.
- **Verhaltensänderung an AKE:**
  - Nummern mit Sonderzeichen werden jetzt korrekt URL-kodiert.
  - Eine Stücklistenzeile ohne Ressourcennummer hat keinen Vault-Link mehr.
