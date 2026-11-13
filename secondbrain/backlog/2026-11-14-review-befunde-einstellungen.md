---
type: backlog
created: 2026-11-14
quelle: "[[2026-11-14-wochenend-review-test-anwenderdoku]]"
status: neu
modul: Einstellungen-Katalog (ServiceSettingDefinitions/AppSettingDefinitions, ADR 0020) + Sammelzeile Module ohne Befund (Teil A3)
stand: "Bündel v1.99.22, 90bbbbd7, feature/2026-08-07-ideal-teile-1-5"
---
# Review-Befunde Einstellungen-Katalog (Teil A3, lesend)

Gegenstand: `git diff main...HEAD` im Bündel-Worktree. Nichts behoben (Schranke 1). Bezug:
[[0020-einstellungen-mit-festem-wertebereich-als-auswahl-und-mit-wirkungsbeschreibung]], [[0008-servicesettings-db-first-mit-typisiertem-katalog]].
Geheim-Typ und `/ServiceSettings`-Freitextweg siehe [[2026-11-14-review-befunde-sdata]] (SD-3).

## EI-1 – Fehlender Anzeigename fällt still auf den Key zurück, kein Wächter
- **Schwere:** Hinweis
- **Fundstelle:** `IdealAkeWms/Models/ServiceSettingDefinitions.cs:188` (Fallback `Name: d.Key, Verwendung: "Dienst"`)
- **Befund:** ADR 0020 verlangt je Einstellung Anzeigename, Wirkung, Verwendung, Standort. Heute hat jeder Katalog-Key einen `Meta`-Eintrag
  (gegengeprüft, auch `SData:Username/Password` über Konstanten), aber `ServiceSettingDefinitionsTests` prüft das nicht; ein neuer Key ohne
  `Meta` erscheint in `/ServiceSettings` still mit technischem Namen und „Dienst“.
- **Vorschlag:** Test „jeder Katalog-Key hat einen Meta-Eintrag (und umgekehrt)“.
- **DEV-Nachmessung 2026-11-14:** nicht messbar – in `/ServiceSettings` (AKE-DEV) kein Feld mit technischem Namen statt Anzeigename; Serverprüfung `/Settings` (99 bzw. erfundener Key) wirkt, nichts gespeichert. ([[04-uebrige-module]])

## Geprüft ohne Befund
- Katalog-Drift: alle per String-Literal gelesenen Dienst-Keys (`ServiceSettings.Get*Async(…, "…")`, 58 Keys) stehen im Katalog; Drift-Test
  `All_ContainsDocumentedServiceReadKey` grün. `SaveSettings` prüft alle Katalog-Keys vor dem ersten Schreiben (alles oder nichts), Choice
  ordinal, Muster-Strings getrimmt.

## Sammelzeile: Module ohne Befund (Teil A3)
**Vormontage Abteilung (v1.99.3)** (`VormontageService.FormatAbteilung`, „Kombigerät“ statt stiller Wahl, Spaltenfilter je Position vor dem
Summieren, Spaltenkatalog und colspan nachgezogen) · **System-Kürzel/Sage-Kennung (v1.99.9)** (`SageSystemKuerzel`: Buchstabe vorne verhindert
LIKE-Überschneidung, ungültiger DB-Wert bricht den Takt ab statt ohne Kürzel zu senden) · **Spaltenfilter-Kern** (`ColumnFilterSyntax.cs` ⇄
`column-filter-syntax.js` Zeile für Zeile parallel: Split mit Anführungszeichen, Operatoren, Datum/Zahl, Negation; SQL-Bau über Konstanten,
EF maskiert) · **Hardware-Scanner/QR** (Passwortfelder ausgenommen, `/api/articles/by-matchcode` lesend wie `search`) · **Sage-Schreibwege**
(nur zwei POST-Stellen, beide hinter Schaltern; Details [[2026-11-14-review-befunde-sdata]] SD-6) · **Migrations-/SQL-Disziplin** der
Bündel-Migrationen (jede MigrationId in `SQL/NN_*.sql` und `00_FreshInstall.sql`; Manifest-Drift-Test grün).

## Tippfehler (nur gemeldet)
- `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs:440,466` („Fertigungmenge“/„Summe Fertigungmenge“), `IdealAkeWms/Views/FaHierarchyVormontage/Index.cshtml:176` und
  `IdealAkeWms/Views/FaHierarchyVormontage/Summiert.cshtml:133` (+ Spaltenköpfe): vermutlich „Fertigungsmenge“. Der Spaltenname
  `Fertigungmenge` stammt aus dem Modell/der Tabelle `FaHierarchy` (`SQL/89_AddFaHierarchy.sql:43`) – nur die Oberflächenbeschriftung wäre zu
  korrigieren, falls die Sage-Quelle nicht selbst so heißt (Prüfung Mensch).

## Nachtrag aus der DEV-Regression B4 ([[04-uebrige-module]])

### N-B4-1 – Hardware-Scanner-Teile erscheinen bei ausgeschaltetem Standort-Schalter
- **Schwere:** niedrig
- **Fundstelle:** `IdealAkeWms/Views/Shared/_Layout.cshtml:18-19,33`; Leser `IdealAkeWms/Views/Account/Profile.cshtml:206`,
  `IdealAkeWms/wwwroot/js/barcode-scanner.js:113` (`hasAttribute('data-hw-scan-praefix')`)
- **Befund:** Razor rendert `data-*`-Attribute auch bei `null` (leerer Wert); AKE-DEV mit Schalter aus: `<body data-hw-scan-praefix="" …>`.
  „Mein Profil“ zeigt deshalb die Karte „Dieses Gerät – Hardware-Scanner auf diesem Gerät verwenden“, das Kamera-Fenster das Feld „Handscanner“,
  obwohl `hardware-scanner.js` nicht geladen ist. TS-99.1a/101.16 („`<body>` ohne Attribut“) nicht erfüllt. ![[N-B4-1-hardware-scanner-karte-im-profil-bei-schalter-aus.jpg]]
- **Vorschlag:** Leser auf nicht leeren Wert prüfen oder Attribute im Layout nur bei aktivem Schalter ausgeben; Wächter-Test „Layout bei
  Schalter aus ohne `data-hw-scan-praefix=`“.
