using IdealAkeWms.Services;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>v1.99.32 (Spec 2026-11-13-bde-vereinfacht-teileliste-blechfertigung): Terminal-Seite BDE-Teileliste (Einzeln/Summiert/Anmelden).</summary>
public class BdeTeilelisteViewModel
{
    public ProductionWorkplace Werkbank { get; set; } = null!;
    public string Kuerzel { get; set; } = string.Empty;
    public List<ProductionWorkplace> Werkbaenke { get; set; } = new();
    public BdeOperator? Werker { get; set; }
    public string? AnmeldeFehler { get; set; }

    public bool Summiert { get; set; }
    public string? KwVon { get; set; }
    public string? KwBis { get; set; }
    public bool GemeldeteZeigen { get; set; }

    public IReadOnlyList<TeilelisteZeile> Zeilen { get; set; } = Array.Empty<TeilelisteZeile>();
    public IReadOnlyList<TeilelisteAggregat> Aggregate { get; set; } = Array.Empty<TeilelisteAggregat>();
    public IReadOnlyDictionary<int, VormontageService.KopfWerte> Kopf { get; set; } = new Dictionary<int, VormontageService.KopfWerte>();
    public PaginationState Pagination { get; set; } = new();
    public int Waisen { get; set; }

    /// <summary>E6: Zelltext „Beschichtet“ je Position (null = Spalte ohne Quelle, Zelle „–“).</summary>
    public Func<FaHierarchyNode, string?>? Beschichtet { get; set; }

    /// <summary>v1.99.72: Zeilen-Getter (Zelle = Filter) fuer beide Zeilenarten, <see cref="BdeTeilelisteService.SpaltenZeile"/>.</summary>
    public IReadOnlyDictionary<string, Func<TeilelisteZeile, string?>> Spalten { get; set; } = new Dictionary<string, Func<TeilelisteZeile, string?>>();

    /// <summary>v1.99.72 (F15): Fraesen-Pfad-Spalte nur bei Schalter an.</summary>
    public bool FraesenPfadAktiv { get; set; }

    /// <summary>Aktuelle URL (Pfad + Query) fuer die Rueckkehr nach dem Melden.</summary>
    public string ReturnUrl { get; set; } = string.Empty;
}
