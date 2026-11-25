-- =====================================================================================================
-- SQL/Einmalig/2026-11-25_v1.99.72_FraesenPfad_Erstimport.sql
--
-- EINMALIGES DATENSKRIPT (ADR 0015/0023) - KEINE Schema-Aenderung, KEIN __EFMigrationsHistory-Eintrag, KEIN Eintrag in
-- SQL/00_FreshInstall.sql, NICHT Teil der nummerierten SQL-Reihe. Nicht loeschend; DB-Backup vor dem Scharflauf trotzdem Pflicht.
-- Spec 2026-11-20-bde-liste-isolierungsfraese-pfad-material (Abschnitt Datenmodell, Einmal-Skript; Antworten 9, 15-18).
--
-- Quelle: backlog/anhaenge/2026-11-19-bde-liste-isolierungsfraese-pfad-material/WICAM-Pfade.xlsx, 179 Zeilen, Spalte A (Matchcode)
-- und Spalte E (Pfad). Spalten B, C, D, F, G werden nicht uebernommen (Rueckfrage 17). Zeile = Excel-Zeilennummer (Meldeliste).
-- Annahme Rueckfrage 15 (offen): Pfad WIE IN SPALTE E, kein Anhaengen von \<Matchcode>.tcn.
--
-- Regeln:
--   * Pfad getrimmt. Beginnt mit Y:\ (Gross/Klein egal) -> \\idealfs1\Daten$\Produktion\CAM\ + Rest (Antwort 18); beginnt mit \\ -> unveraendert;
--     sonst NICHT schreiben, nur melden ("ist enthalten in ...", Zeile 161 "Y\" ohne Doppelpunkt). Ergebnis > 500 Zeichen -> melden.
--   * Zuordnung ueber Articles.Matchcode getrimmt, exakt: 0 Treffer -> unbekannt, > 1 -> mehrdeutig; Matchcode mehrfach in der Liste mit
--     verschiedenen Pfaden -> doppelt in der Liste. Alle nur melden.
--   * Geschrieben wird nur, wo CamPath IS NULL (Handpflege wird nie ueberschrieben; abweichender vorhandener Wert -> "schon gepflegt").
--     Dadurch wiederholbar ohne Sperre: der zweite Trockenlauf liefert TREFFER: 0.
-- REIHENFOLGE: 1. SQL/164 da (v1.99.72).  2. Trockenlauf (Default).  3. DB-Backup.  4. vor SCHARF Antworten auf Rueckfragen 15/16 pruefen.
--   5. @Trockenlauf = 0 (Update-Skript: SCHARF).  6. Zweiter Trockenlauf muss TREFFER: 0 liefern.  7. Meldeliste von Hand pflegen.
-- =====================================================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Trockenlauf BIT = 1;   -- <<< 0 = ausfuehren (nach DB-Backup und Freigabe der Zaehlung)

IF COL_LENGTH(N'dbo.Articles', N'CamPath') IS NULL
BEGIN
    RAISERROR(N'SQL/164 fehlt (dbo.Articles.CamPath). Abbruch, nichts geaendert.', 16, 1);
    RETURN;
END

-- v1.99.79 F-18: eigene Standortpruefung (nicht nur das Manifest) - nur IDEAL = hierarchische Produktionsauftraege
-- (ServiceSetting ProduktionsauftragHierarchisch, dieselbe Auslegung wie ProductionWorkplacesController.IsHierarchicalMasterAsync).
IF NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N'ProduktionsauftragHierarchisch' AND LOWER(LTRIM(RTRIM([Value]))) IN (N'true', N'1', N'on'))
BEGIN
    RAISERROR(N'Kein IDEAL-Standort (ServiceSetting ProduktionsauftragHierarchisch ist nicht true) - Skript nur fuer IDEAL. Abbruch, nichts geaendert.', 16, 1);
    RETURN;
END

IF OBJECT_ID('tempdb..#Liste') IS NOT NULL DROP TABLE #Liste;
IF OBJECT_ID('tempdb..#Kand') IS NOT NULL DROP TABLE #Kand;
CREATE TABLE #Liste (Zeile INT NOT NULL, Matchcode NVARCHAR(400) NOT NULL, PfadRoh NVARCHAR(1000) NOT NULL);

INSERT INTO #Liste (Zeile, Matchcode, PfadRoh) VALUES
    (1, N'A-E-ISO-Lasto-V-Quer-LW-460-V02', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (2, N'A-E-ISO-Lasto-V-Quer-LW-540-V02', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (3, N'A-SÄ-ISO-ZS-1000-1000-20-V01', N'Y:\Schäumerei\Trennschnitt_Variabel'),
    (4, N'A-SÄ-ISO-ZS-1000-1000-30-V01', N'Y:\Schäumerei\Trennschnitt_Variabel'),
    (5, N'A-SÄ-ISO-ZS-1000-1000-40-V01', N'Y:\Schäumerei\Trennschnitt_Variabel'),
    (6, N'A-SÄ-ISO-ZS-1495-274-20-V01', N'Y:\Schäumerei\Zuschnitt_1495x274x20'),
    (7, N'A-VM-ISO-Glas-442-V01', N'Y:\Gerhard\Listen\Vormontage'),
    (8, N'A-VM-ISO-Glas-522-V01', N'Y:\Gerhard\Listen\Vormontage'),
    (9, N'A-VM-ISO-Glas-Meterware-V01', N'Y:\Gerhard\Listen\Vormontage'),
    (10, N'E-KT-ISO-Boden-Decke-ZS-1250-638,5-V01', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (11, N'E-KT-ISO-Lasto-E-Blende-457-333-40', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (12, N'E-KT-ISO-Lasto-E-Blende-457-383-40', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (13, N'E-KT-ISO-Lasto-E-Blende-457-405-40', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (14, N'E-KT-ISO-Lasto-E-Blende-537-333-40', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (15, N'E-KT-ISO-Lasto-E-Blende-537-383-40', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (16, N'E-KT-ISO-Lasto-E-Blende-537-405-40', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (17, N'E-KT-ISO-Mittelholm-A-815-V01', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (18, N'E-KT-ISO-Mittelholm-A-865-V01', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (19, N'E-KT-ISO-Mittelholm-I-815-V01', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (20, N'E-KT-ISO-Mittelholm-I-865-V01', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (21, N'E-KT-ISO-Rueckwand-ZS-1250-716-V01', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (22, N'E-KT-ISO-Rueckwand-ZS-1250-767-V01', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (23, N'E-KT-ISO-Seitenwand-680-815-A-V01', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (24, N'E-KT-ISO-Seitenwand-680-815-I-V01', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (25, N'E-KT-ISO-Seitenwand-680-865-A-V01', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (26, N'E-KT-ISO-Seitenwand-680-865-I-V01', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (27, N'E-KT-ISO-Zuschnitt-1000-1000-20-V01', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (28, N'E-KT-ISO-Zuschnitt-1000-1000-30-V01', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (29, N'E-KT-ISO-Zuschnitt-1000-1000-40-V01', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (30, N'E-KT-ISO-Zuschnitt-1000-775-40-V01', N'Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU'),
    (82, N'G-KT-ISO-Blende-MH+SW-ZS-1100-676-V01', N'Y:\Gerhard\Listen\GN\Zuschnitt_1000x676x30_Werkzeug_Variabel_1003'),
    (83, N'G-KT-ISO-Boden-Auflage-Hinten-ZS-1000-617-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (84, N'G-KT-ISO-Boden-Auflage-Seitlich-ZS-1000-617-V01', N'Y:\Gerhard\Listen\GN\Zuschnitt_1000x616x20_Werkzeug_Variabel_1008'),
    (85, N'G-KT-ISO-Boden-ZS-1250-717-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (86, N'G-KT-ISO-Boden-ZS-440-617-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (87, N'G-KT-ISO-Boden-ZS-477-617-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (88, N'G-KT-ISO-Boden-ZS-880-617-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (89, N'G-KT-ISO-Seitenwand-660-460-A-M-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (90, N'G-KT-ISO-Seitenwand-660-460-A-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (91, N'G-KT-ISO-Seitenwand-660-460-I-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (92, N'G-KT-ISO-Seitenwand-660-510-A-M-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (93, N'G-KT-ISO-Seitenwand-660-510-A-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (94, N'G-KT-ISO-Seitenwand-660-510-I-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (95, N'G-KT-ISO-Seitenwand-660-560-A-M-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (96, N'G-KT-ISO-Seitenwand-660-560-A-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (97, N'G-KT-ISO-Seitenwand-660-560-I-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (98, N'G-KT-ISO-Seitenwand-660-650-A-M-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (99, N'G-KT-ISO-Seitenwand-660-650-A-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (100, N'G-KT-ISO-Seitenwand-660-650-I-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (101, N'G-KT-ISO-Seitenwand-660-700-A-M-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (102, N'G-KT-ISO-Seitenwand-660-700-A-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (103, N'G-KT-ISO-Seitenwand-660-700-I-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (104, N'G-KT-ISO-Seitenwand-660-710-A-M-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (105, N'G-KT-ISO-Seitenwand-660-710-A-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (106, N'G-KT-ISO-Seitenwand-660-710-I-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (107, N'G-KT-ISO-Seitenwand-660-760-A-M-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (108, N'G-KT-ISO-Seitenwand-660-760-A-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (109, N'G-KT-ISO-Seitenwand-660-760-I-V01', N'Y:\Gerhard\Listen\GN\GN_System_20mm_BH_760_BH_710_BH_700_BH_650_RW_SE_BÖ'),
    (110, N'G-TK-ISO-Lasto-Q-I-440-V01', N'Y:\Gerhard\Listen\TK\TK_G_TK_ISO_Lasto_Q_I_440_V01_Mittelsteg'),
    (111, N'S-AK-ISO-Mittelholm-120-V01', N'Y:\Gerhard\Listen\AK'),
    (112, N'S-AK-ISO-Mittelholm-240-V01', N'Y:\Gerhard\Listen\AK'),
    (113, N'S-AK-ISO-Rückwand-120-V01', N'Y:\Gerhard\Listen\AK'),
    (114, N'S-AK-ISO-Rückwand-240-V01', N'Y:\Gerhard\Listen\AK'),
    (115, N'S-AK-ISO-Rückwand-Seite-120-V01', N'Y:\Gerhard\Listen\AK'),
    (116, N'S-AK-ISO-Rückwand-Seite-240-V01', N'Y:\Gerhard\Listen\AK'),
    (117, N'S-AK-ISO-Seitenwand-120-V01', N'Y:\Gerhard\Listen\AK'),
    (118, N'S-AK-ISO-Seitenwand-240-V01', N'Y:\Gerhard\Listen\AK'),
    (119, N'S-F01-ISO-Aufdopplung-Boden-1100-V01', N'Y:\1_NESTCAD\FKV\S-F01-ISO-Aufdopplung-Boden-V01\S_F01_ISO_Aufdopplung_Boden_1100_V01'),
    (120, N'S-F01-ISO-Aufdopplung-Boden-1195-V01', N'Y:\1_NESTCAD\FKV\S-F01-ISO-Aufdopplung-Boden-V01\S_F01_ISO_Aufdopplung_Boden_1195_V01'),
    (121, N'S-F01-ISO-Aufdopplung-Boden-1500-V01', N'Y:\1_NESTCAD\FKV\S-F01-ISO-Aufdopplung-Boden-V01\S_F01_ISO_Aufdopplung_Boden_1500_V01'),
    (122, N'S-F01-ISO-Boden-Stoppel-V01', N'Y:\1_NESTCAD\FKV\Stoppel_Boden\Stoppel_Boden_NEU'),
    (123, N'S-F01-ISO-Gradleiste-1100-L-V01', N'Y:\Gerhard\Listen\FKV\FKV_Gratleiste'),
    (124, N'S-F01-ISO-Gradleiste-1100-R-V01', N'Y:\Gerhard\Listen\FKV\FKV_Gratleiste'),
    (125, N'S-F01-ISO-Gradleiste-1195-L-V01', N'Y:\Gerhard\Listen\FKV\FKV_Gratleiste'),
    (126, N'S-F01-ISO-Gradleiste-1195-R-V01', N'Y:\Gerhard\Listen\FKV\FKV_Gratleiste'),
    (127, N'S-F01-ISO-Gradleiste-1500-L-V01', N'Y:\Gerhard\Listen\FKV\FKV_Gratleiste'),
    (128, N'S-F01-ISO-Gradleiste-1500-R-V01', N'Y:\Gerhard\Listen\FKV\FKV_Gratleiste'),
    (129, N'S-F01-ISO-Gradleiste-1750-L-V01', N'Y:\Gerhard\Listen\FKV\FKV_Gratleiste'),
    (130, N'S-F01-ISO-Gradleiste-1750-R-V01', N'Y:\Gerhard\Listen\FKV\FKV_Gratleiste'),
    (131, N'S-F01-ISO-Gradleiste-1980-L-V01', N'Y:\Gerhard\Listen\FKV\FKV_Gratleiste'),
    (132, N'S-F01-ISO-Gradleiste-1980-R-V01', N'Y:\Gerhard\Listen\FKV\FKV_Gratleiste'),
    (133, N'S-F01-ISO-ZS-1100-1000-20-V01', N'Y:\Gerhard\Listen\FKV\Leistenzuschnitt_20mm_40mm'),
    (134, N'S-F01-ISO-ZS-1100-1000-40-V01', N'Y:\Gerhard\Listen\FKV\Leistenzuschnitt_20mm_40mm'),
    (135, N'S-F01-ISO-ZS-1196-1000-40-V01', N'Y:\Gerhard\Listen\FKV\Leistenzuschnitt_20mm_40mm'),
    (136, N'S-F01-ISO-ZS-1300-1000-17-V01', N'Y:\Gerhard\Listen\FKV\Leistenzuschnitt_20mm_40mm'),
    (137, N'S-F01-ISO-ZS-1500-1000-20-V01', N'Y:\Gerhard\Listen\FKV\Leistenzuschnitt_20mm_40mm'),
    (138, N'S-F01-ISO-ZS-1500-1000-40-V01', N'Y:\Gerhard\Listen\FKV\Leistenzuschnitt_20mm_40mm'),
    (139, N'S-F02-ISO-FRONTVERKLEIDUNG-1100-V01', N'Y:\Gerhard\Listen\FKV\FKV_Innenblenden'),
    (140, N'S-F02-ISO-FRONTVERKLEIDUNG-1500-V01', N'Y:\Gerhard\Listen\FKV\FKV_Innenblenden'),
    (141, N'S-F02-ISO-Frontblende-I-1100-V01', N'Y:\Gerhard\Listen\FKV\FKV_Innenblenden'),
    (142, N'S-F02-ISO-Frontblende-I-1500-V01', N'Y:\Gerhard\Listen\FKV\FKV_Innenblenden'),
    (143, N'S-F02-ISO-Mittelholm-1000-68-V01', N'Y:\1_NESTCAD\FKV\S-F02-ISO-Mittelholm-V01\S-F02-ISO-Mittelholm-1000-68-V01'),
    (144, N'S-F14-ISO-Gradleiste-1100-L-V01', N'Y:\Gerhard\Listen\FKV\FKV_Gratleiste'),
    (145, N'S-F14-ISO-Gradleiste-1100-R-V01', N'Y:\Gerhard\Listen\FKV\FKV_Gratleiste'),
    (146, N'S-F14-ISO-Gradleiste-1195-L-V01', N'Y:\Gerhard\Listen\FKV\FKV_Gratleiste'),
    (147, N'S-F14-ISO-Gradleiste-1195-R-V01', N'Y:\Gerhard\Listen\FKV\FKV_Gratleiste'),
    (148, N'S-F14-ISO-Gradleiste-1500-L-V01', N'Y:\Gerhard\Listen\FKV\FKV_Gratleiste'),
    (149, N'S-F14-ISO-Gradleiste-1500-R-V01', N'Y:\Gerhard\Listen\FKV\FKV_Gratleiste'),
    (150, N'S-WKS2-ISO-Aussteifung-Hinten-ZS-1000-1260-40-V01', N'Y:\Gerhard\Listen\WKS\Aussteifung_vorne_34mm_hinten_40mm'),
    (151, N'S-WKS2-ISO-Aussteifung-Vorne-ZS-1000-1260-34-V01', N'Y:\Gerhard\Listen\WKS\Aussteifung_vorne_34mm_hinten_40mm'),
    (152, N'S-WKS2-ISO-Boden-1-670-715-V01', N'Y:\1_NESTCAD\WKS\Boden_667x490x40_mit_Ausnehmung_Var_2'),
    (153, N'S-WKS2-ISO-Boden-2-670-715-SL-LI-V01', N'Y:\Gerhard\Listen\WKS\Boden_Schloßkasten'),
    (154, N'S-WKS2-ISO-Boden-2-670-715-SL-RE-V01', N'Y:\Gerhard\Listen\WKS\Boden_Schloßkasten'),
    (155, N'S-WKS2-ISO-Boden-2-670-715-V01', N'ist enthalten in S-WKS2-I-Boden-1-670-715-V01'),
    (156, N'S-WKS2-ISO-Decke-1-670-715-V01', N'Y:\1_NESTCAD\WKS\S_WKS2_ISO_Decke'),
    (157, N'S-WKS2-ISO-Decke-2-670-715-V01', N'ist enthalten in S-WKS2-ISO-Decke-1-670-715-V01'),
    (158, N'S-WKS2-ISO-Glas-Doppelbreit-34-V01', N'Y:\1_NESTCAD\WKS\S_WKS2_ISO_Glas_Doppelbreit_34_V01'),
    (159, N'S-WKS2-ISO-Rueckwand-670-1730-V01', N'Y:\1_NESTCAD\WKS\RW_NEU_1110mm_1591x540x30'),
    (160, N'S-WKS2-ISO-Seitenwand-715-1730-V01', N'Y:\1_NESTCAD\WKS\Seite_NEU_1110mm_1591x497x30'),
    (161, N'S-WKS2-ISO-Twin-Kundenseite-670-170-V01', N'Y\Nestcad\WKS\2026\11.2026\23.11.2026_40mm'),
    (162, N'S-WKS2-ISO-VD-Kasten-Hinten-670-170-V01', N'Y:\1_NESTCAD\WKS\S-WKS2-ISO-VD-Kasten-Hinten-670-170-V01'),
    (163, N'S-WKS2-ISO-VD-Kasten-Seite-715-170-V01', N'Y:\1_NESTCAD\WKS\VD_Kasten_Seite_Vorne'),
    (164, N'S-WKS2-ISO-VD-Kasten-Vorne-1-670-170-V01', N'ist enthalten in S-WKS2-ISO-VD-Kasten-Seite-715-170-V01'),
    (165, N'S-WKS2-ISO-VD-Kasten-Vorne-2-670-170-V01', N'ist enthalten in S-WKS2-ISO-VD-Kasten-Seite-715-170-V01'),
    (166, N'S-WKS2-ISO-VD-Kasten-Vorne-3-670-170-V01', N'ist enthalten in S-WKS2-ISO-VD-Kasten-Seite-715-170-V01'),
    (167, N'S-WKS2-ISO-Z-Tuerrahmen-ZS-1000-1110-24-V01', N'Y:\Gerhard\Listen\WKS\Leiste_Türrahmen_1000x1110x24mm'),
    (168, N'S-WKS2-ISO-Zuschnitt-Planen-ZS-1000-700-16-V01', N'Y:\WKS\Planen\Planen_auf_16mm_1000x700'),
    (169, N'T-EKW-ISO-Boden-1230-620', N'Y:\Gerhard\Listen\SCHÄUMEREI_Unten_Ali T-EKW_Boden_Längs_Querteil\EIS_TKW_1230_670_830_Querteil'),
    (170, N'T-EKW-ISO-Boden-670-620', N'Y:\Gerhard\Listen\SCHÄUMEREI_Unten_Ali T-EKW_Boden_Längs_Querteil\EIS_TKW_1230_670_830_Querteil'),
    (171, N'T-EKW-ISO-Boden-830-620', N'Y:\Gerhard\Listen\SCHÄUMEREI_Unten_Ali T-EKW_Boden_Längs_Querteil\EIS_TKW_1230_670_830_Querteil'),
    (172, N'T-EKW-ISO-Längs-1230-540', N'Y:\Gerhard\Listen\SCHÄUMEREI_Unten_Ali T-EKW_Boden_Längs_Querteil\EIS_TKW_1230_670_830_Querteil'),
    (173, N'T-EKW-ISO-Längs-670-540', N'Y:\Gerhard\Listen\SCHÄUMEREI_Unten_Ali T-EKW_Boden_Längs_Querteil\EIS_TKW_1230_670_830_Querteil'),
    (174, N'T-EKW-ISO-Längs-830-540', N'Y:\Gerhard\Listen\SCHÄUMEREI_Unten_Ali T-EKW_Boden_Längs_Querteil\EIS_TKW_1230_670_830_Querteil'),
    (175, N'T-EKW-ISO-Querteil-620-540', N'Y:\Gerhard\Listen\SCHÄUMEREI_Unten_Ali T-EKW_Boden_Längs_Querteil\EIS_TKW_1230_670_830_Querteil'),
    (176, N'T-EKW-ISO-SCHIEBEDECKEL-OBEN-1230-620', N'Y:\Gerhard\Listen\TK\Tk_Wanne_Schiebedeckel\Schiebedeckel_NEU_1mm_Kleiner_ab_13.11.2024.xmlst'),
    (177, N'T-EKW-ISO-SCHIEBEDECKEL-OBEN-830-620', N'Y:\Gerhard\Listen\TK\Tk_Wanne_Schiebedeckel\Schiebedeckel_NEU_1mm_Kleiner_ab_13.11.2024.xmlst'),
    (178, N'T-EKW-ISO-SCHIEBEDECKEL-UNTEN-1230-620', N'Y:\Gerhard\Listen\TK\Tk_Wanne_Schiebedeckel\Schiebedeckel_NEU_1mm_Kleiner_ab_13.11.2024.xmlst'),
    (179, N'T-EKW-ISO-SCHIEBEDECKEL-UNTEN-830-620', N'Y:\Gerhard\Listen\TK\Tk_Wanne_Schiebedeckel\Schiebedeckel_NEU_1mm_Kleiner_ab_13.11.2024.xmlst');

-- ---- Belegestation (G-BL-..., 51 Zeilen, Excel-Zeilen 31-81) ---------------------------------------------
-- Rueckfrage 16 offen: Antwort "leer lassen" -> diesen INSERT-Block (bis zum naechsten Kommentar) entfernen.
INSERT INTO #Liste (Zeile, Matchcode, PfadRoh) VALUES
    (31, N'G-BL-ISO-A-AUFSATZ-HINTEN-ZS-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_40mm'),
    (32, N'G-BL-ISO-A-AUFSATZ-SEITE-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm'),
    (33, N'G-BL-ISO-A-AUFSATZ-VORNE-FACH-250-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm'),
    (34, N'G-BL-ISO-A-AUFSATZ-VORNE-FACH-340-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm'),
    (35, N'G-BL-ISO-A-AUFSATZ-VORNE-FACH-430-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm'),
    (36, N'G-BL-ISO-A-FACHBODEN-250-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm'),
    (37, N'G-BL-ISO-A-FACHBODEN-340-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm'),
    (38, N'G-BL-ISO-A-FACHBODEN-430-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm'),
    (39, N'G-BL-ISO-A-RW-LR-760-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm'),
    (40, N'G-BL-ISO-A-SW-FACH-760-760-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm'),
    (41, N'G-BL-ISO-A-SW-OHNE-760-760-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm'),
    (42, N'G-BL-ISO-Auflage-Luefterblech-ZS-1495-80-V01', N'Y:\GN\Belegestationen\Iso_20mm'),
    (43, N'G-BL-ISO-Boden-1320-760-V01', N'Y:\GN\Belegestationen\Iso_40mm'),
    (44, N'G-BL-ISO-Boden-1760-760-V01', N'Y:\GN\Belegestationen\Iso_40mm'),
    (45, N'G-BL-ISO-Boden-880-760-V01', N'Y:\GN\Belegestationen\Iso_40mm'),
    (46, N'G-BL-ISO-Boden-Hinten-ZS-1495-140-22-V01', N'Y:\GN\Belegestationen\Iso_22mm'),
    (47, N'G-BL-ISO-Decke-1320-760-V01', N'Y:\GN\Belegestationen\Iso_40mm'),
    (48, N'G-BL-ISO-Decke-1760-760-V01', N'Y:\GN\Belegestationen\Iso_40mm'),
    (49, N'G-BL-ISO-Decke-880-760-V01', N'Y:\GN\Belegestationen\Iso_40mm'),
    (50, N'G-BL-ISO-Decke-Stehend-ZS-1495-100-V01', N'Y:\GN\Belegestationen\Iso_40mm'),
    (51, N'G-BL-ISO-ECKPROFIL-ZS-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_40mm'),
    (52, N'G-BL-ISO-Fach-Boden-ZS-1495-388-V01', N'Y:\GN\Belegestationen\Iso_40mm\G-BL-ISO-Fach-Boden-ZS-1495-388-V01'),
    (53, N'G-BL-ISO-Fach-Seitlich-A-V01', N'Y:\GN\Belegestationen\Iso_20mm'),
    (54, N'G-BL-ISO-Fach-Seitlich-I-V01', N'Y:\GN\Belegestationen\Iso_20mm'),
    (55, N'G-BL-ISO-Fach-Vorne-ZS-1495-120-V01', N'Y:\GN\Belegestationen\Iso_40mm'),
    (56, N'G-BL-ISO-I-AUFSATZ-FACH-SEITE-L-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm'),
    (57, N'G-BL-ISO-I-AUFSATZ-FACH-SEITE-R-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm'),
    (58, N'G-BL-ISO-I-AUFSATZ-SEITE-L-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm'),
    (59, N'G-BL-ISO-I-AUFSATZ-SEITE-R-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm'),
    (60, N'G-BL-ISO-I-AUFSATZ-VORNE-ZS-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm'),
    (61, N'G-BL-ISO-I-FACHBODEN-250-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm'),
    (62, N'G-BL-ISO-I-FACHBODEN-340-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm'),
    (63, N'G-BL-ISO-I-FACHBODEN-430-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm'),
    (64, N'G-BL-ISO-I-RW-OBEN-ZS-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm'),
    (65, N'G-BL-ISO-I-SW-FACH-760-760-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm'),
    (66, N'G-BL-ISO-I-SW-OHNE-760-760-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm'),
    (67, N'G-BL-ISO-Oberplatte-Hinten-ZS-1495-152-V01', N'Y:\GN\Belegestationen\Iso_20mm'),
    (68, N'G-BL-ISO-Oberplatte-Seitlich-ZS-425-151-V01', N'Y:\GN\Belegestationen\Iso_20mm'),
    (69, N'G-BL-ISO-Rueckwand-1320-760-V01', N'Y:\GN\Belegestationen\Iso_40mm'),
    (70, N'G-BL-ISO-Rueckwand-1760-760-V01', N'Y:\GN\Belegestationen\Iso_40mm'),
    (71, N'G-BL-ISO-Rueckwand-880-760-V01', N'Y:\GN\Belegestationen\Iso_40mm'),
    (72, N'G-BL-ISO-Rueckwand-Oben-ZS-1495-77-V01', N'Y:\GN\Belegestationen\Iso_40mm'),
    (73, N'G-BL-ISO-Rueckwand-Seitlich-250-V01', N'Y:\GN\Belegestationen\Iso_40mm'),
    (74, N'G-BL-ISO-Rueckwand-Seitlich-340-V01', N'Y:\GN\Belegestationen\Iso_40mm'),
    (75, N'G-BL-ISO-Rueckwand-Seitlich-430-V01', N'Y:\GN\Belegestationen\Iso_40mm'),
    (76, N'G-BL-ISO-Seitenwand-760-760-A-V01', N'Y:\GN\Belegestationen\Iso_20mm'),
    (77, N'G-BL-ISO-Seitenwand-760-760-I-V01', N'Y:\GN\Belegestationen\Iso_20mm'),
    (78, N'G-BL-ISO-Seitenwand-Fach-760-760-A-V01', N'Y:\GN\Belegestationen\Iso_20mm'),
    (79, N'G-BL-ISO-Seitenwand-Fach-760-760-I-V01', N'Y:\GN\Belegestationen\Iso_20mm'),
    (80, N'G-BL-ISO-ZUSCHNITT-140-20-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm'),
    (81, N'G-BL-ISO-ZUSCHNITT-98-20-V01', N'Y:\GN\Belegestationen\Belegestation_und_Marine_20mm\ISO_20mm');
-- ---- Ende Belegestation ----

-- Umrechnung und Zuordnung je Listenzeile
SELECT l.Zeile, LTRIM(RTRIM(l.Matchcode)) AS Matchcode, LTRIM(RTRIM(l.PfadRoh)) AS PfadRoh, p.Pfad,
       CAST(CASE WHEN p.Pfad IS NULL THEN N'kein gueltiger Pfad (weder Y:\ noch \\)'
                 WHEN LEN(p.Pfad) > 500 THEN N'Pfad laenger als 500 Zeichen' END AS NVARCHAR(600)) AS Grund,
       a.Anzahl, a.ArticleId, a.CamPathAlt
INTO #Kand
FROM #Liste l
CROSS APPLY (SELECT LTRIM(RTRIM(l.PfadRoh)) AS t) x
CROSS APPLY (SELECT CASE WHEN LEFT(x.t, 3) = N'Y:\' THEN N'\\idealfs1\Daten$\Produktion\CAM\' + SUBSTRING(x.t, 4, 1000)
                         WHEN LEFT(x.t, 2) = N'\\' THEN x.t END AS Pfad) p
OUTER APPLY (SELECT COUNT(*) AS Anzahl, MIN(ar.Id) AS ArticleId, MIN(ar.CamPath) AS CamPathAlt
             FROM dbo.Articles ar WHERE LTRIM(RTRIM(ar.Matchcode)) = LTRIM(RTRIM(l.Matchcode))) a;

UPDATE #Kand SET Grund = N'doppelt in der Liste (verschiedene Pfade)'
 WHERE Grund IS NULL
   AND Matchcode IN (SELECT Matchcode FROM #Kand WHERE Pfad IS NOT NULL GROUP BY Matchcode HAVING COUNT(DISTINCT Pfad) > 1);
UPDATE #Kand SET Grund = N'Matchcode unbekannt' WHERE Grund IS NULL AND Anzahl = 0;
UPDATE #Kand SET Grund = CONCAT(N'Matchcode mehrdeutig (', Anzahl, N' Artikel)') WHERE Grund IS NULL AND Anzahl > 1;
UPDATE #Kand SET Grund = CONCAT(N'schon gepflegt: ', CamPathAlt) WHERE Grund IS NULL AND CamPathAlt IS NOT NULL AND CamPathAlt <> Pfad;

PRINT N'--- Zaehlung (Trockenlauf und Ausfuehrung)';
SELECT COUNT(DISTINCT CASE WHEN Grund IS NULL AND CamPathAlt IS NULL THEN ArticleId END) AS Schreiben,
       SUM(CASE WHEN Grund IS NULL AND CamPathAlt IS NOT NULL THEN 1 ELSE 0 END) AS BereitsGleich,
       SUM(CASE WHEN Grund IS NOT NULL THEN 1 ELSE 0 END) AS Meldeliste,
       COUNT(*) AS ListenZeilen
FROM #Kand;

PRINT N'--- Schreiben: Matchcode -> Fraesen-Pfad';
SELECT k.Matchcode, k.Pfad, MIN(k.Zeile) AS Zeile
FROM #Kand k WHERE k.Grund IS NULL AND k.CamPathAlt IS NULL
GROUP BY k.Matchcode, k.Pfad ORDER BY k.Matchcode;

PRINT N'--- Meldeliste: von Hand pflegen (Artikelinfo > Fraesen-Pfad) oder Liste korrigieren';
SELECT Zeile, Matchcode, PfadRoh, Grund FROM #Kand WHERE Grund IS NOT NULL ORDER BY Zeile;

IF @Trockenlauf = 1
BEGIN
    DECLARE @Treffer INT = (SELECT COUNT(DISTINCT ArticleId) FROM #Kand WHERE Grund IS NULL AND CamPathAlt IS NULL);
    PRINT CONCAT(N'TREFFER: ', @Treffer);
    PRINT N'TROCKENLAUF: nichts geaendert. Ausfuehren mit @Trockenlauf = 0 (vorher DB-Backup, Zaehlung freigeben).';
    RETURN;
END

BEGIN TRANSACTION;
    UPDATE ar
       SET ar.CamPath           = k.Pfad,
           ar.ModifiedAt        = GETDATE(),
           ar.ModifiedBy        = N'Einmal-Skript Fräsen-Pfad',
           ar.ModifiedByWindows = LEFT(SUSER_SNAME(), 200)
      FROM dbo.Articles ar
      JOIN (SELECT DISTINCT ArticleId, Pfad FROM #Kand WHERE Grund IS NULL AND CamPathAlt IS NULL) k ON k.ArticleId = ar.Id
     WHERE ar.CamPath IS NULL;   -- Doppelsicherung: Handpflege nie ueberschreiben
    PRINT CONCAT(N'Fraesen-Pfad gesetzt: ', @@ROWCOUNT, N' Artikel.');
COMMIT TRANSACTION;

DROP TABLE #Kand;
DROP TABLE #Liste;
