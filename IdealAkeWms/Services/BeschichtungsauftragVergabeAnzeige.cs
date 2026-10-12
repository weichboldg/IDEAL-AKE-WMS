using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdealAkeWms.Data.Repositories;

namespace IdealAkeWms.Services;

/// <summary>Vergabe-Stand eines HauptFA fuer die Vorschlagsliste: sperrende Auftraege (Erst- und Reklamationsauftraege,
/// Status ≠ Storniert), Anzahl noch freier beschichteter Positionen und je Position die Auftragsnummer(n).</summary>
public sealed record BeschichtungsauftragVergabeInfo(
    IReadOnlyList<(int Id, string Nummer)> Auftraege,
    int FreiePositionen,
    IReadOnlyDictionary<PositionsSchluessel, IReadOnlyList<string>> NummernJePosition)
{
    public static readonly BeschichtungsauftragVergabeInfo Leer =
        new(new List<(int, string)>(), 0, new Dictionary<PositionsSchluessel, IReadOnlyList<string>>());
}

/// <summary>
/// Markierung der Vorschlagsliste (Spec 2026-10-06-beschichtungsauftrag-ausbau-teil-4, Anforderung 1): je HauptFA der
/// Seite die Auftragsnummern und ob noch freie Positionen da sind (dann Button „Aus Vorschlagsliste erstellen").
/// Eine Vergabe-Abfrage fuer alle HauptFA der Seite (kein N+1). „Frei" rechnet wie <c>BeschichtungsauftragWorkflow.AnlegenAsync</c>:
/// beschichtete Knoten des HauptFA ohne sperrende Vergabe — hier aus dem gecachten Knoten-Read (Anzeige); der Workflow
/// prueft beim Anlegen ungecacht und lehnt mit Klartext ab, falls der Cache veraltet ist.
/// </summary>
public class BeschichtungsauftragVergabeAnzeige
{
    private readonly IBeschichtungsauftragRepository _repo;
    private readonly IFaHierarchyNodeRepository _nodes;

    public BeschichtungsauftragVergabeAnzeige(IBeschichtungsauftragRepository repo, IFaHierarchyNodeRepository nodes)
    {
        _repo = repo;
        _nodes = nodes;
    }

    public async Task<IReadOnlyDictionary<int, BeschichtungsauftragVergabeInfo>> ErmittelnAsync(IReadOnlyCollection<int> hauptFas)
    {
        var set = hauptFas.ToHashSet();
        if (set.Count == 0) return new Dictionary<int, BeschichtungsauftragVergabeInfo>();

        var vergaben = (await _repo.GetVergabenAsync(set)).ToLookup(v => v.HauptFA);
        var knoten = (await _nodes.GetAllAsync()).Where(n => n.Beschichtet && set.Contains(n.HauptFA)).ToLookup(n => n.HauptFA);

        return set.ToDictionary(h => h, h =>
        {
            var jePosition = vergaben[h].GroupBy(v => v.Schluessel)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(v => v.Nummer).Distinct().OrderBy(n => n).ToList());
            var auftraege = vergaben[h].Select(v => (v.AuftragId, v.Nummer)).Distinct().OrderBy(a => a.Nummer).ToList();
            var frei = knoten[h].Count(n => !jePosition.ContainsKey(PositionsSchluessel.Von(n)));
            return new BeschichtungsauftragVergabeInfo(auftraege, frei, jePosition);
        });
    }
}
