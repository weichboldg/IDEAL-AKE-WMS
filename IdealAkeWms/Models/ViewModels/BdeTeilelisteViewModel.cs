using IdealAkeWms.Services;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>v1.99.32 (Spec 2026-11-13-bde-vereinfacht-teileliste-blechfertigung): Terminal-Seite BDE-Teileliste (Einzeln/Summiert/Anmelden).</summary>
public class BdeTeilelisteViewModel
{
    public ProductionWorkplace Werkbank { get; set; } = null!;
    /// <summary>v1.99.76: null an Werkbaenken ohne Kuerzel (AKE) – dann gibt es nur die FA-Sicht.</summary>
    public string? Kuerzel { get; set; }
    public List<ProductionWorkplace> Werkbaenke { get; set; } = new();
    public BdeOperator? Werker { get; set; }
    public string? AnmeldeFehler { get; set; }

    /// <summary>v1.99.76 (E4-7): Reiter "einzeln" | "summiert" | "fa".</summary>
    public string Sicht { get; set; } = "einzeln";
    public bool Summiert => Sicht == "summiert";
    public bool IstFa => Sicht == "fa";

    /// <summary>v1.99.76: Master <c>ProduktionsauftragHierarchisch</c> (false = AKE: keine Teile-Reiter).</summary>
    public bool Hierarchisch { get; set; }

    /// <summary>v1.99.76: <see cref="WerkbankKuerzelFamilie.TeileSichtVerfuegbar"/> – sonst Teile-Reiter deaktiviert mit Hinweis.</summary>
    public bool TeileSicht { get; set; }

    /// <summary>v1.99.76 (E4-1): Zeilen der FA-Sicht (aktuelle Seite).</summary>
    public IReadOnlyList<BdeFaSichtZeile> FaZeilen { get; set; } = Array.Empty<BdeFaSichtZeile>();

    /// <summary>v1.99.76 (E4-3): Werkbank meldet nur fertig – in 4a ohne Knopf, „Fertig melden“ folgt mit 4b.</summary>
    public bool NurAgFertigmeldung { get; set; }
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
