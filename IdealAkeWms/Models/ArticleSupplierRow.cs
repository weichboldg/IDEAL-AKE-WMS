namespace IdealAkeWms.Models;

/// <summary>
/// v1.80.0 (Spec 2026-11-08-artikeldetails-lieferanten-anzeigen): eine Zeile der Karte „Lieferanten“ in der Artikelinfo – Projektion
/// <c>ArticleSuppliers</c> ⨝ <c>Creditors</c>, nur lesend. Einzelpreis bewusst nicht (Freigabe-Antwort 3: Artikelinfo sieht jeder mit masterdata_read).
/// </summary>
public sealed record ArticleSupplierRow(int CreditorId, string Name, string? SageKto, bool IsActive, string? SageBestellnummer,
    decimal? Mindestbestellmenge, string? Einkaufsmengeneinheit, int? Wiederbeschaffungszeit, bool IstHauptlieferant,
    decimal? UmrechnungsfaktorEK = null);

/// <summary>v1.85.0 Nachbesserung 2: eine Zeile der Karte „Artikel dieses Lieferanten“ (Kreditor-Details).</summary>
public sealed record LieferantArtikelRow(string ArticleNumber, string? Description, string? SageBestellnummer, decimal? Mindestbestellmenge,
    string? Einkaufsmengeneinheit, bool IstHauptlieferant, bool IstLetzterLieferant);
