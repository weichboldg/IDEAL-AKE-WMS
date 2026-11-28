---
type: backlog
created: 2026-11-14
quelle: "[[2026-11-14-wochenend-review-test-anwenderdoku]]"
status: neu
modul: IDEAL-Hierarchie, Werkbänke, Standorteinstellungen, Beschichtungsauftrag (Funde der DEV-Regression B3)
stand: "Bündel v1.99.22, 90bbbbd7, feature/2026-08-07-ideal-teile-1-5"
---
# Review-Befunde IDEAL-Hierarchie / Werkbänke / Standorteinstellungen (aus DEV-Regression B3)

Neue Befunde aus [[03-ideal-hierarchie-beschichtung-bde]]. Nichts behoben (Schranke 1). Format wie
[[2026-11-14-review-befunde-bde]].

## N-B3-1 – Name einer Sage-Werkbank per POST überschreibbar
- **Schwere:** niedrig
- **Fundstelle:** `IdealAkeWms/Controllers/ProductionWorkplacesController.cs:187` (`Edit` POST); Feld `Name` ist im Formular `readonly`,
  der Server schützt laut TS-96.8 b nur `ArbeitsschrittCode` und `SageArbeitsplatznummer`.
- **Befund:** IDEAL-DEV, Werkbank 65 (Sage-Nr. 140). POST mit `Name = "Manipuliert"`, `ArbeitsschrittCode = "XX"`,
  `SageArbeitsplatznummer = ""`: Kürzel und Sage-Nr. bleiben (EG/140), der **Name** wird gespeichert („Werkbank 'Manipuliert' wurde
  aktualisiert.“). Sage ist für Name führend; der Abgleich überschreibt ihn erst beim nächsten Lauf. Zurückgesetzt über dieselbe Maske.
- **Vorschlag:** Bei Sage-Werkbänken auch `Name` serverseitig aus dem Bestand übernehmen (wie Kürzel/Sage-Nr.); Test analog FA-a8.

## N-B3-2 – Erfolgsmeldung der Standorteinstellungen erscheint doppelt
- **Schwere:** Hinweis
- **Fundstelle:** `IdealAkeWms/Views/StandortEinstellungen/Index.cshtml:13-15` und `IdealAkeWms/Views/Shared/_Layout.cshtml:442-445`
  rendern beide `TempData["SuccessMessage"]` (Controller `StandortEinstellungenController.cs:111`).
- **Befund:** Nach „Speichern“ steht „Standorteinstellungen gespeichert.“ zweimal untereinander (3 Speichervorgänge, jedes Mal).
  Gleiches Muster wie WE-3 in [[2026-11-14-review-befunde-wareneingang]].
- **Vorschlag:** Block in `Index.cshtml` entfernen (Layout zeigt die Meldung bereits).

## N-B3-3 – IDEAL-DEV: Briefkopf-Pfad zeigt auf das Testsystem; Anschrift weicht von der Vorlage ab
- **Schwere:** Hinweis (Umgebung/Daten, kein Code)
- **Befund:** AppSetting `BriefkopfVorlagePfad` auf `IDEAL_WMS_DEV` = `C:\inetpub\wwwroot\IDEALAKEWMS_Test\Branding\briefkopf.html`
  (Pfad des Testsystems, auf dem DEV-PC nicht vorhanden) → Druck BA zeigt korrekt den gelben Hinweis „Briefkopf-Vorlage nicht
  gefunden … — Ersatz-Kopf.“. Mit dem Worktree-Pfad (`Branding\briefkopf.html`) kommen Logo, Fußzeile und Barcode, aber zwei Hinweise
  „CSS nicht gefunden: NimbusSanNov-Reg.otf / NimbusSanNov-SemBol.otf“ (Schriftdateien liegen nicht im Repo-Ordner `Branding`).
  Außerdem: Einstellung `Firmenanschrift` = „In der Schörihub **38**, **4840** Gmunden“, Fußzeile der Vorlage „In der Schörihub **28**,
  **4810** Gmunden“.
- **Prüffrage an Gerald:** Welche Anschrift ist richtig? Soll IDEAL-DEV einen eigenen Branding-Ordner (mit Schriften) bekommen?

## N-B3-4 – Menüpunkt „FA-Struktur“ auch bei AKE sichtbar (leere Seite)
- **Schwere:** Hinweis
- **Fundstelle:** `IdealAkeWms/Views/Shared/_Layout.cshtml:93` (`showFaHierarchy = canPick || canViewTracking || canManagePickingRelease
  || hasVorbauAccess`, kein Master-/Standort-Gate), `:143-147`.
- **Befund:** AKE-DEV (Master aus, 0 `FaHierarchyNodes`) zeigt in der Menüleiste „FA-Struktur“; die Seite heißt „FA-Struktur (IDEAL)“
  und meldet „Keine Strukturen gefunden.“. TS-58 verlangt kein Gate, für Anwender ist der Punkt bei AKE aber ohne Nutzen.
- **Vorschlag:** Menüpunkt an Master `ProduktionsauftragHierarchisch` (oder vorhandene Strukturen) koppeln; sonst bewusst so lassen
  und in der Anwender-Doku erwähnen.

## N-B3-6 – Altlast: zwei freigegebene Sub-FAs auf IDEAL-DEV
- **Schwere:** Hinweis (Daten)
- **Befund:** TS-79.16-Zählung auf `IDEAL_WMS_DEV` = 2 (`ProductionOrderPickingStatus` 101/103, FA 1035235 / Sub 1043421, 1043423,
  `ModifiedBy = admin`, 20.10.2026). Sie erscheinen wie erwartet **nicht** in `/Picking` (strukturell gefiltert). Der Bereinigungs-SQL aus
  TS-79.16 wurde nicht ausgeführt (nur Testsystem vorgesehen).
- **Vorschlag:** Bei Gelegenheit den TS-79.16-Lauf auf DEV ausführen, damit DEV dem Zielzustand entspricht.

## Sichtung 2026-11-28 (gegen main v1.99.89)

Erledigt: N-B3-4. Offen: N-B3-1, N-B3-2, N-B3-6 (Daten DEV). Frage offen: N-B3-3 (Briefkopf-Anschrift).
