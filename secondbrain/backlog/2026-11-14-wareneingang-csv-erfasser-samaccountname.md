---
type: backlog
status: offen
created: 2026-11-14
quelle: Gerald, Chat 2026-11-14 16:55
---
# Wareneingang-CSV: Erfasser als sAMAccountName statt Anzeigename

## Wunsch (Gerald)
- Für Windows-Benutzer den sAMAccountName verwenden, nicht den Anzeigenamen.
- Mindestens im CSV-Export Lieferschein (Wareneingang): sAMAccountName übergeben, wenn vorhanden – sonst den internen WMS-Benutzer.

## Ist-Stand (belegt, Worktree v1.99.23)
- Spalte `Erfasser` = `r.CreatedBy` – `IDEALAKEWMSService/Services/WareneingangCsvService.cs:139-141`.
- `CreatedBy` = Anzeigename des WMS-Benutzers (`CurrentUserService.GetDisplayName()`, `IdealAkeWms/Services/CurrentUserService.cs:43-53`).
- `PurchaseOrderReceipt` hat keine Benutzer-Id; die Lagerbewegung schon: `StockMovement.UserId` (`IdealAkeWms/Models/StockMovement.cs:37`), gesetzt beim Wareneingang (`PurchaseOrdersController.Receive.cs` um Zeile 139).
- sAMAccountName steht am Benutzer: `User.WindowsUserName = sam` beim AD-Anlegen (`UsersController.cs:203`).
- `CreatedByWindows` ist **nicht** geeignet: Windows-Identität des Geräts/der Sitzung (`DOMAIN\sam`, bei Formular-Anmeldung an einem Terminal ggf. das Terminal-Konto oder leer) – `CurrentUserService.cs:33-41`.

## Vorschlag
- Erfasser = `User.WindowsUserName` (über `StockMovement.UserId`), Domäne abgeschnitten; leer → `User.Name`; ohne UserId → `CreatedBy` (Altbestand).
- Kopfzeile `Erfasser` unverändert lassen (Abnehmer der CSV nicht brechen).
- Entschieden (Gerald 2026-11-16 16:50): **ohne Domäne** (`sam`); Leser ist enaio. Gerald 16:52: **nur in der CSV** – andere Ausgaben (Mail, Sage-Belege, Druck) bleiben unverändert, auch nicht auflisten.

## Ergänzung Gerald 2026-11-16 16:31
- **CSV ohne Spaltenüberschriften** (Kopfzeile `Barcode;Kreditornummer;…` entfällt, `WareneingangCsvService.cs:139`); Leser ist **enaio**.
- Zusammen mit dem Erfasser-Wechsel in einer kleinen Spec umsetzen (beides ändert das Dateiformat für enaio → einmal abstimmen, einmal testen).
