/* =====================================================================================================
   Sage 100 PPS – Prüfabfragen für den Abgleich Recherche ↔ Echtbestand (2026-11-02)
   Bezug: [[2026-11-02-sage100-pps-fa-bde-recherche]] Abschnitt 8, [[2026-11-02-sage-pps-abgleich]]

   STRIKT NUR LESEND. Ausschließlich SELECT, INFORMATION_SCHEMA, sys.*, OBJECT_DEFINITION / sp_helptext.
   Kein INSERT/UPDATE/DELETE/MERGE/EXEC einer Prozedur außer sp_helptext.
   Von Claude NICHT ausgeführt (keine nachweislich nur lesende Verbindung konfiguriert) – Ausführung durch Gerald.

   Ablauf je Installation (AKE und IDEAL GETRENNT, Ergebnisse getrennt ablegen):
     1. Im SSMS mit einem Konto verbinden, das nur Leserechte hat (oder als Gerald, ausschließlich dieses Skript).
     2. Datenbank wählen:
          AKE   Test: AKESQL20.ake.at                         / ake_TEST2026            (Produktiv: [ake])
          IDEAL Test: idealsql2.ideal.ideal-ake.at\SAGE100TEST / IDEAL_TEST_2026_05_03   (Produktiv: {IDEAL-Produktiv-DB})
        Quelle der Testnamen: [[2026-10-29-dev-umgebung-lokal]], Nachtrag 2026-10-29.
     3. Parameter unten setzen: @Mandant und je eine Beispiel-FA-Nummer
        ({FA-mehrstufig} = Haupt-FA mit Unter-FAs, {FA-einstufig}).
     4. Ergebnisse je Abfrage-Nr. als CSV/Screenshot nach
        secondbrain/tests/anhaenge/2026-11-02-sage-pps-abgleich/{AKE|IDEAL}-Q<Nr>.csv
   Erwartung = was die Recherche bzw. das WMS annimmt; Abweichung bitte im Abgleichsbericht vermerken.
   ===================================================================================================== */

SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;   -- keine Sperren auf Sage-Tabellen

DECLARE @Mandant      smallint = 1;       -- {Mandant}: siehe Q0, NICHT raten
DECLARE @FaMehrstufig int      = 0;       -- {FA-mehrstufig}: Belegnummer bzw. StrukturID eines Haupt-FA mit Unter-FAs
DECLARE @FaEinstufig  int      = 0;       -- {FA-einstufig}

/* Q0 – Version und Mandanten
   Erwartung: eine Sage-100-Version 9.0.x (Recherche: Fix „Als Unterbaugruppe“ in 9.0, EinlastenDispo ab 9.0.6 Build 7908 [U]).
   Mandanten-Liste: liefert den Wert für @Mandant. */
SELECT @@VERSION AS SqlServerVersion, DB_NAME() AS Datenbank;
SELECT TOP 20 TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME LIKE '%Version%' OR TABLE_NAME LIKE 'KHKMandant%' ORDER BY TABLE_NAME;
-- Danach die gefundene Versions-/Mandantentabelle mit SELECT TOP 20 * ansehen (Name hier nicht geraten).
SELECT Mandant, COUNT(*) AS AnzahlFA FROM dbo.KHKPpsFaBelege GROUP BY Mandant;   -- Erwartung: genau ein Mandant je Installation

/* Q1 – PPS-Tabellen und Spalten (Recherche Q1)
   Erwartung: KHKPpsFaBelege, KHKPpsFaBelegePositionen, KHKPpsFaBelegeAGPositionen, KHKPpsFaBelegeBedarfVerursacher,
   KHKPpsFaBelegeProtokoll, KHKPpsRueckmeldungen(+Chargen/SerienNr/Lager/Fibu/FibuLog), KHKPpsRessourcen*, KHKPpsRessourcenbelegung. */
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE, ORDINAL_POSITION
FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME LIKE 'KHKPps%'
ORDER BY TABLE_NAME, ORDINAL_POSITION;

/* Q2 – Primärschlüssel (Recherche Q2)
   Erwartung: KHKPpsFaBelege (Mandant, BelID); Positionen (Mandant, BelPosID); AG-Positionen (Mandant, BelAGPosID);
   Rückmeldungen (Mandant, RueckmeldungID) – alles [U]. */
SELECT tc.TABLE_NAME, kcu.COLUMN_NAME, kcu.ORDINAL_POSITION
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
  ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME AND tc.TABLE_NAME = kcu.TABLE_NAME
WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY' AND tc.TABLE_NAME LIKE 'KHKPps%'
ORDER BY tc.TABLE_NAME, kcu.ORDINAL_POSITION;

/* Q3 – Status- und Hierarchiespalten (Recherche Q3, erweitert um Nachfolger/Verursacher/Aktiv/Fertigungsart)
   Erwartung Recherche: Datums-/Kennzeichenfelder Eingelastet, Begonnen, Beendet, Reaktiviert, Aktiv; Hierarchie StrukturID [U].
   Erwartung WMS (IDEAL-View FAListe): KHKPpsFaBelege.StrukturID und KHKPpsFaBelegePositionen.BelIDNachfolger existieren.
   SData (AKE-Test 2026-10-30): dtsFaUebersicht liefert IstEingelastet/IstBegonnen/IstBeendet (-1/0) – ob Tabellenspalten
   gleich heißen, ist offen. */
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME LIKE 'KHKPpsFaBelege%'
  AND (COLUMN_NAME LIKE '%Status%'    OR COLUMN_NAME LIKE '%Struktur%' OR COLUMN_NAME LIKE '%Haupt%'
    OR COLUMN_NAME LIKE '%Ober%'      OR COLUMN_NAME LIKE '%Eingelastet%' OR COLUMN_NAME LIKE '%Beendet%'
    OR COLUMN_NAME LIKE '%Begonnen%'  OR COLUMN_NAME LIKE '%Reaktiv%'  OR COLUMN_NAME LIKE '%Ebene%'
    OR COLUMN_NAME LIKE '%Nachfolger%' OR COLUMN_NAME LIKE '%Verursach%' OR COLUMN_NAME LIKE '%Aktiv%'
    OR COLUMN_NAME LIKE '%Fertigungsart%' OR COLUMN_NAME LIKE '%Unter%' OR COLUMN_NAME LIKE '%Gesperrt%'
    OR COLUMN_NAME LIKE '%Sperr%')
ORDER BY TABLE_NAME, COLUMN_NAME;

/* Q3b – Verteilung der Statusfelder (erst ausführen, wenn Q3 die echten Spaltennamen geliefert hat; Namen einsetzen)
   Erwartung: Belege mit „eingelastet“ ≠ leer sind die arbeitsbereiten FAs; Vergleich mit SData dtsFaNachStatus
   (AKE-Test 30.10.: Neu 20, Eingelastet 7, Begonnen 0).
SELECT CASE WHEN {SpalteEingelastet} IS NULL THEN 'neu' ELSE 'eingelastet' END AS Eingelastet,
       CASE WHEN {SpalteBegonnen}   IS NULL THEN '-'   ELSE 'begonnen'    END AS Begonnen,
       CASE WHEN {SpalteBeendet}    IS NULL THEN '-'   ELSE 'beendet'     END AS Beendet,
       {SpalteAktiv} AS Aktiv, COUNT(*) AS Anzahl
FROM dbo.KHKPpsFaBelege WHERE Mandant = @Mandant
GROUP BY ... ;
*/

/* Q4 – FA-Struktur eines mehrstufigen FA (Recherche Q4, angepasst)
   Erwartung: alle Belege einer Struktur haben dieselbe StrukturID; Haupt-FA: BelID = StrukturID (WMS-Annahme:
   IDEAL-View „Wurzel: SubFA = HauptFA“) [U]. */
SELECT f.Mandant, f.StrukturID, f.BelID, f.Belegjahr, f.Belegnummer, f.Artikelnummer, f.Variante,
       CASE WHEN f.BelID = f.StrukturID THEN 'Haupt' ELSE 'Unter' END AS RolleAnnahme
FROM dbo.KHKPpsFaBelege f
WHERE f.Mandant = @Mandant
  AND f.StrukturID = (SELECT TOP 1 StrukturID FROM dbo.KHKPpsFaBelege
                      WHERE Mandant = @Mandant AND (Belegnummer = @FaMehrstufig OR BelID = @FaMehrstufig))
ORDER BY f.BelID;

/* Q4b – Kanten über Positionen (so baut die IDEAL-View FAListe die Hierarchie)
   Erwartung: MA-Positionen mit BelIDNachfolger <> 0 zeigen auf die Unter-FA-BelIDs aus Q4. */
SELECT p.BelID AS VaterBelID, p.Position, p.RessourceTyp, p.RessourceNummer, p.BelIDNachfolger, p.Menge
FROM dbo.KHKPpsFaBelegePositionen p
WHERE p.Mandant = @Mandant
  AND p.BelID IN (SELECT BelID FROM dbo.KHKPpsFaBelege WHERE Mandant = @Mandant
                  AND StrukturID = (SELECT TOP 1 StrukturID FROM dbo.KHKPpsFaBelege
                                    WHERE Mandant = @Mandant AND (Belegnummer = @FaMehrstufig OR BelID = @FaMehrstufig)))
ORDER BY p.BelID, p.Position;

/* Q5 – Verursacher-Kanten (Recherche Q5)
   Erwartung Recherche [A]: Unter-FA hat Verursachertyp 70 (Fertigung) mit VerursacherID = BelID des Ober-FA.
   Vergleich mit Q4b: liefern beide Wege dieselben Kanten? */
SELECT v.*, f.Belegnummer, f.Artikelnummer, f.StrukturID
FROM dbo.KHKPpsFaBelegeBedarfVerursacher v
JOIN dbo.KHKPpsFaBelege f ON f.Mandant = v.Mandant AND f.BelID = v.BelID
WHERE v.Mandant = @Mandant
  AND f.StrukturID = (SELECT TOP 1 StrukturID FROM dbo.KHKPpsFaBelege
                      WHERE Mandant = @Mandant AND (Belegnummer = @FaMehrstufig OR BelID = @FaMehrstufig))
ORDER BY v.BelID;
SELECT Verursachertyp, COUNT(*) AS Anzahl FROM dbo.KHKPpsFaBelegeBedarfVerursacher WHERE Mandant = @Mandant GROUP BY Verursachertyp;

/* Q6 – Rückmeldungen (Recherche Q6, erweitert)
   Erwartung: Tabelle existiert; Spalten wie SData dtsRueckKorrektur (RueckmeldungID, BelID, BelPosID, Typ, Buchungstyp,
   Menge, MengeGut, Schwund, DatumZeitStart/Ende, Mitarbeiternummer, Arbeitsplatznummer, LagerBuchID, Status) [U].
   Zweck Verteilung: Wird heute schon zurückgemeldet (Sage-Client, DPS-MDE, Sage-BDE)? Von wem? */
SELECT TOP 200 r.* FROM dbo.KHKPpsRueckmeldungen r
WHERE r.Mandant = @Mandant
  AND r.BelID IN (SELECT BelID FROM dbo.KHKPpsFaBelege WHERE Mandant = @Mandant
                  AND StrukturID = (SELECT TOP 1 StrukturID FROM dbo.KHKPpsFaBelege
                                    WHERE Mandant = @Mandant AND (Belegnummer = @FaMehrstufig OR BelID = @FaMehrstufig)));
SELECT COUNT(*) AS AnzahlRueckmeldungen FROM dbo.KHKPpsRueckmeldungen WHERE Mandant = @Mandant;
-- Wenn Q1 die Spalten „Typ“ und ein Datumsfeld bestätigt hat, Spaltennamen einsetzen:
-- SELECT Typ, YEAR({Datumsspalte}) AS Jahr, COUNT(*) FROM dbo.KHKPpsRueckmeldungen WHERE Mandant=@Mandant GROUP BY Typ, YEAR({Datumsspalte});

/* Q6b – Gibt es Positionen der Typen AG/AP (Arbeitsgänge) in FAs? (Idee 2026-10-30)
   Erwartung: AKE: Arbeitspläne aus Vault, Arbeitszeiten fehlen (Lastenheft); IDEAL: Arbeitsgänge fehlen in Stücklisten
   → wenig/keine AG-Positionen. Entscheidet, ob Zeit-RM je AG überhaupt adressierbar ist. */
SELECT RessourceTyp, COUNT(*) AS Anzahl FROM dbo.KHKPpsFaBelegePositionen WHERE Mandant = @Mandant GROUP BY RessourceTyp;
SELECT RessourceTyp, COUNT(*) AS Anzahl FROM dbo.KHKPpsRessourcenPositionen WHERE Mandant = @Mandant GROUP BY RessourceTyp;

/* Q7 – Ablage der Produktions-Grundlageneinstellungen (Recherche Q7, erweitert)
   Erwartung [U]: eigene Tabelle (Name unbekannt) oder Mandanten-Einstellungstabelle. Gesucht sind die Werte im
   Register „Rückmeldungen“: Materialentnahmen buchen, Verf. Mengen aus Lager rückmelden, Gesamtfertigmeldung bucht,
   Zugang Fertig-/Halbfertigteile buchen, Letzter Arbeitsschritt löst Fertigmeldung aus, Übernahme Lager in FA,
   Bedarfsprüfung Unterbaugruppen. Spaltennamen NICHT raten – Treffer gemeinsam mit DPS deuten. */
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME LIKE '%Pps%Grundl%' OR TABLE_NAME LIKE '%PpsSys%' OR TABLE_NAME LIKE '%PpsEinst%'
   OR TABLE_NAME LIKE '%PpsSetup%' OR TABLE_NAME LIKE 'KHKMandanten%' OR TABLE_NAME LIKE '%Grundlagen%'
ORDER BY TABLE_NAME;
SELECT TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME LIKE '%Pps%'
  AND (COLUMN_NAME LIKE '%Rueckm%' OR COLUMN_NAME LIKE '%Retro%' OR COLUMN_NAME LIKE '%Gesamtfertig%'
    OR COLUMN_NAME LIKE '%Materialentn%' OR COLUMN_NAME LIKE '%Bedarfspr%' OR COLUMN_NAME LIKE '%Zugang%')
ORDER BY TABLE_NAME, COLUMN_NAME;

/* Q8 – Artikelschalter (Recherche Q8, erweitert)
   Erwartung: Spalten für „Als Unterbaugruppe“, „Eigenfertigung“, „Schwund“, Chargen-/Seriennummernpflicht. */
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('KHKArtikel', 'KHKArtikelVarianten')
  AND (COLUMN_NAME LIKE '%Unterbau%' OR COLUMN_NAME LIKE '%Fertig%' OR COLUMN_NAME LIKE '%Schwund%'
    OR COLUMN_NAME LIKE '%Produktion%' OR COLUMN_NAME LIKE '%Charge%' OR COLUMN_NAME LIKE '%Serie%'
    OR COLUMN_NAME LIKE '%Eigen%' OR COLUMN_NAME LIKE '%ME%Lager%' OR COLUMN_NAME LIKE '%Lager%ME%')
ORDER BY TABLE_NAME, COLUMN_NAME;

/* Q9 – Ressourcenpositionen (Recherche Q9) – Aufbau einer Ressourcenliste, Positionstypen MA/AG/AP/WZ/FF/VS */
SELECT TOP 100 * FROM dbo.KHKPpsRessourcenPositionen WHERE Mandant = @Mandant;

/* Q10 – Trigger auf PPS- und Lagertabellen (Recherche Q10, erweitert um Lager)
   Erwartung: Partner-Trigger (DPS/AKE-Pakete) möglich – jeder Trigger ist ein Grund mehr, nie direkt zu schreiben. */
SELECT t.name AS TriggerName, OBJECT_NAME(t.parent_id) AS Tabelle, t.is_disabled, t.create_date, t.modify_date
FROM sys.triggers t
WHERE OBJECT_NAME(t.parent_id) LIKE 'KHKPps%' OR OBJECT_NAME(t.parent_id) LIKE 'KHKLager%'
ORDER BY Tabelle, TriggerName;

/* Q11 – Definition der vom WMS gelesenen Views (Recherche: sp_helptext 'vw_AKE_Kommissionierung_WAListe')
   Ziel: Statusfilter, Hierarchie-Herleitung und Mandantenfilter belegen. Nur in der DB ausführen, in der die View existiert.
   AKE:   vw_AKE_Kommissionierung_WAListe, vw_AKE_Kommissionierung_StuecklistenDB
   IDEAL: vw_IDEAL_AKE_WMS_FAListe, vw_IDEAL_AKE_WMS_FAInfos, vw_IDEAL_AKE_WMS_Arbeitsgaenge, vw_IDEAL_AKE_WMS_Artikel,
          vw_IDEAL_AKE_WMS_FAZusatzinformationen, [vw_IDEAL-AKE_Kommissionierung_FAListe] (Altname)
   Erwartung: Filter auf Eingelastet/Beendet/Aktiv sichtbar ODER kein Statusfilter (dann liefert die View auch
   nicht eingelastete und beendete FAs – Folge für die Reconciliation). */
SELECT s.name + '.' + o.name AS ViewName, OBJECT_DEFINITION(o.object_id) AS Definition
FROM sys.objects o JOIN sys.schemas s ON s.schema_id = o.schema_id
WHERE o.type = 'V'
  AND (o.name LIKE 'vw_AKE_Kommissionierung%' OR o.name LIKE 'vw_IDEAL_AKE_WMS%' OR o.name LIKE 'vw_IDEAL-AKE%')
ORDER BY o.name;
-- alternativ je View: EXEC sp_helptext 'dbo.vw_AKE_Kommissionierung_WAListe';

/* Q12 – Verschwundene Unter-FAs: gelöscht oder archiviert? (Recherche 3: Bedarfsprüfung löscht Unter-FAs beim Einlasten)
   Erwartung [A]: Gelöschte Unter-FAs stehen NICHT mehr in KHKPpsFaBelege; ob KHKPpsFaBelegeProtokoll sie enthält, ist offen.
   Ersatz-Beleg: Die Vater-Position hat danach BelIDNachfolger = 0 (Material statt Baugruppe). */
SELECT TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'KHKPpsFaBelegeProtokoll' ORDER BY ORDINAL_POSITION;
SELECT TOP 50 * FROM dbo.KHKPpsFaBelegeProtokoll WHERE Mandant = @Mandant ORDER BY 1 DESC;

/* Q13 – Lagerplatz-/Chargen-Bezug (Recherche 4) – existieren Chargen-/Seriennummern-Tabellen der Rückmeldung? */
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME LIKE 'KHKPpsRueckmeldungen%' OR TABLE_NAME LIKE 'KHKLagerplatz%' OR TABLE_NAME LIKE '%Charge%'
ORDER BY TABLE_NAME;

/* Q14 – Partner-Felder MDE/BDE am FA (SData 30.10.: USER_DPSBSMDEFreigabe, USER_DPSBSBereitstellungBeendet)
   Erwartung: USER_-Spalten an KHKPpsFaBelege; zeigt, ob DPS-MDE bereits FA-Freigaben/Bereitstellungen führt. */
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME LIKE 'KHKPps%' AND COLUMN_NAME LIKE 'USER[_]%'
ORDER BY TABLE_NAME, COLUMN_NAME;
