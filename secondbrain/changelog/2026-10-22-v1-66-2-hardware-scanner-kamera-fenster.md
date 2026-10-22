---
type: changelog
version: 1.66.2
date: 2026-10-22
---
# v1.66.2 — Hardware-Scanner: offenes Kamera-Fenster als Ziel auf allen Seiten mit mehreren Scan-Feldern

Nachbesserung zu [[2026-10-21-v1-66-1-hardware-scanner-nachbesserung]] laut Freigabe-Nachtrag 3 der Spec
[[2026-10-15-scan-handler-hardware-scanner-spec]]. Basis `1d78e7f5`, Commits `543edbe2`, `845e38c9` (qa). **Keine Migration**.

## Kernpunkte

- `data-hw-scan-kamera-ziel` an 21 weiteren Kamera-Knöpfen, insgesamt 22. Betroffen sind Inbound, InboundBulk, Outbound, Transfer,
  LocationTransfer, StockOverview, ProductionOrders, PickingLeitstand, OseonIndex und der Ziel-Lagerplatz der Stückliste. Ein
  Hardware-Scan bei offenem Fenster wirkt wie ein Kamera-Treffer.
- Seiten mit genau einem Scan-Feld bleiben bei Regel (d): Articles/Info, OutboundAll und die Bewegungsliste.
- Die Kamera-Knöpfe und die Kamera-Erfassung sind unverändert. Der Wächter-Test prüft die vollständige Liste Seite|Knopf und dass keine
  View einen eigenen `Html5Qrcode` baut, damit die Sperre aus v1.66.1 überall greift.

## Deploy-Risiken

Nur Web (Dienst nur wegen `AppVersion.cs`). Vorbestehend: Am OSEON-Artikelknopf hängen zwei Klick-Handler (QA Q3-2). Mit Kamera kann
deshalb das falsche Fenster offen bleiben. Bei Bedarf ist das ein Ein-Zeilen-Fix in `initScanner`.
