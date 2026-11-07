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
        layout.Should().Contain("<body data-hw-scan-praefix=\"@hwScanPraefix\" data-hw-scan-abstand=\"@hwScanAbstand\"");  // v1.68.0: weitere body-Attribute dahinter
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
        // v1.68.1 (Freigabe-Nachtrag 6): sichtbares, antippbares Feld - nicht mehr fuer Screenreader verborgen, nicht aus der Tab-Folge
        // v1.71.1 (Freigabe-Nachtrag 7): KEIN inputmode im Markup - sonst startet ein Tipp am Netum keine Eingabemethode
        feld.Should().NotContain("inputmode", "erst nach einem Tipp setzt das Skript inputmode=none").And.Contain("autocomplete=\"off\"").And.Contain(" hidden")
            .And.Contain("autocorrect=\"off\"").And.Contain("autocapitalize=\"off\"").And.Contain("spellcheck=\"false\"")
            .And.Contain("form-control form-control-lg", "Daumengroesse").And.Contain("placeholder=\"Zum Scannen hier tippen\"")
            .And.NotContain("aria-hidden").And.NotContain("tabindex=\"-1\"")
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
        var css = Read("wwwroot", "css", "site.css").Replace("\r\n", "\n");
        var regel = css[css.IndexOf(".hw-scan-ohne-feld {", StringComparison.Ordinal)..];
        regel = regel[..regel.IndexOf("}", StringComparison.Ordinal)];
        regel.Should().Contain("dashed").And.NotContain("opacity").And.NotContain("pointer-events").And.NotContain("1px;");
        // v1.71.1: gruen nur mit .hw-scan-bereit (nach Tipp); reiner :focus (Skript-Fokus) bleibt grau
        var fokus = css[css.IndexOf(".hw-scan-ohne-feld:focus {", StringComparison.Ordinal)..];
        fokus[..fokus.IndexOf("}", StringComparison.Ordinal)].Should().NotContain("#198754").And.NotContain("#d1e7dd")
            .And.Contain("outline: 2px solid #495057", "QA v1.71.1: Fokusring im Zustand 2 mit mindestens 3:1, nicht halbtransparent");
        // QA v1.71.1: inputmode=none genau einmal gesetzt, und zwar im Timer nach der Geste (nachTipp), nie beim Laden oder im parken
        var setzen = js.IndexOf("ohneFeld.setAttribute('inputmode', 'none');", StringComparison.Ordinal);
        setzen.Should().BeGreaterThan(js.IndexOf("var nachTipp = function", StringComparison.Ordinal)).And.BeLessThan(js.IndexOf("var tipp = function", StringComparison.Ordinal));
        js.IndexOf("ohneFeld.setAttribute('inputmode', 'none');", setzen + 1, StringComparison.Ordinal).Should().Be(-1, "kein zweiter Setz-Ort");
        css.Should().Contain(".hw-scan-ohne-feld.hw-scan-bereit:focus {", "Fokus-Anzeige gruen erst nach Tipp");
        js.Should().Contain("ohneFeld.hidden = false;", "nur bei Schalter an sichtbar")
            .And.Contain("diag('Scan-Feld Fokus an (' + (durchTipp ? 'durch Tipp' : 'per Skript') + '), inputmode=' + inputmode());")
            .And.Contain("diag('Scan-Feld Fokus aus')")
            .And.Contain("ohneFeld.addEventListener('pointerdown', tipp);", "Benutzergeste von Skript-Fokus unterscheiden")
            .And.Contain("if (durchTipp) nachTipp();", "inputmode=none und gruen nur nach Geste")
            .And.Contain("ohneFeld.removeAttribute('inputmode');", "bei blur weg, damit der naechste Tipp neu startet")
            .And.Contain("'✓ Scan bereit'").And.Contain("if (ohne) { ohneFeld.value = ''; vor = { el: ohneFeld, wert: '' }; }", "nach jedem Scan leer, Basis der Text-Erkennung mit")
            .And.Contain("'  [aktiv: '", "jede Diagnose-Zeile mit aktivem Element");
        var profil = Read("Views", "Account", "Profile.cshtml");
        profil.Should().Contain("id=\"hwScanOhneFeldAn\"").And.Contain("'wms.hardwareScanner.ohneFeld'")
            .And.Contain("if (ohne.checked) localStorage.setItem(OHNE, 'an'); else localStorage.removeItem(OHNE);", "Standard aus");
    }

    [Fact]
    public void QrErstesFeld_MatchcodeZweigNurImGlobalenWrapper_ModusArtikelnummerSynchron()
    {
        // Spec 2026-10-23-qr-code-matchcode-statt-artikelnummer AK 27: eine Abfrage-Stelle, innere Auswertung unveraendert.
        var js = Read("wwwroot", "js", "barcode-scanner.js").Replace("\r\n", "\n");
        js.Split("url: '/api/articles/by-matchcode").Length.Should().Be(2, "genau eine Abfrage-Stelle");
        var innen = js[js.IndexOf("function processScannedValue(", StringComparison.Ordinal)..js.IndexOf("function showScanFeedback(", StringComparison.Ordinal)];
        innen.Should().NotContain("by-matchcode").And.NotContain("data-qr-erstes-feld");
        var wrapper = js[js.IndexOf("processScannedValue = function (", StringComparison.Ordinal)..];
        wrapper.IndexOf("by-matchcode", StringComparison.Ordinal).Should().BePositive();
        wrapper.Should().Contain("""
    if (!matchcode) {
        _processScannedValueSync(value, targetSelectId, scanType, qrFaEnabled, faTargetId, nichtGefunden);
        return;
    }
""", "Modus Artikelnummer (kein Attribut) und Nicht-Artikel-Scans: synchron, ohne Abfrage, mit dem optionalen nichtGefunden");
        wrapper.Should().Contain("scanType === 'article' && document.body.getAttribute('data-qr-erstes-feld') === 'Matchcode'");
        wrapper.Should().Contain("'“ nicht gefunden'").And.Contain("'“ ist mehrdeutig ('").And.Contain("'“ konnte nicht geprüft werden'");

        var layout = Read("Views", "Shared", "_Layout.cshtml");
        layout.Should().Contain("data-qr-erstes-feld=\"@qrErstesFeld\"").And.Contain("? \"Matchcode\" : null;", "bei Artikelnummer kein Attribut");
        AppSettingDefinitions.SeedEintraege.Should().Contain(e => e.Key == "QrErstesFeld" && e.Value == "Artikelnummer", "Laufzeit-Seed, sonst zeigt /Settings die Zeile nicht");
        // v1.77.0: Gruppen kommen aus dem Katalog — die drei QR-Einstellungen stehen zusammen.
        AppSettingDefinitions.All.Where(d => d.Key is "QrMitFaNummer" or "QrErstesFeld" or "QrTrennzeichen").Select(d => d.Category).Distinct()
            .Should().ContainSingle();
    }

    [Fact]
    public void QrTrennzeichen_KeinFestesSemikolonInDerQrZerlegung_StandardOhneAttribut()
    {
        // Spec 2026-10-23-qr-code-matchcode-statt-artikelnummer, Freigabe-Nachtrag 2 Punkt 2/3 (v1.69.0).
        var bs = Read("wwwroot", "js", "barcode-scanner.js");
        bs.Should().Contain("return document.body.getAttribute('data-qr-trennzeichen') === ',' ? ',' : ';';", "Standard ;")
            .And.Contain("var parts = value.split(qrTrennzeichen());")
            .And.Contain("String(value).split(qrTrennzeichen())[0].trim()").And.Contain("String(value).indexOf(qrTrennzeichen())")
            .And.Contain("HwScanCore.zuordnung(d.code, function (c) { return !!option(c); }, qrTrennzeichen());");
        bs.Should().NotContain("split(';')").And.NotContain("indexOf(';')");
        var hw = Read("wwwroot", "js", "hardware-scanner.js");
        hw.Should().Contain("c.indexOf(trenn || ';') >= 0").And.NotContain("c.indexOf(';')");

        Read("Views", "Shared", "_Layout.cshtml").Should().Contain("data-qr-trennzeichen=\"@qrTrennzeichen\"")
            .And.Contain("?.Trim() == \",\" ? \",\" : null;", "bei ; kein Attribut = Seite wie v1.68.x");
        AppSettingDefinitions.SeedEintraege.Should().Contain(e => e.Key == "QrTrennzeichen" && e.Value == ";", "Laufzeit-Anlage, Standard ;");
        var settings = Read("Views", "Settings", "Index.cshtml");
        settings.Should().Contain("<partial name=\"_SettingField\"", "v1.77.0: jedes Feld ueber das gemeinsame Partial");
        Read("Views", "Shared", "_SettingField.cshtml").Should().Contain("case SettingType.Choice:").And.Contain("(ungültig – bitte wählen)");
        AppSettingDefinitions.All.Single(d => d.Key == AppSettingKeys.QrTrennzeichen).Werte!.Select(w => w.Wert).Should().Equal(";", ",");
        AppSettingDefinitions.All.Single(d => d.Key == AppSettingKeys.QrErstesFeld).Werte!.Select(w => w.Wert).Should().Equal("Artikelnummer", "Matchcode");
    }

    [Fact]
    public void Profil_GeraeteSchalter_NurLocalStorage()
    {
        var view = Read("Views", "Account", "Profile.cshtml");
        view.Should().Contain("'wms.hardwareScanner.geraet'").And.Contain("id=\"hwScanGeraet\" hidden")
            .And.Contain("document.body.hasAttribute('data-hw-scan-praefix')");
        Read("wwwroot", "js", "hardware-scanner.js").Should().Contain("localStorage.getItem('wms.hardwareScanner.geraet') === 'aus'");
    }

    [Fact]
    public void Bestandsuebersicht_QrFaUebernahme_FolgtQrMitFaNummer_NichtFest()
    {
        // Spec 2026-11-07-bestandsuebersicht-qr-fa-uebernahme-fest (Variante A)
        var view = Read("Views", "StockOverview", "Index.cshtml");
        view.Should().NotContain("data-qr-fa-enabled=\"true\"").And.Contain("ViewBag.QrMitFaNummer")
            .And.Contain("data-fa-target=\"filterProductionOrder\"", "FA-Feld wird beim Artikel-Scan auch bei ausgeschalteter Einstellung geleert");
        var ctrl = Read("Controllers", "StockMovementsController.cs");
        ctrl.Split("ViewBag.QrMitFaNummer").Length.Should().Be(7,
            "nur Ein-, Aus- und Umbuchung (je GET + POST) setzen den Wert; die toten Zuweisungen im Sammel-Wareneingang sind entfernt");
    }

    [Fact]
    public void EanZweig_VorDemBisherigenWeg_NurArtikelOhneTrennzeichen_Auswahlfenster()
    {
        // Spec 2026-11-07-ean-einkaufsartikel-scan: ein aeusserer Wrapper, den alle Scan-Wege ueber die globale processScannedValue nutzen.
        var bs = Read("wwwroot", "js", "barcode-scanner.js");
        var ean = bs.IndexOf("var _artikelScanBisherig = processScannedValue;", StringComparison.Ordinal);
        var matchcode = bs.IndexOf("v1.68.0 QR erstes Feld = Matchcode", StringComparison.Ordinal);
        ean.Should().BeGreaterThan(matchcode, "EAN-Zweig umschliesst den Matchcode-Wrapper (EAN zuerst, kein Treffer -> bisheriger Weg)");
        bs.Should().Contain("scanType === 'article'").And.Contain("ean.indexOf(qrTrennzeichen()) >= 0").And.Contain(@"/^(\d{8}|\d{12,14})$/")
            .And.Contain("/api/articles/by-ean?ean=").And.Contain("_eanAuswahl(ean, treffer, uebernehmen")
            .And.Contain("gehört zu mehr als 5 Artikeln – bitte den Artikel von Hand wählen").And.Contain("konnte nicht geprüft werden")
            .And.Contain("min-height:44px");
    }
}
