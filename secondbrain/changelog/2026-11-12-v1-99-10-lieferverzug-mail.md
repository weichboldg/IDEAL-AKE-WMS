---
type: changelog
version: 1.99.10
date: 2026-11-12
---
# v1.99.10 — Offene Bestellungen: tägliche Erinnerungs-Mail bei Lieferverzug

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commits `37c831a4` (Code, SQL/145, Tests, Manifest), `dae22a49` (TS-128, Hilfe, Changelog, README).
Spec [[2026-11-12-offene-bestellungen-mahnmail-lieferverzug-spec]]. Nicht gemergt, nicht gepusht, vor QA (qa-agent steht aus).

## Kernpunkte

- Neuer Dienst-Lauf „Lieferverzug-Mails“ (`SyncLogServices.LieferverzugMail`) im `SyncWorker` direkt nach „Offene Bestellungen (Sage)“, Gate
  `Lieferverzug:MailAktiv` (Default aus, beide Standorte, IDEAL aus bis zur SData-Schnittstelle). Quelle nur der Cache `SagePurchaseOrderLines`.
- Fällig: `heute >= Liefertermin + TageNachTermin` (Kalendertage, Default 3) und Rest > 0 nach der Rest-Regel (`ErfasstJeBelPos.Rest` – dieselbe
  Funktion wie die Maske „Wareneingang zu Bestellung“). Mo–Fr ab `Lieferverzug:Uhrzeit` (Default 7), höchstens einmal am Tag (Protokolltabelle).
- Einkauf-Sammelliste je Lieferant mit Statuszeile; Kreditor-Mail DE+EN je Lieferant, Wiederholung je (BelPosId, Liefertermin) nach 7 Tagen,
  Grenze 30 echte Kreditor-Mails je Lauf (alle oder keine, Warnung + Fehlermail), keine Kreditor-Mail bei Stand > 24 h, gesperrtem Lieferanten,
  inaktivem/fehlendem Kreditor, abgeschaltet, keiner/ungültiger Adresse.
- **Testbetrieb (Default an):** Kreditor-Mails gehen unverändert an die Einkauf-Empfänger, Betreff + orangefarbener Kopf „TESTBETRIEB – wäre an … gegangen“.
- Freigabe-Antwort 7: eigene Absenderadresse der Kreditor-Mail (`Lieferverzug:KreditorAbsender`, leer = `MailSettings:FromAddress`), Reply-To
  einstellbar, CI-Auswahl AKE/IDEAL (`Lieferverzug:Standort`, Hausfarbe + Firmenadresse aus `Branding/briefkopf.html`, Logo per CID).
- Kreditor: `ReminderDisabled`, `ReminderEmail` (WMS-Felder, auch bei Sage-Kreditoren pflegbar; Maske „Erinnerungen bei Lieferverzug“).
- `IMailService.SendMitOptionenAsync` (Absender, Reply-To, Bilder, `false` bei stillem Überspringen); neue appsettings-Option
  `MailSettings:PickupDirectory` (nur DEV/QA: `.eml` statt SMTP).
- Server prüft Mail-Adressen beim Speichern (`SettingDefinition.Mail`, eine Regel `MailAdressen`).

## Migration / SQL

Migration `20261112143552_AddPurchaseOrderReminders`, **SQL/145_AddPurchaseOrderReminders.sql** (additiv, nicht daten-destruktiv): Tabelle
`PurchaseOrderReminders`, Spalten `Creditors.ReminderDisabled`/`ReminderEmail`. FreshInstall Schema + MigrationId. SQL/AgentJobs nicht betroffen.

## Deploy-Risiken

Web + Dienst + Schema. Reihenfolge: Dienst stoppen → SQL/145 → Web → Dienst. Danach `/ServiceSettings` → „Lieferverzug-Mail“: Hauptschalter aus,
Testbetrieb an. Manifest `SQL/145`, `Pruefung/LieferverzugEmpfaenger` (Nachher), `v1.99.10-Lieferverzug` (Einstellung), `v1.99.10-LieferverzugInfo`.
Vor dem Echtbetrieb: Relay/Absender extern mit der IT (P-M4), erste Welle P-M3, Bedeutung des Liefertermins P-M1. `MailSettings:PickupDirectory`
in Produktion **nicht** setzen.
