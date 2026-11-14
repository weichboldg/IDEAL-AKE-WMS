---
type: bug
title: "Anmeldung mit leerem Kennwort für Benutzer ohne Kennwort, Erstinstallation bricht in Abschnitt 15 ab, Installer schreibt Zugangsdaten im Klartext"
status: offen
severity: hoch
created: 2026-11-14
standort: beide
affected_code:
  - "IdealAkeWms/Controllers/AccountController.cs:82-101 (kein Hash -> leeres Kennwort reicht), :104-110 (Sitzung)"
  - "IdealAkeWms/Controllers/UsersController.cs:200-209 (CreateAdUser ohne Hash), :136-137 (Create: Kennwort optional)"
  - "IdealAkeWms/Program.cs:259-279 (admin mit Hash des leeren Kennworts), :55-61 (Sitzungs-Cookie ohne SecurePolicy)"
  - "IdealAkeWms/Views/Account/Login.cshtml:79 (Hinweis „Leer lassen, wenn kein Passwort vergeben“)"
  - "SQL/00_FreshInstall.sql:1260-1263 (Indizes auf Spalten, die ProductionOrders nicht hat), :1803/:1838 (CREATE TABLE ohne Guard)"
  - "scripts/deploy/Install-IdealAkeWms.ps1:269-274, :362-369 (Klartext-Zugangsdaten), :371 (wirkungsloser Schalter)"
  - "IDEALAKEWMSService/Services/MailService.cs:78-82 (SMTP ohne TLS, Anmeldung unverschlüsselt möglich)"
source_backlog: "[[2026-11-14-sicherheit-anmeldung-und-installer]]"
weitere_quellen:
  - "[[2026-11-14-betriebshandbuch-goal-lauf]]"
  - "[[2026-11-04-ideal-produktiv-installation]]"
  - "[[2026-11-12-freshinstall-leere-db-fehler]]"
spec: "[[2026-11-14-sicherheit-anmeldung-und-installer-spec]]"
---

# Anmeldung ohne Kennwort, Erstinstallation, Installer (BH-1 bis BH-5)

## Symptom

1. **BH-1 (hoch):** Jeder aktive Benutzer ohne gespeichertes Kennwort meldet sich im Formular mit seinem WMS-Namen und **leerem**
   Kennwortfeld an. AD-Benutzer werden immer ohne Kennwort angelegt; damit ist für sie das Formular offen, an jedem Gerät, auch ohne
   Domäne. Die Anmeldeseite fordert selbst dazu auf („Leer lassen, wenn kein Passwort vergeben.“).
2. **BH-2 (hoch bei Neuinstallation):** `SQL/00_FreshInstall.sql` bricht auf einer leeren DB in Abschnitt „15. Indexes“ mit
   Meldung 207 „Ungültiger Spaltenname AssignedPickerId“ ab (IDEAL-Prod 2026-11-04; QA-Lauf 2026-11-12 zusätzlich Meldung 2714
   für `WarehouseRequisitions`/`WarehouseRequisitionItems`).
3. **BH-3:** Das Installationsskript schreibt übergebene SData- und SMTP-Zugangsdaten im Klartext in `appsettings*.json`.
4. **BH-4:** Der Installer-Schutz `Sync.ProductionOrdersEnabled = false` in der Dienst-`appsettings.json` wirkt nicht.
5. **BH-5:** `admin` hat nach jeder Neuinstallation ein leeres Kennwort; Sitzungs-Cookie ohne `Secure`; SMTP ohne TLS möglich.

## Reproduktion

1. BH-1: Als Admin einen lokalen Benutzer ohne Kennwort anlegen (`/Users/Create`, Feld leer) oder einen AD-Benutzer
   (`/Users/CreateAdUser`). Abmelden, im Formular den Namen eingeben, Kennwort leer, „Anmelden“ → angemeldet mit allen Rollen
   des Benutzers. Beleg: `AccountController.cs:92-101` prüft nur, dass das Feld leer ist.
2. BH-2: `00_FreshInstall.sql` auf einer leeren Instanz ausführen (Kopf `USE [IDEAL_AKE_WMS]` beachten, `:8-17`) → Meldung 207.
3. BH-3: `Install-IdealAkeWms.ps1 … -SageLagerbuchungCredential (Get-Credential) -SmtpCredential (Get-Credential)` → Kennwörter
   stehen lesbar in `<Web>\appsettings.Production.json` und `<Dienst>\appsettings.json`.
4. BH-4: Nach der Installation zeigt `/ServiceSettings` `Sync:ProductionOrdersEnabled = true`; der Dienst liest nur die DB
   (`IDEALAKEWMSService/Common/ServiceSettings.cs:19-36`, `Workers/SyncWorker.cs:43`).

## Root Cause

- BH-1: Die Regel „kein Hash = Anmeldung ohne Kennwort“ stammt aus dem Rollenkonzept (lokale Benutzer mit „Freipasswort“) und
  wurde bei der AD-Benutzer-Anlage (v1.23.0) nicht neu entschieden; die Design-Spec setzt „AD-User = PasswordHash NULL“ voraus
  und nimmt an, AD-Benutzer hätten keinen Passwort-Login (`docs/superpowers/specs/2026-07-09-windows-auth-ua-gate-design.md:31`).
  Der Formularweg prüft das nicht.
- BH-2: Nach der Auslagerung der Kommissionier-Felder in `ProductionOrderPickingStatus` (SQL/60, Abschnitt F.3 `:423-441`) blieben
  die alten Index-Zeilen im konsolidierten Abschnitt 15 stehen. Die Indizes gibt es an der neuen Tabelle schon (`:456-460`).
- BH-3/BH-4: Das Skript stammt aus der Zeit vor ADR 0024 bzw. vor DB-first (ADR 0008) und wurde nicht nachgezogen.
- BH-5: bewusste frühere Vereinfachungen ohne Entscheidung (siehe Spec).

## Fix / Verweis

Spec [[2026-11-14-sicherheit-anmeldung-und-installer-spec]] (Entwurf, Zielversion 1.99.24).
