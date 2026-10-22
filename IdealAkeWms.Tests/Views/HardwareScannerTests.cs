using System.Diagnostics;
using FluentAssertions;
using IdealAkeWms.Models;
using IdealAkeWms.Models.Standort;
using Xunit;

namespace IdealAkeWms.Tests.Views;

/// <summary>
/// Hardware-Barcodescanner (v1.66.0, Spec 2026-10-15-scan-handler-hardware-scanner): Node-Selbstpruefung des reinen Kerns plus
/// QUELLTEXT-WAECHTER fuer die Verdrahtung. Ob ein echter Scanner am Geraet ankommt (Layout, Tottaste, IME), ist Manual-UAT TS-99.
/// </summary>
public class HardwareScannerTests
{
    private static readonly string Root = FindRoot();

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "IdealAkeWms", "wwwroot", "js", "hardware-scanner.js")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new FileNotFoundException("Repository-Root nicht gefunden");
    }

    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine(new[] { Root, "IdealAkeWms" }.Concat(parts).ToArray()));

    [SkippableFact]
    public void Kern_NodeSelbstpruefung()
    {
        var script = Path.Combine(Root, "IdealAkeWms.Tests", "Js", "hardware-scanner.check.js");
        var module = Path.Combine(Root, "IdealAkeWms", "wwwroot", "js", "hardware-scanner.js");
        Process? p = null;
        try
        {
            p = Process.Start(new ProcessStartInfo("node", $"\"{script}\" \"{module}\"")
            {
                RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
            });
        }
        catch (System.ComponentModel.Win32Exception)
        {
            Skip.If(true, "Node ist auf diesem Rechner nicht installiert — Kern-Pruefung uebersprungen.");
        }
        using (p!)
        {
            var output = p!.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
            p.WaitForExit(30_000).Should().BeTrue();
            p.ExitCode.Should().Be(0, output);
            output.Should().Contain("hardware-scanner core ok");
        }
    }

    [Fact]
    public void Layout_LaedtSkriptNurBeiSchalterAn_VorDenSeitenSkripten()
    {
        var layout = Read("Views", "Shared", "_Layout.cshtml");
        layout.Should().Contain("AppSettingKeys.HardwareScannerAktiv), \"true\", StringComparison.OrdinalIgnoreCase)",
            "nur exakt \"true\" schaltet ein (AK 1, ADR 0011)");
        var script = layout.IndexOf("~/js/hardware-scanner.js", StringComparison.Ordinal);
        var guard = layout.LastIndexOf("@if (hwScanAktiv)", script, StringComparison.Ordinal);
        guard.Should().BePositive("das Skript steht im Schalter-Block");
        script.Should().BeLessThan(layout.IndexOf("RenderSectionAsync(\"Scripts\"", StringComparison.Ordinal));
        layout.Should().Contain("<body data-hw-scan-praefix=\"@hwScanPraefix\" data-hw-scan-abstand=\"@hwScanAbstand\">");
    }

    [Fact]
    public void DomTeil_PasswortIMEUndSynthetischeTastenNieAbfangen()
    {
        var js = Read("wwwroot", "js", "hardware-scanner.js");
        js.Should().Contain("if (!e.isTrusted || e.isComposing || e.keyCode === 229 || geraetAus()) return;",
            "das eigene synthetische Enter darf nicht erneut verarbeitet werden; IME ist nicht abfangbar");
        js.Should().Contain("aktiv.type === 'password') return;", "AK 6");
        js.Should().Contain("el.readOnly || !el.getClientRects().length) return false;",
            "ein ausgeblendetes, noch fokussiertes Feld (BDE scanOperator nach der Anmeldung) ist kein freies Feld (QA-Befund B-1)");
        js.Should().Contain("getModifierState('AltGraph')", "~ kommt auf DE-Layout per AltGr und ist kein Tastenkuerzel");
        js.Should().Contain("window.addEventListener('keydown'").And.Contain("}, true);", "Capture vor den Seiten-Handlern");
        js.Should().Contain("box.textContent = ", "Wert nie als HTML (AK 11)");
        js.Should().Contain("setAttribute('role', 'status')");
        js.Should().NotContain("innerHTML");
    }

    [Fact]
    public void BarcodeScanner_RegistriertBeideInitWege_UndNutztGlobaleAuswertung()
    {
        var js = Read("wwwroot", "js", "barcode-scanner.js");
        System.Text.RegularExpressions.Regex.Matches(js, @"_hwScanTargets\.push\(").Count.Should().Be(2,
            "initScanner und initTextInputScanner tragen ihre Ziele ein");
        js.Should().Contain("processScannedValue(d.code, ziel.targetId, ziel.scanType, ziel.qrFaEnabled, ziel.faTargetId);",
            "dieselbe (gewrappte) Auswertung wie der Kamera-Scan — keine zweite Erkennung");
        js.Should().Contain("if (ziel.textInput) _textInputScanCallback = ziel.textInput;");
        js.Should().Contain("if (d.freiesFeld) return;", "Regel (b) vor (d)");
    }

    [Fact]
    public void OffenesKameraFenster_IstZiel_WieKameraTreffer()
    {
        // v1.66.1 N-1 / v1.66.2 Nachtrag 3: offenes Kamera-Fenster eines Knopfs mit data-hw-scan-kamera-ziel = angetipptes Ziel, VOR Regel (a)/(d).
        var js = Read("wwwroot", "js", "barcode-scanner.js");
        js.Should().Contain("modal.dataset.zielId = targetSelectId;");
        js.Should().Contain("btn.hasAttribute('data-hw-scan-kamera-ziel')");
        var listener = js[js.IndexOf("document.addEventListener('wms:hardware-scan'", StringComparison.Ordinal)..];
        listener.IndexOf("hwScanKameraZiel(vorhanden)", StringComparison.Ordinal)
            .Should().BePositive().And.BeLessThan(listener.IndexOf("hwScanFokusIn(z, aktiv)", StringComparison.Ordinal));
        listener.Should().Contain("_scannerClosing = true;").And.Contain("closeScannerModal();",
            "dieselbe Abfolge wie der Kamera-Treffer in startCameraScanner: sperren, auswerten, schliessen");
        // QA v1.66.1: html5-qrcode ruft den Treffer-Callback auch fuer einen Decode, der vor stop() lief - nach dem Schliessen ignorieren.
        js.Should().Contain("if (_scannerClosing || _activeScanner !== html5QrCode) return;");

        // Kamera-Treffer gibt es nur ueber openScannerModal/startCameraScanner: keine Seite baut einen eigenen Html5Qrcode, damit gilt der
        // QA-Fix (kein Treffer nach dem Schliessen) auf allen Seiten.
        var viewsRoot = Path.Combine(Root, "IdealAkeWms", "Views");
        var views = Directory.EnumerateFiles(viewsRoot, "*.cshtml", SearchOption.AllDirectories)
            .ToDictionary(f => Path.GetRelativePath(viewsRoot, f).Replace('\\', '/'), File.ReadAllText);
        views.Where(v => v.Value.Contains("new Html5Qrcode")).Select(v => v.Key).Should().BeEmpty();

        // Freigabe-Nachtrag 3: jeder Kamera-Knopf jeder Seite mit mehr als einem Scan-Feld - und nur diese.
        var erwartet = new[]
        {
            "Picking/Bom.cshtml|btnScanBomArticle", "Picking/Bom.cshtml|btnScanTargetLocation",
            "StockMovements/Inbound.cshtml|btnScanArticle", "StockMovements/Inbound.cshtml|btnScanStorageLocation",
            "StockMovements/InboundBulk.cshtml|btnScanStorageLocation", "StockMovements/InboundBulk.cshtml|btnScanArticleBulk",
            "StockMovements/Outbound.cshtml|btnScanArticle", "StockMovements/Outbound.cshtml|btnScanStorageLocation",
            "StockMovements/Transfer.cshtml|btnScanArticle", "StockMovements/Transfer.cshtml|btnScanSourceLocation",
            "StockMovements/Transfer.cshtml|btnScanStorageLocation",
            "StockMovements/LocationTransfer.cshtml|btnScanSourceLocation", "StockMovements/LocationTransfer.cshtml|btnScanTargetLocation",
            "StockOverview/Index.cshtml|btnScanArticleFilter", "StockOverview/Index.cshtml|btnScanLocationFilter", "StockOverview/Index.cshtml|btnScanPO",
            "ProductionOrders/Index.cshtml|btnScanOrderNumber", "ProductionOrders/Index.cshtml|btnScanArticleNumber",
            "PickingLeitstand/Index.cshtml|btnScanOrderNumber", "PickingLeitstand/Index.cshtml|btnScanArticleNumber",
            "Tracking/OseonIndex.cshtml|btnScanCustomerOrder", "Tracking/OseonIndex.cshtml|btnScanArticle",
        };
        views.SelectMany(v => System.Text.RegularExpressions.Regex.Matches(v.Value, @"<[^>]*\bdata-hw-scan-kamera-ziel\b[^>]*>")
                .Select(m => v.Key + "|" + System.Text.RegularExpressions.Regex.Match(m.Value, @"\bid=""([^""]+)""").Groups[1].Value))
            .Should().BeEquivalentTo(erwartet, "Kamera-Knopf je Seite mit mehr als einem Scan-Feld (Freigabe-Nachtrag 3)");
    }

    [Fact]
    public void BdeTerminal_BeideScanfelderSindZiele_OhneEigeneLogik()
    {
        var view = Read("Views", "BdeTerminal", "Index.cshtml");
        view.Should().Contain("id=\"scanOperator\"").And.Contain("id=\"scanFaAg\"");
        System.Text.RegularExpressions.Regex.Matches(view, @"\bdata-hw-scan-ziel\b").Count.Should().BeGreaterThanOrEqualTo(2);
        Read("wwwroot", "js", "bde-terminal.js").Should().NotContain("hw-scan", "bde-terminal.js bleibt unveraendert (Spec)");
    }

    [Fact]
    public void Standorteinstellungen_GruppeMitDreiKeys_KeinSeed()
    {
        StandortSettingsCatalog.GroupOrder.Should().Contain(StandortSettingsCatalog.GroupHardwareScanner);
        StandortSettingsCatalog.Fields.Where(f => f.Group == StandortSettingsCatalog.GroupHardwareScanner)
            .Select(f => (f.Key, f.Type, f.Backend)).Should().Equal(
                (AppSettingKeys.HardwareScannerAktiv, StandortFieldType.Bool, StandortBackend.App),
                (AppSettingKeys.HardwareScannerPraefix, StandortFieldType.String, StandortBackend.App),
                (AppSettingKeys.HardwareScannerMaxZeichenabstandMs, StandortFieldType.Int, StandortBackend.App));
        var abstand = StandortSettingsCatalog.Fields.Single(f => f.Key == AppSettingKeys.HardwareScannerMaxZeichenabstandMs);
        (abstand.Min, abstand.Max, abstand.EmptyResetsToDefault).Should().Be((10, 500, true), "v1.66.1 N-2, gleiche Grenzen wie HwScanCore.abstand");
        Read("wwwroot", "js", "hardware-scanner.js").Should().Contain("return n >= 10 && n <= 500 ? n : 50;")
            .And.Contain("var maxGapMs = abstand(body.getAttribute('data-hw-scan-abstand'));");
    }

    [Fact]
    public void Profil_GeraeteSchalter_NurLocalStorage()
    {
        var view = Read("Views", "Account", "Profile.cshtml");
        view.Should().Contain("'wms.hardwareScanner.geraet'").And.Contain("id=\"hwScanGeraet\" hidden")
            .And.Contain("document.body.hasAttribute('data-hw-scan-praefix')");
        Read("wwwroot", "js", "hardware-scanner.js").Should().Contain("localStorage.getItem('wms.hardwareScanner.geraet') === 'aus'");
    }
}
