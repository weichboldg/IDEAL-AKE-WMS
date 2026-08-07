/*
========================================================================================
 View:      dbo.[vw_IDEAL-AKE_Kommissionierung_FAListe]
 Standort:  IDEAL
 Zweck:     Flache Liste aller Stuecklistenpositionen einer Struktur (Haupt-FA + alle
            Sub-FAs + Blaetter). Eine Zeile = eine Position bzw. der Hauptauftrag selbst (Wurzel).
 Datenbank: Sage-Instanz des IDEAL-Standorts (z.B. IDEAL_TEST_2026_05_03 am Testsystem,
            produktiv analog). NICHT Teil der WMS-DB — dieses Skript ist reine
            Versionskontroll-Dokumentation der Fremd-DB-Objekte, KEINE WMS-Migration.

 WICHTIG: STRUKTUR LAUT ANHANG (secondbrain/ideen/anhaenge/2026-07-29-standort-ideal/
          sage-views-ideal.md). Der vollstaendige View-Text (Joins/WHERE) liegt in der
          Sage-DB und ist hier bewusst NICHT nachgebaut — nur Spalten, Typen und Herkunft
          sind dokumentiert. Beim naechsten Zugriff auf die reale View den echten
          CREATE-VIEW-Text hier eintragen (SSMS: Skript als CREATE).

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
 Sollmenge         | decimal            | KHKPpsFaBelegePositionen.Menge
 Fertigungmenge    | decimal            | KHKPpsFaBelegePositionen.Menge
 Beschaffungsartikel | nvarchar (Ja/Nein) | KHKArtikel.IstBestellartikel
 Artikelgruppe     | nvarchar           | KHKArtikel.Artikelgruppe + KHKArtikelgruppen.Bezeichnung ("CODE - Bezeichnung")
 Hauptlagerplatz   | nvarchar           | KHKLagerplaetze.Kurzbezeichnung
 BemerkungPN       | nvarchar           | Position bevorzugt, Beleg als Fallback
 FertigungsInfoPN  | nvarchar           | Position bevorzugt, Beleg als Fallback
 Kommissionieren   | nvarchar           | Position bevorzugt, Beleg als Fallback (leer = nicht kommissionieren)
 Arbeitsbereich    | nvarchar           | Position bevorzugt, Beleg als Fallback (positionsbezogen!)
 Arbeitsschritte   | nvarchar           | Position bevorzugt, Beleg als Fallback (LEERZEICHEN-getrennt)
 Artikeltyp        | nvarchar           | Position bevorzugt, Beleg als Fallback
 Beschichtet       | smallint (-1/0)    | Position bevorzugt, Beleg als Fallback (-1 = beschichtet)
 Material          | nvarchar           | Position bevorzugt, Beleg als Fallback
 Breite/Hoehe/Tiefe| decimal            | Position bevorzugt, Beleg als Fallback
 EKBedarf          | Bit/Ja-Nein        | Position bevorzugt, Beleg als Fallback
 VMBedarf          | nvarchar           | Position bevorzugt, Beleg als Fallback (Vormontage-Bereich)
----------------------------------------------------------------------------------------
 Baum-Beziehung:
   Wurzel    : VaterFA IS NULL, Position IS NULL, SubFA = HauptFA
   Baugruppe : SubFA <> 0, weitere Zeilen mit VaterFA = SubFA
   Blatt     : SubFA = 0, keine weiteren Zeilen darunter
========================================================================================
*/

-- TODO: echten CREATE-VIEW-Text aus der Sage-DB hier eintragen (Struktur laut Anhang).
