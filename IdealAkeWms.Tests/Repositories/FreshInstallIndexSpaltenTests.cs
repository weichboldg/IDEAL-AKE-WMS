using System.Text.RegularExpressions;
using FluentAssertions;
using IdealAkeWms.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IdealAkeWms.Tests.Repositories;

/// <summary>
/// WAECHTER fuer <c>SQL/00_FreshInstall.sql</c> (Spec 2026-11-14-sicherheit-anmeldung-und-installer, BH-2, AK 11): jede Spalte, die eine
/// <c>CREATE … INDEX</c>-Anweisung nennt (Schluessel, INCLUDE, WHERE), muss im relationalen EF-Modell an genau dieser Tabelle existieren.
/// Warum: ein unbekannter Spaltenname verwirft beim Kompilieren den GANZEN Batch (Msg 207) – 2026-11-04 fehlten so auf IDEAL-Prod alle
/// Indizes des Abschnitts 15. Nur Modell, keine Verbindung (Muster <see cref="DeployScriptRunsMigrationSqlTests"/>).
/// ponytail: prueft nur Index-Spalten, nicht die Spaltenlisten der CREATE TABLE; Ausbau, wenn dort Drift auffaellt.
/// </summary>
public class FreshInstallIndexSpaltenTests
{
    /// <summary>Tabellen ohne EF-Abbild, mit Grund. Indizes darauf werden nicht geprueft.</summary>
    private static readonly Dictionary<string, string> OhneEfAbbild = new(StringComparer.OrdinalIgnoreCase)
    {
        ["__EFMigrationsHistory"] = "EF-eigene Verwaltungstabelle",
    };

    private static readonly Regex IndexRegex = new(
        @"CREATE\s+(?:UNIQUE\s+)?(?:NONCLUSTERED\s+|CLUSTERED\s+)?INDEX\s+\[?(?<name>\w+)\]?\s+ON\s+(?:\[?dbo\]?\.)?\[?(?<tabelle>\w+)\]?\s*\((?<spalten>[^)]*)\)" +
        @"(?:\s*INCLUDE\s*\((?<include>[^)]*)\))?(?:\s*WHERE\s+(?<where>[^;\r\n]*))?",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static string FreshInstallOhneKommentare()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SQL", "00_FreshInstall.sql"))) dir = dir.Parent;
        var text = File.ReadAllText(Path.Combine(dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden"),
            "SQL", "00_FreshInstall.sql"));
        return Regex.Replace(text, @"--[^\r\n]*", "");
    }

    private static Dictionary<string, HashSet<string>> SpaltenJeTabelle()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=nonexistent-host-for-tests;Database=x;TrustServerCertificate=True;Connect Timeout=1").Options;
        using var db = new ApplicationDbContext(options);
        var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var et in db.Model.GetEntityTypes())
        {
            var tabelle = et.GetTableName();
            if (tabelle == null) continue;
            if (!result.TryGetValue(tabelle, out var set)) result[tabelle] = set = new(StringComparer.OrdinalIgnoreCase);
            foreach (var p in et.GetProperties())
                if (p.GetColumnName() is { } c) set.Add(c);
        }
        return result;
    }

    private static IEnumerable<string> Spaltennamen(string liste) =>
        liste.Split(',').Select(s => Regex.Replace(s, @"\s+(ASC|DESC)\s*$", "", RegexOptions.IgnoreCase).Trim().Trim('[', ']').Trim())
            .Where(s => s.Length > 0);

    [Fact]
    public void JedeIndexSpalte_ExistiertImEfModell()
    {
        var sql = FreshInstallOhneKommentare();
        var modell = SpaltenJeTabelle();
        var treffer = IndexRegex.Matches(sql);
        treffer.Count.Should().BeGreaterThan(100, "der Parser muss die Index-Anweisungen des Skripts finden");

        var befunde = new List<string>();
        foreach (Match m in treffer)
        {
            var tabelle = m.Groups["tabelle"].Value;
            var name = m.Groups["name"].Value;
            if (OhneEfAbbild.ContainsKey(tabelle)) continue;
            if (!modell.TryGetValue(tabelle, out var spalten))
            {
                befunde.Add($"{name}: Tabelle {tabelle} hat kein EF-Abbild (in OhneEfAbbild mit Grund aufnehmen oder Index pruefen)");
                continue;
            }
            var genannt = Spaltennamen(m.Groups["spalten"].Value).Concat(Spaltennamen(m.Groups["include"].Value))
                .Concat(Regex.Matches(m.Groups["where"].Value, @"\[(\w+)\]").Select(w => w.Groups[1].Value));
            befunde.AddRange(genannt.Where(s => !spalten.Contains(s)).Select(s => $"{name}: Spalte {tabelle}.{s} fehlt im EF-Modell"));
        }

        befunde.Should().BeEmpty("jede Index-Spalte in 00_FreshInstall.sql muss existieren, sonst verwirft SQL Server den ganzen Batch");
    }

    [Fact]
    public void WarehouseRequisitions_TabellenMitGuard()
    {
        var sql = FreshInstallOhneKommentare();
        sql.Should().Contain("IF OBJECT_ID(N'dbo.WarehouseRequisitions', N'U') IS NULL")
            .And.Contain("IF OBJECT_ID(N'dbo.WarehouseRequisitionItems', N'U') IS NULL");
    }
}
