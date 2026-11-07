---
typ: feature
# epic: true   # vermutlich zwei bis drei Etappen – beim Umzug nach backlog/ entscheiden
---
# Lager-Tools: FA als eigenes Feld, Verlinkung auf den FA, FA-Auswahl; Bestandsübersicht Artikel-Scan ohne FA

**Aufgenommen 2026-11-07** (Gerald, über die Koordination; Denkraum – noch nicht im Backlog).

Gerald: „Hier [Bestandsübersicht] haben wir 2 QR-Code-Scans, 1× Artikel und 1× FA. Je nachdem. In den Lager-Tools (z. B. ein-, ausbuchen …)
eventuell eine Verbesserung planen: der FA soll vom Bemerkungs-/Notiz-Feld losgelöst werden. Wenn ein FA gefunden wird, soll das eine
Verlinkung auf den FA ermöglichen. FA-Liste mit Filter eventuell im ersten Step?“

## Worum geht es
1. **Bestandsübersicht:** Es gibt zwei Scan-Knöpfe (Artikel, FA). Der Artikel-Knopf soll nur den Artikel filtern, der FA-Knopf den FA –
   unabhängig von `QrMitFaNummer` (heute übernimmt der Artikel-Knopf die FA aus dem QR, `StockOverview/Index.cshtml:18`; die Spec
   [[2026-11-07-bestandsuebersicht-qr-fa-uebernahme-fest-spec]] bindet das nur an `QrMitFaNummer`, das an beiden Standorten an ist → keine
   sichtbare Änderung). Vorschlag: in der Bestandsübersicht Artikel-QR nie → FA.
2. **Ein-/Aus-/Umbuchung:** FA als eigenes, strukturiertes Feld statt im Bemerkungs-/Notizfeld. Ungeprüft: wie der FA heute gespeichert und
   angezeigt wird (`StockMovement` hat laut SData-Spec B2 ein `ProductionOrder`-Feld, `StockMovementsController.cs:427-443`) – beim /spec mit
   Datei:Zeile belegen, inkl. Altdaten (FA-Nummern, die nur in Bemerkungen stehen → Migration/Erkennung?).
3. **FA prüfen und verlinken:** gescannter/eingegebener FA wird gegen die FAs im WMS geprüft (Nummer, Bezeichnung, Status; Warnung bei
   unbekannt/beendet) und überall als Link auf FA-Details/FA-Struktur angezeigt: Buchungsmaske, Bewegungshistorie, Bestandsübersicht.
4. **FA-Auswahl:** Auswahl aus offenen FAs (Suche/Autocomplete, Filter z. B. Werkbank/Status) statt Freitext (Gedanke ADR 0020: kein
   Tippfehler). Optional als erster Schritt: „Buchen für FA“ – FA aus gefilterter Liste wählen, dann Artikel buchen mit vorbelegtem FA.

## Weitere Vorschläge der Koordination
- **Grundlage für Sage:** Entnahmen mit FA-Bezug gehen später über das Rückmelde-Epic nach Sage ([[2026-11-02-sage-pps-fa-bde-abgleich-spec]];
  heute bewusst ausgeschlossen, SData-Spec F9/B2). Dafür braucht es den FA sauber im Feld, nicht im Text.
- **Auswertung:** Bewegungen/Materialverbrauch je FA (Liste mit Filter, CSV-Export) – Bezug [[2026-10-30-sage-sdata-fa-persistenz]].
- **QR-Etikett:** Bei Artikel-Scan mit FA im QR in Buchungsmasken den FA vorschlagen, aber als eigenes Feld sichtbar und änderbar.

## Entscheidungen Gerald 2026-11-07 (verbindlich für die Spec)
1. **Umfang:** alle Lager-Tools **außer Kommissionierung** – Ein-, Aus-, Umbuchung, Sammel-Wareneingang, dazu Bewegungshistorie und
   Bestandsübersicht (Anzeige/Link). Vollständige Liste der Masken beim /spec mit Datei:Zeile belegen.
2. **Altdaten:** einmalig erkennen und übernehmen – FA-Nummern per Muster aus Bemerkungen ziehen, nur übernehmen, wenn der FA im WMS
   existiert (eindeutiger Treffer); Bemerkung bleibt unverändert; Ergebnis protokollieren (übernommen / mehrdeutig / unbekannt), vorher
   Trockenlauf mit Zählung.
3. **Pflicht:** standardmäßig optional (mit Prüfung und Warnung bei unbekanntem/beendetem FA); zusätzlich Einstellung je Standort nach
   ADR 0020, die den FA für bestimmte Buchungsarten (mindestens Entnahme/Warenausgang in die Fertigung) zur Pflicht macht. Default: aus.

## Reif für den Backlog?
Wenn ja: nach ../backlog/ verschieben (startet die Spec-Kette) – Kandidat fürs nächste Bündel mit [[2026-11-07-artikeldetails-lieferanten-anzeigen]].
