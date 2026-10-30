namespace IdealAkeWms.Services.SageSdata;

/// <summary>Position samt der eindeutig zugeordneten Sub-FA (sonst null).</summary>
public sealed record SageFaPositionZeile(SageFaPosition Position, int? SubFaBelId);

/// <summary>Ein FA-Knoten der flachen, tiefensortierten Baumliste.</summary>
public sealed record SageFaKnoten(SageFaKopf Fa, int Ebene, IReadOnlyList<SageFaPositionZeile> Positionen);

/// <summary>Ergebnis des Baumaufbaus: Baum ab Haupt-FA, nicht zuordenbare FAs und sichtbare Meldungen.</summary>
public sealed record SageFaStruktur(
    SageFaKopf? HauptFa, IReadOnlyList<SageFaKnoten> Knoten, IReadOnlyList<SageFaKnoten> NichtZugeordnet, IReadOnlyList<string> Meldungen);

/// <summary>
/// Spec 2026-10-30-sage-sdata-fa-ansicht F-4/F-5: reiner Baumaufbau Haupt-FA → Sub-FAs → Positionen aus den Sage-Antworten.
/// Vater-Kind ist in Sage kein Feld: Eine MA-Position verweist auf eine Sub-FA, wenn ihre Ressourcenummer der Artikelnummer
/// GENAU EINER Sub-FA entspricht und dieser Artikel an GENAU EINER MA-Position der Struktur steht. Alles andere wird gemeldet,
/// nie still zugeordnet; nichts wird weggelassen. Kein Bezug zu <c>FaHierarchyTreeBuilder</c>.
/// </summary>
public static class SageSdataFaTreeBuilder
{
    public static SageFaStruktur Build(IReadOnlyList<SageFaKopf> fas, IReadOnlyList<SageFaPosition> positionen)
    {
        var meldungen = new List<string>();
        var strukturIds = fas.Select(f => f.BelID).ToHashSet();

        var fremde = positionen.Count(p => p.BelID is not int b || !strukturIds.Contains(b));
        if (fremde > 0)
            meldungen.Add($"{fremde} Position(en) gehören zu keiner FA dieser Struktur und werden nicht angezeigt.");

        var posJeFa = positionen.Where(p => p.BelID is int b && strukturIds.Contains(b))
            .GroupBy(p => p.BelID!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Wurzel: BelID = StrukturID. VerursacherTyp (MAN oder VK) ist KEIN Kriterium (Spec F-5c, nachgezogen 2026-10-30).
        var hauptFa = fas.FirstOrDefault(f => f.BelID == f.StrukturID);
        if (hauptFa is null)
            meldungen.Add("Die Struktur hat keinen Haupt-FA (BelID = StrukturID). Alle FAs stehen unter „Nicht eindeutig zugeordnete Sub-FAs“.");

        var subFas = fas.Where(f => f != hauptFa).ToList();

        // MA-Positionen aller FAs der Struktur je Artikel (Ressourcenummer).
        var maJeArtikel = posJeFa.Values.SelectMany(l => l)
            .Where(p => string.Equals(p.Ressourcetyp?.Trim(), "MA", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(p.Ressourcenummer))
            .GroupBy(p => p.Ressourcenummer!.Trim())
            .ToDictionary(g => g.Key, g => g.ToList());

        var subFaZuPosition = new Dictionary<SageFaPosition, int>(ReferenceEqualityComparer.Instance);
        foreach (var gruppe in subFas.GroupBy(f => f.Artikelnummer?.Trim() ?? string.Empty))
        {
            var subs = gruppe.ToList();
            var ids = string.Join(", ", subs.Select(s => s.BelID));
            maJeArtikel.TryGetValue(gruppe.Key, out var treffer);
            var anzahl = treffer?.Count ?? 0;

            if (gruppe.Key.Length == 0)
                meldungen.Add($"Sub-FA {ids} ohne Artikelnummer – keine Zuordnung möglich.");
            else if (subs.Count > 1)
                meldungen.Add($"Sub-FAs {ids} haben denselben Artikel {gruppe.Key} – Zuordnung nicht eindeutig.");
            else if (anzahl == 0)
                meldungen.Add($"Sub-FA {ids} (Artikel {gruppe.Key}) ist keiner Position zugeordnet.");
            else if (anzahl > 1)
                meldungen.Add($"Artikel {gruppe.Key} der Sub-FA {ids} steht an {anzahl} Positionen – Zuordnung nicht eindeutig.");
            else
                subFaZuPosition[treffer![0]] = subs[0].BelID;
        }

        var faJeId = fas.GroupBy(f => f.BelID).ToDictionary(g => g.Key, g => g.First());
        var knoten = new List<SageFaKnoten>();
        var besucht = new HashSet<int>();

        void Ablaufen(SageFaKopf fa, int ebene)
        {
            besucht.Add(fa.BelID);
            var zeilen = (posJeFa.GetValueOrDefault(fa.BelID) ?? new List<SageFaPosition>())
                .Select(p => new SageFaPositionZeile(p, subFaZuPosition.TryGetValue(p, out var sub) ? sub : null))
                .ToList();
            knoten.Add(new SageFaKnoten(fa, ebene, zeilen));
            foreach (var z in zeilen)
                if (z.SubFaBelId is int kind && !besucht.Contains(kind) && faJeId.TryGetValue(kind, out var kindFa))
                    Ablaufen(kindFa, ebene + 1);
        }

        if (hauptFa is not null)
            Ablaufen(hauptFa, 0);

        // Nicht erreicht (mehrdeutig, ohne Position, Zyklus, ohne Haupt-FA): eigener Abschnitt, mit Positionen.
        var nichtZugeordnet = fas.Where(f => !besucht.Contains(f.BelID))
            .Select(f => new SageFaKnoten(f, 0, (posJeFa.GetValueOrDefault(f.BelID) ?? new List<SageFaPosition>())
                .Select(p => new SageFaPositionZeile(p, null)).ToList()))
            .ToList();

        var zugeordnetAberNichtErreicht = nichtZugeordnet.Where(k => subFaZuPosition.ContainsValue(k.Fa.BelID)).Select(k => k.Fa.BelID).ToList();
        if (zugeordnetAberNichtErreicht.Count > 0)
            meldungen.Add($"Sub-FA {string.Join(", ", zugeordnetAberNichtErreicht)} ist vom Haupt-FA aus nicht erreichbar (Zyklus oder Verweis aus nicht zugeordneter FA).");

        return new SageFaStruktur(hauptFa, knoten, nichtZugeordnet, meldungen);
    }

    /// <summary>Anzahl und Summe <c>MengeGut</c> je BelID (Rueckmeldungen bzw. Fertigmeldungen, getrennt aufgerufen).</summary>
    public static IReadOnlyDictionary<int, (int Anzahl, decimal MengeGut)> Summiere(IEnumerable<SageRueckmeldung> zeilen) =>
        zeilen.Where(z => z.BelID is not null)
            .GroupBy(z => z.BelID!.Value)
            .ToDictionary(g => g.Key, g => (g.Count(), g.Sum(z => z.MengeGut ?? 0m)));
}
