using IdealAkeWms.Services.SyncLogger;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E3a): Quellen-Weiche fuer <see cref="ISageArtikelReader"/> (Schalter
/// <c>Sync:Quelle:Artikel</c>, Default View). View = <see cref="SageArtikelReader"/> (bisheriger Lese-Code unveraendert),
/// SData = <see cref="SdataArtikelReader"/>. G16: <c>SData</c> ist gesperrt bis P-R1 (IDEAL-Filter der Artikel-View) und wirkt wie
/// <c>View</c> mit Warnung; <c>Vergleich</c> ist erlaubt und schreibt nur aus der View (also nie den Satelliten).
/// Registrierung: <see cref="SdataRegistrierung.AddArtikelQuelle"/>.
/// </summary>
public class ArtikelQuelleSwitch : SageQuelleWeiche, ISageArtikelReader
{
    /// <summary>Vergleich je Artikelnummer auf den Feldern, die beide Wege nach <c>Articles</c> schreiben (F5, Gruppe als reiner Code).</summary>
    public static readonly SdataVergleich.Feld<SageArtikelDto>[] VergleichsFelder =
    {
        new("Description", d => d.Description),
        new("Unit", d => d.Unit),
        new("ArticleGroup", d => d.ArticleGroup),
        new("ReorderLevel", d => d.ReorderLevel),
        new("PrimaryStorageLocation", d => d.PrimaryStorageLocation),
        new("Matchcode", d => d.Matchcode),
    };

    private readonly ISageArtikelReader _view;
    private readonly ISageArtikelReader _sdata;

    public ArtikelQuelleSwitch(ISageArtikelReader view, ISageArtikelReader sdata, IConfiguration configuration,
        ISyncErrorNotifier errorNotifier, ISyncLogger syncLogger)
        : base(configuration, errorNotifier, syncLogger)
    {
        _view = view;
        _sdata = sdata;
    }

    protected override string Bereich => SageQuelleSchalter.Artikel;

    // TODO(P-R1): Sperre aufheben, sobald der IDEAL-Filter der Artikel-View belegt und im SdataArtikelReader nachgebaut ist (G16).
    protected override string? SdataGesperrtBis => "P-R1";

    /// <summary>Hinweise des SData-Readers (Ressourcen, Bloecke, Zeilen) — leer, wenn diesmal nur die View gelesen wurde.</summary>
    public IReadOnlyList<SdataHinweis> SdataHinweise =>
        GenutzteQuelle is SageQuelle.SData or SageQuelle.Vergleich && _sdata is SdataArtikelReader r ? r.Hinweise : Array.Empty<SdataHinweis>();

    public Task<SageArtikelLesung> GetAllAsync(CancellationToken ct = default) =>
        LeseAsync<SageArtikelLesung, SageArtikelDto>(_view.GetAllAsync, _sdata.GetAllAsync, l => l.Artikel, d => d.ArticleNumber?.TrimEnd(),
            VergleichsFelder, ct);
}
