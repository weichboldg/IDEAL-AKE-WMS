---
typ: aufraeumen
status: offen
created: 2026-11-25
herkunft: "Gerald 2026-11-25 im Design-Gespräch BDE-Masken (Rückfrage 2)"
standort: beide
---
# AppSetting `BdeNurFaMeldung` entfernen (Altlast)

Bezug: [[2026-11-25-bde-masken-vereinheitlichung-spec]] (Rückfrage 2).

- Gerald 2026-11-25: Einstellung steht produktiv auf `false` und wird nicht mehr benötigt.
- Im Code noch an 12 Stellen gelesen (Bündel-Worktree, Stand 2026-11-25), u. a. `Services/BdeBookingService.cs:287,522,543`,
  `Controllers/BdeApiController.cs:90,122,213`, `Controllers/BdeTerminalController.cs:61,84`, `Controllers/BdeTeilelisteController.cs:69`,
  `Services/BdeTeilelisteService.cs:260`, `Services/WerkbankKuerzelFamilie.cs:45-53`, `Controllers/ProductionWorkplacesController.cs:274`,
  `Controllers/SettingsController.cs:131`, Katalog `Models/AppSettingDefinitions.cs:221-225`, `AppSettingKeys.cs:10`, Hilfe `Views/Help/Index.cshtml:2004`.
- Entfernen = alle Zweige mit `nurFa == true` streichen, Katalog-Eintrag + abhängige Einstellung (`abhaengig:`) prüfen, DB-Zeile per SQL-Skript löschen,
  Hilfe/README nachziehen. Erst **nach** dem BDE-Masken-Epic (dort wird die Einstellung bereits ignoriert), um Konflikte zu vermeiden.
