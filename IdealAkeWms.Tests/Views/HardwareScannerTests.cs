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
        // v1.67.1: Kopf umgebaut (Diagnose-Zeile je Entscheidung, Enter-Suffix der Fokus-Eingabe vor der 229-Ausnahme), Regeln gleich.
        var lf = js.Replace("\r\n", "\n");
        var taste = lf[lf.IndexOf("window.addEventListener('keydown'", StringComparison.Ordinal)..];
        taste.Should().StartWith("window.addEventListener('keydown', function (e) {\n        if (!e.isTrusted) return;",
            "das eigene synthetische Enter darf nicht erneut verarbeitet werden");
        taste.Should().Contain("if (e.isComposing || e.keyCode === 229) {", "IME ist nicht abfangbar");
        taste.Should().Contain("if (geraetAus()) {");
        taste.Should().Contain("aktiv.type === 'password' && !core.pending()) { diag('keydown (Passwortfeld) → ignoriert'); return; }", "AK 6, Taste nie protokolliert");
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
    public void FokusEingabe_TextErkennung_NurEchteEingaben_KeineDoppelauswertung()
    {
        // Freigabe-Nachtrag 4 (Netum): Praefix am eingefuegten Text erkennen, ueber dieselbe Zustellung wie der Tasten-Scan.
        var js = Read("wwwroot", "js", "hardware-scanner.js");
        js.Should().Contain("window.addEventListener('input', function (e) {").And.Contain("window.addEventListener('beforeinput', function (e) {");
        js.Should().Contain("if (!e.isTrusted || !freiesFeld(el)) return;", "synthetische Ereignisse (feldFuellen) nie erneut; Passwort ist kein freies Feld");
        js.Should().Contain("/^(text|search|tel|url|email|number)$/i").And.NotContain("|password", "Passwortfelder ausgenommen");
        js.Should().Contain("if (core.pending()) { diag(info + ' → ignoriert: Tasten-Scan läuft'); return; }", "keine Doppelauswertung");
        js.Should().Contain("e.stopImmediatePropagation();  // Seiten-Handler").And.Contain("el.value = r.wert;", "der Praefix bleibt nie im Feld");
        js.Should().Contain("if (r.scan) zustellen(r.scan);", "dieselbe Zustellung (Kamera-Fenster, Regel a/b/d, Hinweis) wie der Tasten-Scan");
        var taste = js[js.IndexOf("window.addEventListener('keydown'", StringComparison.Ordinal)..];
        taste.IndexOf("textCore.aktiv() && (e.key === 'Enter' || e.keyCode === 13)", StringComparison.Ordinal)
            .Should().BePositive().And.BeLessThan(taste.IndexOf("e.keyCode === 229) {", StringComparison.Ordinal),
                "das Enter-Suffix wird abgefangen, bevor es ein Formular absendet - auch wenn das Geraet sonst 229 meldet");
    }

    [Fact]
    public void Diagnose_NurDiesesGeraet_NichtsAnDenServer()
    {
        var js = Read("wwwroot", "js", "hardware-scanner.js");
        js.Should().Contain("localStorage.getItem('wms.hardwareScanner.diagnose') === 'an'").And.Contain("if (diagZeilen.length > 30)");
        js.Should().Contain("diagPre.textContent = ").And.NotContain("innerHTML");
        foreach (var weg in new[] { "fetch(", "XMLHttpRequest", "sendBeacon", "$.ajax", "$.post" })
            js.Should().NotContain(weg, "Diagnose-Daten bleiben im Browser");
        var view = Read("Views", "Account", "Profile.cshtml");
        view.Should().Contain("id=\"hwScanDiagnoseAn\"").And.Contain("'wms.hardwareScanner.diagnose'")
            .And.Contain("if (diag.checked) localStorage.setItem(DIAG, 'an'); else localStorage.removeItem(DIAG);", "Standard aus");
    }

    [Fact]
    public void KameraFenster_HandscannerFeld_FuerFokusEingabe()
    {
        var js = Read("wwwroot", "js", "barcode-scanner.js");
        js.Should().Contain("${document.body.hasAttribute('data-hw-scan-praefix') ? `", "nur bei Standort-Schalter an");
        js.Should().Contain("id=\"scannerHwInput\" class=\"form-control\" inputmode=\"none\"", "keine Bildschirmtastatur ueber dem Kamerabild");
        js.Should().Contain("if (hwFeld) hwFeld.focus({ preventScroll: true });", "ohne Fokus gibt ein Fokus-Eingabe-Geraet nichts aus");
        js.Should().Contain("|| document.activeElement === document.getElementById('scannerHwInput')", "auch auf Seiten mit einem Scan-Feld");
    }

    [Fact]
    public void ScannenOhneFeld_NurAufEinbuchung_UnsichtbarOhneTastatur_NurMitSchalter()
    {
        // Freigabe-Nachtrag 5 (v1.67.2): Pilot nur auf StockMovements/Inbound.
        var views = Path.Combine(Root, "IdealAkeWms", "Views");
        Directory.GetFiles(views, "*.cshtml", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains("data-hw-scan-ohne-feld"))
            .Select(f => Path.GetRelativePath(views, f).Replace('\\', '/'))
            .Should().Equal(new[] { "StockMovements/Inbound.cshtml" }, "andere Seiten bleiben unveraendert");
        var inbound = Read("Views", "StockMovements", "Inbound.cshtml");
        var feld = inbound[inbound.IndexOf("<input type=\"text\" id=\"hwScanOhneFeld\"", StringComparison.Ordinal)..];
        feld = feld[..feld.IndexOf("/>", StringComparison.Ordinal)];
        feld.Should().Contain("inputmode=\"none\"", "keine Bildschirmtastatur").And.Contain("aria-hidden=\"true\"").And.Contain("tabindex=\"-1\"")
            .And.Contain("data-artikel-ziel=\"ArticleId\"").And.Contain("data-lagerplatz-ziel=\"StorageLocationId\"");
        inbound.IndexOf("id=\"hwScanOhneFeld\"", StringComparison.Ordinal).Should().BeLessThan(inbound.IndexOf("<form", StringComparison.Ordinal),
            "ausserhalb des Formulars: ein Enter im Feld kann nie absenden");

        var js = Read("wwwroot", "js", "hardware-scanner.js");
        js.Should().Contain("localStorage.getItem('wms.hardwareScanner.ohneFeld') === 'an' && !geraetAus()", "nur mit Geraete-Schalter, sonst wie v1.67.1")
            .And.Contain("ohneFeld.focus({ preventScroll: true });", "Fokus ohne Scrollen")
            .And.Contain("if (ohne) return;", "nie Regel (b) ins unsichtbare Feld")
            .And.Contain("'über Scan-Feld (ohne Fokus) → '", "Diagnose zeigt den Weg und die Zuordnung");
        var bs = Read("wwwroot", "js", "barcode-scanner.js");
        bs.IndexOf("if (d.ohneFeld) { hwScanOhneFeld(d, vorhanden); return; }", StringComparison.Ordinal)
            .Should().BePositive().And.BeGreaterThan(bs.IndexOf("var kamera = hwScanKameraZiel(vorhanden);", StringComparison.Ordinal),
                "offenes Kamera-Fenster hat Vorrang");
        bs.Should().Contain("HwScanCore.zuordnung(d.code,").And.Contain("'Code ' + d.code + ' nicht zuordenbar'");
        Read("wwwroot", "css", "site.css").Should().Contain(".hw-scan-ohne-feld { position: fixed;");
        var profil = Read("Views", "Account", "Profile.cshtml");
        profil.Should().Contain("id=\"hwScanOhneFeldAn\"").And.Contain("'wms.hardwareScanner.ohneFeld'")
            .And.Contain("if (ohne.checked) localStorage.setItem(OHNE, 'an'); else localStorage.removeItem(OHNE);", "Standard aus");
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
