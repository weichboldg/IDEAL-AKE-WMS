---
typ: backlog
status: erledigt
created: 2026-11-19
herkunft: "Gerald 2026-11-19 19:44: Dashboard aktualisieren, passende Kacheln für die neuen Features in passenden Kategorien, Kacheln nach Berechtigung"
standort: beide
art: feature
---
# Dashboard: Kacheln für die neuen Features, nach Kategorie und Berechtigung

## Wunsch (Gerald)
- Dashboard aktualisieren: Kacheln für die neuen Funktionen, einsortiert in passende Kategorien.
- Jede Kachel nur zeigen, wenn der Benutzer die Berechtigung hat (und das Modul aktiv ist).

## Ist-Stand (Koordination, Bündel v1.99.44, nur gelesen)
- `IdealAkeWms/Views/Home/Index.cshtml` (281 Z.): Gruppen Lager (Einbuchung, Ausbuchung, Bestände, Bewegungen, Kommissionierung,
  Fertigungsaufträge – alle unter `canPick`, Z. 18-105), Leitstand (Z. 115), Teileverfolgung OSEON (Z. 137), BDE Terminal/Cockpit/
  Buchungen (Z. 159-210), OSEON AG-Übersicht (Z. 214), Stammdaten Artikel/Lagerplätze/Benutzer/Arbeitsplätze (Z. 239-280).
- Rechte/Schalter kommen per ViewBag aus `Controllers/HomeController.cs:20-41` (CanPick, CanViewTracking, CanManagePickingRelease,
  HasMasterData(Read)Access, CanUseBde, CanManageBdeShiftlead, CanReportOperations; Module Teileverfolgung, Leitstand, BDE).
- **Fehlen** u. a.: FA-Struktur/Vormontage, IDEAL-Kommissionierliste (Summiert), Beschichtung (Vorschlagsliste, Aufträge),
  BF-Warenkorb/BF-Aufträge (Blechfertigung), BDE-Teileliste/Werkbank, Lager-/Glasbestellungen, Bedarfsmeldungen, Wareneingang zu
  Bestellung, Offene Bestellungen, Bestellvorschläge/Einkauf, Fehlteile, Sage-AG-Anlage – jeweils mit eigenem Modulschalter/Recht.

## Hinweise für die Spec
- Bedingungen je Kachel **dieselben wie im Menü** (`Views/Shared/_Layout.cshtml`, z. B. `bestellungenAktiv`, `BlechfertigungAktiv`,
  Kommissionierlisten-Modul, Beschichtung je Standort, Rollen beschichtungsauftrag/_schreiben, schachtler, einkauf) – eine Quelle,
  am besten ein gemeinsames Menü-/Kachel-Modell statt doppelter `@if`-Ketten (sonst driften Menü und Dashboard, vgl. Bugfix
  Empfängergruppen-Menü v1.99.44).
- Kategorien-Vorschlag: Lager · Kommissionierung · Produktion/BDE · Blechfertigung · Beschichtung · Einkauf/Wareneingang · Stammdaten · Administration.
- Standortunterschiede AKE/IDEAL beachten; Leere Kategorien ausblenden; Kontrast/Trefferflächen wie Nachtrag v1.99.36.
- Ohne Rückfrage spezifizierbar.
