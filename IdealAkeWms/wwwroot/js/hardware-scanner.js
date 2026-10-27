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

    // v1.67.1 Fokus-Eingabe (Freigabe-Nachtrag 4, Netum AIDC): das Geraet schreibt '~09<Code>' als Ganzes ins Feld mit Fokus, Tasten
    // kommen hoechstens als keyCode 229. Erkannt wird daher am eingefuegten Text einer Eingabe-Folge (Abstand <= Zeichenabstand).
    // eingefuegt(alt, neu): zusammenhaengend eingefuegter Text ueber gemeinsamen Anfang/Ende -> { pos, text, entfernt }.
    function eingefuegt(alt, neu) {
        var p = 0, max = Math.min(alt.length, neu.length), s = 0;
        while (p < max && alt.charAt(p) === neu.charAt(p)) p++;
        while (s < max - p && alt.charAt(alt.length - 1 - s) === neu.charAt(neu.length - 1 - s)) s++;
        return { pos: p, text: neu.slice(p, neu.length - s), entfernt: alt.length - p - s };
    }

    // createText: eingabe(vorher, wert, t) je input-Ereignis; vorher = Feldwert davor (nur am Folgen-Anfang gebraucht).
    //   -> { action: 'pass' | 'zuruecksetzen', wert?, pos?, scan?, zurueck? }
    //   zuruecksetzen = Feld auf wert (Stand vor der Folge) setzen, Cursor auf pos: der Praefix steht danach nie im Feld.
    //   zurueck = Praefix ohne Code war doch Text und gehoert an pos wieder ins Feld (wie das Replay der Tasten-Erkennung).
    // enter() beendet einen laufenden Scan (Enter-Suffix), tick(t) per Zeitgrenze.
    function createText(opts) {
        var prefix = opts.prefix, gap = opts.maxGapMs;
        var basis = null, code = null, pos = 0, last = 0;

        function ende(enter) {
            var out = {};
            if (code) out.scan = { code: code, endedWithEnter: enter };
            else if (code === '') { out.zurueck = prefix; out.pos = pos; }
            basis = null; code = null;
            return out;
        }

        return {
            aktiv: function () { return code !== null; },
            enter: function () { return code !== null ? ende(true) : {}; },
            tick: function (t) { return basis !== null && t - last > gap ? ende(false) : {}; },
            eingabe: function (vorher, wert, t) {
                if (basis !== null && t - last > gap) {
                    // ponytail: Zeitgrenze verpasst (Timer gedrosselt) - alte Folge liefern, diese Eingabe geht ungeprueft durch.
                    var alt = ende(false);
                    if (alt.scan || alt.zurueck) { alt.action = 'pass'; return alt; }
                }
                if (basis === null) basis = vorher;
                last = t;
                var d = eingefuegt(basis, wert), text = null;
                if (d.entfernt === 0 && code !== null) text = d.text;
                else if (d.entfernt === 0 && d.text.indexOf(prefix) === 0) { code = ''; pos = d.pos; text = d.text.slice(prefix.length); }

                if (text === null) {
                    if (code !== null) { var e = ende(false); e.action = 'pass'; return e; }  // Feld anders geaendert: Scan endet
                    if (!(d.entfernt === 0 && d.text && prefix.indexOf(d.text) === 0)) basis = null;  // kein Praefix-Anfang
                    return { action: 'pass' };
                }
                var out = { action: 'zuruecksetzen', wert: basis, pos: pos }, i = text.search(/[\r\n]/);
                code += i < 0 ? text : text.slice(0, i);
                if (i >= 0) { var f = ende(true); out.scan = f.scan; out.zurueck = f.zurueck; }
                return out;
            }
        };
    }

    // Zeichenabstand aus dem body-Attribut: nur 10..500 ms (wie StandortSettingsCatalog), sonst Standard 50 - ein negativer oder
    // unsinniger Wert darf die Erkennung nie still abschalten.
    function abstand(raw) {
        var n = parseInt(raw, 10);
        return n >= 10 && n <= 500 ? n : 50;
    }

    // v1.67.2 Scannen ohne Feld (Freigabe-Nachtrag 5, Pilot Einbuchung): Zuordnung eines Scans ueber das unsichtbare Scan-Feld nach Inhalt.
    // istLagerplatz(code) = es gibt eine Lagerplatz-Option mit genau diesem Code.
    //   -> { feld: 'artikel' | 'lagerplatz' | null, art } ; null = nicht zuordenbar.
    function zuordnung(code, istLagerplatz) {
        var c = String(code == null ? '' : code).trim();
        if (!c) return { feld: null, art: 'leer' };
        if (c.indexOf(';') >= 0) return { feld: 'artikel', art: 'Artikel-QR' };
        if (istLagerplatz(c)) return { feld: 'lagerplatz', art: 'Lagerplatz' };
        return { feld: 'artikel', art: 'Artikelnummer' };
    }

    var api = { create: create, createText: createText, eingefuegt: eingefuegt, abstand: abstand, zuordnung: zuordnung };
    if (typeof module !== 'undefined' && module.exports) { module.exports = api; return; }
    root.HwScanCore = api;

    // --- DOM-Anbindung ---
    var body = document.body;
    var maxGapMs = abstand(body.getAttribute('data-hw-scan-abstand'));
    var core = create({ prefix: body.getAttribute('data-hw-scan-praefix') || '~09', maxGapMs: maxGapMs });
    var timer = null;

    function geraetAus() {
        try { return localStorage.getItem('wms.hardwareScanner.geraet') === 'aus'; } catch (e) { return false; }
    }

    // Scanner-Diagnose (v1.67.1, Mein Profil, nur dieses Geraet): die letzten 30 Ereignisse mit der Entscheidung des Skripts als Liste am
    // Seitenende. Nichts geht an den Server; Passwortfelder werden ohne Taste/Text protokolliert.
    var diagAn = false, diagZeilen = [], diagZeit = 0, diagPre = null;
    try { diagAn = localStorage.getItem('wms.hardwareScanner.diagnose') === 'an'; } catch (e) { }

    function diag(text) {
        if (!diagAn) return;
        var t = performance.now();
        diagZeilen.push('+' + (diagZeit ? Math.round(t - diagZeit) : 0) + ' ms  ' + text);
        diagZeit = t;
        if (diagZeilen.length > 30) diagZeilen.shift();
        if (diagPre) diagPre.textContent = diagZeilen.join('\n');
    }

    function diagBauen() {
        var karte = document.createElement('section');
        karte.className = 'card mx-3 my-3';
        karte.setAttribute('aria-labelledby', 'hwDiagTitel');
        var kopf = document.createElement('div');
        kopf.className = 'card-header d-flex flex-wrap gap-2 justify-content-between align-items-center';
        var titel = document.createElement('h2');
        titel.className = 'h6 mb-0';
        titel.id = 'hwDiagTitel';
        titel.textContent = 'Scanner-Diagnose – nur dieses Gerät, nichts wird gesendet';
        var knopf = document.createElement('button');
        knopf.type = 'button';
        knopf.className = 'btn btn-outline-secondary';
        knopf.textContent = 'Kopieren';
        var status = document.createElement('span');
        status.className = 'small';
        status.setAttribute('role', 'status');
        var rechts = document.createElement('div');
        rechts.className = 'd-flex flex-wrap gap-2 align-items-center';
        rechts.append(status, knopf);
        kopf.append(titel, rechts);
        diagPre = document.createElement('pre');
        diagPre.className = 'card-body mb-0';
        diagPre.style.cssText = 'white-space:pre-wrap;word-break:break-all;font-size:.875rem;max-height:50vh;overflow:auto';
        diagPre.tabIndex = 0;  // scrollbarer Bereich per Tastatur erreichbar
        diagPre.setAttribute('aria-labelledby', 'hwDiagTitel');
        diagPre.textContent = 'Noch keine Ereignisse. Ein Feld antippen und scannen.';
        karte.append(kopf, diagPre);

        knopf.addEventListener('click', function () {
            var text = 'Präfix ' + (body.getAttribute('data-hw-scan-praefix') || '~09') + ', Zeichenabstand ' + maxGapMs + ' ms, '
                + navigator.userAgent + '\n' + diagZeilen.join('\n');
            var markieren = function () {
                var r = document.createRange(); r.selectNodeContents(diagPre);
                var s = window.getSelection(); s.removeAllRanges(); s.addRange(r);
                status.textContent = 'Kopieren nicht möglich – Text ist markiert, bitte lange drücken und kopieren.';
            };
            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(text).then(function () { status.textContent = 'In die Zwischenablage kopiert.'; }, markieren);
            } else markieren();
        });

        var fuss = document.querySelector('footer.footer');  // vor dem absolut positionierten Fuss, sonst ueberdeckt er die Liste
        if (fuss) fuss.parentNode.insertBefore(karte, fuss); else body.appendChild(karte);
    }
    if (diagAn) diagBauen();

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

    function feldName(el) { return el ? '#' + (el.id || el.name || el.tagName.toLowerCase()) : '–'; }

    function zustellen(scan) {
        var aktiv = document.activeElement;
        var ohne = !!(aktiv && aktiv === ohneFeld);  // v1.67.2: Fokus im unsichtbaren Scan-Feld = kein Feld angetippt
        var detail = { code: scan.code, endedWithEnter: scan.endedWithEnter, freiesFeld: !ohne && freiesFeld(aktiv), ohneFeld: ohne,
            handled: false, ambiguous: false, ziel: null,
            hinweis: function (text) { diag('  ' + text); hinweis(text); } };  // auch spaeter (AJAX-Ergebnis) aufrufbar
        document.dispatchEvent(new CustomEvent('wms:hardware-scan', { detail: detail }));
        if (detail.handled) { diag('  Scan "' + scan.code + '" ' + (ohne && !/Kamera/.test(detail.ziel) ? 'über Scan-Feld (ohne Fokus) → ' : 'an Feld #') + detail.ziel); return; }
        if (ohne) return;  // nie Regel (b) ins unsichtbare Feld; Zuordnung und Hinweis macht barcode-scanner.js

        if (detail.freiesFeld) {  // (b)
            diag('  Scan "' + scan.code + '" an Feld ' + feldName(aktiv));
            feldFuellen(aktiv, scan.code, false, scan.endedWithEnter);
            return;
        }

        var ziele = Array.prototype.filter.call(document.querySelectorAll('[data-hw-scan-ziel]'), sichtbar);
        if (ziele.length === 1 && !detail.ambiguous) {  // (d) z. B. BDE-Terminal scanFaAg nach der Anmeldung
            diag('  Scan "' + scan.code + '" an Feld ' + feldName(ziele[0]) + ' (einziges Ziel)');
            ziele[0].focus();
            feldFuellen(ziele[0], scan.code, true, scan.endedWithEnter);
            return;
        }
        diag('  Scan "' + scan.code + '": kein Ziel' + (detail.ambiguous || ziele.length > 1 ? ' (mehrere Felder, keins angetippt)' : ''));
        hinweis(detail.ambiguous || ziele.length > 1
            ? 'Code ' + scan.code + ' gescannt – bitte zuerst das Feld antippen, in das er soll'
            : 'Code ' + scan.code + ' gescannt – auf dieser Seite nicht verwendbar');
    }

    function auswerten(r) {
        if (r.replay && freiesFeld(document.activeElement)) einfuegen(document.activeElement, r.replay);
        if (r.scan) zustellen(r.scan);
    }

    // --- Scannen ohne Feld (v1.67.2, Freigabe-Nachtrag 5, nur Einbuchung): ein Fokus-Eingabe-Geraet (Netum) gibt ohne Feld mit Fokus
    // keinen Text aus. Das unsichtbare Feld [data-hw-scan-ohne-feld] bekommt darum den Fokus, sobald kein anderes Feld/Knopf ihn hat;
    // sein Text laeuft durch die Text-Erkennung unten. Nur mit Geraete-Schalter 'Scannen ohne Feld' (Mein Profil), sonst wie v1.67.1.
    var ohneFeld = null;
    try { if (localStorage.getItem('wms.hardwareScanner.ohneFeld') === 'an' && !geraetAus()) ohneFeld = document.querySelector('[data-hw-scan-ohne-feld]'); } catch (e) { }
    if (ohneFeld) {
        var parken = function () {
            setTimeout(function () {
                var a = document.activeElement;
                // frei = nichts fokussiert, oder Select2 hat den Fokus nach dem Schliessen an seine Auswahl zurueckgegeben
                var frei = !a || a === body || a === document.documentElement
                    || (a.closest && a.closest('.select2-container') && !document.querySelector('.select2-container--open'));
                if (frei) ohneFeld.focus({ preventScroll: true });
            }, 0);
        };
        document.addEventListener('focusout', parken, true);
        new MutationObserver(parken).observe(body, { childList: true });  // Select2-Dropdown und Kamera-Fenster haengen direkt am body
        parken();
    }

    // --- Fokus-Eingabe (v1.67.1): Erkennung am eingefuegten Text, Kern createText ---
    var textCore = createText({ prefix: body.getAttribute('data-hw-scan-praefix') || '~09', maxGapMs: maxGapMs });
    var textTimer = null, vor = null;  // vor = Feldwert vor der Eingabe (beforeinput/focusin), Basis einer neuen Folge

    function textAuswerten(r, el) {
        clearTimeout(textTimer);
        if (r.zurueck) { el.value = el.value.slice(0, r.pos) + r.zurueck + el.value.slice(r.pos); el.dispatchEvent(new Event('input', { bubbles: true })); }
        vor = { el: el, wert: el.value };
        if (r.scan) zustellen(r.scan);
        if (textCore.aktiv()) textTimer = setTimeout(function () { diag('Zeitgrenze → Text-Scan beendet'); textAuswerten(textCore.tick(performance.now()), el); }, maxGapMs + 5);
    }

    document.addEventListener('focusin', function (e) { if (freiesFeld(e.target)) vor = { el: e.target, wert: e.target.value }; }, true);

    window.addEventListener('beforeinput', function (e) {
        if (!e.isTrusted || !freiesFeld(e.target) || geraetAus()) return;
        if (textCore.aktiv() && /^insert(LineBreak|Paragraph)$/.test(e.inputType)) {
            e.preventDefault();
            diag('beforeinput ' + e.inputType + ' → Enter beendet Text-Scan');
            textAuswerten(textCore.enter(), e.target);
            return;
        }
        vor = { el: e.target, wert: e.target.value };
    }, true);

    window.addEventListener('input', function (e) {
        var el = e.target;
        if (!e.isTrusted || !freiesFeld(el)) return;  // eigene/synthetische Ereignisse (feldFuellen) zaehlen nie; Passwort ist kein freies Feld
        var info = 'input ' + (e.inputType || '?') + ' "' + String(e.data == null ? '' : e.data).slice(0, 60) + '" in ' + feldName(el);
        if (geraetAus()) { diag(info + ' → ignoriert: Gerät aus'); return; }
        if (core.pending()) { diag(info + ' → ignoriert: Tasten-Scan läuft'); return; }  // keine Doppelauswertung
        var lief = textCore.aktiv(), r = textCore.eingabe(vor && vor.el === el ? vor.wert : '', el.value, e.timeStamp);
        if (r.action === 'zuruecksetzen') {
            e.stopImmediatePropagation();  // Seiten-Handler (Select2-Suche, Live-Filter) sehen den Praefix nie
            el.value = r.wert;
            try { el.setSelectionRange(r.pos, r.pos); } catch (x) { }  // email/number kennen keine Auswahl
        }
        diag(info + ' → ' + (r.action === 'zuruecksetzen' ? (lief ? 'Teil des Codes (Text)' : 'Präfix erkannt (Text)') + (r.scan ? ', Ende per Zeilenumbruch' : '') : r.scan ? 'Feld geändert, Text-Scan beendet' : 'durchgelassen'));
        textAuswerten(r, el);
    }, true);

    window.addEventListener('keydown', function (e) {
        if (!e.isTrusted) return;
        var aktiv = document.activeElement;
        var info = 'keydown key="' + e.key + '" keyCode=' + e.keyCode + ' isComposing=' + (e.isComposing ? 'ja' : 'nein');
        if (aktiv && aktiv.type === 'password' && !core.pending()) { diag('keydown (Passwortfeld) → ignoriert'); return; }
        if (geraetAus()) { diag(info + ' → ignoriert: Gerät aus'); return; }
        if (textCore.aktiv() && (e.key === 'Enter' || e.keyCode === 13)) {  // Enter-Suffix der Fokus-Eingabe: nie zum Formular
            e.preventDefault(); e.stopImmediatePropagation();
            diag(info + ' → Enter beendet Text-Scan');
            textAuswerten(textCore.enter(), aktiv);
            return;
        }
        if (e.isComposing || e.keyCode === 229) { diag(info + ' → ignoriert: keyCode 229 / Komposition'); return; }  // IME nicht abfangbar (Spec Risiken)
        var k = e.key;
        // Tastenkuerzel durchlassen; AltGr (~ auf DE-Layout) meldet unter Windows ctrl+alt und ist kein Kuerzel.
        if ((e.ctrlKey || e.metaKey || e.altKey) && !(e.getModifierState && e.getModifierState('AltGraph')) && !MODIFIER[k]) k = 'Kuerzel';
        var r = core.key(k, e.timeStamp);
        if (r.action !== 'pass') { e.preventDefault(); e.stopImmediatePropagation(); }
        diag(info + ' → ' + (r.action === 'hold' ? 'gehalten (Präfix?)' : r.action === 'swallow' ? 'verschluckt (Scan)' : 'durchgelassen'));
        auswerten(r);
        clearTimeout(timer);
        if (core.pending()) timer = setTimeout(function () { auswerten(core.tick(performance.now())); }, maxGapMs + 5);
    }, true);
})(this);
