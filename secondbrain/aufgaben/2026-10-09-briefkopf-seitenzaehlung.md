---
typ: aufgabe
status: offen
erstellt: 2026-10-09
---
# Briefkopf: „Seite X von Y" je HauptFA-Dokument

Aus [[2026-10-06-beschichtungsauftrag-ausbau-teil-2-spec]] (Anforderung 9 „nice to have", AK 14), im Dev-Lauf v1.49.0
bewusst weggelassen.

**Warum nicht gebaut:** CSS-Randfelder (`@page { @bottom-right { content: counter(page) " / " counter(pages) } }`) sind
billig, zählen aber im **Bildschirmdruck** mehrerer HauptFA-Dokumente über den ganzen Druck — Chromium setzt den
Seitenzähler nicht je `.fa-group` zurück. Korrekt je Dokument wäre es nur im **PDF** (eine Gruppe je Datei,
`PdfSummiert`/`Pdf`). AK 14 verlangt „je Dokument korrekt (PDF **und** Bildschirmdruck) oder gar nicht".

**Optionen, falls gewünscht:**
1. Nur im PDF: CSS-Randfelder per Klasse nur beim PDF-Rendern aktivieren (eigenes Flag im Druck-ViewModel); vorher an
   der Edge-Version des Servers prüfen, ob `@page`-Randfelder mit `--no-pdf-header-footer` gerendert werden.
2. Edge über das DevTools-Protokoll (`Page.printToPDF` mit `footerTemplate`) — Umbau von `EdgeProcessRunner`, teurer.
3. Bildschirmdruck je HauptFA einzeln öffnen statt Sammeldruck.

Entscheidung beim Menschen; bis dahin keine Seitenzählung.
