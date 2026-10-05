---
typ: feature
---
# Vormontage-Druckansicht (Teil 5) — Vorstufe fuer den PDF-Anschluss

Herausgeloest aus [[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]] (Freigabe-Antwort 9, 2026-09-13).
Der PDF-Baustein bedient Teil 3 (Kommissionierliste) und Teil 4 (Beschichtungsauftrag); die
Vormontage ([[2026-07-29-standort-ideal-teil-5-spec]], `FaHierarchyVormontageController`) hat **keine**
Bildschirmdruck-Action/-View — nur `Index` und `Summiert`. Eine `Print.cshtml` dafuer ist
Teil-5-Umfang, nicht PDF-Infrastruktur.

## Worum geht es

Sobald die Vormontage eine `Print.cshtml` hat (CSS eingebettet, Bilder als Data-URI — dasselbe Prinzip
wie Teil 3/4), ist der PDF-Knopf ein Anschluss von Minuten: `Pdf(int hauptFa)`-Action nach dem Muster
der beiden bestehenden Controller, `PdfVerfuegbar`-Flag ins ViewModel, ein `@if` in der Index-View.

## Vorfrage, die vor der Spec zu klaeren ist

Teil 5 hat `Index` **und** `Summiert`. **Welche der beiden wird gedruckt?** Das hat niemand
entschieden (Antwort 9 der PDF-Spec) — ohne diese Entscheidung keine Spec.

## Bezug

- Baustein: [[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]]
- Verbraucher-Vorbilder: `FaHierarchyKommissionierListenController.Pdf`, `FaHierarchyBeschichtungController.Pdf`
