---
typ: backlog
status: in-spec
created: 2026-11-19
herkunft: "Gerald 2026-11-19 20:10: Blechfertigung – im Warenkorb gibt es bei der Artikelsuche keine Type-ahead-Suche"
standort: IDEAL
art: bugfix
---
# Bugfix: BF-Warenkorb – Artikelsuche ohne Type-ahead

## Befund (Koordination, Bündel v1.99.44, nur gelesen)
- `IdealAkeWms/Views/BfAuftraege/Details.cshtml:63-80`: Artikelsuche ist ein Suchfeld + Knopf „Suchen“ (Server-Suche, Kommentar Z. 63
  „kein eigenes JS“), Treffer erst nach Absenden; Filter nur Sage-Lagerartikel mit Artikeltyp BG/FT.
- Andere Masken haben Vorschläge beim Tippen über `Views/Shared/_Select2ArticlePartial.cshtml` (Select2 + `GET api/articles/search`,
  `Controllers/ArticlesApiController.cs:21-22`), z. B. Beschichtungsauftrag (`Views/Beschichtungsauftraege/Details.cshtml:439`),
  Vorschlagsliste Beschichtung, FA-Vervollständigung.

## Vorschlag
- Type-ahead im BF-Warenkorb über denselben Baustein (`_Select2ArticlePartial` / `api/articles/search`), aber **mit dem BF-Filter**
  (Lagerartikel + Artikeltyp BG/FT aus den Sage-Benutzerfeldern, `SageUserfelder`) – entweder neuer Parameter am Such-Endpunkt oder
  eigener schmaler Endpunkt unter `api/bf-auftraege` (`Controllers/Api/BfAuftraegeApiController.cs`). Keine zweite Suchlogik: der
  Filter muss derselbe sein wie die heutige Server-Suche.
- Scanner (`data-hw-scan-ziel`, Z. 70) und Enter → Hinzufügen müssen weiter funktionieren; ohne JS bleibt die Server-Suche.
- Ohne Rückfrage spezifizierbar; kleiner Fix für den nächsten Sammellauf.
