---
type: bug
title: Kommissionierfotos ohne Anmeldung abrufbar; Foto-Löschen ohne FA-Prüfung
status: offen
severity: hoch
created: 2026-11-08
affected_code:
  - "IdealAkeWms/Program.cs:432-434 (Foto-Ordner unter wwwroot)"
  - "IdealAkeWms/Program.cs:462-470 (Login-Ausnahme path.Contains(\".\"))"
  - "IdealAkeWms/Program.cs:488 (UseStaticFiles liefert wwwroot/Fotos aus)"
  - "IdealAkeWms/Controllers/Api/PhotoController.cs:81-97 (Delete nur mit fileName, ohne FA-Bezug)"
  - "IdealAkeWms/wwwroot/js/photo-upload.js:116-129 (deletePhoto sendet nur fileName)"
source_backlog: "[[2026-11-08-kommissionierfotos-ohne-anmeldung-abrufbar]]"
spec: "[[2026-11-08-kommissionierfotos-ohne-anmeldung-abrufbar-spec]]"
---

## Symptom
Kommissionierfotos lassen sich ohne Anmeldung über `/Fotos/Kommissionierung/<datei>.jpg` abrufen. Außerdem lässt sich ein beliebiges Foto über `POST /api/photos/delete` löschen, auch eines eines anderen FA.

## Reproduktion
Nachgestellt am 2026-11-08 auf AKE-DEV (`localhost:8081`, Bündel v1.94.0; der betroffene Code ist laut `git diff` gleich wie in main). Testdatei `D:\Dev\AKE\Web\wwwroot\Fotos\Kommissionierung\REPRO-TEST_20261108000000_1.jpg`. curl ohne Anmeldung:
- `/Fotos/Kommissionierung/REPRO-TEST_20261108000000_1.jpg` liefert **200 image/jpeg**.
- `/` liefert 302 auf `/Account/Login` (die Anmeldepflicht greift sonst).
- `/Home/Index.x` liefert 404 statt 302: Jeder Pfad mit Punkt umgeht die Login-Umleitung.

Details stehen in der Spec, Abschnitt „Nachgestellt“.

## Root Cause
- Die Login-Middleware nimmt jeden Pfad mit `.` aus (`Program.cs:470`). Danach liefert `UseStaticFiles` (`:488`) alles unter `wwwroot` aus, also auch `wwwroot/Fotos/Kommissionierung` (`:433`).
- `PhotoController.Delete` (`PhotoController.cs:82-96`) bekommt nur `fileName`. Es fehlt jede Prüfung, zu welchem FA die Datei gehört. Der Rollenfilter `[RequirePickingAccess]` (`:9`) prüft nur das Recht Kommissionierung.

## Fix / Verweis
[[2026-11-08-kommissionierfotos-ohne-anmeldung-abrufbar-spec]] (Hotfix v1.30.4 aus main)
