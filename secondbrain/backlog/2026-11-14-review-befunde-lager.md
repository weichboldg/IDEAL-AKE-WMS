---
type: backlog
created: 2026-11-14
status: offen
quelle: "[[2026-11-14-wochenend-review-test-anwenderdoku]]"
---
# Review-Befunde Lager (Ein-/Aus-/Umbuchen) – aus der Anwender-Doku C3 (2026-11-14)

- **L-1 (niedrig):** „Lagerplatz ausbuchen“ bietet keine Kommissionierwagen an – der Controller lädt die Plätze mit
  `GetActiveOrderedExcludingPickingTransportAsync` (`IdealAkeWms/Controllers/StockMovementsController.cs`, OutboundAll, ca. Z. 487). Die Hilfe
  nennt den Fall aber als „praktisch für Kommissionierwagen-Rückgabe“; erreichbar ist ein Wagen nur über „Kommissionierwagen belegt“ in der
  Stückliste oder per URL. „Lagerplatz umbuchen“ zeigt alle Plätze. *Vorschlag:* Wagen in der Auswahl zulassen oder Hilfe korrigieren (Entscheidung Gerald).
- **L-2 (Hinweis, Hilfe veraltet):** Hilfe „Lagerplatz ausbuchen/umbuchen“ sagt „nur Lager-Keyuser und Admin“, der Code erlaubt auch Kommissionierer
  (`CanTransferStockAsync`: Admin, Lager Keyuser, Kommissionierer). *Frage an Gerald:* gewollt? Danach Hilfe oder Code anpassen.
- **L-3 (Hinweis, Hilfe veraltet):** Hilfe „Tabellenansicht anpassen → Verfügbar in“ nennt nur vier Listen; seit v1.65 haben fast alle Listen das Zahnrad.
- **L-4 (Frage):** Ausbuchung startet mit Menge 0, Einbuchung mit 1 – gewollt?

Kein Befund: Mehrfach-Einbuchung erhöht beim zweiten Scan die Menge (gewollt, Haken „Bei Scan immer neue Zeile erzwingen“).

## Sichtung 2026-11-28 (gegen main v1.99.89)

Teilweise: L-2 (Views/Help/Index.cshtml:58,68 veraltet). Offen: L-3. Frage offen: L-1, L-4.
