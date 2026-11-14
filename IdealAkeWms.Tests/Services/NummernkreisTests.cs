using System.Reflection;
using FluentAssertions;
using IdealAkeWms.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdealAkeWms.Tests.Services;

/// <summary>v1.99.23 (N-2/AK-1): Deadlock bzw. UNIQUE-Verletzung in jeder Tiefe der Ausnahme-Kette.</summary>
public class NummernkreisTests
{
    /// <summary>SqlException hat keinen oeffentlichen Konstruktor – per Reflection (SqlError-Ctor mit den meisten Parametern).</summary>
    internal static SqlException SqlFehler(int nummer)
    {
        var errorCtor = typeof(SqlError).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length).First();
        var args = errorCtor.GetParameters().Select(p =>
            p.Name == "infoNumber" ? nummer
            : p.ParameterType == typeof(string) ? "test"
            : p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType)
            : null).ToArray();
        var error = (SqlError)errorCtor.Invoke(args);
        var collection = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), true)!;
        typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(collection, [error]);
        var create = typeof(SqlException).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .First(m => m.Name == "CreateException" && m.GetParameters().Length == 2
                        && m.GetParameters()[0].ParameterType == typeof(SqlErrorCollection));
        return (SqlException)create.Invoke(null, [collection, "16.0"])!;
    }

    [Fact]
    public void SqlFehler_Helfer_SetztNummer() => SqlFehler(1205).Number.Should().Be(1205);

    [Fact]
    public void IstDeadlock_VerpackteKette_Erkannt()
    {
        var ex = new InvalidOperationException("x", new DbUpdateException("y", SqlFehler(1205)));
        Nummernkreis.IstDeadlock(ex).Should().BeTrue();
        Nummernkreis.IstDeadlock(SqlFehler(1205)).Should().BeTrue();
        Nummernkreis.IstDeadlock(new DbUpdateException("y", SqlFehler(2627))).Should().BeFalse();
        Nummernkreis.IstDeadlock(new InvalidOperationException("x")).Should().BeFalse();
    }

    [Fact]
    public void IstSqlFehler_Unique_InKette()
    {
        Nummernkreis.IstSqlFehler(new DbUpdateException("y", SqlFehler(2601)), 2601, 2627).Should().BeTrue();
        Nummernkreis.IstSqlFehler(new DbUpdateException("y", SqlFehler(547)), 2601, 2627).Should().BeFalse();
    }

    [Fact]
    public async Task MitVersuchen_VerpackterDeadlock_WirdWiederholt()
    {
        var versuche = 0;
        var (ok, wert) = await Nummernkreis.MitVersuchenAsync(3, () =>
        {
            versuche++;
            if (versuche == 1) throw new InvalidOperationException("x", new DbUpdateException("y", SqlFehler(1205)));
            return Task.FromResult(42);
        }, () => { }, NullLogger.Instance, "Test");

        ok.Should().BeTrue();
        wert.Should().Be(42);
        versuche.Should().Be(2);
    }
}
