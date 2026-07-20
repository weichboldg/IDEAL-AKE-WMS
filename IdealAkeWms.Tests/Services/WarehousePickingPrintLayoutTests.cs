using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Xunit;

namespace IdealAkeWms.Tests.Services;

public class WarehousePickingPrintLayoutTests
{
    private static WarehouseRequisitionDetailItemViewModel Item(
        int pos, string art, string desc, int req, int? picked, string unit,
        string storage, string? note = null, string? noteEk = null,
        ShortageStatus shortage = ShortageStatus.None) =>
        new(pos, pos, art, desc, unit, req, picked, storage, note, shortage, noteEk);

    [Fact]
    public void ResolveColumns_NoPrefs_ReturnsAllInDefaultOrder()
    {
        var cols = WarehousePickingPrintLayout.ResolveColumns((string?)null);

        cols.Select(c => c.Key).Should().Equal(
            "pos", "article-number", "description", "requested", "picked",
            "unit", "storage", "note-lager", "note-ek", "shortage");
    }

    [Fact]
    public void ResolveColumns_HiddenColumn_Omitted_LockedKept()
    {
        // note-ek versteckt; pos (locked) faelschlich visible=false -> bleibt trotzdem.
        var json = """
        {"columns":[
          {"key":"pos","visible":false,"order":0},
          {"key":"article-number","visible":true,"order":1},
          {"key":"description","visible":true,"order":2},
          {"key":"requested","visible":true,"order":3},
          {"key":"picked","visible":true,"order":4},
          {"key":"unit","visible":true,"order":5},
          {"key":"storage","visible":true,"order":6},
          {"key":"note-lager","visible":true,"order":7},
          {"key":"note-ek","visible":false,"order":8},
          {"key":"shortage","visible":true,"order":9}
        ],"defaultSortColumn":null,"defaultSortDirection":"asc"}
        """;

        var cols = WarehousePickingPrintLayout.ResolveColumns(json);

        cols.Select(c => c.Key).Should().Contain("pos");        // locked, trotz visible=false
        cols.Select(c => c.Key).Should().NotContain("note-ek"); // versteckt
    }

    [Fact]
    public void ResolveColumns_Reordered_RespectsOrder()
    {
        // storage (order 1) vor article-number (order 6) ziehen; pos bleibt order 0.
        var json = """
        {"columns":[
          {"key":"pos","visible":true,"order":0},
          {"key":"storage","visible":true,"order":1},
          {"key":"article-number","visible":true,"order":6},
          {"key":"description","visible":true,"order":2},
          {"key":"requested","visible":true,"order":3},
          {"key":"picked","visible":true,"order":4},
          {"key":"unit","visible":true,"order":5},
          {"key":"note-lager","visible":true,"order":7},
          {"key":"note-ek","visible":true,"order":8},
          {"key":"shortage","visible":true,"order":9}
        ],"defaultSortColumn":null,"defaultSortDirection":"asc"}
        """;

        var cols = WarehousePickingPrintLayout.ResolveColumns(json);

        cols.Select(c => c.Key).Should().Equal(
            "pos", "storage", "description", "requested", "picked", "unit",
            "article-number", "note-lager", "note-ek", "shortage");
    }

    [Fact]
    public void CellText_RendersExpectedStrings()
    {
        var i = Item(1, "ART-1", "Schraube", 5, null, "Stk", "L01",
            note: "nl", noteEk: "ek", shortage: ShortageStatus.WillBeRestocked);

        WarehousePickingPrintLayout.CellText(i, "pos").Should().Be("1");
        WarehousePickingPrintLayout.CellText(i, "article-number").Should().Be("ART-1");
        WarehousePickingPrintLayout.CellText(i, "requested").Should().Be("5");
        WarehousePickingPrintLayout.CellText(i, "picked").Should().Be("");        // null -> leer
        WarehousePickingPrintLayout.CellText(i, "shortage").Should().Be("Fehlteil");
    }

    [Fact]
    public void SortItems_ByStorageDesc_OrdersByCellText()
    {
        var items = new[]
        {
            Item(1, "A", "x", 1, null, "Stk", "L01"),
            Item(2, "B", "y", 1, null, "Stk", "L09"),
            Item(3, "C", "z", 1, null, "Stk", "L05"),
        };

        var sorted = WarehousePickingPrintLayout.SortItems(
            items, sortCol: "storage", sortDir: "desc",
            defaultSortColumn: null, defaultSortDirection: null);

        sorted.Select(s => s.StorageLocations).Should().Equal("L09", "L05", "L01");
    }

    [Fact]
    public void SortItems_ByRequested_NumericNotLexical()
    {
        var items = new[]
        {
            Item(1, "A", "x", 9,  null, "Stk", "L"),
            Item(2, "B", "y", 10, null, "Stk", "L"),
            Item(3, "C", "z", 2,  null, "Stk", "L"),
        };

        var sorted = WarehousePickingPrintLayout.SortItems(
            items, sortCol: "requested", sortDir: "asc",
            defaultSortColumn: null, defaultSortDirection: null);

        sorted.Select(s => s.QuantityRequested).Should().Equal(2m, 9m, 10m);
    }

    [Fact]
    public void SortItems_NoSortCol_UsesDefaultSortColumn()
    {
        var items = new[]
        {
            Item(1, "A", "x", 1, null, "Stk", "L02"),
            Item(2, "B", "y", 1, null, "Stk", "L01"),
        };

        var sorted = WarehousePickingPrintLayout.SortItems(
            items, sortCol: null, sortDir: null,
            defaultSortColumn: "storage", defaultSortDirection: "asc");

        sorted.Select(s => s.StorageLocations).Should().Equal("L01", "L02");
    }

    [Fact]
    public void SortItems_NoSortAtAll_KeepsPositionOrder()
    {
        var items = new[]
        {
            Item(3, "A", "x", 1, null, "Stk", "L"),
            Item(1, "B", "y", 1, null, "Stk", "L"),
            Item(2, "C", "z", 1, null, "Stk", "L"),
        };

        var sorted = WarehousePickingPrintLayout.SortItems(
            items, sortCol: null, sortDir: null,
            defaultSortColumn: null, defaultSortDirection: null);

        sorted.Select(s => s.Position).Should().Equal(1, 2, 3); // Fallback = pos asc
    }

    [Fact]
    public void ParsePrefs_InvalidJson_ReturnsNull()
    {
        WarehousePickingPrintLayout.ParsePrefs("not-json").Should().BeNull();
        WarehousePickingPrintLayout.ParsePrefs(null).Should().BeNull();
        WarehousePickingPrintLayout.ParsePrefs("").Should().BeNull();
    }
}
