/*
========================================================================================
 View:      dbo.[vw_IDEAL-AKE_Kommissionierung_FAInfos]
 Standort:  IDEAL
 Zweck:     PPS-Auftragsdaten (Kunde, Termine, Beschichtung, Priorisierung).
            Eine Zeile = ein Auftrag (Kombination aus ABNr + Pos). Kombinationsgeraete
            koennen denselben HauptFA mit unterschiedlicher [Abteilung] haben
            (dann ist die Abteilung der zusaetzliche Identifier).
 Quelle:    Cross-DB-Sicht [PPSImport].[dbo].[vw_PPSData] (PPS-Anwendung der IDEAL).
 Datenbank: Sage-Instanz des IDEAL-Standorts (selber Server wie WMS). NICHT Teil der
            WMS-DB — reine Versionskontroll-Dokumentation, KEINE WMS-Migration.

 SPALTEN gegen die REALE View verifiziert 2026-08-11 (INFORMATION_SCHEMA am IDEAL-System).
          Typen: ABNr/Pos/HauptFA/Prio = int; KO_Termin/FE_Termin/Start_Beschichtung/
          Beschichten_Retour/Neuer_PT_PPS/Verladetermin_Vsl = date; Montagestunden = decimal;
          Rest = nvarchar. Der vollstaendige CREATE-VIEW-Text liegt in der Sage-DB und ist hier
          bewusst NICHT nachgebaut (Sage-verwaltet) — beim Zugriff darauf ggf. eintragen.

 Verknuepfungsschluessel: HauptFA (= PPSData.FA_Nr) -> FAListe.HauptFA (= StrukturID).
----------------------------------------------------------------------------------------
 Spalte              | Beschreibung
----------------------------------------------------------------------------------------
 ABNr                | Sage-Auftragsbestaetigungsnummer
 Pos                 | Position innerhalb der AB
 Kunde               | Endkunde
 KO_Termin           | Konstruktions-Termin
 FE_Termin           | Wann muss die Blechfertigung fertig sein
 Abteilung           | Montage-Abteilung (reale Spalte heisst 'Abteilung', Anhang nannte sie 'Montage-Abteilung'; im Import AS MontageAbteilung) — zweiter Identifier bei Kombigeraeten
 HauptFA             | Verknuepfungs-Schluessel zur FAListe-View
 Status              | PPS-Status (leer/- vor Konstruktionsbeginn)
 Start_Beschichtung  | Wann Beschichtung starten
 Dienstleister       | Wer beschichtet
 Montagestunden      | Kalkulierte Montagestunden
 Prio                | Prioritaet
 RAL                 | Beschichtungsfarbe
 Beschichten_Retour  | Rueckliefer-Datum vom Beschichter
 Neuer_PT_PPS        | Aktueller PPS-Fertigstellungstermin
 Verladetermin_Vsl   | Voraussichtlicher Verladetermin
 Bemerkung_Uhrzeit   | Spezielle Uhrzeit bei der Verladung
----------------------------------------------------------------------------------------
 Datenverfuegbarkeits-Regel (View filtert bereits im WHERE):
   WHERE FA_Nr IS NOT NULL AND FA_Nr <> '' AND FA_Nr <> '-'
     AND [Status] IS NOT NULL AND [Status] <> '' AND [Status] <> '-'
 -> Ein Auftrag erscheint erst, wenn FA-Nummer UND PPS-Status gefuellt sind.
   Der WMS-Import bildet das als reine EXISTS-Pruefung auf HauptFA ab (kein Zeilen-Join,
   sonst Fan-out bei Kombinationsgeraeten mit mehreren Montage-Abteilungen).
========================================================================================
*/

-- TODO: echten CREATE-VIEW-Text aus der Sage-DB hier eintragen (Struktur laut Anhang).
