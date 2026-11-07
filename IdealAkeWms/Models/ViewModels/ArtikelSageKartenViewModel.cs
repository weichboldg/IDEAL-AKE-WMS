namespace IdealAkeWms.Models.ViewModels;

/// <summary>v1.85.0 (Spec 2026-11-08-artikeldetails-lieferanten-anzeigen, Nachbesserung 1): Daten des Partials <c>_ArtikelSageKarten</c>
/// (Sage-Zusatzfelder + Lieferanten) fuer Artikelinfo und „Artikel bearbeiten“, nur lesend.</summary>
public class ArtikelSageKartenViewModel
{
    public ArticleSageDetail? SageDetail { get; set; }
    public Creditor? HauptlieferantKreditor { get; set; }
    public Creditor? LetzterLieferantKreditor { get; set; }
    public List<ArticleSupplierRow> Lieferanten { get; set; } = new();
}
