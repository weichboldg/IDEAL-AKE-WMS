using System.Globalization;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.Extensions.Logging;

namespace IdealAkeWms.Data.Repositories;

/// <summary>
/// BOM-Bridge (Spec 2026-09-08): zweite Implementierung von <see cref="IBomRepository"/> UND
/// <see cref="IBomCacheRepository"/>, die im hierarchischen Modus (IDEAL) die Stueckliste direkt aus der
/// lokalen FA-Struktur (<see cref="FaHierarchyNode"/>) liest — KEIN Zugriff auf den persistenten AKE-BOM-Cache,
/// kein MemoryCache-Decorator (die Struktur ist lokal und ueber CachedFaHierarchyNodeRepository 5 min gecacht).
/// Mengen sind Auftragsgesamtmengen (<c>MengeIstAuftragsmenge = true</c>).
/// Feld-Mapping laut Anhang sage-views-ideal: Kopf <c>Artikelnummer</c> = Artikel des Elternknotens,
/// <c>Ressourcenummer</c> = <c>Artnr</c>, <c>Baugruppe</c> = Artnr des unmittelbaren Elternknotens.
/// </summary>
public sealed class FaHierarchyBomRepository : IBomRepository, IBomCacheRepository
{
    private readonly IFaHierarchyNodeRepository _nodes;
    private readonly FaHierarchyTreeBuilder _treeBuilder;
    private readonly ILogger<FaHierarchyBomRepository> _logger;

    public FaHierarchyBomRepository(
        IFaHierarchyNodeRepository nodes,
        FaHierarchyTreeBuilder treeBuilder,
        ILogger<FaHierarchyBomRepository> logger)
    {
        _nodes = nodes;
        _treeBuilder = treeBuilder;
        _logger = logger;
    }

    private static BomQueryResult Empty() =>
        new(new List<BomItem>(), BomDataSources.FaHierarchie, MengeIstAuftragsmenge: true);

    // ---------------------------------------------------------------- IBomRepository

    public async Task<BomQueryResult> GetBomItemsAsync(BomKey key, BomScope scope = BomScope.DirectChildren)
    {
        if (!int.TryParse(key.SubOrderNumber, NumberStyles.Integer, CultureInfo.InvariantCulture, out var subFa))
        {
            _logger.LogWarning("FaHierarchyBom: SubOrderNumber '{Sub}' ist keine Sub-FA-Nummer — leere Stueckliste.", key.SubOrderNumber);
            return Empty();
        }

        var group = await LoadGroupAsync(key.OrderNumber, subFa);
        var self = group.FirstOrDefault(n => n.SubFA == subFa);
        if (self == null)
        {
            _logger.LogWarning("FaHierarchyBom: Sub-FA {SubFa} nicht in der Struktur (HauptFA {HauptFa}) — leere Stueckliste.", subFa, key.OrderNumber);
            return Empty();
        }

        // FullStructure ist nur an der Wurzel definiert (Fachliche Anforderung 6); an einem Sub-FA
        // faellt der Aufruf bewusst auf DirectChildren zurueck.
        var items = scope == BomScope.FullStructure && self.VaterFA == null
            ? BuildFullStructure(group, self)
            : BuildDirectChildren(group, self);

        return new BomQueryResult(items, BomDataSources.FaHierarchie, MengeIstAuftragsmenge: true);
    }

    /// <summary>HauptFA aus dem Key (gecachter Gruppen-Lookup); ohne HauptFA Fallback ueber GetAllAsync.</summary>
    private async Task<List<FaHierarchyNode>> LoadGroupAsync(string? orderNumber, int subFa)
    {
        if (int.TryParse(orderNumber, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hauptFa))
            return await _nodes.GetByHauptFaAsync(hauptFa);

        var all = await _nodes.GetAllAsync();
        var owner = all.FirstOrDefault(n => n.SubFA == subFa);
        return owner == null ? new List<FaHierarchyNode>() : all.Where(n => n.HauptFA == owner.HauptFA).ToList();
    }

    private static List<BomItem> BuildDirectChildren(List<FaHierarchyNode> group, FaHierarchyNode parent)
    {
        var children = group
            .Where(n => n.VaterFA == parent.SubFA)
            .OrderBy(n => n.Position ?? int.MaxValue)
            .ToList();

        var used = new HashSet<string>(StringComparer.Ordinal);
        var items = new List<BomItem>(children.Count);
        foreach (var child in children)
        {
            var segment = PositionSegment(child);
            var (position, collided) = Unique(segment, used);
            items.Add(Map(child, parent, position, ebene: 1, isWaise: false, collided));
        }
        return items;
    }

    private List<BomItem> BuildFullStructure(List<FaHierarchyNode> group, FaHierarchyNode root)
    {
        var items = new List<BomItem>();
        var structures = _treeBuilder.Build(group, Array.Empty<FaHierarchyOrderInfo>(), FaHierarchyTreeBuilder.DefaultDepthCap);

        var main = structures.FirstOrDefault(s => !s.IsOrphan && s.Roots.Count > 0 && s.Roots[0].Node.SubFA == root.SubFA);
        if (main != null)
        {
            if (main.HasError)
                _logger.LogWarning("FaHierarchyBom: Struktur HauptFA {HauptFa} unvollstaendig ({Reason}) — Stueckliste zeigt den aufgebauten Teil.", root.HauptFA, main.ErrorMessage);
            WalkChildren(main.Roots[0], parentPath: null, ebene: 1, isWaise: false, items);
        }

        // Waisen (VaterFA ohne importierten Elternknoten) markiert anhaengen: eigener Pfad-Praefix "W<n>",
        // damit die Punktnotation der View weiterhin gueltige Eltern hat.
        var w = 0;
        foreach (var orphanStructure in structures.Where(s => s.IsOrphan && s.Roots.Count > 0))
        {
            w++;
            var pseudoRoot = orphanStructure.Roots[0];
            var path = "W" + w.ToString(CultureInfo.InvariantCulture);
            var parentStub = new FaHierarchyNode { HauptFA = root.HauptFA, SubFA = pseudoRoot.Node.VaterFA ?? 0, Artnr = null, HauptArtnr = root.HauptArtnr };
            items.Add(Map(pseudoRoot.Node, parentStub, path, ebene: 1, isWaise: true, collided: false));
            WalkChildren(pseudoRoot, parentPath: path, ebene: 2, isWaise: true, items);
        }
        return items;
    }

    private void WalkChildren(FaHierarchyTreeNode parent, string? parentPath, int ebene, bool isWaise, List<BomItem> items)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var child in parent.Children.OrderBy(c => c.Node.Position ?? int.MaxValue))
        {
            var segment = PositionSegment(child.Node);
            var (uniqueSegment, collided) = Unique(segment, used);
            if (collided)
                _logger.LogWarning("FaHierarchyBom: Positions-Kollision unter Sub-FA {Parent} (Position {Pos}) — Pfad eindeutig gemacht.", parent.Node.SubFA, segment);
            var path = parentPath == null ? uniqueSegment : parentPath + "." + uniqueSegment;
            items.Add(Map(child.Node, parent.Node, path, ebene, isWaise, collided));
            WalkChildren(child, path, ebene + 1, isWaise, items);
        }
    }

    private static string PositionSegment(FaHierarchyNode node) =>
        node.Position.HasValue ? node.Position.Value.ToString(CultureInfo.InvariantCulture) : "0";

    /// <summary>Freigabe-Bedingung (b): gleiche Position unter demselben Eltern-FA wird mit Suffix "~n"
    /// eindeutig gemacht und als Kollision markiert — nie still ueberschrieben.</summary>
    private static (string Segment, bool Collided) Unique(string segment, HashSet<string> used)
    {
        if (used.Add(segment)) return (segment, false);
        var n = 2;
        while (!used.Add(segment + "~" + n.ToString(CultureInfo.InvariantCulture))) n++;
        return (segment + "~" + n.ToString(CultureInfo.InvariantCulture), true);
    }

    private static FaHierarchyBomItem Map(FaHierarchyNode node, FaHierarchyNode parent, string position, int ebene, bool isWaise, bool collided)
    {
        var parentArticle = parent.Artnr ?? parent.HauptArtnr ?? string.Empty;
        return new FaHierarchyBomItem
        {
            Artikelnummer = parentArticle,
            Position = position,
            Baugruppe = parentArticle,
            Ressourcenummer = node.Artnr,
            Bezeichnung1 = node.Bezeichnung1,
            Bezeichnung2 = node.Bezeichnung2,
            Menge = node.Sollmenge,
            Beschaffungsartikel = node.Beschaffungsartikel ? "Ja" : "Nein",
            Artikelgruppe = node.Artikelgruppe,
            IsBaugruppe = node.SubFA != 0,
            Kommissionieren = node.Kommissionieren,
            Hauptlagerplatz = node.Hauptlagerplatz,
            Ebene = ebene,
            VaterSubFa = (node.VaterFA ?? parent.SubFA).ToString(CultureInfo.InvariantCulture),
            SagePosition = node.Position?.ToString(CultureInfo.InvariantCulture),
            IsWaise = isWaise,
            PositionsKollision = collided,
        };
    }

    // ---------------------------------------------------------------- IBomCacheRepository

    public async Task<BomQueryResult?> GetByArticleNumberAsync(BomKey key) =>
        await GetBomItemsAsync(key, BomScope.DirectChildren);

    /// <summary>Artikelinfo (Spec Design G): "Geraete" sind hier die SubOrderNumbers der Eltern-Sub-FAs,
    /// deren direktes Kind das Bauteil ist (Partition DirectChildren, jeder Knoten genau einmal).</summary>
    public async Task<List<string>> GetDeviceArticleNumbersByComponentAsync(string componentArticleNumber)
    {
        if (string.IsNullOrWhiteSpace(componentArticleNumber)) return new List<string>();
        var all = await _nodes.GetAllAsync();
        return all
            .Where(n => n.VaterFA.HasValue && string.Equals(n.Artnr, componentArticleNumber, StringComparison.Ordinal))
            .Select(n => n.VaterFA!.Value.ToString(CultureInfo.InvariantCulture))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public async Task<Dictionary<string, decimal>> GetComponentMengePerDeviceAsync(string componentArticleNumber)
    {
        if (string.IsNullOrWhiteSpace(componentArticleNumber)) return new Dictionary<string, decimal>();
        var all = await _nodes.GetAllAsync();
        return all
            .Where(n => n.VaterFA.HasValue && string.Equals(n.Artnr, componentArticleNumber, StringComparison.Ordinal))
            .GroupBy(n => n.VaterFA!.Value.ToString(CultureInfo.InvariantCulture), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Sum(n => n.Sollmenge), StringComparer.Ordinal);
    }

    // Sync-/Cache-interne Methoden: werden durch die Klasse-D-Gates (Task 4) im hierarchischen Modus nie
    // erreicht — hier nur Sicherheitsnetz: leer bzw. No-op mit Log, nie Exception.
    // (Signaturen 1:1 aus IBomCacheRepository.cs Zeilen 19-42 uebernommen.)
    public Task<HashSet<string>> GetArticleNumbersWithCoatingPartsAsync(
        string lackierteilCategoryName,
        List<string> articleNumbers)
        => Task.FromResult(new HashSet<string>(StringComparer.Ordinal));

    public Task<Dictionary<string, (string Hash, DateTime CachedAt)>> GetHeaderHashesAsync(
        List<string> articleNumbers)
        => Task.FromResult(new Dictionary<string, (string Hash, DateTime CachedAt)>());

    public Task UpsertBomAsync(
        string articleNumber,
        string source,
        string contentHash,
        List<BomItem> items)
    {
        _logger.LogWarning("FaHierarchyBom: UpsertBomAsync im hierarchischen Modus ignoriert (kein BOM-Cache).");
        return Task.CompletedTask;
    }

    public Task DeleteOrphansAsync(List<string> currentArticleNumbers)
    {
        _logger.LogWarning("FaHierarchyBom: DeleteOrphansAsync im hierarchischen Modus ignoriert (kein BOM-Cache).");
        return Task.CompletedTask;
    }
}
