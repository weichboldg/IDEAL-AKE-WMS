using IdealAkeWms.Data;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E3b/F5): schreibt die Sage-Benutzerfelder als Text-Merkmale. Regel wie beim Satelliten:
/// nur Artikel mit <see cref="SageArtikelDto.Benutzerfelder"/> != null (= SData-Weg); View und Vergleich fassen Merkmale nie an.
/// <list type="bullet">
/// <item>Definition je USER-Feld: gefunden ueber <c>SyncSource = "Sage"</c> + <c>SyncFieldName</c>, sonst angelegt (Name = Feld ohne
/// <c>USER_</c>, Typ Text, aktiv, hinten einsortiert). Gibt es den Namen schon als manuelles Merkmal (Name ist UNIQUE), wird nichts
/// ueberschrieben, sondern ein Konflikt gemeldet und das Feld uebersprungen.</item>
/// <item>Wert: leer → Wert-Zeile entfernen; &gt; 1000 Zeichen → gekuerzt + Warnung; Audit nur bei echter Aenderung.</item>
/// </list>
/// </summary>
public static class ArtikelBenutzerfelderWriter
{
    public const string SyncSource = ArticleAttributeDefinition.SyncSourceSage;
    public const int MaxLaenge = ArticleAttributeValue.TextValueMaxLength;

    public sealed record Ergebnis(int DefinitionenNeu, int Neu, int Aktualisiert, int Entfernt, int OhneArtikel, List<string> Warnungen);

    public static async Task<Ergebnis> SchreibenAsync(ApplicationDbContext ctx, IEnumerable<SageArtikelDto> artikel, CancellationToken ct)
    {
        var mit = artikel.Where(a => a.Benutzerfelder is not null).ToList();
        var warnungen = new List<string>();
        if (mit.Count == 0) return new(0, 0, 0, 0, 0, warnungen);

        var jetzt = DateTime.Now; // Ortszeit des Dienstrechners (ADR 0025)
        var windows = $@"{Environment.UserDomainName}\{Environment.UserName}";

        // Definitionen
        var defs = await ctx.ArticleAttributeDefinitions.ToListAsync(ct);
        var nextSort = defs.Count == 0 ? 0 : defs.Max(d => d.SortOrder) + 1;
        var defJeFeld = new Dictionary<string, ArticleAttributeDefinition>(StringComparer.OrdinalIgnoreCase);
        int defsNeu = 0;
        foreach (var feld in mit.SelectMany(a => a.Benutzerfelder!.Keys).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var def = defs.FirstOrDefault(d => d.SyncSource == SyncSource && string.Equals(d.SyncFieldName, feld, StringComparison.OrdinalIgnoreCase));
            if (def is null)
            {
                var name = MerkmalName(feld);
                // SQL-Server-Kollation: Name-Vergleich ohne Gross/Klein (UNIQUE-Index auf Name).
                if (defs.Any(d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    warnungen.Add($"Benutzerfeld {feld}: Merkmal „{name}“ existiert bereits (manuell) – nicht übernommen, Wert unverändert (Konflikt).");
                    continue;
                }
                def = new ArticleAttributeDefinition
                {
                    Name = name, AttributeType = AttributeType.Text, SortOrder = nextSort++, IsActive = true,
                    SyncSource = SyncSource, SyncFieldName = feld,
                    CreatedAt = jetzt, CreatedBy = ArticleSageDetailsWriter.Schreiber, CreatedByWindows = windows,
                };
                ctx.ArticleAttributeDefinitions.Add(def);
                defs.Add(def);
                defsNeu++;
            }
            defJeFeld[feld] = def;
        }
        if (defsNeu > 0) await ctx.SaveChangesAsync(ct); // Ids fuer die Werte

        // Werte
        var idJeNummer = await ArticleSageDetailsWriter.ArtikelIdsAsync(ctx, ct);
        var defIds = defJeFeld.Values.Select(d => d.Id).ToList();
        var vorhanden = (await ctx.ArticleAttributeValues.Where(v => defIds.Contains(v.ArticleAttributeDefinitionId)).ToListAsync(ct))
            .ToDictionary(v => (v.ArticleId, v.ArticleAttributeDefinitionId));
        int neu = 0, geaendert = 0, entfernt = 0, ohneArtikel = 0, gekuerzt = 0;
        foreach (var a in mit)
        {
            if (!idJeNummer.TryGetValue(a.ArticleNumber.TrimEnd(), out var articleId)) { ohneArtikel++; continue; }
            foreach (var (feld, def) in defJeFeld)
            {
                if (!a.Benutzerfelder!.TryGetValue(feld, out var wert)) continue; // Feld diesmal nicht gelesen = unberuehrt
                if (string.IsNullOrWhiteSpace(wert)) wert = null;
                if (wert is { Length: > MaxLaenge })
                {
                    if (++gekuerzt <= 20)
                        warnungen.Add($"Benutzerfeld {feld} an Artikel {a.ArticleNumber.TrimEnd()}: {wert.Length} Zeichen, auf {MaxLaenge} gekürzt.");
                    wert = wert[..MaxLaenge];
                }

                var zeile = vorhanden.GetValueOrDefault((articleId, def.Id));
                if (wert is null)
                {
                    if (zeile is null) continue;
                    ctx.ArticleAttributeValues.Remove(zeile);
                    vorhanden.Remove((articleId, def.Id));
                    entfernt++;
                }
                else if (zeile is null)
                {
                    zeile = new ArticleAttributeValue
                    {
                        ArticleId = articleId, ArticleAttributeDefinitionId = def.Id, TextValue = wert,
                        CreatedAt = jetzt, CreatedBy = ArticleSageDetailsWriter.Schreiber, CreatedByWindows = windows,
                    };
                    ctx.ArticleAttributeValues.Add(zeile);
                    vorhanden[(articleId, def.Id)] = zeile;
                    neu++;
                }
                else if (zeile.TextValue != wert)
                {
                    zeile.TextValue = wert;
                    zeile.ModifiedAt = jetzt;
                    zeile.ModifiedBy = ArticleSageDetailsWriter.Schreiber;
                    zeile.ModifiedByWindows = windows;
                    geaendert++;
                }
            }
        }
        if (gekuerzt > 20) warnungen.Add($"… insgesamt {gekuerzt} Benutzerfeld-Werte auf {MaxLaenge} Zeichen gekürzt.");

        await ctx.SaveChangesAsync(ct);
        return new(defsNeu, neu, geaendert, entfernt, ohneArtikel, warnungen);
    }

    /// <summary>Merkmal-Name eines USER-Felds: <c>USER_Maschine</c> → <c>Maschine</c>.</summary>
    public static string MerkmalName(string feld) =>
        feld.StartsWith("USER_", StringComparison.OrdinalIgnoreCase) && feld.Length > 5 ? feld[5..] : feld;
}
