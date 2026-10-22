using System.Text.RegularExpressions;
using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Models.Standort;
using Xunit;

namespace IdealAkeWms.Tests.Views;

/// <summary>
/// Vault- und enaio-Links konfigurierbar (v1.67.0, Spec 2026-10-21-vault-links-per-schalter): QUELLTEXT-WAECHTER fuer Seed, Katalog
/// und Verdrahtung. Die Regel selbst pruefen die LinkVorlageTests; die Optik ist Manual-UAT TS-100.
/// </summary>
public class VaultEnaioLinksTests
{
    private static readonly string Web = FindWeb();

    private static string FindWeb()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "Program.cs")))
            dir = dir.Parent;
        return Path.Combine(dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden"), "IdealAkeWms");
    }

    private static readonly string Sep = Path.DirectorySeparatorChar.ToString();

    private static IEnumerable<(string Pfad, string Text)> Quellen(params string[] muster) =>
        muster.SelectMany(m => Directory.EnumerateFiles(Web, m, SearchOption.AllDirectories))
            .Where(f => !f.Contains(Sep + "obj" + Sep) && !f.Contains(Sep + "bin" + Sep) && !f.Contains(Sep + "lib" + Sep))
            .Select(f => (Path.GetRelativePath(Web, f).Replace('\\', '/'), File.ReadAllText(f)));

    [Fact]
    public void Adressen_stehen_nur_im_Seed_und_jede_Vault_Stelle_nutzt_das_Partial()
    {
        Quellen("*.cs", "*.cshtml", "*.js").Where(q => q.Text.Contains("akevault24") || q.Text.Contains("akeosapp")).Select(q => q.Pfad)
            .Should().BeEquivalentTo(new[] { "Program.cs" }, "Adressen nur noch als Seed-Wert (AK 19)");

        var stellen = new[]
        {
            "Views/Articles/Info.cshtml", "Views/Articles/Index.cshtml", "Views/StockOverview/Index.cshtml",
            "Views/StockMovements/Index.cshtml", "Views/Picking/Bom.cshtml", "Views/Shared/_FaDocumentLinks.cshtml",
            "Views/ProductionOrders/_ProductionOrderRow.cshtml", "Views/PickingLeitstand/_PickingLeitstandRow.cshtml",
            "Views/FaCompletion/_FaCompletionRow.cshtml", "Views/FaWorklist/_FaWorklistRow.cshtml",
        };
        Quellen("*.cshtml").Where(q => q.Text.Contains("PartialAsync(\"_VaultLink\"")).Select(q => q.Pfad).Should().BeEquivalentTo(stellen);
        File.ReadAllText(Path.Combine(Web, "Views", "Picking", "Bom.cshtml"))
            .Should().Contain("new VaultLinkViewModel(item.Ressourcenummer)", "leere Ressourcenummer = kein Link (Antwort 6)");
        File.ReadAllText(Path.Combine(Web, "Views", "Shared", "_EnaioDmsBadges.cshtml"))
            .Should().Contain("LinkVorlage.Build(enaioAktiv, enaioVorlage, IdealAkeWms.Helpers.LinkVorlage.PlatzhalterObjektId")
            .And.Contain("if (dmsUrl == null) { continue; }");
    }

    [Fact]
    public void Stuecklisten_Knopf_bleibt_kein_Ausblenden_ueber_Klassen()
    {
        File.ReadAllText(Path.Combine(Web, "Views", "Shared", "_FaDocumentLinks.cshtml"))
            .Should().MatchRegex(@"asp-action=""Bom""[^>]*\s+class=""vault-link"" title=""Stückliste öffnen""");
        // Abgeschaltet wird nur serverseitig im Partial; kein CSS/JS darf ueber .vault-link/.dms-link ausblenden.
        Quellen("*.cshtml", "*.js", "*.css")
            .Where(q => Regex.IsMatch(q.Text, @"querySelector(All)?\(\s*['""][^'""]*\.(vault|dms)-link|\.(vault|dms)-link[^{]*\{[^}]*display\s*:\s*none"))
            .Select(q => q.Pfad).Should().BeEmpty();
    }

    [Fact]
    public void Seed_und_Standort_Katalog()
    {
        var program = File.ReadAllText(Path.Combine(Web, "Program.cs"));
        foreach (var (key, wert) in new[]
                 {
                     ("VaultLinksAktiv", "\"true\""),
                     ("VaultLinkVorlage", "\"http://akevault24.ake.at/AutodeskTC/AKE-VAULT01/explore?search={Artikelnummer}&searchContext=0\""),
                     ("EnaioLinksAktiv", "\"true\""),
                     ("EnaioLinkVorlage", "\"http://akeosapp.ake.at/oscontentviewer/viewer/{ObjektId}/?pagecount=1\""),
                 })
            program.Should().Contain($"(IdealAkeWms.Models.AppSettingKeys.{key}, {wert},",
                "Laufzeit-Seed Pflicht: die Maske zeigt ein Bool ohne Zeile als aus und schreibt beim Speichern false");

        StandortSettingsCatalog.GroupOrder.TakeLast(2).Should().Equal(StandortSettingsCatalog.GroupVault, StandortSettingsCatalog.GroupEnaio);
        StandortSettingsCatalog.Fields.Where(f => f.Group == StandortSettingsCatalog.GroupVault).Select(f => (f.Key, f.Type, f.Backend))
            .Should().Equal((AppSettingKeys.VaultLinksAktiv, StandortFieldType.Bool, StandortBackend.App),
                            (AppSettingKeys.VaultLinkVorlage, StandortFieldType.String, StandortBackend.App));
        StandortSettingsCatalog.Fields.Where(f => f.Group == StandortSettingsCatalog.GroupEnaio).Select(f => (f.Key, f.Type, f.Backend))
            .Should().Equal((AppSettingKeys.EnaioLinksAktiv, StandortFieldType.Bool, StandortBackend.App),
                            (AppSettingKeys.EnaioLinkVorlage, StandortFieldType.String, StandortBackend.App),
                            ("Sync:EnaioDmsViewName", StandortFieldType.String, StandortBackend.Service));
        StandortSettingsCatalog.Fields.Where(f => f.Group is StandortSettingsCatalog.GroupVault or StandortSettingsCatalog.GroupEnaio)
            .Should().OnlyContain(f => !string.IsNullOrEmpty(f.Hint));
        StandortSettingsCatalog.Fields.Should().NotContain(f => f.Key == "Sync:EnaioDmsEnabled", "Sync-Schalter bleibt in /ServiceSettings");

        ServiceSettingDefinitions.TryGet("Sync:EnaioDmsViewName", out var def).Should().BeTrue();
        (def!.Type, def.DefaultValue, def.Category).Should().Be(
            (ServiceSettingType.String, "[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]", "Sync"));
        ServiceSettingDefinitions.TryGet("Sync:EnaioDmsEnabled", out var schalter).Should().BeTrue();
        (schalter!.Type, schalter.DefaultValue, schalter.Category).Should().Be((ServiceSettingType.Bool, "false", "Sync"));
    }
}
