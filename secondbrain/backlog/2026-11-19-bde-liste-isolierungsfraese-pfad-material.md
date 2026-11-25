---
typ: backlog
status: in-spec
created: 2026-11-19
herkunft: "Walter Kronberger (Mails, über Gerald 2026-11-19 20:43): BDE-Listen Bemerkung/Fertigungsinfo/Material; Isolierungsfräse (Clemens, Gerhard), Pfad je Artikel"
standort: IDEAL
art: feature
anhaenge:
  - anhaenge/2026-11-19-bde-liste-isolierungsfraese-pfad-material/WICAM-Pfade.xlsx
---
# BDE-Liste: Bemerkung/Fertigungsinfo/Material/Blechdicke, Isolierungsfräse mit Pfad und Lagerbestell-Teilen

## Wunsch (Walter, sinngemäß)
1. Bemerkung und Fertigungsinfo (geben AV/Konstruktion gezielt für die Produktion mit) in den BDE-Listen **einblendbar**; Material auch.
2. Isolierungsfräse: Heute filtert Clemens die Mails der **Lagerbestellungen**: Teile mit Pfad `…\Gerhard\Listen\…` (fix definierte
   Programme) druckt er als Liste für die Fräse; die übrigen (Variantenprogramme, Teile entstehen über den Matchcode im Schachtelprogramm)
   schachtelt er. Ziel: **bestellte Isolierungen mit Arbeitsgang „IF“ erscheinen in der BDE-Liste**, filterbar nach „IF“ und **Pfad** –
   der Pfad entscheidet, wer was bearbeitet. Clemens genügt eine Auflistung im BDE, ggf. Excel-Export.
3. **Material und Blechdicke** in der Fräsen-Ansicht und auch in den anderen BDE-Ansichten.
4. Den **Pfad je Artikel im WMS pflegen** (keine separate Datenbank). Fräse hat dann Lagerteile und auftragsbezogene Teile beisammen und
   kann rückmelden. (Frage an JOK, ob es weitere Optimierungen gibt.)
- Beispieldaten (Auszug aus Walters Datenbank): Matchcode → Pfad, z. B. `A-E-ISO-Lasto-V-Quer-LW-460-V02` →
  `Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU`, `A-SÄ-ISO-ZS-1000-1000-20-V01` → `Y:\Schäumerei\Trennschnitt_Variabel`,
  `A-VM-ISO-Glas-442-V01` → `Y:\Gerhard\Listen\Vormontage`, `E-FK-ISO-Boden-get-400-280-40-ISO30-V01` →
  `Y:\Schäumerei\E_FK_Flaschenkühler\FK4_FK6` (29 Zeilen in der Mail).

## Ist-Stand (Koordination, Bündel v1.99.44, nur gelesen)
- BDE-Teileliste **Einzeln** (`Views/BdeTeileliste/Index.cshtml`): Spalten Material (sichtbar, Z. 35/212), **Bemerkung PN** und
  **Fertigungsinfo PN** vorhanden, aber standardmäßig ausgeblendet (`defaultHidden`, Z. 221-222) → über das Zahnrad schon einblendbar.
  Quelle: FA-Struktur `FaHierarchyNode.BemerkungPN/FertigungsInfoPN/Material` (`Models/FaHierarchyNode.cs:42-48`).
- BDE-Teileliste **Summiert** (`Summiert.cshtml`): nur HauptFA, Matchcode, Anzahl, Sollmenge, FE-Termin, Abteilung, Status – **kein**
  Material/Bemerkung/Fertigungsinfo.
- **Blechdicke**: kein eigenes Feld in der FA-Struktur. Am Artikel gibt es das Sage-Merkmal `USER_OSMaterialnummerPN` (Werte wie „3,00“,
  Blechteile-Nachlesen v1.99.35) – ob das die Blechdicke ist: **ungeprüft**, Frage an Walter.
- **Pfad**: kein Feld. Artikelmerkmale gibt es schon (`Models/ArticleAttributeDefinition.cs`, Werte `ArticleAttributeValue`, Seite
  Stammdaten → Artikelmerkmale) – ein manuell gepflegtes Merkmal „Fräsen-Pfad“ wäre ohne neue Tabelle möglich (Pflegeweg, Import der
  Liste prüfen).
- **Lagerbestellungen** in der BDE-Liste: heute nicht – die BDE-Teileliste zeigt nur Positionen der FA-Struktur je Werkbank
  (Arbeitsschritte-Kürzel, z. B. IF = Werkbank 75). Lagerbestellungen (`WarehouseRequisitions`) haben keinen Arbeitsgang/keine Rückmeldung.

## Rückfragen
Mail an Walter vorbereitet 2026-11-19 20:47 (Hinweis: Bemerkung PN/Fertigungsinfo PN in „Einzeln“ über das Zahnrad einblendbar, Summiert fehlt – braucht ihr es dort?). Fragen (Empfehlung jeweils a):
1. Blechdicke = Sage „Materialnummer PN“ (z. B. „3,00“)? a ja / b anderes Sage-Feld / c im WMS pflegen
2. Pfad: a WMS-Artikelmerkmal „Fräsen-Pfad“ (Erstimport aus Walters Liste) / b Sage-Benutzerfeld, WMS liest mit / c nur kurzer Ordnerwert zur Auswahl
3. Lagerbestell-Isolierungen: a in der BDE-Liste der Werkbank IF neben FA-Teilen, fertig meldbar / b eigene Liste „Isolierungsfräse“ mit Pfad-Filter + Excel, ohne Rückmeldung / c erst nur Excel-Export
4. „Fertig melden“ Lagerteil: a Position gilt als gefertigt, keine Lagerbuchung / b Menge auf Lagerplatz einbuchen / c nur Info-Status
5. Erkennung Fräsen-Teil: a Arbeitsschritt „IF“ (USER_OSArbeitsschritte) / b Matchcode „-ISO-“ / c nur Pfad

## Antworten Walter (Mail, über Gerald 2026-11-19 22:07)
1. Blechdicke: **b – eigenes Sage-Feld „Blechdicke“**, Benjamin weiß Bescheid (genauer Feldname/USER_-Name **ungeprüft**, Prüfabfrage bzw. Benjamin).
   → **Feldname belegt (Koordination 2026-11-20 11:25):** `USER_OSBlechdicke`, Typ decimal (maxLength 8, nillable). Fundstelle: SData-`$schema`
   `CommonWawiServices/IDEAL;1` (Prod), in `FaBelegUserfield--type` und `FaBelegPositionUserfield--type` – also am FA-Kopf und an der
   FA-Position, neben `USER_OSMaterial`, `USER_OSMaterialnummerPN`, `USER_OSArbeitsschritte`. Gerald 11:22 bestätigt „USER_OSBlechdicke ist
   vorhanden“. Am **Artikel** ist das Feld wahrscheinlich ebenfalls vorhanden (gleiche OS-Feldfamilie), **ungeprüft** → im Dev-Lauf über die
   Einstellung `Sync:Userfeld:…` (ADR 0033, `SageUserfelder`) anbinden, nicht fest codieren; für die BDE-Liste aus der FA-Struktur bzw. dem
   Artikel lesen.
2. Pfad: **a – WMS-Artikelmerkmal „Fräsen-Pfad“**, einmalig aus Walters Liste übernommen, danach im WMS gepflegt. Zusatzwunsch: Datei
   **direkt aus dem WMS öffnen** und weiterbearbeiten (Hinweis Koordination: Browser öffnen `Y:\…`/`file://`-Links aus einer Web-Seite
   meist nicht – Spec muss den Weg klären, z. B. Pfad kopierbar + Hinweis, oder `file://` nur im Intranet-Browser mit Richtlinie).
3. Lagerbestell-Isolierungen: **a – Positionen mit Arbeitsschritt „IF“ in der BDE-Liste der Werkbank IF neben den Auftragsteilen, fertig meldbar.**
4. Fertig melden: **a – Position gilt als gefertigt, ohne Lagerbuchung**; „wie alle anderen Rückmeldungen in der BDE-Liste, IF ist als
   Arbeitsgang erledigt, Lagerbuchungen laufen ja sonst auch anders“.
5. Erkennung: **a – Arbeitsschritt „IF“ am Artikel (Sage-Feld Arbeitsschritte).**
- Offen (keine Antwort sichtbar): Bemerkung/Fertigungsinfo/Material auch in „Summiert“? → Spec mit Empfehlung (ja, ausblendbar).
