using IdealAkeWms.Data.Repositories;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>Reiter „Arbeitsgänge" der BDE-Stammdaten (Spec 2026-10-09-arbeitsgang-stammdaten-vereinheitlichen).</summary>
public class OperationConfigIndexViewModel
{
    /// <summary>Seite der gefilterten Arbeitsgang-Stammdaten.</summary>
    public List<OperationConfig> Configs { get; set; } = new();

    /// <summary>Kuerzel ohne Eintrag aus OSEON und WMS-Arbeitsgaengen (ohne „01").</summary>
    public List<UnconfiguredOperation> Unconfigured { get; set; } = new();

    /// <summary>Eintraege, die getrimmt und case-insensitiv gleich sind (Altdaten) — Warnhinweis, „melden statt still".</summary>
    public List<string> DuplicateNames { get; set; } = new();

    /// <summary>Arbeitsgang-Abgleich eingeschaltet (<c>Sync:OperationConfigSyncEnabled</c>) — nur dann wird „nicht in Sage" angezeigt.</summary>
    public bool MarkerActive { get; set; }

    /// <summary>Alle gekennzeichneten Eintraege (seitenuebergreifend, fuer den Hinweis oberhalb der Tabelle).</summary>
    public List<OperationConfig> NotInSage { get; set; } = new();

    /// <summary>Handwert und Sage-Wert der Sage-Arbeitsgangnummer verschieden (Banner „Abweichung Sage ↔ Hand“, beide Standorte).</summary>
    public List<OperationConfig> Abweichungen { get; set; } = new();

    /// <summary>Pruefliste der Sage-Zuordnung; null = aus (Master ProduktionsauftragHierarchisch nicht an, z. B. AKE).</summary>
    public Pruefliste? Pruefliste { get; set; }

    public PaginationState Pagination { get; set; } = new();
}

/// <summary>Pruefliste (a): Kuerzel aus den FA-Strukturknoten ohne wirksame Sage-Arbeitsgangnummer, mit Anzahl der Knoten.</summary>
public sealed record KuerzelOhneSageAg(string Kuerzel, string Grund, int Knoten);

/// <summary>Pruefliste (b2): Werkbank, deren Nummer der Sage-Arbeitsgangnummer der genannten Arbeitsgaenge gleicht.</summary>
public sealed record WerkbankNummerGleichAg(ProductionWorkplace Werkbank, string Arbeitsgaenge);

/// <summary>Pruefliste der Sage-Zuordnung (Spec 2026-11-16-arbeitsgang-mapping-sage-arbeitsgangnummer FA-6), reine Anzeige.</summary>
public sealed record Pruefliste(
    List<KuerzelOhneSageAg> KuerzelOhneSageAg, int KuerzelOhneSageAgGesamt,
    List<ProductionWorkplace> WerkbaenkeOhneArbeitsplatz, List<WerkbankNummerGleichAg> WerkbankNummerGleichAg);
