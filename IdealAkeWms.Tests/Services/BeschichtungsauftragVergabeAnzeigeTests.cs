using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IdealAkeWms.Tests.Services;

/// <summary>
/// Markierung der Vorschlagsliste (Spec 2026-10-06-beschichtungsauftrag-ausbau-teil-4, Anforderung 1, AK 2/2a/5c):
/// Auftragsnummern je HauptFA, Anzahl freier Positionen (Button ja/nein) und Nummern je Position (Badge). Die Auftraege
/// entstehen ueber den echten Teil-3-Workflow, damit die Anzeige dieselben Vergaben sieht wie die Anlage.
/// </summary>
public class BeschichtungsauftragVergabeAnzeigeTests
{
    private readonly ApplicationDbContext _db = TestDbContextFactory.Create(Guid.NewGuid().ToString());
    private readonly BeschichtungsauftragWorkflow _wf;
    private readonly BeschichtungsauftragVergabeAnzeige _anzeige;

    public BeschichtungsauftragVergabeAnzeigeTests()
    {
        var repo = new BeschichtungsauftragRepository(_db);
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetDisplayName()).Returns("Tester");
        user.Setup(u => u.GetWindowsUserName()).Returns(@"DOM\tester");
        _wf = new BeschichtungsauftragWorkflow(repo, new FaHierarchyNodeRepository(_db), new FaHierarchyOrderInfoRepository(_db),
            user.Object, TimeProvider.System, NullLogger<BeschichtungsauftragWorkflow>.Instance);
        _anzeige = new BeschichtungsauftragVergabeAnzeige(repo, new FaHierarchyNodeRepository(_db));

        foreach (var h in new[] { 1, 2 })
        {
            for (var p = 1; p <= 3; p++)
                _db.FaHierarchyNodes.Add(new FaHierarchyNode { HauptFA = h, VaterFA = 10, SubFA = 0, Position = p, Beschichtet = true, Sollmenge = 1m, Artnr = "A" + p, HauptArtnr = "H", Matchcode = "MC" });
            _db.FaHierarchyNodes.Add(new FaHierarchyNode { HauptFA = h, VaterFA = 10, SubFA = 0, Position = 9, Beschichtet = false, Artnr = "X", HauptArtnr = "H", Matchcode = "MC" });
            _db.FaHierarchyOrderInfos.Add(new FaHierarchyOrderInfo { HauptFA = h, Dienstleister = "Huber" });
        }
        _db.SaveChanges();
    }

    private async Task<Beschichtungsauftrag> Anlegen(int hauptFa)
    {
        var r = await _wf.AnlegenAsync(hauptFa);
        r.Fehler.Should().BeEmpty();
        return r.Auftrag!;
    }

    [Fact]
    public async Task OhneAuftrag_AlleBeschichtetenFrei()
    {
        var info = (await _anzeige.ErmittelnAsync(new[] { 1 }))[1];

        info.Auftraege.Should().BeEmpty();
        info.FreiePositionen.Should().Be(3, "die unbeschichtete Position zaehlt nicht");
        info.NummernJePosition.Should().BeEmpty();
    }

    [Fact]
    public async Task VollVergeben_KeineFreien_NummerJePosition_AndererHauptFaUnberuehrt()
    {
        var a = await Anlegen(1);

        var infos = await _anzeige.ErmittelnAsync(new[] { 1, 2 });

        infos[1].Auftraege.Should().Equal((a.Id, a.Nummer));
        infos[1].FreiePositionen.Should().Be(0);
        infos[1].NummernJePosition.Should().HaveCount(3).And.AllSatisfy(kv => kv.Value.Should().Equal(a.Nummer));
        infos[2].Auftraege.Should().BeEmpty();
        infos[2].FreiePositionen.Should().Be(3);
    }

    [Fact]
    public async Task TeilweiseVergeben_FreieZaehlenWieder()
    {
        var a = await Anlegen(1);
        var entfernt = a.Positionen.First(p => p.Position == 2).Id;
        (await _wf.EntwurfBearbeitenAsync(a.Id, new KopfDaten("Huber", null, null, null),
            new Dictionary<int, decimal>(), new[] { entfernt }, a.RowVersion)).Fehler.Should().BeEmpty();

        var info = (await _anzeige.ErmittelnAsync(new[] { 1 }))[1];

        info.Auftraege.Should().ContainSingle();
        info.FreiePositionen.Should().Be(1);
        info.NummernJePosition.Keys.Should().NotContain(new PositionsSchluessel(10, 0, 2));
    }

    [Fact]
    public async Task Storniert_GibtFrei()
    {
        var a = await Anlegen(1);
        (await _wf.StatusWechselnAsync(a.Id, BeschichtungsauftragStatus.Storniert, a.RowVersion)).Fehler.Should().BeEmpty();

        var info = (await _anzeige.ErmittelnAsync(new[] { 1 }))[1];

        info.Auftraege.Should().BeEmpty();
        info.FreiePositionen.Should().Be(3);
    }

    [Fact]
    public async Task Reklamation_ZaehltMit_PositionTraegtBeideNummern()
    {
        var a = await Anlegen(1);
        (await _wf.StatusWechselnAsync(a.Id, BeschichtungsauftragStatus.Uebergeben, a.RowVersion)).Fehler.Should().BeEmpty();
        (await _wf.StatusWechselnAsync(a.Id, BeschichtungsauftragStatus.Erledigt, a.RowVersion)).Fehler.Should().BeEmpty();
        var pos1 = a.Positionen.First(p => p.Position == 1).Id;
        var rekl = (await _wf.ReklamationAnlegenAsync(a.Id, new[] { pos1 })).Auftrag!;

        var info = (await _anzeige.ErmittelnAsync(new[] { 1 }))[1];

        info.Auftraege.Select(x => x.Nummer).Should().Equal(new[] { a.Nummer, rekl.Nummer }.OrderBy(n => n));
        info.NummernJePosition[new PositionsSchluessel(10, 0, 1)].Should().BeEquivalentTo(new[] { a.Nummer, rekl.Nummer });
        info.FreiePositionen.Should().Be(0);
    }

    [Fact]
    public async Task LeereMenge_LeeresErgebnis()
        => (await _anzeige.ErmittelnAsync(Array.Empty<int>())).Should().BeEmpty();
}
