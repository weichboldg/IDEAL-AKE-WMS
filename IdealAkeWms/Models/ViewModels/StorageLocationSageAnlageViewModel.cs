using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>v1.99.71 (Spec 2026-11-24-lagerplatz-anlage-wms-nach-sage F1/F1a): Formular „Lagerplätze in Sage anlegen“.</summary>
public class StorageLocationSageAnlageViewModel
{
    /// <summary>Wert der Auswahl „Neues Lager erfassen …“.</summary>
    public const string NeuesLagerWert = "neu";

    /// <summary>StorageArea-Id als Text oder <see cref="NeuesLagerWert"/>.</summary>
    [Display(Name = "Lagerort")]
    public string? Lagerort { get; set; }

    [Display(Name = "Dimension 1 von")] public int D1Von { get; set; } = 1;
    [Display(Name = "Dimension 1 bis")] public int D1Bis { get; set; } = 1;
    [Display(Name = "Dimension 2 von")] public int D2Von { get; set; }
    [Display(Name = "Dimension 2 bis")] public int D2Bis { get; set; }
    [Display(Name = "Dimension 3 von")] public int D3Von { get; set; }
    [Display(Name = "Dimension 3 bis")] public int D3Bis { get; set; }

    [Display(Name = "Buchbar")] public bool IstBuchbar { get; set; } = true;
    [Display(Name = "Sage-Buchung erlaubt")] public bool SageBuchungErlaubt { get; set; }

    // ----- F1a: neues Lager -----
    [Display(Name = "Lagerkennung")] public string? NeueKennung { get; set; }
    [Display(Name = "Bezeichnung")] public string? NeueBezeichnung { get; set; }
    [Display(Name = "Lagertechnik")] public string? NeueTechnik { get; set; }
    [Display(Name = "Lagerplätze anlegen (platzverwaltet)")] public bool Platzverwaltet { get; set; }

    // ----- Anzeige (vom Controller gefuellt) -----
    public List<LagerortOption> Lagerorte { get; set; } = new();
    public List<Services.SageLagerplatzAnlage.Technik> Techniken { get; set; } = new();
    public bool LagerAnlageFreigegeben => Techniken.Count > 0;

    public sealed record LagerortOption(int Id, string Text, bool Waehlbar);
}
