-- =====================================================================================================
-- IDEAL-AKE WMS — Neuinstallation PRODUKTIVSYSTEM IDEAL — Teil 2: Standort-Konfiguration IDEAL
-- Stand: Bündel v1.71.1
--
-- Reihenfolge: Teil 1 (Schema)  ->  Web-App EINMAL starten (Seite aufrufen; legt fehlende Rollen,
--              AppSettings und ALLE ServiceSettings aus dem Katalog an)  ->  DIESES Skript  ->  Dienst starten.
-- Der Windows-Dienst darf vorher NICHT laufen: Er würde sonst mit den Standardwerten syncen
-- (u. a. Sync:ProductionOrdersEnabled = true -> AKE-View, die es bei IDEAL nicht gibt).
--
-- Ausführung: SSMS oder  sqlcmd -S <Server> -E -b -f 65001 -i "IDEAL-Prod-2-Konfiguration.sql"
-- Wiederholbar: setzt nur Werte, legt fehlende Schlüssel an, löscht nichts.
--
-- Werte, die du VOR dem Lauf prüfen/ersetzen musst, stehen im Abschnitt "PARAMETER" (Platzhalter <...>).
-- Quellen der IDEAL-Werte: secondbrain\aufgaben\2026-08-07-ideal-teile-1-5.md (UAT IDEAL, u. a.
-- ProductionOrdersEnabled=false, PPSImport-Grant), tests\2026-09-08-uat-protokoll-ideal-buendel-chrome.md,
-- README.md (Bündel) Z. 407-429 und 826-834, Einstellungen idealweb01 Stand 20.10.2026.
--
-- NICHT in diesem Skript (bewusst):
--   * Master "ProduktionsauftragHierarchisch" (Einwegtor) -> nur über die Seite /HierarchieUmstellung,
--     nach dem ersten erfolgreichen Struktur-Import (siehe Abschnitt "Danach" am Ende).
--   * Werkbank-Anlage aus Sage und Arbeitsgang-Erkennung -> erst nach dem Master, mit DryRun.
--   * Benutzer/Passwörter. admin hat nach der Installation ein LEERES Passwort -> sofort ändern.
-- =====================================================================================================

USE [IDEAL_AKE_WMS];
GO
SET NOCOUNT ON;
GO

-- ---- Vorbedingung: Teil 1 gelaufen und Web einmal gestartet? -------------------------------------------
IF OBJECT_ID(N'dbo.ServiceSettings', N'U') IS NULL
   OR NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N'ProduktionsauftragHierarchisch')
   OR NOT EXISTS (SELECT 1 FROM dbo.AppSettings     WHERE [Key] = N'FaHierarchyMaxTiefe')
BEGIN
    RAISERROR(N'ABBRUCH: Die Web-App wurde noch nicht gestartet (Katalog-Schlüssel fehlen). Erst Seite aufrufen, dann dieses Skript.', 16, 1) WITH NOWAIT;
    SET NOEXEC ON;
END
GO

IF EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N'ProduktionsauftragHierarchisch' AND [Value] = N'true')
    PRINT N'HINWEIS: Master ProduktionsauftragHierarchisch ist bereits true — dieses Skript ändert ihn nicht.';
GO

-- =====================================================================================================
-- PARAMETER — vor dem Lauf prüfen
-- =====================================================================================================
DECLARE @AppBaseUrl          nvarchar(500) = N'<https://wms.ideal-ake.at>';            -- Adresse der Web-App (Links in Mails)
DECLARE @Fehlermail          nvarchar(500) = N'<it@ideal-ake.at>';                     -- Empfänger Sync-Fehlermails (kommagetrennt), leer = aus
DECLARE @BriefkopfPfad       nvarchar(500) = N'<C:\inetpub\IDEALAKEWMS\Web\Branding\briefkopf.html>';
DECLARE @Firmenname          nvarchar(500) = N'IDEAL Kältetechnik';                    -- wie idealweb01
DECLARE @Firmenanschrift     nvarchar(500) = N'In der Schörihub 38, 4840 Gmunden';     -- wie idealweb01
DECLARE @FeiertagRegion      nvarchar(10)  = N'AT-4';                                  -- Gmunden = Oberösterreich; bitte bestätigen
DECLARE @ArtikelView         nvarchar(200) = N'[vw_IDEAL_AKE_WMS_Artikel]';            -- erst nach Prüfabfrage P4 (Spec ideal-sage-views-umstellung); leer = alter Select (AKE)
DECLARE @ErsterLaufDryRun    nvarchar(5)   = N'true';                                  -- true: erster Sync-Zyklus nur lesen/protokollieren, danach auf false (Abschnitt "Danach")
DECLARE @BdeAktiv            nvarchar(5)   = N'false';                                 -- idealweb01: true; Produktion bewusst entscheiden
DECLARE @WindowsAuthAktiv    nvarchar(5)   = N'false';                                 -- erst einschalten, NACHDEM die ersten AD-Benutzer angelegt sind
DECLARE @WindowsAuthGruppe   nvarchar(200) = N'<IDEAL-AKE-WMS_Users>';                 -- AD-Gruppe (SAM-Name) für "AD-Benutzer anlegen"

-- =====================================================================================================
-- AppSettings (fachliche Schalter, /Settings und /StandortEinstellungen)
-- =====================================================================================================
DECLARE @App TABLE ([Key] nvarchar(100) PRIMARY KEY, [Value] nvarchar(500) NOT NULL, Grund nvarchar(300));
INSERT INTO @App ([Key], [Value], Grund) VALUES
    -- IDEAL-Module
    (N'FaHierarchyKommissionierlistenAktiv', N'true',  N'IDEAL Kommissionierlisten (UAT IDEAL)'),
    (N'FaHierarchyBeschichtungAktiv',        N'true',  N'IDEAL Beschichtung (UAT IDEAL)'),
    (N'FaHierarchyVormontageAktiv',          N'true',  N'IDEAL Vormontage (UAT IDEAL)'),
    (N'FaHierarchyMaxTiefe',                 N'500',   N'Default'),
    -- Firmendaten / Druck
    (N'Firmenname',                          @Firmenname,      N'Briefkopf-Ersatzkopf'),
    (N'Firmenanschrift',                     @Firmenanschrift, N'Briefkopf-Ersatzkopf'),
    (N'BriefkopfVorlagePfad',                @BriefkopfPfad,   N'Branding\briefkopf.html im Web-Ordner (README Z. 481-506)'),
    -- Module wie idealweb01 (20.10.2026)
    (N'LeitstandAktiv',                      N'true',  N'wie idealweb01'),
    (N'LagerbestellungAktiv',                N'true',  N'wie idealweb01 (Seed-Default)'),
    (N'KommissionierungMitZuweisung',        N'false', N'wie idealweb01'),
    (N'FaCompletionAktiv',                   N'false', N'AKE-Vorbau, bei IDEAL nicht im Einsatz'),
    (N'TeileverfolgungAktiv',                N'false', N'OSEON gibt es nur bei AKE'),
    (N'BestellungenAktiv',                   N'false', N'wie idealweb01'),
    (N'NegativeBuchungErlaubt',              N'false', N'wie idealweb01'),
    (N'LackierteilKategorieName',            N'',      N'IDEAL: Lack-Kennzeichen kommt aus der Struktur'),
    (N'QrMitFaNummer',                       N'false', N'wie idealweb01'),
    (N'BdeAktiv',                            @BdeAktiv, N'Parameter'),
    (N'WindowsAuthAktiv',                    @WindowsAuthAktiv,  N'Parameter'),
    (N'WindowsAuthBerechtigungsgruppe',      @WindowsAuthGruppe, N'Parameter'),
    -- Links (v1.67): IDEAL ohne Vault; enaio-Links nur, wenn der enaio-Sync läuft (hier aus)
    (N'VaultLinksAktiv',                     N'false', N'README Z. 424: IDEAL ohne Vault'),
    (N'EnaioLinksAktiv',                     N'false', N'kein enaio-Sync am Standort IDEAL; Default-Vorlage zeigt auf AKE'),
    -- QR (v1.68/1.69): erstes Feld Matchcode laut QR-Spec; Trennzeichen erst nach Prüfabfrage umstellen
    (N'QrErstesFeld',                        N'Matchcode', N'aufgaben 2026-10-23 qr-code-matchcode'),
    -- SData-FA-Ansicht (v1.71): aus, bis SData-URL/Dataset/Zugangsdaten für IDEAL feststehen
    (N'SageSdataFaAnsichtAktiv',             N'false', N'braucht SData:BaseUrl, SData:Dataset, Web-Zugangsdaten');

UPDATE a SET a.[Value] = v.[Value]
FROM dbo.AppSettings a JOIN @App v ON v.[Key] = a.[Key];
INSERT INTO dbo.AppSettings ([Key], [Value], [Description])
SELECT v.[Key], v.[Value], N'IDEAL-Installation: ' + v.Grund
FROM @App v WHERE NOT EXISTS (SELECT 1 FROM dbo.AppSettings a WHERE a.[Key] = v.[Key]);

-- =====================================================================================================
-- ServiceSettings (Windows-Dienst, /ServiceSettings) — DB gewinnt, appsettings sind nur Default
-- =====================================================================================================
DECLARE @Svc TABLE ([Key] nvarchar(100) PRIMARY KEY, [Value] nvarchar(500) NOT NULL, Grund nvarchar(300));
INSERT INTO @Svc ([Key], [Value], Grund) VALUES
    -- AKE-Pfade aus
    (N'Sync:ProductionOrdersEnabled',          N'false', N'AKE-Import (vw_AKE_Kommissionierung_WAListe) gibt es bei IDEAL nicht (aufgaben 2026-08-07, UAT 1)'),
    (N'Sync:ProductionOrderReconcileEnabled',  N'false', N'hängt am AKE-Import'),
    (N'Sync:OseonTrackingEnabled',             N'false', N'nur AKE'),
    (N'Sync:OseonArticleCategoryEnabled',      N'false', N'nur AKE'),
    (N'Sync:EnaioDmsEnabled',                  N'false', N'nur AKE'),
    (N'Sync:BomCacheEnabled',                  N'false', N'AKE-Heuristik; im hierarchischen Modus übersprungen'),
    (N'Sync:FaWorkStepDetectionEnabled',       N'false', N'AKE-Heuristik'),
    (N'Sync:CoatingDetectionEnabled',          N'false', N'AKE-Heuristik'),
    (N'Sync:FaZusatzinfoEnabled',              N'false', N'IDEAL-Quelle ungeklärt'),
    (N'SageLagerbuchungAktiv',                 N'false', N'SData für IDEAL noch nicht konfiguriert'),
    -- IDEAL: Artikel, Lager, Struktur
    (N'Sync:ArticlesEnabled',                  N'true',  N'IDEAL-Artikel'),
    (N'Sync:ArticlesViewName',                 @ArtikelView, N'Parameter; gesetzt VOR dem ersten Lauf -> keine 00er-Artikel, Bereinigungsskript entfällt'),
    (N'Sync:LagerplaetzeEnabled',              N'true',  N'wie idealweb01'),
    (N'Sync:LagerbestandEnabled',              N'true',  N'wie idealweb01'),
    (N'Sync:HierarchicalFaEnabled',            N'true',  N'Struktur-Import IDEAL (Views FAListe/FAInfos, Katalog-Defaults)'),
    (N'Sync:FaHierarchyListeViewName',         N'[vw_IDEAL_AKE_WMS_FAListe]', N'Katalog-Default'),
    (N'Sync:FaHierarchyInfosViewName',         N'[vw_IDEAL_AKE_WMS_FAInfos]', N'Katalog-Default'),
    -- erst nach dem Master (Abschnitt "Danach")
    (N'Sync:ProductionWorkplaceSyncEnabled',   N'false', N'erst nach Master-Flip, mit DryRun'),
    (N'Sync:WorkOperationStructureDetectionEnabled', N'false', N'erst nach Werkbank-Anlage, mit DryRun'),
    (N'Sync:OperationConfigSyncEnabled',       N'false', N'erst nach Werkbank-Anlage'),
    (N'Sync:OperationConfigSyncFremdsystemRelevant', N'false', N'Katalog: IDEAL false'),
    -- Feiertage, Bereinigung, Worker
    (N'Sync:FeiertagSyncEnabled',              N'true',  N'Arbeitstags-Rechnung'),
    (N'Sync:FeiertagCountryCode',              N'AT',    N''),
    (N'Sync:FeiertagRegion',                   @FeiertagRegion, N'Parameter'),
    (N'Cleanup:AktivitaetsprotokollAufbewahrungTage', N'180', N'Default'),
    (N'WorkerSettings:SyncDryRun',             @ErsterLaufDryRun, N'Parameter: erster Zyklus nur lesen'),
    -- Mails
    (N'ErrorNotification:Enabled',             CASE WHEN @Fehlermail = N'' THEN N'false' ELSE N'true' END, N'Parameter'),
    (N'ErrorNotification:Recipients',          @Fehlermail, N'Parameter'),
    (N'Notifications:AppBaseUrl',              @AppBaseUrl, N'Parameter'),
    (N'Sync:PartRequisitionEmailEnabled',      N'false', N'Bedarfsmeldungen aus'),
    (N'Sync:WarehouseRequisitionEmailEnabled', N'false', N'erst einschalten, wenn Empfänger gepflegt sind');

UPDATE s SET s.[Value] = v.[Value]
FROM dbo.ServiceSettings s JOIN @Svc v ON v.[Key] = s.[Key];
INSERT INTO dbo.ServiceSettings ([Key], [Value], [Category], [Description])
SELECT v.[Key], v.[Value], N'IDEAL-Installation', v.Grund
FROM @Svc v WHERE NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings s WHERE s.[Key] = v.[Key]);
GO

-- =====================================================================================================
-- Rollen, die kein Seed anlegt (README Z. 419; aufgaben 2026-08-07 "Rolle NICHT im Program.cs-Seed")
-- =====================================================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE [Key] = N'beschichtungsauftrag')
    INSERT INTO dbo.Roles ([Key], [Name], [Description], [IsSystem], [SortOrder], [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES (N'beschichtungsauftrag', N'Beschichtungsauftrag', N'Beschichtung: Vorschlagsliste und Aufträge lesen und drucken (IDEAL)',
            0, (SELECT ISNULL(MAX([SortOrder]), 0) + 1 FROM dbo.Roles), GETDATE(), N'IDEAL-Installation', SYSTEM_USER);
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE [Key] = N'beschichtungsauftrag_schreiben')
    INSERT INTO dbo.Roles ([Key], [Name], [Description], [IsSystem], [SortOrder], [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES (N'beschichtungsauftrag_schreiben', N'Beschichtungsauftrag anlegen', N'Beschichtungsaufträge anlegen, bearbeiten, übergeben (IDEAL)',
            0, (SELECT ISNULL(MAX([SortOrder]), 0) + 1 FROM dbo.Roles), GETDATE(), N'IDEAL-Installation', SYSTEM_USER);
GO

-- =====================================================================================================
-- Kontrolle
-- =====================================================================================================
SET NOEXEC OFF;
GO
SELECT [Key], [Value] FROM dbo.ServiceSettings
WHERE [Key] IN (N'Sync:ProductionOrdersEnabled', N'Sync:HierarchicalFaEnabled', N'Sync:ArticlesViewName',
                N'WorkerSettings:SyncDryRun', N'ProduktionsauftragHierarchisch', N'Sync:ProductionWorkplaceSyncEnabled')
ORDER BY [Key];
SELECT [Key], [Value] FROM dbo.AppSettings
WHERE [Key] LIKE N'FaHierarchy%' OR [Key] IN (N'BriefkopfVorlagePfad', N'VaultLinksAktiv', N'QrErstesFeld')
ORDER BY [Key];
SELECT [Key], [Name] FROM dbo.Roles WHERE [Key] LIKE N'beschichtungsauftrag%';
IF EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Value] LIKE N'<%>') OR EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Value] LIKE N'<%>')
    PRINT N'WARNUNG: Es stehen noch Platzhalter <...> in den Einstellungen — Parameter oben ersetzen und Skript erneut ausführen.';
GO

-- =====================================================================================================
-- Danach (Reihenfolge laut SyncWorker.cs und Spec ideal-bde-arbeitsgaenge-aus-struktur, von Hand):
--   1. Dienst starten. Erster Zyklus mit DryRun: /SyncLog -> FaHierarchy, Article, Lagerplatz, Lagerbestand
--      lesen ohne Fehler (Counts plausibel).
--   2. WorkerSettings:SyncDryRun = false (/ServiceSettings). Nächster Zyklus: Struktur-Import schreibt,
--      /FaHierarchy zeigt Strukturen, Artikel sind da.
--   3. Master scharf: Seite /HierarchieUmstellung (Link "Umschalten" in Stammdaten > Standorteinstellungen).
--      Einwegtor — vorher Backup. Danach Materialisierung im nächsten Zyklus.
--   4. Sync:ProductionWorkplaceSyncEnabled = true mit SyncDryRun = true -> Lauf prüfen -> DryRun false.
--      Neue Werkbänke sind BDE-inaktiv. Quelle bleibt KHKPpsArbeitsplaetze bis Prüfabfrage P3b
--      (README Z. 826-834), dann [vw_IDEAL_AKE_WMS_Arbeitsgaenge].
--   5. Sync:OperationConfigSyncEnabled = true; Sync:WorkOperationStructureDetectionEnabled = true (DryRun wie 4).
--   6. Mail-Schalter erst, wenn Empfänger und Empfängergruppen gepflegt sind.
-- =====================================================================================================
