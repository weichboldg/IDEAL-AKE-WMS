---
typ: feature
# epic: true
---
# Qualitätsmasken: Wareneingang, Warenausgang (Versand), Endprüfung – Fragebogen, Fotodokumentation, Übergabe an enaio

**Aufgenommen 2026-11-07** (Gerald): „Qualitätsmasken (Wareneingang, Warenausgang (Versand), Endprüfungen) – Fragebogen,
Fotodokumentation, Ergebnisse an enaio übergeben (dateibasiert).“

## Worum geht es
- Prüfvorlagen (Fragebogen) je Prüfart und ggf. je Artikelgruppe/Gerätetyp: Fragen ja/nein, Messwert mit Grenzen, Auswahl, Freitext,
  Pflicht-Foto.
- Prüfung am Tablet/Handy durchführen: Bezug Bestellung/Wareneingang, FA/Gerät (Endprüfung), Lieferschein/Versand; Fotos aufnehmen,
  Ergebnis (i. O./n. i. O.), Prüfer, Zeit.
- Ergebnis als Prüfprotokoll (PDF) + Fotos **dateibasiert an enaio** übergeben (Ablageordner mit Index-/Metadatei, die enaio importiert).

## Bezüge
- enaio ist das ECM/DMS (Produktdatenbank), heute mit Dokumentexport per VBScript in den Shopware-Webshop – die Import-Gegenrichtung
  (Datei + Indexdaten → enaio) ist zu klären.
- Wareneingang aus Bestellungen: [[2026-11-07-wareneingang-aus-sage-bestellungen]]; Versand: [[2026-11-07-versandmasken-geraete-fas]].
- PDF-Erzeugung gibt es im WMS bereits (FA-Struktur-Druck, Briefkopf/Fußzeile).

## Offene Fragen / Unklarheiten
- enaio-Import: welches Format (Ordner + XML/CSV-Index, Hotfolder)? Welche Indexfelder (Artikel, FA, Seriennummer, Lieferant, Datum,
  Prüfart)? Wer liefert die Spezifikation (enaio-Betreuer)?
- Wer pflegt die Fragebögen (Qualitätssicherung im WMS selbst)? Versionierung der Vorlagen?
- Seriennummern je Gerät vorhanden?
- n. i. O.: Folgeprozess (Sperrlager, Reklamation an Lieferant, Nacharbeit)?
- Fotos: Größe/Komprimierung, Speicherort bis zur Übergabe, Löschen nach erfolgreichem Import?

## Recherche Koordination 2026-11-07
**Sage**
- Sage hat **keine** Prüf-/QS-Ressourcen im Lesevertrag; Bezugsdaten kommen aus EK-/VK-Belegen (siehe Wareneingang/Versand) und FAs.
- Sage-DMS-Services (CommonWawiServices, [[sage-sdata-commonwawiservices]] „DMS“): `DMSGetDocumentTypesService`,
  `DMSGetInsertDocumentInfoService` (Indexe je Beleg), **`DMSRegisterExternalDocumentsService`** (Datei als Base64 + DocumentInfo ins an Sage
  angebundene DMS-Archiv). Ungetestet. **Frage:** Ist enaio als DMS an Sage angebunden? Dann könnte das Prüfprotokoll **über Sage** mit
  Beleg-Indexen (Bestellung, Lieferschein) in enaio landen – ohne eigene enaio-Anbindung.
**enaio (OPTIMAL SYSTEMS) – drei Wege**
1. **Dateibasiert (Geralds Vorgabe):** enaio „Automatische Aktionen“ (enaio utilities) – Import aus einem Verzeichnis mit Index-Datei
   (Indexdaten + Dateiverweis) in einen Dokumenttyp. Format und Felder legt der enaio-Administrator in der Import-Aktion fest. Online-Doku
   (help.optimal-systems.com) für Details nicht abrufbar (robots) → vom enaio-Betreuer die Importdefinition anfordern.
2. **REST, AppConnector (OSREST):** `POST /osrest/api/documents/insert/<locationId>` (multipart, Metadaten + Datei).
3. **REST, DMS Service:** `POST /api/dms/objects` (multipart: Teil `data` mit JSON `objects[].properties` (`system:objectTypeId`,
   `system:parentId`, Indexfelder) + `contentStreams[].cid`, Datei als Teil gleichen Namens) → 201 Created.
**Empfehlung:** Dateibasiert starten (wie gewünscht, entkoppelt, enaio-Ausfall stoppt das WMS nicht): Das WMS schreibt je Prüfung PDF +
Fotos + Index-Datei (Format nach Vorgabe der Import-Aktion) in einen Übergabeordner, enaio importiert; Erfolg prüfen (Datei verschwindet/
Fehlerordner) und Status in der Prüfung führen. REST später, wenn Rückmeldung (Dokument-ID/Link aus enaio im WMS) gebraucht wird.
**Einbau im WMS (Vorschlag)**
- Tabellen: `QsVorlagen` (Prüfart, Gültigkeit Artikelgruppe/Gerätetyp, Version), `QsFragen` (Typ ja/nein, Messwert mit Grenzen, Auswahl,
  Freitext, Foto-Pflicht), `QsPruefungen` (Bezug: EK-Position/Wareneingang, FA/Seriennr., VK-Beleg; Prüfer, Zeit, Ergebnis, enaio-Status),
  `QsAntworten`, `QsFotos` (Datei im Dateisystem, nicht in der DB).
- Masken mobil (Tablet/Handy, Kamera), Vorlagen-Editor für QS, PDF-Protokoll (bestehende PDF-Erzeugung), Dienst-Lauf „enaio-Übergabe“
  mit Wiederholung und Fehlerprotokoll (Muster Outbox wie Sage-Übergabe, ADR 0020 für Schalter/Pfade).

## Entscheidung Gerald 2026-11-07 16:09 – enaio-Übergabe
- **Dateibasiert über eine Ordnerfreigabe auf dem enaio-Server**, darin **je QM-Typ ein Unterordner** (z. B. `Wareneingang`, `Warenausgang`,
  `Endpruefung`). Je Prüfung: **CSV + Dokumente** (Bilder, PDF) ablegen.
- **CSV-Aufbau definieren wir selbst**, z. B. Kundenauftrag, AB-Nummer, FA-Nummer, Bild-/Dokumentenname. Vorschlag für die Spec: eine Zeile je
  Datei; Spalten u. a. `QmTyp`, `PruefungId`, `Datum`, `Pruefer`, `Ergebnis`, `Kundenauftrag`, `AbNummer`, `FaNummer`, `Seriennummer`,
  `Artikelnummer`, `Lieferant`/`Bestellnummer` (Wareneingang), `Dateiname`, `Dokumentart` (Foto/Protokoll); UTF-8, Trennzeichen `;`;
  CSV zuletzt schreiben (erst wenn alle Dateien da sind) bzw. als `.tmp` schreiben und umbenennen, damit enaio keine halben Sätze importiert.
- Offen: Freigabepfad und Dienstkonto mit Schreibrecht; was enaio nach dem Import mit den Dateien macht (verschieben/löschen) – davon hängt ab,
  wie das WMS den Erfolg erkennt; die Import-Aktion in enaio richtet euer enaio-Betreuer nach unserem CSV ein.

## Reif für den Backlog?
Vermutlich Epic; zuerst enaio-Importweg klären.
