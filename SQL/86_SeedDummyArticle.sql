-- SQL/86_SeedDummyArticle.sql
-- Teil-7: reiner DATEN-Seed (KEIN Schema, KEIN __EFMigrationsHistory-Eintrag).
-- Legt den einen, reservierten DUMMY-Artikel an, den die Werkbank bei unbekannter
-- EK-Nummer waehlt (die individuelle Bezeichnung lebt je Position auf
-- WarehouseRequisitionItem.ArticleDescription, NICHT auf Article.Description).
--
-- ArticleNumber 'DUMMY' liegt ausserhalb des Sage-Namensraums -> der Artikel-Sync
-- (SyncArticlesAsync, Upsert per ArticleNumber, kein Prune) beruehrt ihn nie.
-- Idempotent: zweiter Lauf legt keinen zweiten an.
-- Die Werte hier MUESSEN mit Article.DummyArticleNumber / Article.DummyDefaultDescription
-- uebereinstimmen (C#-Konstanten fuer den "zwingend geaendert"-Vergleich).
SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Articles] WHERE [ArticleNumber] = 'DUMMY')
BEGIN
    INSERT INTO [dbo].[Articles]
        ([ArticleNumber], [Description], [Unit], [ReorderLevel], [ArticleGroup],
         [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES
        ('DUMMY', N'DUMMY – Bezeichnung bitte eintragen', NULL, NULL, NULL,
         GETDATE(), 'System-Seed', 'System-Seed');
    PRINT 'DUMMY-Artikel geseedet.';
END
GO
