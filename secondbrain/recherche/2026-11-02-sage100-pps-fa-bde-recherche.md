---
typ: recherche
datum: 2026-11-02
thema: Sage 100 Produktion (PPS) – Fertigungsaufträge, Baugruppen, BDE – Anbindung IDEAL AKE WMS
status: ungeprüft gegen Echtbestand
---

# Sage 100 Produktion (PPS): Fertigungsaufträge, Baugruppen und BDE – Recherche für das IDEAL AKE WMS

Kennzeichnung: **[B]** belegt durch Quelle · **[A]** Annahme/Empfehlung · **[U]** ungeprüft, vor Ort verifizieren.
Quellen: Sage-Onlinehilfe (OL 2014 / Sage 100 8.0), DPS-Projekthandbuch „Handbuch AKE_Ideal_Produktion“ (2023, Seitenangaben „S. …“), sage-forum.de, Partner-Doku (TerraCom, desk-firm, IAS, abacus), älteres Office-Line-„Anhang Datenbankschema“ (nur Snippets). Quellenliste am Ende.

## Kernaussagen
- Mehrstufige FAs: Sage legt für Unterbaugruppen eigene Unter-FAs an (FA-Anlage mit „Unterbaugruppen erzeugen“, Artikel „Als Unterbaugruppe = Ja“), verknüpft über „Verursacher“ und Baumstruktur. WMS muss die Hierarchie lesen und Unter-FAs vor Haupt-FA fertigmelden. [B]/[A]
- Schreibweg: Rückmeldungen nur über die Sage-Rückmelde-Engine bzw. .NET-Assemblies `Sagede.OfficeLine.Pps.*`, nie per SQL-INSERT. Die SData-/REST-Schnittstelle deckt Produktionsrückmeldungen laut TerraCom nicht ab → eigene .NET-4.8-x86-Komponente („SageBridge“) nötig. [B]/[A]
- Größtes Risiko Doppelbuchungen; Empfehlung: „Materialentnahmen buchen = Manuell bzw. Gesamtfertigmeldung“, „Verf. Mengen aus Lager rückmelden = Keine“, „Gesamtfertigmeldung bucht = Rest zu Soll“, plus Idempotenz-ID je WMS-Buchung. [A]

## 1. Module
- [B] Sage 100 Produktion (Enterprise): Fertigungsaufträge, Fertigungspapiere, Fertigungssteuerung, Ressourcenlisten, Rückmeldungen, CAD-Schnittstelle, Simulation, Vor-/Nachkalkulation, Pluspaket, Projektfertigerlösung, Leitstand.
- [B] Pluspaket Produktion: BDE-Grundmodul, erweiterte Kalkulation, Fremdfertigung, Produktionsdisposition.
- [B] Zusatzpaket „Chargen- und Seriennummern Produktion“.
- [B] Davon getrennt: Produktionsstücklisten der Warenwirtschaft / interne Fertigungsaufträge (KHKIFAuftraege), ohne Reservierung.
- [B] Sage-BDE (Server/Terminals) abgekündigt: Verkauf Ende 12/2025, End of Support **01.08.2027**. Partnerlösungen buchen „über die Rückmelde- und Lagerengine von Sage“, nicht direkt in die DB; dafür Third-Party-Lizenz nötig (TerraCom).
- [B, Lastenheft BDE 07/2025] IDEAL und AKE: getrennte Sage-100-Installationen. AKE: Blech über OSEON, Arbeitspläne aus Vault, Arbeitszeiten fehlen. IDEAL: Arbeitsgänge in Stücklisten fehlen. Ziel „SOLL wird IST-Zeit“ per Einzelscan.

## 2. Stammdaten
- [B] Ressourcenliste = Stückliste + Arbeitsplan + Werkzeuge + Fremdfertigung als Positionen eines Objekts.
- [B] Positionstypen: MA Material, AG Arbeitsgang, AP Arbeitsplatz (Unterposition AG), WZ Werkzeug, FF Fremdfertigung, VS Verschnitt; Kosten/Stillstand als Rückmeldetypen. Terminierung mit Kalender des Arbeitsplatzes, sonst der ersten HR-Ressource.
- [B] „Eigenfertigung = Ja“ → kostenrechnerisch Unterbaugruppe. „Als Unterbaugruppe = Nein“ → kein Unter-FA, Entnahme wie Material (DPS S. 93). Fix in 9.0 zu diesem Verhalten (Hilltop) → [U] Build-Version prüfen.
- [U] Kein belegter Phantom-Positionstyp in Sage 100 DACH (X3/Operations-Doku gilt nicht).
- [B] Normal-/Industrieminuten; Rüst-/Stückzeit am AG; Puffer-, Warte-, Transportzeit, Leistungs-/Zeitgrad am Arbeitsplatz. Schwund im Artikelstamm, erhöht Material-RM automatisch, prozentual nach oben gerundet auf Dezimalstellen der Lager-ME (DPS S. 99). Oberauftrag Min./Max.-Losgröße, Unteraufträge nur Min. (DPS S. 93).

## 3. Lebenszyklus
```
Kundenauftrag (AB) / Produktionsdisposition / manuell (MAN)
  → FA anlegen (Unterbaugruppen erzeugen, Losgrößen, Terminierung, ggf. sofort einlasten)
    Haupt-FA (Verursacher AB/MAN)
      ├─ Unter-FA A (Verursacher = Haupt-FA) └─ Unter-FA A1 …
      └─ Unter-FA B
  → EINLASTEN (Dispo/Kapazität wirksam; Bedarfsprüfung kann Unter-FAs löschen)
  → BEGONNEN (Iststart mit erster RM)
  → Rückmeldungen (Zeit, Material, Verschnitt, Werkzeug, Kosten, Fremdarbeit, Stillstand, Fertigmeldung)
  → BEENDET (MEK-Neuberechnung)  → ggf. REAKTIVIERT
```
- [B] Verursacher: AB, anderer FA (Unterbaugruppe) oder MAN. Kopf-Statusfelder: Eingelastet (Datum), Reaktiviert, Begonnen am, Beendet am, Aktiv/inaktiv, Fertigungsart Eigen/Fremd/Einkauf (Einkauf → inaktiv, Dispo/Belegung zurück) (DPS S. 88–90).
- [B] Keine belegte Status-Enumeration „erfasst/freigegeben/…“ → Status aus Datums-/Kennzeichenfeldern ableiten [A]; Spaltennamen [U].
- [B] Bedarfsprüfung Unterbaugruppen „Beim Einlasten“: Unterbaugruppen mit ausreichendem Bestand werden **gelöscht**; eingelastete FAs nicht entlastbar (DPS S. 127).
- [B] Sperre nur auf geladenem FA; Unter-/Oberaufträge erst bei Markierung im Baum (DPS S. 87). Projekt vererbbar; Unterbaugruppen ohne eigene Kostenkonten belasten den Oberauftrag.
- [A] Folgen: nur eingelastete FAs als arbeitsbereit synchronisieren; nach Einlasten verschwundener Unter-FA = in Material umgewandelt, nicht storniert; bestehende Reconciliation (Storno gelöschter FAs) muss das unterscheiden.

## 4. Material
| Weg | Einstellung | Wirkung |
|---|---|---|
| Manuell / Gesamtfertigmeldung | „Manuell bzw. Gesamtfertigmeldung“ | Material-RM einzeln oder über Gesamtfertigmeldung (Schalter MA/AP/FF) |
| Retrograd je AG | „Automatisch bei Fertigmeldung Arbeitsgang“ | davor angeordnete MA-Positionen werden mit zurückgemeldet |
| Beim Einlasten | „Verf. Mengen aus Lager rückmelden: Bei Speichern/Einlastung“ | Bestand als Material-RM beim Einlasten |
- [B] Kombination „bei Einlastung“ + „Automatisch bei Fertigmeldung AG“ = zwei Entnahmen, laut Sage nicht empfohlen.
- [B] DPS-Handbuch: MA in Gesamtfertigmeldung, Verf. Mengen aus Lager, Unterbaugruppen rückmelden „entfällt, da Nutzung BDE“ → [U] tatsächliche Einstellung prüfen.
- [B] „Übernahme Lager in FA“: keines (Hauptlager Artikel; ohne → RM fehlerhaft) / RL-Lager / RL-Lager sonst Hauptlager. Zugangs-Lagerplatz im FA-Kopf. Erweiterte Lagerwirtschaft: FIFO/LIFO, Lageraufteilungsdialog.
- [B] Je Charge eine Material-RM; Chargenverwaltung erfordert „Manuell bzw. Gesamtfertigmeldung“. Fertigteil-Charge manuell / FA-Nummer / aus Material. Seriennummern „erste SN vorgeben“ oder „aus TAN“, Präfix+Suffix ≤ 12 Zeichen.
- [B] Rundung nach Lager-ME: Entnahme auf-, Zugang abrunden; Rundungsreste in Sammel-RM bleiben ungebucht.
- [B] Ungeplante Entnahme mit Position 0. Verschnitt-RM bucht zu. Rückgabe regulären Materials [A] über Storno/negative Menge – Zulässigkeit [U].
- [A] WMS-Umbuchung auf Kommissionierwagen = Lagerumbuchung, nicht Verbrauch; Verbrauch = Material-RM vom Produktionsplatz.

## 5. Rückmeldung / BDE
- [B] Typen: Zeit-RM (Satz aus Arbeitsplatz/Mitarbeiter, Arbeitsplatz-/Lohnanteil 0–1, geplant mit Position, ungeplant Position 0), Material, Verschnitt, Kosten, Fremdarbeit, Stillstand (mit Begründung), Werkzeug, Fertigmeldung; Fehlerkennzeichen Spalte „Kz“.
- [B] „Zugang Fertig-/Halbfertigteile buchen“: manuelle RM letzter AG (Gutmenge löst Fertigmeldung aus) / Gesamtfertigmeldung / Keinen. Sammel-RM: nur letzter AG löst aus, wenn „Letzter Arbeitsschritt löst Fertigmeldung aus“.
- [B] Gesamtfertigmeldung: Mitarbeiter, Menge, Datum, Charge/SN, „Auftrag beendet“, „Unterbaugruppen fertigmelden“; Fremdfertigung nur per Wareneingang. Modi: „Ist = Soll bei nicht vorhandener RM“, „Anteilig Material und Zeit“ (prüft bestehende RMs nicht → Doppelbuchung bei Unterbaugruppe+Oberauftrag), „Rest zu Soll“.
- [B] „Rest zu Soll“ + Material + Schwund → „Es ist zu viel zurückgemeldet…“ (sage-forum 07/2025). 9.0-Release: Gesamtfertigmeldung bucht Netto + Schwund. Dispo rechnet bei Teil-RM immer vollen Schwund.
- [B] Sammel-RM verteilt Zeiten anteilig nach Soll-Zeiten (sonst gleich); nur eine Charge je Material.
- [B] Kostenbasis MEK/LEK/kalk. EK (Empfehlung MEK); „Bei Fertigmeldung FA = Ja“ → MEK neu; Teilfertigmeldungsbehandlung (aufgelaufene Kosten / alter MEK, aufgelaufene beim Beenden / kalk./letzter EK); Gemeinkostenzuschläge. MEK beim Beenden (abacus).
- [A] Späte Zeit-/Material-RM nach Fertigmeldung verfälscht Lagerwert. Unter-FA vor Haupt-FA fertigmelden (Bottom-up).

## 6. Technik
| Tabelle | Bedeutung | Status |
|---|---|---|
| KHKPpsFaBelege | FA-Kopf: BelID, Mandant, StrukturID, Belegjahr, Belegnummer, Artikelnummer, Variante, Bezeichnung1 | [B] altes Schema; 9.0 [U] |
| KHKPpsFaBelegePositionen | FA-Positionen | Name [B], Felder [U] |
| KHKPpsFaBelegeAGPositionen | Unterpositionen zu AG (BelAGPosID, Mandant, BelID) | Name [B], Rest [U] |
| KHKPpsFaBelegeBedarfVerursacher | BelID, RueckmeldungID, BatchID, Verursachertyp (0 Auftrag … 70 Fertigung), VerursacherID, ReferenzID, MengeBasis, MengeLager | [B] Snippet; Hierarchie-Deutung [A] |
| KHKPpsFaBelegeProtokoll | archivierte FA-Köpfe | [B] |
| KHKPpsRueckmeldungen (+Chargen, SerienNr, Lager, Fibu, FibuLog) | Rückmeldungen, Kinder per RueckmeldungID/BatchID | Namen [B], Felder [U] |
| KHKPpsRessourcenkopf/-Positionen/-AGPositionen | Ressourcenlisten | [B] Forum, PK [U] |
| KHKPpsRessourcenbelegung | Kapazität, Belegungstatus 0 geplant / 1 echt | [B] |
| tKHKPps* | temporär je ConnID – nicht verwenden | [B] |
| KHKArtikelVarianten / KHKLagerplatzbestaende / KHKLagerplatzbuchungen | Bestände, Lagerplatzbuchungen | [B] |
- [B] Alle Tabellen mit Mandant (SmallInt); KHKBoolean -1/0. Hierarchie vermutlich StrukturID + Verursachertyp 70 [U]. Referenz: Datenbankdokumentation Sage 100 9.0.7.2 (desk-firm), Abschnitt KHKPps* nicht ausgewertet.
- [B] Assemblies `Sagede.OfficeLine.Pps.Fertigungsauftrag.dll` (`Disponieren` → `EinlastenDispo` ab 9.0.6 Build 7908), `…Pps.RueckmeldungEngine.dll`, `…Pps.RealTimeData.dll`. Session `ApplicationEngine.CreateSession`, `CreateMandant`; Dispose zwingend (Lizenz). .NET 4.8, 32 Bit.
- [U] Signaturen der RueckmeldungEngine öffentlich nicht dokumentiert; SDK nur für Tech-Partner → über DPS klären.
- Nicht tun: INSERT in KHKPpsRueckmeldungen / KHKLagerplatzbuchungen (umgeht Lager, MEK, FIBU, Dispo).

Fallstricke: Doppelbuchung durch Einstellungen; „Anteilig“ + Unterbaugruppen; Schwund + „Rest zu Soll“; Lager-ME vs. ME-Fertigung; Gut/Ausschuss getrennt (Ausschussfeld [U]); FA in Sage-Oberfläche gesperrt → Retry; AKE/IDEAL getrennt, Schlüssel (Standort, Mandant, BelID); RM auf beendetem FA ablehnen; Materialentnahme bei nicht begonnenem FA (historischer BDE-Fehler) → erst Zeit-RM/Start.

## 7. Empfohlene Abgleichslogik [A]
- Lesen (5–15 min, lesender SQL-User): Kopf, Hierarchie (StrukturID/Verursacher → Kanten/Ebene), Positionen MA/AG/AP, Rückmeldungen als Wahrheit.
- Schreiben über Queue → SageBridge:

| WMS-Ereignis | Sage-Buchung | Voraussetzung |
|---|---|---|
| AG Start/Ende, Mitarbeiter, Gut/Ausschuss | Zeit-RM | eingelastet, nicht beendet |
| Material verbraucht | Material-RM je Charge/Lagerplatz | Entnahme manuell |
| Letzter AG gut | Fertigmeldung bzw. AG-RM | alle Unter-FAs fertig |
| Abschließen | „Auftrag beendet“ | keine offenen Jobs |
- Idempotenz: GUID je Buchung, Mapping GUID ↔ RueckmeldungID/BatchID; GUID im Memofeld [U]; Duplikat-Heuristik.
- Reihenfolge je StrukturID seriell: Unter-FA vor Haupt-FA; Zeit-RM → Material-RM → Fertigmeldung.
- Konflikte: Sage gewinnt bei Stammdaten/Hierarchie/abgeschlossenen FAs; FA beendet/gelöscht → offene Jobs „Konflikt – manuelle Klärung“. Reconciliation (Migration 80) um „in Material umgewandelt“ erweitern.

## 8. Lesende Prüfabfragen (SQL Server, Sage-DB)
```sql
-- 1. PPS-Tabellen und Spalten
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, ORDINAL_POSITION
FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME LIKE 'KHKPps%'
ORDER BY TABLE_NAME, ORDINAL_POSITION;
-- 2. Primärschlüssel
SELECT tc.TABLE_NAME, kcu.COLUMN_NAME, kcu.ORDINAL_POSITION
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY' AND tc.TABLE_NAME LIKE 'KHKPps%'
ORDER BY tc.TABLE_NAME, kcu.ORDINAL_POSITION;
-- 3. Status-/Hierarchiespalten
SELECT TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME LIKE 'KHKPpsFaBelege%'
  AND (COLUMN_NAME LIKE '%Status%' OR COLUMN_NAME LIKE '%Struktur%' OR COLUMN_NAME LIKE '%Haupt%'
    OR COLUMN_NAME LIKE '%Ober%' OR COLUMN_NAME LIKE '%Eingelastet%' OR COLUMN_NAME LIKE '%Beendet%'
    OR COLUMN_NAME LIKE '%Begonnen%' OR COLUMN_NAME LIKE '%Reaktiv%' OR COLUMN_NAME LIKE '%Ebene%');
-- 4. FA-Struktur
DECLARE @Mandant smallint = 1, @Belegnummer int = 12345;
SELECT f.Mandant, f.StrukturID, f.BelID, f.Belegjahr, f.Belegnummer, f.Artikelnummer, f.Variante
FROM KHKPpsFaBelege f
WHERE f.Mandant = @Mandant
  AND f.StrukturID = (SELECT StrukturID FROM KHKPpsFaBelege WHERE Mandant=@Mandant AND Belegnummer=@Belegnummer)
ORDER BY f.BelID;
-- 5. Verursacher-Kanten
SELECT v.*, f.Belegnummer, f.Artikelnummer
FROM KHKPpsFaBelegeBedarfVerursacher v
JOIN KHKPpsFaBelege f ON f.Mandant = v.Mandant AND f.BelID = v.BelID
WHERE v.Mandant = @Mandant ORDER BY v.BelID;
-- 6. Rückmeldungen eines FA
SELECT TOP 200 r.* FROM KHKPpsRueckmeldungen r
WHERE r.Mandant=@Mandant AND r.BelID IN (SELECT BelID FROM KHKPpsFaBelege WHERE Mandant=@Mandant AND Belegnummer=@Belegnummer);
-- 7. Ablage Grundlageneinstellungen
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME LIKE '%Pps%Grundl%' OR TABLE_NAME LIKE '%PpsSys%' OR TABLE_NAME LIKE 'KHKMandanten%';
-- 8. Artikelschalter
SELECT TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME='KHKArtikel' AND (COLUMN_NAME LIKE '%Unterbau%' OR COLUMN_NAME LIKE '%Fertig%'
  OR COLUMN_NAME LIKE '%Schwund%' OR COLUMN_NAME LIKE '%Produktion%');
-- 9. Ressourcenpositionen
SELECT TOP 100 * FROM KHKPpsRessourcenPositionen WHERE Mandant=@Mandant;
-- 10. Trigger auf PPS-Tabellen
SELECT t.name, OBJECT_NAME(t.parent_id) AS Tabelle FROM sys.triggers t WHERE OBJECT_NAME(t.parent_id) LIKE 'KHKPps%';
```
Zusätzlich: `sp_helptext 'vw_AKE_Kommissionierung_WAListe'`.

## Caveats
- DB-Feldbeschreibungen aus älterem Schema, nur Snippets → gegen 9.0 verifizieren.
- Engine-API nicht öffentlich dokumentiert.
- DPS-Handbuch = Soll-Prozess 2023, nicht zwingend aktuelle Einstellungen.
- Sage X3/Operations-Inhalte (Phantom, „Fest/Geplant“) gelten nicht für Sage 100 DACH.

## Quellen
- https://www.sage.com/de-de/produkte/sage-100/produktion/
- https://dl.business-software.at/smb/sage_100/Modulbeschreibung_Sage100_web.pdf
- https://onlinehilfe.sage.de/onlinehilfe/ol/80/abf/leistungsumfang_produktion_doku.htm
- https://onlinehilfe.sage.de/onlinehilfe/ol/80/abf/schnelleinstieg_pps_doku.htm
- https://onlinehilfe.sage.de/onlinehilfe/ol/62/abf/idh_frmppssyssetup.htm
- https://onlinehilfe.sage.de/onlinehilfe/ol/60/abf/idh_ersteseite_zp_chargen.htm
- https://sage-software.desk-firm.de/blog/sage-100-bde-end-of-support-2027-jetzt-auf-bde-neo-umsteigen/
- https://sage-software.desk-firm.de/storage/2024/11/Datenbankdokumentation.pdf
- https://www.terracom.de/blog/betriebsdatenerfassung-sage-100/
- https://www.terracom.de/blog/sage-100-rest-api/
- https://www.hilltop-consulting.de/sage-updates/
- https://wiki.abacus-edv.de/doku.php?id=handbuch:erweitertefremdfertigung
- https://www.officium-inservio.com/sage-100-breaking-changes/pps1
- https://www.2mgr.de/wp-content/uploads/2015/08/Wartungstext623_Stand_14.07.2015.pdf
- https://pdfcoffee.com/anhang-datenbankschema-pdf-free.html
- sage-forum.de: Threads zu Produktionsstückliste, Bedarfsprüfung Unterbaugruppen, Fertigungsartikel vom Lager, Gesamtfertigmeldung, Ressourcenliste-Tabellen, tKHK-Tabellen, Sitzung erstellen / AppServer 9.0
