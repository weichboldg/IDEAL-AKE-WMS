/*
========================================================================================
 View:      dbo.[vw_IDEAL-AKE_Kommissionierung_FAListe]
 Standort:  IDEAL
 Zweck:     Flache Liste aller Stuecklistenpositionen einer Struktur (Haupt-FA + alle
            Sub-FAs + Blaetter). Eine Zeile = eine Position bzw. der Hauptauftrag selbst (Wurzel).
 Datenbank: Sage-Instanz des IDEAL-Standorts (z.B. IDEAL_TEST_2026_05_03 am Testsystem,
            produktiv analog). NICHT Teil der WMS-DB — dieses Skript ist reine
            Versionskontroll-Dokumentation der Fremd-DB-Objekte, KEINE WMS-Migration.

 SPALTEN + TYPEN gegen die REALE View verifiziert 2026-08-11 (INFORMATION_SCHEMA am IDEAL-
          System). Textspalten sind ueberwiegend varchar (Ausnahmen nvarchar: Beschaffungsartikel,
          Artikelgruppe); Mengen/Masse sind money; Beschichtet/EKBedarf sind smallint. Der reale
          View traegt zusaetzlich die Spalte 'Verwendungsart' (im WMS-Import NICHT verwendet). Der
          vollstaendige CREATE-VIEW-Text (Joins/WHERE) liegt in der Sage-DB, ist Sage-verwaltet und
          hier bewusst NICHT nachgebaut.

 Ausgangspunkt laut Anhang: KHKPpsFaBelegePositionen (mit RessourceTyp='MA'), NICHT
 KHKPpsFaBelege — das vermeidet Duplikate durch verwaiste Belege.
----------------------------------------------------------------------------------------
 Spalte            | Typ                | Herkunft (laut Anhang)
----------------------------------------------------------------------------------------
 HauptFA           | int                | KHKPpsFaBelege.StrukturID (einziger Produktions-Identifier)
 HauptArtnr        | nvarchar           | KHKPpsFaBelege.Artikelnummer (Wurzel)
 VaterFA           | int NULL           | KHKPpsFaBelegePositionen.BelID (NULL bei Wurzel)
 Position          | int NULL           | KHKPpsFaBelegePositionen.Position (NULL bei Wurzel)
 SubFA             | int                | KHKPpsFaBelegePositionen.BelIDNachfolger (0 = Blatt)
 Artnr             | nvarchar           | KHKPpsFaBelegePositionen.RessourceNummer
 Matchcode         | nvarchar           | Position bevorzugt, KHKArtikel als Fallback
 Bezeichnung1      | nvarchar           | Sub-FA-Beleg bevorzugt, Stammdaten als Fallback
 Bezeichnung2      | nvarchar           | Sub-FA-Beleg bevorzugt, Stammdaten als Fallback
 Sollmenge         | money              | KHKPpsFaBelegePositionen.Menge
 Fertigungmenge    | money              | KHKPpsFaBelegePositionen.Menge
 Beschaffungsartikel | nvarchar (Ja/Nein) | KHKArtikel.IstBestellartikel (NOT NULL)
 Artikelgruppe     | nvarchar           | KHKArtikel.Artikelgruppe + KHKArtikelgruppen.Bezeichnung ("CODE - Bezeichnung")
 Hauptlagerplatz   | varchar            | KHKLagerplaetze.Kurzbezeichnung
 BemerkungPN       | varchar            | Position bevorzugt, Beleg als Fallback
 FertigungsInfoPN  | varchar            | Position bevorzugt, Beleg als Fallback
 Kommissionieren   | varchar            | Position bevorzugt, Beleg als Fallback (leer = nicht kommissionieren)
 Arbeitsbereich    | varchar            | Position bevorzugt, Beleg als Fallback (positionsbezogen!)
 Arbeitsschritte   | varchar            | Position bevorzugt, Beleg als Fallback (LEERZEICHEN-getrennt)
 Artikeltyp        | varchar            | Position bevorzugt, Beleg als Fallback
 Beschichtet       | smallint (-1/0)    | Position bevorzugt, Beleg als Fallback (-1 = beschichtet)
 Material          | varchar            | Position bevorzugt, Beleg als Fallback
 Verwendungsart    | varchar            | reale View-Spalte; im WMS-Import NICHT selektiert/verwendet
 Breite/Hoehe/Tiefe| money              | Position bevorzugt, Beleg als Fallback
 EKBedarf          | smallint           | Position bevorzugt, Beleg als Fallback (VB6: -1/0)
 VMBedarf          | varchar            | Position bevorzugt, Beleg als Fallback (Vormontage-Bereich)
----------------------------------------------------------------------------------------
 Baum-Beziehung:
   Wurzel    : VaterFA IS NULL, Position IS NULL, SubFA = HauptFA
   Baugruppe : SubFA <> 0, weitere Zeilen mit VaterFA = SubFA
   Blatt     : SubFA = 0, keine weiteren Zeilen darunter
========================================================================================
*/

-- TODO: echten CREATE-VIEW-Text aus der Sage-DB hier eintragen (Struktur laut Anhang).
