using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace IdealAkeWms.Tests.Deploy;

/// <summary>
/// DRIFT-WAECHTER des Nachlauf-Manifests <c>scripts/deploy/nachlauf.psd1</c> (Spec 2026-11-09-update-skript-je-zielsystem-mit-nachlauf,
/// v1.98.0, AK 25-27/32; Muster <see cref="Repositories.OperationConfigsMigrationSqlTests"/>). Liest das Manifest als Text (kein
/// PowerShell im Test). Ein vergessener Manifest-Eintrag faellt damit im naechsten Dev-Lauf auf, nicht erst am Zielserver.
/// Beweist NICHT, dass das Update-Skript auf einem Server richtig laeuft - das ist TS-125.
/// </summary>
public class NachlaufManifestDriftTests
{
    private const string VorManifestMigration = "20260806120650_AllowMultipleDummyRequisitionItems";   // SQL/88

    /// <summary>Bewusst nicht im Manifest (Freigabe-Antwort 4, Gerald 2026-11-11): ein Lauf heute loeschte echte Eingaben.</summary>
    private static readonly string[] NichtImManifest = { "2026-09-28_Hotfix-1.30.1_ResetAutosaveZeroQuantityPicked.sql" };

    /// <summary>Deploy-Reihenfolge der Schema-Skripte (ADR 0004 Nachtrag, Spec-Tabelle), nie Dateisortierung.</summary>
    private static readonly string[] SchemaReihenfolge =
    {
        "SQL/89", "SQL/90", "SQL/91", "SQL/92", "SQL/93", "SQL/94", "SQL/95", "SQL/96", "SQL/97", "SQL/98", "SQL/99", "SQL/100", "SQL/101",
        "SQL/102", "SQL/103", "SQL/104", "SQL/105", "SQL/106", "SQL/107", "SQL/109", "SQL/112", "SQL/113", "SQL/133", "SQL/140", "SQL/141",
        "SQL/142", "SQL/143", "SQL/144", "SQL/145",
    };

    private static readonly string Root;

    static NachlaufManifestDriftTests()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "scripts", "deploy", "nachlauf.psd1"))) dir = dir.Parent;
        Root = dir?.FullName ?? throw new FileNotFoundException("Repository-Root mit scripts/deploy/nachlauf.psd1 nicht gefunden");
    }

    private static string ManifestText => File.ReadAllText(Path.Combine(Root, "scripts", "deploy", "nachlauf.psd1"));

    /// <summary>Eintraege als Feld-Woerterbuch: jeder Block beginnt mit "@{ Id='...'".</summary>
    internal static List<Dictionary<string, string>> Eintraege(string text) =>
        Regex.Split(text, @"(?m)^\s*(?=@\{ Id=)").Where(b => b.TrimStart().StartsWith("@{ Id=")).Select(b =>
            Regex.Matches(b, @"(\w+)='((?:[^']|'')*)'").ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value.Replace("''", "'"))).ToList();

    private static List<string> MigrationIdsNachSql88() =>
        Directory.GetFiles(Path.Combine(Root, "IdealAkeWms", "Migrations"), "*.Designer.cs")
            .Select(f => Regex.Match(File.ReadAllText(f), @"\[Migration\(""([^""]+)""\)\]").Groups[1].Value)
            .Where(id => string.CompareOrdinal(id, VorManifestMigration) > 0).OrderBy(id => id, StringComparer.Ordinal).ToList();

    /// <summary>Fehlende Eintraege: jede Migration nach SQL/88, jede SQL/&lt;Nr&gt;_*.sql ab 89, jede SQL/Einmalig/*.sql.</summary>
    internal static List<string> Fehlend(string text)
    {
        var e = Eintraege(text);
        var ids = e.Where(x => x.ContainsKey("MigrationId")).Select(x => x["MigrationId"]).ToHashSet();
        var dateien = e.Where(x => x.ContainsKey("Datei")).Select(x => x["Datei"].Replace('\\', '/')).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sql = Directory.GetFiles(Path.Combine(Root, "SQL"), "*.sql")
            .Select(Path.GetFileName).Where(n => int.TryParse(n!.Split('_')[0], out var nr) && nr >= 89).Select(n => n!);
        var einmalig = Directory.GetFiles(Path.Combine(Root, "SQL", "Einmalig"), "*.sql")
            .Select(Path.GetFileName).Where(n => !NichtImManifest.Contains(n)).Select(n => "Einmalig/" + n);
        return MigrationIdsNachSql88().Where(id => !ids.Contains(id))
            .Concat(sql.Concat(einmalig).Where(d => !dateien.Contains(d))).ToList();
    }

    [Fact]
    public void Manifest_EnthaeltJedeMigrationUndJedesSkript() => Fehlend(ManifestText).Should().BeEmpty();

    [Fact]
    public void Manifest_EntfernterEintrag142_MachtWaechterRot()
    {
        var ohne142 = Regex.Replace(ManifestText, @"(?ms)^@\{ Id='SQL/142'.*?\}\r?$", "");
        ohne142.Should().NotBe(ManifestText);
        Fehlend(ohne142).Should().Contain("20261111174202_AddDeployScriptRuns").And.Contain("142_AddDeployScriptRuns.sql");
    }

    [Fact]
    public void Manifest_EntfernterEintrag_MachtWaechterRot()
    {
        var ohne141 = Regex.Replace(ManifestText, @"(?ms)^@\{ Id='SQL/141'.*?\}\r?$", "");
        ohne141.Should().NotBe(ManifestText);
        Fehlend(ohne141).Should().Contain("20261108071811_AddPickingOrderAltbestandPruefen").And.Contain("141_AddPickingOrderAltbestandPruefen.sql");
    }

    [Fact]
    public void Manifest_HotfixEinmalSkript_NichtEnthalten() =>
        ManifestText.Should().NotContain("Datei='Einmalig\\2026-09-28_Hotfix-1.30.1", "Freigabe-Antwort 4: Hotfix-1.30.1 entfaellt ueberall");

    [Fact]
    public void Manifest_DateienExistierenUndEnthaltenIhreMigrationId()
    {
        foreach (var x in Eintraege(ManifestText).Where(x => x.ContainsKey("Datei")))
        {
            var pfad = Path.Combine(Root, "SQL", x["Datei"].Replace('\\', Path.DirectorySeparatorChar));
            File.Exists(pfad).Should().BeTrue($"{x["Id"]}: {pfad}");
            if (x.ContainsKey("MigrationId")) File.ReadAllText(pfad).Should().Contain(x["MigrationId"], x["Id"]);
        }
    }

    [Fact]
    public void Manifest_SchemaInDeployReihenfolge_UndAllesAutomatisch()
    {
        var schema = Eintraege(ManifestText).Where(x => x.GetValueOrDefault("Typ") == "Schema").ToList();
        schema.Select(x => x["Id"]).Should().Equal(SchemaReihenfolge);
        schema.Should().OnlyContain(x => x["Ausfuehrung"] == "automatisch" && x["Phase"] == "Schema" && x["Firma"] == "beide");
    }

    /// <summary>
    /// v1.99.0 (ADR 0023, geaendert aus Manifest_DatenUndEinmalSkripte_NieAutomatisch): Daten-/Einmal-Skripte laufen nur mit Bestaetigung am
    /// Zielserver - nie automatisch. Trockenlauf-Dateien: Literal genau einmal (das Skript ersetzt es im Speicher) und Zeile TREFFER;
    /// Trockenlauf- und Transaktion-Dateien ohne ROLLBACK (beendete die aeussere Transaktion samt Marker) und ohne sqlcmd-Direktiven.
    /// </summary>
    [Fact]
    public void Manifest_DatenUndEinmalSkripte_NurMitBestaetigung() => DatenFehler(ManifestText, SqlText).Should().BeEmpty();

    [Fact]
    public void Manifest_TrockenlaufDateiOhneTreffer_MachtWaechterRot() =>
        DatenFehler(ManifestText, d => SqlText(d).Replace("TREFFER: ", "TREFFER ")).Should().Contain(f => f.Contains("ohne Zeile TREFFER"));

    private static string SqlText(string datei) => File.ReadAllText(Path.Combine(Root, "SQL", datei.Replace('\\', Path.DirectorySeparatorChar)));

    internal static List<string> DatenFehler(string manifest, Func<string, string> sql)
    {
        var fehler = new List<string>();
        var daten = Eintraege(manifest).Where(x => x["Typ"] is "Daten" or "Einmalig").ToList();
        if (!daten.Any(x => x["Id"] == "SQL/108") || !daten.Any(x => x["Id"] == "SQL/110")) fehler.Add("SQL/108 bzw. SQL/110 fehlt");
        foreach (var x in daten)
        {
            var a = x["Ausfuehrung"];
            if (a is not ("anzeigen" or "Trockenlauf" or "Transaktion")) { fehler.Add($"{x["Id"]}: Ausfuehrung {a}"); continue; }
            if (a == "anzeigen") continue;
            var t = sql(x["Datei"]);
            if (a == "Trockenlauf" && Regex.Matches(t, Regex.Escape("DECLARE @Trockenlauf BIT = 1;")).Count != 1) fehler.Add($"{x["Id"]}: Literal nicht genau einmal");
            if (a == "Trockenlauf" && !t.Contains("TREFFER: ")) fehler.Add($"{x["Id"]}: ohne Zeile TREFFER");
            if (Regex.IsMatch(t, @"(?i)\bROLLBACK\b")) fehler.Add($"{x["Id"]}: ROLLBACK");
            if (Regex.IsMatch(t, @"(?m)^\s*:") || t.Contains("$(")) fehler.Add($"{x["Id"]}: sqlcmd-Direktive");
        }
        return fehler;
    }

    /// <summary>Kopie des Schluesselwort-Waechters aus Invoke-Lesen (Update-IdealAkeWms.ps1, function Invoke-Lesen) - beide gleich halten.</summary>
    private const string LeseWaechter = @"(?i)\b(INSERT|UPDATE|DELETE|MERGE|DROP|ALTER|CREATE|EXEC|EXECUTE|TRUNCATE|GRANT)\b";

    /// <summary>v1.99.0: Typ Pruefung - lesende Abfrage (passiert den Waechter), Reparatur mit genau einem von Reparatur/Datei, Phase Haltepunkt/Nachher.</summary>
    [Fact]
    public void Manifest_Pruefungen_Gueltig()
    {
        var p = Eintraege(ManifestText).Where(x => x["Typ"] == "Pruefung").ToList();
        p.Select(x => x["Id"]).Should().Contain(new[] { "Pruefung/Kommissionierbereiche", "Pruefung/RCSI", "Pruefung/RollenBeschichtungsauftrag", "Pruefung/DummyArtikel" });
        File.ReadAllText(Path.Combine(Root, "scripts", "deploy", "Update-IdealAkeWms.ps1"))
            .Should().Contain(@"'\b(INSERT|UPDATE|DELETE|MERGE|DROP|ALTER|CREATE|EXEC|EXECUTE|TRUNCATE|GRANT)\b'", "Waechter im Skript = Kopie hier");
        foreach (var x in p)
        {
            x.Should().ContainKey("Pruefung", x["Id"]);
            Regex.IsMatch(x["Pruefung"], LeseWaechter).Should().BeFalse($"{x["Id"]}: Pruefung muss lesend sein");
            x["Phase"].Should().BeOneOf(new[] { "Haltepunkt", "Nachher" }, x["Id"]);
            x["Ausfuehrung"].Should().BeOneOf(new[] { "Reparatur", "anzeigen" }, x["Id"]);
            if (x["Ausfuehrung"] == "Reparatur") (x.ContainsKey("Reparatur") ^ x.ContainsKey("Datei")).Should().BeTrue($"{x["Id"]}: genau eines von Reparatur/Datei");
            if (x["Phase"] == "Nachher") x["Ausfuehrung"].Should().Be("anzeigen", $"{x["Id"]}: keine Reparatur nach dem Start");
        }
    }

    /// <summary>v1.99.0: jedes Braucht verweist auf einen Schema-Eintrag mit MigrationId (sonst Abbruch im Skript).</summary>
    [Fact]
    public void Manifest_BrauchtVerweistAufVorhandeneIds()
    {
        var schema = Eintraege(ManifestText).Where(x => x.ContainsKey("MigrationId")).Select(x => x["Id"]).ToHashSet();
        var braucht = Regex.Matches(ManifestText, @"Braucht=@\(([^)]*)\)")
            .SelectMany(m => Regex.Matches(m.Groups[1].Value, "'([^']+)'").Select(b => b.Groups[1].Value)).ToList();
        braucht.Should().NotBeEmpty();
        braucht.Should().OnlyContain(b => schema.Contains(b));
    }

    /// <summary>v1.99.0 Bugfix 2: in PS 5.1 gehoert ? zum Variablennamen - "$PaketVersion?" liest die nicht gesetzte Variable PaketVersion?.</summary>
    [Fact]
    public void DeploySkripte_KeineVariableMitFragezeichenImText()
    {
        foreach (var f in Directory.GetFiles(Path.Combine(Root, "scripts", "deploy")))
            VariableMitFragezeichen(File.ReadAllText(f)).Should().BeEmpty(Path.GetFileName(f));
        VariableMitFragezeichen("Write-Host \"v$PaketVersion?\"").Should().NotBeEmpty("Rotprobe");
    }

    private static List<string> VariableMitFragezeichen(string text) => Regex.Matches(text, @"\$[A-Za-z_]\w*\?").Select(m => m.Value).ToList();

    [Fact]
    public void Manifest_IdsEindeutig_FelderGueltig()
    {
        var e = Eintraege(ManifestText);
        e.Select(x => x["Id"]).Should().OnlyHaveUniqueItems();
        e.Should().OnlyContain(x => new[] { "AKE", "IDEAL", "beide" }.Contains(x["Firma"]));
        e.Should().OnlyContain(x => new[] { "Vorher", "Schema", "Haltepunkt", "VorStart", "Nachher" }.Contains(x["Phase"]));
        e.Should().OnlyContain(x => new[] { "Schema", "Daten", "Einmalig", "Pruefung", "Einstellung", "Leitstand", "Info", "Ordner" }.Contains(x["Typ"]));
        e.Select(x => Version.TryParse(x["Version"], out var v) ? v : null).Should().NotContainNulls();
    }

    [Fact]
    public void DeploySkripte_NurAscii_OhneSage_RechteNurHinzufuegend()
    {
        foreach (var f in Directory.GetFiles(Path.Combine(Root, "scripts", "deploy")))
        {
            var bytes = File.ReadAllBytes(f);
            bytes.Should().OnlyContain(b => b < 0x80, $"{Path.GetFileName(f)}: PS 5.1 liest Dateien ohne BOM als ANSI");
            var text = Encoding.ASCII.GetString(bytes);
            if (Path.GetFileName(f) is "Update-IdealAkeWms.ps1" or "IdealAkeWms.Deploy.psm1")
                text.Should().NotContainAny("SageConnection", "SData", "Sage100");
            text.Should().NotContainAny("/reset", "/remove", "/inheritance:r", "/setowner");
        }
    }

    /// <summary>F3 (v1.98.1): die Url aus ziele.psd1 hat Vorrang vor der IIS-Bindung - jedes der 5 Systeme hat eine gueltige http/https-Url.</summary>
    [Fact]
    public void Ziele_AlleFuenfSystemeMitGueltigerHttpUrl()
    {
        var ziele = Regex.Matches(File.ReadAllText(Path.Combine(Root, "scripts", "deploy", "ziele.psd1")), @"(?m)^\s*@\{ Firma=.*$")
            .Select(z => Regex.Matches(z.Value, @"(\w+)='([^']*)'").ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value)).ToList();
        ziele.Select(z => z["Firma"] + "-" + z["System"]).Should().BeEquivalentTo("IDEAL-Prod", "IDEAL-Test", "AKE-Prod", "AKE-Test", "AKE-Test-IDEAL");
        foreach (var z in ziele)
        {
            z.Should().ContainKey("Url", $"{z["Firma"]}-{z["System"]}");
            Uri.TryCreate(z["Url"], UriKind.Absolute, out var u).Should().BeTrue(z["Url"]);
            u!.Scheme.Should().BeOneOf(new[] { "http", "https" }, z["Url"]);
            u.Host.Should().NotBeNullOrEmpty();
        }
    }
}
