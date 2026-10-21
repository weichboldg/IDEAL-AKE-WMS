// Hardware-Barcodescanner als Tastatur (v1.66.0, Spec 2026-10-15-scan-handler-hardware-scanner): eine Tastenfolge, die mit dem
// Praefix (HardwareScannerPraefix, Default ~09) beginnt und im Scanner-Tempo (<= HardwareScannerMaxZeichenabstandMs) kommt, wird
// abgefangen und als EIN Ereignis 'wms:hardware-scan' weitergegeben. Ausgewertet wird in barcode-scanner.js ueber dieselbe
// processScannedValue-Kette wie der Kamera-Scan; hier nur Regel (b) freies Feld, (d) einziges [data-hw-scan-ziel] und der Hinweis.
// Nur geladen, wenn der Standort-Schalter HardwareScannerAktiv an ist (_Layout).
(function (root) {
    'use strict';

    // Reiner Kern ohne DOM (Node-Selbstpruefung IdealAkeWms.Tests/Js/hardware-scanner.check.js).
    // key(k, t): k = KeyboardEvent.key (ein Zeichen, 'Enter', 'Tab', sonst Name), t = event.timeStamp in ms.
    //   -> { action: 'pass' | 'hold' | 'swallow', replay?: string, scan?: { code, endedWithEnter } }
    //   replay = gepufferte Zeichen eines Fehlstarts, die VOR der aktuellen Taste ins Feld gehoeren.
    // tick(t): Zeitgrenze ohne neue Taste -> { replay?, scan? }.
    var MODIFIER = { Shift: 1, Control: 1, Alt: 1, AltGraph: 1, Meta: 1, CapsLock: 1, Dead: 1 };

    function create(opts) {
        var prefix = opts.prefix, gap = opts.maxGapMs;
        var held = '';     // Praefix-Kandidat
        var code = null;   // null = kein Scan; '' oder Zeichen = Scan laeuft (Praefix bestaetigt)
        var last = 0;

        function pending() { return held !== '' || code !== null; }
        function expire() {
            var out = {};
            if (code) out.scan = { code: code, endedWithEnter: false };
            else if (code === '') out.replay = prefix;  // Praefix ohne Code: war doch Text
            else if (held) out.replay = held;
            held = ''; code = null;
            return out;
        }

        return {
            pending: pending,
            tick: function (t) { return pending() && t - last > gap ? expire() : {}; },
            key: function (k, t) {
                var out = pending() && t - last > gap ? expire() : {};
                var replay = out.replay || '';
                if (MODIFIER[k]) { out.action = 'pass'; return out; }  // Shift fuer Grossbuchstaben bricht nichts ab
                last = t;

                if (code !== null) {
                    if (k === 'Enter' || k === 'Tab') {
                        if (code) out.scan = { code: code, endedWithEnter: k === 'Enter' };
                        else replay += prefix;
                        held = ''; code = null;
                        out.action = out.scan ? 'swallow' : 'pass';
                    } else if (k.length === 1) {
                        code += k; out.action = 'swallow';
                    } else {
                        out.action = 'pass';
                    }
                    if (replay) out.replay = replay;
                    return out;
                }

                if (k.length === 1 && k === prefix.charAt(held.length)) {
                    held += k;
                    if (held === prefix) { held = ''; code = ''; }
                    out.action = 'hold';
                } else {
                    replay += held; held = '';
                    if (k.length === 1 && k === prefix.charAt(0)) {
                        held = k; out.action = 'hold';
                        if (held === prefix) { held = ''; code = ''; }
                    } else {
                        out.action = 'pass';
                    }
                }
                if (replay) out.replay = replay;
                return out;
            }
        };
    }

    var api = { create: create };
    if (typeof module !== 'undefined' && module.exports) { module.exports = api; return; }
    root.HwScanCore = api;

    // --- DOM-Anbindung ---
    var body = document.body;
    var maxGapMs = parseInt(body.getAttribute('data-hw-scan-abstand'), 10) || 50;
    var core = create({ prefix: body.getAttribute('data-hw-scan-praefix') || '~09', maxGapMs: maxGapMs });
    var timer = null;

    function geraetAus() {
        try { return localStorage.getItem('wms.hardwareScanner.geraet') === 'aus'; } catch (e) { return false; }
    }

    // Text-Eingabefeld, in das Zeichen gehoeren (nie Passwort). Unsichtbar zaehlt nicht: je nach Browser bleibt der Fokus auf einem
    // ausgeblendeten Feld (BDE: scanOperator nach der Anmeldung) - der Scan muss dann nach Regel (d) ins sichtbare scanFaAg.
    function freiesFeld(el) {
        if (!el || el.disabled || el.readOnly || !el.getClientRects().length) return false;
        if (el.tagName === 'TEXTAREA') return true;
        return el.tagName === 'INPUT' && /^(text|search|tel|url|email|number)$/i.test(el.type || 'text');
    }

    function einfuegen(el, text) {
        try { el.setRangeText(text, el.selectionStart, el.selectionEnd, 'end'); }
        catch (e) { el.value += text; }  // email/number kennen keine Auswahl
        el.dispatchEvent(new Event('input', { bubbles: true }));
    }

    function feldFuellen(el, text, ersetzen, enter) {
        if (ersetzen) { el.value = text; el.dispatchEvent(new Event('input', { bubbles: true })); }
        else einfuegen(el, text);
        el.dispatchEvent(new Event('change', { bubbles: true }));
        // Seiten-Enter-Handler (BDE, ENTER-Spaltenfilter) laufen lassen; ein synthetisches Ereignis loest keinen Submit aus.
        if (enter) el.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', code: 'Enter', bubbles: true, cancelable: true }));
    }

    var hinweisTimer = null;
    function hinweis(text) {
        var box = document.getElementById('hw-scan-hinweis');
        if (!box) {
            box = document.createElement('div');
            box.id = 'hw-scan-hinweis';
            box.className = 'alert alert-warning shadow-sm position-fixed top-0 start-50 translate-middle-x mt-2 py-2 px-3';
            box.style.zIndex = '1090';
            box.style.maxWidth = 'calc(100% - 32px)';
            box.setAttribute('role', 'status');
            box.setAttribute('aria-live', 'polite');
            body.appendChild(box);
        }
        box.textContent = '⚠ ' + text;  // Zeichen + Text, Wert nie als HTML
        box.hidden = false;
        clearTimeout(hinweisTimer);
        hinweisTimer = setTimeout(function () { box.hidden = true; }, 5000);
    }

    function sichtbar(el) { return el.offsetParent !== null; }

    function zustellen(scan) {
        var aktiv = document.activeElement;
        var detail = { code: scan.code, endedWithEnter: scan.endedWithEnter, freiesFeld: freiesFeld(aktiv), handled: false, ambiguous: false };
        document.dispatchEvent(new CustomEvent('wms:hardware-scan', { detail: detail }));
        if (detail.handled) return;

        if (detail.freiesFeld) { feldFuellen(aktiv, scan.code, false, scan.endedWithEnter); return; }  // (b)

        var ziele = Array.prototype.filter.call(document.querySelectorAll('[data-hw-scan-ziel]'), sichtbar);
        if (ziele.length === 1 && !detail.ambiguous) {  // (d) z. B. BDE-Terminal scanFaAg nach der Anmeldung
            ziele[0].focus();
            feldFuellen(ziele[0], scan.code, true, scan.endedWithEnter);
            return;
        }
        hinweis(detail.ambiguous || ziele.length > 1
            ? 'Code ' + scan.code + ' gescannt – bitte zuerst das Feld antippen, in das er soll'
            : 'Code ' + scan.code + ' gescannt – auf dieser Seite nicht verwendbar');
    }

    function auswerten(r) {
        if (r.replay && freiesFeld(document.activeElement)) einfuegen(document.activeElement, r.replay);
        if (r.scan) zustellen(r.scan);
    }

    window.addEventListener('keydown', function (e) {
        if (!e.isTrusted || e.isComposing || e.keyCode === 229 || geraetAus()) return;  // IME nicht abfangbar (Spec Risiken)
        var aktiv = document.activeElement;
        if (!core.pending() && aktiv && aktiv.type === 'password') return;
        var k = e.key;
        // Tastenkuerzel durchlassen; AltGr (~ auf DE-Layout) meldet unter Windows ctrl+alt und ist kein Kuerzel.
        if ((e.ctrlKey || e.metaKey || e.altKey) && !(e.getModifierState && e.getModifierState('AltGraph')) && !MODIFIER[k]) k = 'Kuerzel';
        var r = core.key(k, e.timeStamp);
        if (r.action !== 'pass') { e.preventDefault(); e.stopImmediatePropagation(); }
        auswerten(r);
        clearTimeout(timer);
        if (core.pending()) timer = setTimeout(function () { auswerten(core.tick(performance.now())); }, maxGapMs + 5);
    }, true);
})(this);
