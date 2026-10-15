namespace IDEALAKEWMSService.Services;

/// <summary>
/// Reiner, unit-testbarer Baustein des Artikel-Syncs (Spec 2026-10-14-ideal-sage-views-umstellung): waehlt das
/// Sage-SQL. Ohne View (<c>Sync:ArticlesViewName</c> leer) der bisherige Select ZEICHENGLEICH; mit View ein Select
/// mit derselben Spaltenreihenfolge und denselben Aliasen, damit Reader und Upsert unveraendert bleiben.
/// </summary>
public static class SageArticleSql
{
    /// <summary>Der bisherige Select (AKE und IDEAL bis zur Umstellung), unveraendert aus SageImportService.</summary>
    public const string LegacySql = """
        WITH RawArticles AS (
            SELECT DISTINCT
                CAST(r.Ressourcenummer AS nvarchar(100))   AS ArticleNumber,
                CAST(a.Bezeichnung1 AS nvarchar(500))      AS Description,
                CAST(a.Lagermengeneinheit AS nvarchar(20)) AS Unit,
                CAST(a.Artikelgruppe AS nvarchar(100))     AS ArticleGroup,
                CAST(v.Meldebestand AS nvarchar(20))       AS ReorderLevel,
                CAST(lp.Kurzbezeichnung AS nvarchar(100))  AS PrimaryStorageLocation,
                CAST(a.Matchcode AS nvarchar(200))         AS Matchcode
            FROM [dbo].[KHKPpsRessourcenPositionen] r
            LEFT JOIN [dbo].[KHKArtikel] a ON a.Artikelnummer = r.Ressourcenummer
            LEFT JOIN [dbo].[KHKArtikelvarianten] v ON a.Artikelnummer = v.Artikelnummer
            LEFT JOIN [dbo].[KHKLagerplaetze] lp ON a.PlatzID = lp.PlatzID
            WHERE r.Ressourcenummer IS NOT NULL AND r.Ressourcenummer != ''

            UNION

            SELECT
                CAST(a.Artikelnummer AS nvarchar(100))     AS ArticleNumber,
                CAST(a.Bezeichnung1 AS nvarchar(500))      AS Description,
                CAST(a.Lagermengeneinheit AS nvarchar(20)) AS Unit,
                CAST(a.Artikelgruppe AS nvarchar(100))     AS ArticleGroup,
                CAST(v.Meldebestand AS nvarchar(20))       AS ReorderLevel,
                CAST(lp.Kurzbezeichnung AS nvarchar(100))  AS PrimaryStorageLocation,
                CAST(a.Matchcode AS nvarchar(200))         AS Matchcode
            FROM [dbo].[KHKArtikel] a
            LEFT JOIN [dbo].[KHKArtikelvarianten] v ON a.Artikelnummer = v.Artikelnummer
            LEFT JOIN [dbo].[KHKLagerplaetze] lp ON a.PlatzID = lp.PlatzID
            WHERE a.IstBestellartikel = -1 AND a.Aktiv = -1
        )
        SELECT
            ArticleNumber,
            MAX(Description)  AS Description,
            MAX(Unit)         AS Unit,
            MAX(ArticleGroup) AS ArticleGroup,
            MAX(ReorderLevel) AS ReorderLevel,
            MAX(PrimaryStorageLocation) AS PrimaryStorageLocation,
            MAX(Matchcode) AS Matchcode
        FROM RawArticles
        WHERE ArticleNumber IS NOT NULL AND ArticleNumber != ''
        GROUP BY ArticleNumber
        """;

    /// <summary>
    /// Select auf die WMS-Artikel-View. Spaltennamen = die des bisherigen Selects (Antwort 4 „sollten gleich sein“,
    /// UNVERIFIZIERT bis Pruefabfrage P4). Fehlt eine Spalte, scheitert der Lauf laut (Fehler + Mail).
    /// GROUP BY/MAX wie im bisherigen Select: Schutz gegen Dubletten in der View.
    /// </summary>
    public static string BuildViewSql(string quotedView) => $"""
        SELECT
            CAST(Artikelnummer AS nvarchar(100))           AS ArticleNumber,
            MAX(CAST(Bezeichnung1 AS nvarchar(500)))       AS Description,
            MAX(CAST(Lagermengeneinheit AS nvarchar(20)))  AS Unit,
            MAX(CAST(Artikelgruppe AS nvarchar(100)))      AS ArticleGroup,
            MAX(CAST(Meldebestand AS nvarchar(20)))        AS ReorderLevel,
            MAX(CAST(Kurzbezeichnung AS nvarchar(100)))    AS PrimaryStorageLocation,
            MAX(CAST(Matchcode AS nvarchar(200)))          AS Matchcode
        FROM {quotedView}
        WHERE Artikelnummer IS NOT NULL AND CAST(Artikelnummer AS nvarchar(100)) != ''
        GROUP BY CAST(Artikelnummer AS nvarchar(100))
        """;

    /// <param name="quotedView">null = keine View konfiguriert; sonst bereits per ValidateAndQuote geprueft.</param>
    public static string Select(string? quotedView) => quotedView is null ? LegacySql : BuildViewSql(quotedView);
}
