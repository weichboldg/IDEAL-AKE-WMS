using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using IDEALAKEWMSService.Models;
using IDEALAKEWMSService.Services;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class BomCacheSyncServiceHashTests
{
    // ComputeContentHash is internal static — invoke via reflection for testing.
    private static string Hash(List<BomCacheItem> items)
    {
        var mi = typeof(BomCacheSyncService).GetMethod(
            "ComputeContentHash",
            BindingFlags.Static | BindingFlags.NonPublic);
        return (string)mi!.Invoke(null, new object[] { items })!;
    }

    private static BomCacheItem Item(string pos, string res, decimal menge, string bez1 = "")
        => new BomCacheItem
        {
            Position = pos, Ressourcenummer = res, Menge = menge,
            Bezeichnung1 = bez1, Bezeichnung2 = "", Baugruppe = "",
            Beschaffungsartikel = "", Artikelgruppe = ""
        };

    [Fact]
    public void Hash_IsDeterministic_ForSameInput()
    {
        var items = new List<BomCacheItem>
        {
            Item("10", "R1", 2m, "A"),
            Item("20", "R2", 3m, "B")
        };
        Hash(items).Should().Be(Hash(items));
    }

    [Fact]
    public void Hash_IsOrderIndependent_DueToSort()
    {
        var a = new List<BomCacheItem>
        {
            Item("10", "R1", 2m, "A"),
            Item("20", "R2", 3m, "B")
        };
        var b = new List<BomCacheItem>
        {
            Item("20", "R2", 3m, "B"),
            Item("10", "R1", 2m, "A")
        };
        Hash(a).Should().Be(Hash(b));
    }

    [Fact]
    public void Hash_Differs_WhenQuantityChanges()
    {
        var a = new List<BomCacheItem> { Item("10", "R1", 2m) };
        var b = new List<BomCacheItem> { Item("10", "R1", 3m) };
        Hash(a).Should().NotBe(Hash(b));
    }

    [Fact]
    public void Hash_HandlesNullFields()
    {
        var items = new List<BomCacheItem>
        {
            new BomCacheItem { Position = null!, Ressourcenummer = null!, Menge = 1m,
                Bezeichnung1 = null!, Bezeichnung2 = null!, Baugruppe = null!,
                Beschaffungsartikel = null!, Artikelgruppe = null! }
        };
        var result = Hash(items);
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().Be(64); // SHA256 hex
    }

    [Fact]
    public void Hash_Is64HexChars()
    {
        var items = new List<BomCacheItem> { Item("10", "R1", 1m) };
        Hash(items).Should().MatchRegex("^[0-9a-f]{64}$");
    }
}
