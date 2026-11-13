using IdealAkeWms.Services;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>v1.99.20 (Spec 2026-11-08-einkauf-bestellvorschlaege F8): Liste „Einkauf → Bestellvorschläge“.</summary>
public class PurchaseProposalListViewModel
{
    public const string UmfangVorschlaege = "vorschlaege";
    public const string UmfangAlle = "alle";

    public List<PurchaseProposalRow> Items { get; set; } = new();
    public PaginationState Pagination { get; set; } = new();
    public string? Lieferant { get; set; }
    public string? Artikelgruppe { get; set; }
    public string Umfang { get; set; } = UmfangVorschlaege;
    public string? Dringlichkeit { get; set; }
    public List<(string Kto, string Text)> Lieferanten { get; set; } = new();
    public List<string> Artikelgruppen { get; set; } = new();

    // ---- Sammelbanner (melden statt still, F8) ----
    /// <summary>Nachbestellartikel insgesamt (ReorderLevel &gt; 0).</summary>
    public int ArtikelGesamt { get; set; }
    public int OhneSageDetail { get; set; }
    public int OhneArtikelLieferant { get; set; }
    public int BedarfeEinheitAbweichend { get; set; }
    /// <summary>E2: Stand des Caches offener Sage-Bestellungen (max SyncedAt); null = Cache leer → Banner, Spalte „–“.</summary>
    public DateTime? BestellungenStand { get; set; }
    /// <summary>E2: offene Bestellpositionen der Nachbestellartikel mit abweichender Einheit (nicht eingerechnet).</summary>
    public int PositionenEinheitAbweichend { get; set; }
    /// <summary>Welche offenen Bedarfe eingerechnet werden (Einstellung EinkaufOffeneBedarfe), Anzeigetext.</summary>
    public string BedarfeText { get; set; } = "";
}

/// <summary>Eine Zeile der Bestellvorschläge (Artikel + aufgelöste Anzeige + Ergebnis des Rechners).</summary>
public class PurchaseProposalRow
{
    public required Article Article { get; init; }
    public ArticleSupplierRow? Lieferant { get; init; }
    /// <summary>„Name (Kto)“, ersatzweise nur der Kto aus <c>ArticleSageDetail.Hauptlieferant</c>; null = kein Lieferant.</summary>
    public string? LieferantText { get; init; }
    public string? LieferantKto { get; init; }
    public decimal BestandWms { get; init; }
    public decimal OffeneBedarfe { get; init; }
    /// <summary>E2: Summe der Reste offener Sage-Bestellungen (EBB und EBZ); null = Cache leer.</summary>
    public decimal? OffenBestellt { get; init; }
    /// <summary>E2: frühester Liefertermin der Positionen mit Rest &gt; 0.</summary>
    public DateTime? NaechsterLiefertermin { get; init; }
    public required PurchaseProposalResult Ergebnis { get; init; }
    public int? Wbz { get; init; }
    public DateTime? Wunschtermin { get; init; }
    public List<string> Hinweise { get; init; } = new();

    public ArticleSageDetail? Sage => Article.SageDetail;
    public DateTime? SageStand => Sage == null ? null : Sage.ModifiedAt ?? Sage.CreatedAt;
    public string Hinweis => string.Join(" · ", Hinweise.Concat(Ergebnis.Hinweise));
}
