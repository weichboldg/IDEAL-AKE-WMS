// AKE BDE Light - Barcode/QR Scanner
// Verwendet html5-qrcode Library
// Unterstützt Kamera-Scan (HTTPS) und Bild-Upload als Fallback

var _activeScanner = null;
var _scannerClosing = false;
// Ziele fuer den Hardware-Scanner (v1.66.0): jeder initScanner/initTextInputScanner-Aufruf traegt sich ein.
var _hwScanTargets = [];

function initScanner(buttonId, targetSelectId, scanType) {
    const button = document.getElementById(buttonId);
    if (!button) return;

    var qrFaEnabled = button.getAttribute('data-qr-fa-enabled') === 'true';
    var faTargetId = button.getAttribute('data-fa-target') || null;

    button.addEventListener('click', function () {
        openScannerModal(targetSelectId, scanType, qrFaEnabled, faTargetId);
    });
    _hwScanTargets.push({ buttonId: buttonId, targetId: targetSelectId, scanType: scanType, qrFaEnabled: qrFaEnabled, faTargetId: faTargetId });
}

function isCameraSupported() {
    return !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia);
}

function isSecureContext() {
    return window.isSecureContext || location.protocol === 'https:' || location.hostname === 'localhost';
}

/**
 * Pre-Warm der Camera-Permission im synchronen User-Gesture-Stack.
 * iOS Safari verweigert sonst die Permission, wenn getUserMedia erst nach Modal-Show
 * (asynchron) aufgerufen wird. Bei Erfolg: Stream sofort wieder stoppen — wir wollen
 * nur die Permission etablieren, der eigentliche Scanner startet spaeter via html5-qrcode.
 *
 * Returns true bei Erfolg, false bei Fehler (Permission denied, kein Camera-Support, etc.).
 */
async function requestCameraPermission() {
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        return false;
    }
    try {
        const stream = await navigator.mediaDevices.getUserMedia({
            video: { facingMode: 'environment' }
        });
        // Stream sofort wieder stoppen
        stream.getTracks().forEach(track => track.stop());
        return true;
    } catch (err) {
        console.warn('Camera permission pre-warm failed:', err);
        return false;
    }
}

async function openScannerModal(targetSelectId, scanType, qrFaEnabled, faTargetId) {
    // iOS-Fix: Permission im synchronen User-Gesture-Stack anfragen, BEVOR das Modal
    // geoeffnet wird. html5-qrcode wuerde getUserMedia sonst erst nach Modal-Show rufen,
    // und iOS Safari verweigert dann die Permission.
    let cameraAvailable = false;
    const secureCtx = isSecureContext();
    if (isCameraSupported() && secureCtx) {
        cameraAvailable = await requestCameraPermission();
    }

    _scannerClosing = false;

    // Altes Modal entfernen
    let modal = document.getElementById('scannerModal');
    if (modal) modal.remove();
    const title = scanType === 'article' ? 'Artikel QR-Code scannen' : 'Lagerplatz Barcode scannen';

    modal = document.createElement('div');
    modal.id = 'scannerModal';
    modal.dataset.zielId = targetSelectId;  // Hardware-Scanner (v1.66.1): offenes Fenster als Ziel, siehe hwScanKameraZiel
    modal.className = 'scanner-modal';
    modal.innerHTML = `
        <div class="scanner-modal-content">
            <div class="scanner-modal-header">
                <h5>${title}</h5>
                <button type="button" class="btn-close btn-close-white" id="scannerCloseBtn"></button>
            </div>
            ${cameraAvailable ? `
                <div id="scannerTabs" class="scanner-tabs">
                    <button type="button" class="scanner-tab active" data-tab="camera">Kamera</button>
                    <button type="button" class="scanner-tab" data-tab="file">Bild hochladen</button>
                </div>
                <div id="scannerCameraPane">
                    <div id="scannerReader" style="width: 100%;"></div>
                </div>
            ` : `
                <div class="scanner-info-box">
                    ${!secureCtx
                        ? 'Kamera-Scan erfordert HTTPS. Bitte ein Bild des Codes hochladen.'
                        : 'Kamera nicht verfügbar. Bitte ein Bild des Codes hochladen.'}
                </div>
            `}
            <div id="scannerFilePane" style="${cameraAvailable ? 'display:none;' : ''}">
                <div class="scanner-file-upload">
                    <label for="scannerFileInput" class="scanner-file-label">
                        <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" fill="currentColor" viewBox="0 0 16 16">
                            <path d="M.5 9.9a.5.5 0 0 1 .5.5v2.5a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1v-2.5a.5.5 0 0 1 1 0v2.5a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2v-2.5a.5.5 0 0 1 .5-.5"/>
                            <path d="M7.646 1.146a.5.5 0 0 1 .708 0l3 3a.5.5 0 0 1-.708.708L8.5 2.707V11.5a.5.5 0 0 1-1 0V2.707L5.354 4.854a.5.5 0 1 1-.708-.708z"/>
                        </svg>
                        <span>Bild auswählen oder Foto aufnehmen</span>
                    </label>
                    <input type="file" id="scannerFileInput" accept="image/*" capture="environment" style="display:none;" />
                </div>
            </div>
            <div id="scannerResult" class="scanner-result" style="display:none;">
                <span id="scannerResultText"></span>
            </div>
            ${document.body.hasAttribute('data-hw-scan-praefix') ? `
                <div class="scanner-hw-feld">
                    <label for="scannerHwInput" class="form-label mb-1">Handscanner</label>
                    <input type="text" id="scannerHwInput" class="form-control" inputmode="none" autocomplete="off" aria-describedby="scannerHwText" />
                    <div class="form-text" id="scannerHwText">Code mit dem Handscanner scannen – das Feld ist schon ausgewählt, die Kamera läuft weiter.</div>
                </div>
            ` : ''}
            <div class="scanner-modal-footer">
                <button type="button" class="btn btn-outline-light" id="scannerCancelBtn">Abbrechen</button>
            </div>
        </div>
    `;
    document.body.appendChild(modal);
    // v1.67.1 Fokus-Eingabe (Netum): ohne Feld mit Fokus gibt das Geraet nichts aus; inputmode="none" haelt die Bildschirmtastatur zu.
    var hwFeld = document.getElementById('scannerHwInput');
    if (hwFeld) hwFeld.focus({ preventScroll: true });

    // Event-Handler mit addEventListener (robuster als onclick)
    document.getElementById('scannerCloseBtn').addEventListener('click', closeScannerModal);
    document.getElementById('scannerCancelBtn').addEventListener('click', closeScannerModal);

    // Tab-Handler
    document.querySelectorAll('.scanner-tab').forEach(function (tab) {
        tab.addEventListener('click', function () {
            switchScannerTab(this.getAttribute('data-tab'));
        });
    });

    // File-Input Handler
    var fileInput = document.getElementById('scannerFileInput');
    if (fileInput) {
        fileInput.addEventListener('change', function (e) {
            if (e.target.files && e.target.files.length > 0) {
                scanFromFile(e.target.files[0], targetSelectId, scanType, qrFaEnabled, faTargetId);
            }
        });
    }

    // Kamera starten wenn verfügbar
    if (cameraAvailable) {
        startCameraScanner(targetSelectId, scanType, qrFaEnabled, faTargetId);
    }
}

function switchScannerTab(tab) {
    var cameraPane = document.getElementById('scannerCameraPane');
    var filePane = document.getElementById('scannerFilePane');
    var tabs = document.querySelectorAll('.scanner-tab');

    tabs.forEach(function (t) { t.classList.remove('active'); });

    if (tab === 'camera') {
        if (cameraPane) cameraPane.style.display = '';
        if (filePane) filePane.style.display = 'none';
        if (tabs[0]) tabs[0].classList.add('active');
    } else {
        if (cameraPane) cameraPane.style.display = 'none';
        if (filePane) filePane.style.display = '';
        if (tabs[1]) tabs[1].classList.add('active');
        stopScanner();
    }
}

function getSupportedFormats() {
    var formats = [];
    var F = Html5QrcodeSupportedFormats;
    if (F) {
        formats = [F.QR_CODE, F.CODE_128, F.CODE_39, F.EAN_13, F.EAN_8, F.CODE_93];
    }
    return formats;
}

function startCameraScanner(targetSelectId, scanType, qrFaEnabled, faTargetId) {
    var formats = getSupportedFormats();
    var html5QrCode = formats.length > 0
        ? new Html5Qrcode("scannerReader", { formatsToSupport: formats, verbose: false })
        : new Html5Qrcode("scannerReader");
    _activeScanner = html5QrCode;

    var config = {
        fps: 10,
        qrbox: { width: 300, height: scanType === 'article' ? 300 : 150 },
        rememberLastUsedCamera: true
    };

    html5QrCode.start(
        { facingMode: "environment" },
        config,
        function (decodedText) {
            // Gestoppter Scanner (Fenster per Hardware-Scan/Abbrechen geschlossen): ein noch laufender Decode darf nicht nachtraeglich auswerten.
            if (_scannerClosing || _activeScanner !== html5QrCode) return;
            _scannerClosing = true;

            // Zuerst Wert verarbeiten, dann Modal schließen
            processScannedValue(decodedText, targetSelectId, scanType, qrFaEnabled, faTargetId);
            closeScannerModal();
        },
        function (errorMessage) {
            // Ignoriere kontinuierliche Scan-Fehler
        }
    ).catch(function (err) {
        _activeScanner = null;
        // Kamera fehlgeschlagen - auf File-Upload wechseln
        var resultEl = document.getElementById('scannerResult');
        var resultText = document.getElementById('scannerResultText');
        if (resultEl && resultText) {
            resultEl.style.display = 'block';
            resultText.textContent = 'Kamera nicht verfügbar. Bitte Bild hochladen.';
        }
        var cameraPane = document.getElementById('scannerCameraPane');
        var filePane = document.getElementById('scannerFilePane');
        var tabsEl = document.getElementById('scannerTabs');
        if (cameraPane) cameraPane.style.display = 'none';
        if (filePane) filePane.style.display = '';
        if (tabsEl) tabsEl.style.display = 'none';
    });
}

function stopScanner() {
    var scanner = _activeScanner;
    _activeScanner = null;
    if (scanner) {
        try {
            scanner.stop().catch(function () {});
        } catch (e) {
            // Ignorieren
        }
    }
}

function scanFromFile(file, targetSelectId, scanType, qrFaEnabled, faTargetId) {
    var resultEl = document.getElementById('scannerResult');
    var resultText = document.getElementById('scannerResultText');

    if (resultEl && resultText) {
        resultEl.style.display = 'block';
        resultText.textContent = 'Bild wird verarbeitet...';
    }

    var formats = getSupportedFormats();
    var html5QrCode = formats.length > 0
        ? new Html5Qrcode("scannerFileReader_temp", { formatsToSupport: formats, verbose: false })
        : new Html5Qrcode("scannerFileReader_temp", false);
    html5QrCode.scanFile(file, true)
        .then(function (decodedText) {
            processScannedValue(decodedText, targetSelectId, scanType, qrFaEnabled, faTargetId);
            closeScannerModal();
        })
        .catch(function (err) {
            if (resultEl && resultText) {
                resultEl.style.display = 'block';
                resultText.textContent = 'Kein Code im Bild erkannt. Bitte erneut versuchen.';
            }
            var fileInput = document.getElementById('scannerFileInput');
            if (fileInput) fileInput.value = '';
        });
}

function closeScannerModal() {
    // Scanner stoppen (fire-and-forget)
    stopScanner();

    // Modal sofort entfernen - killt auch den Video-Stream
    var modal = document.getElementById('scannerModal');
    if (modal) {
        modal.remove();
    }

    _scannerClosing = false;
}

// v1.69.0 QrTrennzeichen (body data-qr-trennzeichen, nur bei ","): Trennzeichen der Artikel-QR-Felder, sonst ";".
function qrTrennzeichen() {
    return document.body.getAttribute('data-qr-trennzeichen') === ',' ? ',' : ';';
}

// nichtGefunden (optional, v1.67.2 Scannen ohne Feld): Rueckruf, wenn Artikel/Lagerplatz nicht gefunden wurde.
function processScannedValue(value, targetSelectId, scanType, qrFaEnabled, faTargetId, nichtGefunden) {
    var searchValue = value;
    var faNumber = null;

    if (scanType === 'article' || scanType === 'productionOrder') {
        // QR-Code-Format: Artikelnummer;Feld2;FA-Nummer[,Suffix];... (Trennzeichen laut QrTrennzeichen, ; oder ,)
        var parts = value.split(qrTrennzeichen());

        if (scanType === 'productionOrder') {
            // FA-Nummer extrahieren (3. Position), Komma-Suffix abschneiden
            if (parts.length >= 3 && parts[2].trim()) {
                searchValue = parts[2].trim().split(',')[0];
            } else {
                // Fallback: gesamten gescannten Wert verwenden (evtl. manuell eingetippt)
                searchValue = value.trim().split(',')[0];
            }
        } else {
            // Artikel: Erster Teil vor dem Trennzeichen ist die Artikelnummer
            searchValue = parts[0].trim();

            // FA-Nummer aus QR extrahieren (Index 2) wenn Setting aktiv und >= 3 Teile
            if (qrFaEnabled && parts.length >= 3 && parts[2].trim()) {
                faNumber = parts[2].trim().split(',')[0];
            }
        }
    }

    var select = document.getElementById(targetSelectId);
    if (!select) return;

    // Input-Elemente (text, hidden etc.): Wert direkt setzen und Change-Event auslösen
    if (select.tagName === 'INPUT') {
        select.value = searchValue;
        select.dispatchEvent(new Event('change'));
        if (select.type !== 'hidden') {
            showScanFeedback(select, true, searchValue);
        }
        clearAndFillFaNumber(faNumber, faTargetId);
        return;
    }

    // Select2 AJAX-Artikel: API-Lookup statt Option-Iteration
    if (scanType === 'article' && $(select).hasClass('select2-article')) {
        var feedbackTarget = $(select).closest('.input-group').length
            ? $(select).closest('.input-group')[0]
            : select;
        $.ajax({
            url: '/api/articles/by-number/' + encodeURIComponent(searchValue),
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                var option = new Option(data.text, data.id, true, true);
                $(select).append(option).trigger('change');
                showScanFeedback(feedbackTarget, true, searchValue);
                clearAndFillFaNumber(faNumber, faTargetId);
            },
            error: function () {
                showScanFeedback(feedbackTarget, false, searchValue);
                if (nichtGefunden) nichtGefunden();
            }
        });
        return;
    }

    // Native <select>: Option-Iteration (Lagerplätze etc.)
    var found = false;
    for (var i = 0; i < select.options.length; i++) {
        var optionText = select.options[i].text.trim();
        if (optionText === searchValue || optionText.startsWith(searchValue)) {
            select.selectedIndex = i;
            select.dispatchEvent(new Event('change'));
            found = true;
            showScanFeedback(select, true, searchValue);
            break;
        }
    }

    if (!found) {
        showScanFeedback(select, false, searchValue);
        if (nichtGefunden) nichtGefunden();
    }
}

// meldung (optional, v1.68.0): eigener Fehlertext statt "Nicht gefunden: <Wert>", 10 s sichtbar (laengerer Text bei "mehrdeutig").
function showScanFeedback(element, success, value, meldung) {
    var parent = element.parentNode;
    if (!parent) return;
    var existing = parent.querySelector('.scan-feedback');
    if (existing) existing.remove();

    var feedback = document.createElement('div');
    feedback.className = 'scan-feedback mt-1';

    if (success) {
        feedback.innerHTML = '<small class="text-success"><strong>&#10003;</strong> Gescannt: ' + escapeHtml(value) + '</small>';
    } else {
        feedback.innerHTML = '<small class="text-danger"><strong>&#10007;</strong> ' + (meldung ? escapeHtml(meldung) : 'Nicht gefunden: ' + escapeHtml(value)) + '</small>';
    }

    parent.appendChild(feedback);

    setTimeout(function () {
        feedback.remove();
    }, meldung ? 10000 : 5000);
}

function clearAndFillFaNumber(faNumber, faTargetId) {
    if (!faTargetId) return;
    var faInput = document.getElementById(faTargetId);
    if (!faInput) return;
    // Immer zuerst leeren, dann nur befüllen wenn FA vorhanden
    faInput.value = faNumber || '';
    faInput.dispatchEvent(new Event('change'));
}

function escapeHtml(text) {
    var div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// --- Text-Input Scanner ---
// Lightweight scanner for plain text inputs (no Select2).
// valueExtractor: 'article' | 'fa' | 'raw'
// onScanned: optional callback(extractedValue) after value is set
function initTextInputScanner(buttonId, targetInputId, valueExtractor, onScanned) {
    var btn = document.getElementById(buttonId);
    var target = document.getElementById(targetInputId);
    if (!btn || !target) return;

    btn.addEventListener('click', function () {
        // Store callback info in a global so openScannerModal's pipeline can use it
        _textInputScanCallback = {
            targetInputId: targetInputId,
            valueExtractor: valueExtractor,
            onScanned: onScanned || null
        };
        openScannerModal(targetInputId, valueExtractor === 'fa' ? 'productionOrder' : 'article', false, null);
    });
    _hwScanTargets.push({ buttonId: buttonId, targetId: targetInputId, scanType: valueExtractor === 'fa' ? 'productionOrder' : 'article',
        qrFaEnabled: false, faTargetId: null, textInput: { targetInputId: targetInputId, valueExtractor: valueExtractor, onScanned: onScanned || null } });
}

var _textInputScanCallback = null;

// Hook into processScannedValue: after filling an INPUT, call the stored callback
var _origProcessScannedValue = processScannedValue;
function _processScannedValueSync(value, targetSelectId, scanType, qrFaEnabled, faTargetId, nichtGefunden) {
    _origProcessScannedValue(value, targetSelectId, scanType, qrFaEnabled, faTargetId, nichtGefunden);

    // If there's a pending text-input callback and the target matches, fire it
    if (_textInputScanCallback && _textInputScanCallback.targetInputId === targetSelectId) {
        var target = document.getElementById(targetSelectId);
        if (target && target.tagName === 'INPUT') {
            // Also dispatch input event (original only dispatches change)
            target.dispatchEvent(new Event('input', { bubbles: true }));
            if (typeof _textInputScanCallback.onScanned === 'function') {
                _textInputScanCallback.onScanned(target.value);
            }
        }
        _textInputScanCallback = null;
    }
}

// v1.68.0 QR erstes Feld = Matchcode (AppSetting QrErstesFeld, body data-qr-erstes-feld): Feld 1 eines Artikel-Scans wird ueber
// /api/articles/by-matchcode in die Artikelnummer uebersetzt, danach die unveraenderte Auswertung. Steht im Wrapper, weil der
// onScanned-Callback (Filterseiten, Sammel-Wareneingang) sonst vor der asynchronen Antwort feuert. Modus Artikelnummer: synchron wie bisher.
// Kein Treffer / mehrdeutig / Fehler: Meldung, nichts befuellen, keine FA, kein Callback (kein Submit).
processScannedValue = function (value, targetSelectId, scanType, qrFaEnabled, faTargetId, nichtGefunden) {
    var matchcode = scanType === 'article' && document.body.getAttribute('data-qr-erstes-feld') === 'Matchcode'
        ? String(value).split(qrTrennzeichen())[0].trim() : '';
    if (!matchcode) {
        _processScannedValueSync(value, targetSelectId, scanType, qrFaEnabled, faTargetId, nichtGefunden);
        return;
    }
    // Callback jetzt merken: bis zur Antwort darf ihn kein anderer Scan verbrauchen
    var cb = _textInputScanCallback && _textInputScanCallback.targetInputId === targetSelectId ? _textInputScanCallback : null;
    if (cb) _textInputScanCallback = null;
    var fehler = function (text) {
        _qrMatchcodeMeldung(targetSelectId, matchcode, text);
        if (nichtGefunden) nichtGefunden();
    };
    $.ajax({
        url: '/api/articles/by-matchcode?matchcode=' + encodeURIComponent(matchcode),
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            var treffer = (data && data.articles) || [];
            if (treffer.length === 1 && !data.more) {
                var i = String(value).indexOf(qrTrennzeichen());
                _textInputScanCallback = cb || _textInputScanCallback;
                _processScannedValueSync(treffer[0].articleNumber + (i >= 0 ? String(value).slice(i) : ''), targetSelectId, scanType, qrFaEnabled, faTargetId, nichtGefunden);
            } else if (treffer.length === 0) {
                fehler('Matchcode „' + matchcode + '“ nicht gefunden');
            } else {
                fehler('Matchcode „' + matchcode + '“ ist mehrdeutig (' + (data.more ? 'mehr als 5' : treffer.length) + ' Artikel: '
                    + treffer.map(function (a) { return a.articleNumber; }).join(', ') + (data.more ? ', …' : '') + ') – bitte den Artikel von Hand wählen');
            }
        },
        error: function () {
            fehler('Matchcode „' + matchcode + '“ konnte nicht geprüft werden');
        }
    });
};

// v1.78.0 EAN von Einkaufsartikeln (Spec 2026-11-07-ean-einkaufsartikel-scan): ein Artikel-Scan ohne QR-Trennzeichen aus 8, 12, 13 oder 14
// Ziffern wird zuerst ueber /api/articles/by-ean aufgeloest (Server: exakte Artikelnummer vor EAN). 1 Treffer -> Artikelnummer wie ein
// Artikelnummer-Scan; 0 -> unveraendert der bisherige Weg (inkl. Matchcode-Modus); 2-5 -> Auswahlfenster; mehr / Fehler -> Meldung, nichts
// befuellen. Steht als aeusserer Wrapper, damit Kamera, Hardware-Scanner und "Scannen ohne Feld" ihn ohne View-Aenderung nutzen.
var _artikelScanBisherig = processScannedValue;
processScannedValue = function (value, targetSelectId, scanType, qrFaEnabled, faTargetId, nichtGefunden) {
    var ean = scanType === 'article' ? String(value).trim() : '';
    if (!ean || ean.indexOf(qrTrennzeichen()) >= 0 || !/^(\d{8}|\d{12,14})$/.test(ean)) {
        _artikelScanBisherig(value, targetSelectId, scanType, qrFaEnabled, faTargetId, nichtGefunden);
        return;
    }
    var cb = _textInputScanCallback && _textInputScanCallback.targetInputId === targetSelectId ? _textInputScanCallback : null;
    if (cb) _textInputScanCallback = null;
    var uebernehmen = function (artikelnummer) {
        _textInputScanCallback = cb || _textInputScanCallback;
        _processScannedValueSync(artikelnummer, targetSelectId, scanType, qrFaEnabled, faTargetId, nichtGefunden);
    };
    var fehler = function (text) {
        _qrMatchcodeMeldung(targetSelectId, ean, text);
        if (nichtGefunden) nichtGefunden();
    };
    $.ajax({
        url: '/api/articles/by-ean?ean=' + encodeURIComponent(ean),
        type: 'GET',
        dataType: 'json',
        success: function (data) {
            var treffer = (data && data.articles) || [];
            if (treffer.length === 1 && !data.more) {
                uebernehmen(treffer[0].articleNumber);
            } else if (treffer.length === 0) {
                _textInputScanCallback = cb || _textInputScanCallback;
                _artikelScanBisherig(value, targetSelectId, scanType, qrFaEnabled, faTargetId, nichtGefunden);
            } else if (data.more) {
                fehler('EAN ' + ean + ' gehört zu mehr als 5 Artikeln – bitte den Artikel von Hand wählen');
            } else {
                _eanAuswahl(ean, treffer, uebernehmen, function () { if (nichtGefunden) nichtGefunden(); });
            }
        },
        error: function () {
            fehler('EAN ' + ean + ' konnte nicht geprüft werden');
        }
    });
};

// Auswahlfenster bei mehreren Artikeln je EAN (F5): Bootstrap-Modal, einmal je Aufruf erzeugt; grosse Ziele (>= 44 px) fuer Handgeraete,
// Text statt nur Farbe, Fokus auf dem ersten Eintrag, Abbrechen/Escape = nichts befuellen.
function _eanAuswahl(ean, treffer, uebernehmen, abbrechen) {
    var alt = document.getElementById('eanAuswahlModal');
    if (alt) alt.remove();
    var modal = document.createElement('div');
    modal.id = 'eanAuswahlModal';
    modal.className = 'modal fade';
    modal.tabIndex = -1;
    modal.setAttribute('aria-labelledby', 'eanAuswahlTitel');
    modal.innerHTML =
        '<div class="modal-dialog modal-dialog-centered"><div class="modal-content">' +
        '<div class="modal-header"><h5 class="modal-title" id="eanAuswahlTitel">Mehrere Artikel zur EAN ' + escapeHtml(ean) + '</h5>' +
        '<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Abbrechen"></button></div>' +
        '<div class="modal-body"><p class="mb-2">Bitte den gescannten Artikel wählen:</p><div class="d-grid gap-2" id="eanAuswahlListe"></div></div>' +
        '<div class="modal-footer"><button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal" style="min-height:44px">Abbrechen</button></div>' +
        '</div></div>';
    var liste = modal.querySelector('#eanAuswahlListe');
    var gewaehlt = false;
    treffer.forEach(function (a) {
        var b = document.createElement('button');
        b.type = 'button';
        b.className = 'btn btn-outline-primary text-start';
        b.style.minHeight = '44px';
        b.textContent = a.text || a.articleNumber;
        b.addEventListener('click', function () { gewaehlt = true; bootstrap.Modal.getInstance(modal).hide(); uebernehmen(a.articleNumber); });
        liste.appendChild(b);
    });
    document.body.appendChild(modal);
    modal.addEventListener('shown.bs.modal', function () { var erster = liste.querySelector('button'); if (erster) erster.focus(); });
    modal.addEventListener('hidden.bs.modal', function () { modal.remove(); if (!gewaehlt) abbrechen(); });
    new bootstrap.Modal(modal).show();
}

// Meldungsort wie showScanFeedback: Select2 an der .input-group, verstecktes Ziel (Stueckliste, Sammel-Wareneingang) am Scan-Knopf.
function _qrMatchcodeMeldung(targetSelectId, matchcode, text) {
    var el = document.getElementById(targetSelectId);
    if (!el) return;
    if (el.type === 'hidden') {
        var ziel = _hwScanTargets.filter(function (z) { return z.targetId === targetSelectId; })[0];
        el = (ziel && document.getElementById(ziel.buttonId)) || el;
    } else if (el.closest('.input-group')) {
        el = el.closest('.input-group');
    }
    showScanFeedback(el, false, matchcode, text);
}

// --- Hardware-Scanner (v1.66.0, hardware-scanner.js) ---
// Regel (a): Fokus im Ziel, seinem Scan-Knopf, derselben .input-group oder im offenen Select2-Dropdown -> dieses Ziel.
// Regel (d): kein Ziel im Fokus und kein freies Textfeld fokussiert -> genau ein Ziel der Seite, sonst ambiguous (Hinweis).
// Ausgewertet wird wie beim Kamera-Scan ueber die GLOBALE (gewrappte) processScannedValue.
function hwScanFokusIn(ziel, aktiv) {
    var el = document.getElementById(ziel.targetId);
    if (!el || !aktiv) return false;
    if (aktiv === el || aktiv.id === ziel.buttonId) return true;
    var gruppe = el.closest('.input-group');
    if (gruppe && gruppe.contains(aktiv)) return true;
    var select2 = el.nextElementSibling;  // Select2-Container steht direkt hinter dem <select>, das Suchfeld am body-Ende
    return !!(select2 && select2.classList.contains('select2-container--open') && aktiv.closest('.select2-dropdown'));
}

// v1.66.1/v1.66.2: Ist das Kamera-Fenster eines Ziels offen, dessen Knopf data-hw-scan-kamera-ziel traegt (jede Seite mit mehr als einem
// Scan-Feld, Freigabe-Nachtrag 3), gilt das Fenster als angetipptes Ziel - vor allen anderen Regeln. Am Touch-Geraet oeffnet Antippen des
// Knopfs die Kamera, der Fokus liegt dann auf keinem Ziel.
function hwScanKameraZiel(ziele) {
    var modal = document.getElementById('scannerModal');
    if (!modal) return null;
    return ziele.filter(function (z) {
        var btn = document.getElementById(z.buttonId);
        // v1.67.1: Fokus im Handscanner-Feld des Fensters macht es auch auf Seiten mit einem Scan-Feld zum Ziel (sonst Regel (b) ins Fensterfeld).
        return z.targetId === modal.dataset.zielId && btn
            && (btn.hasAttribute('data-hw-scan-kamera-ziel') || document.activeElement === document.getElementById('scannerHwInput'));
    })[0] || null;
}

document.addEventListener('wms:hardware-scan', function (e) {
    var d = e.detail, aktiv = document.activeElement;
    var vorhanden = _hwScanTargets.filter(function (z) { return document.getElementById(z.targetId); });
    var kamera = hwScanKameraZiel(vorhanden);
    if (kamera) {
        // Wie ein Kamera-Treffer (startCameraScanner): sperren, auswerten, Fenster schliessen.
        _scannerClosing = true;
        if (kamera.textInput) _textInputScanCallback = kamera.textInput;
        processScannedValue(d.code, kamera.targetId, kamera.scanType, kamera.qrFaEnabled, kamera.faTargetId);
        closeScannerModal();
        d.handled = true; d.ziel = kamera.targetId + ' (Kamera-Fenster)';
        return;
    }
    if (d.ohneFeld) { hwScanOhneFeld(d, vorhanden); return; }
    var ziel = vorhanden.filter(function (z) { return hwScanFokusIn(z, aktiv); })[0];
    if (!ziel) {
        if (d.freiesFeld) return;  // Regel (b) im Layout-Skript
        if (vorhanden.length !== 1) { d.ambiguous = vorhanden.length > 1; return; }
        ziel = vorhanden[0];
    }
    var el = document.getElementById(ziel.targetId);
    if (window.jQuery && $(el).hasClass('select2-hidden-accessible')) $(el).select2('close');
    if (ziel.textInput) _textInputScanCallback = ziel.textInput;  // wie der Klick-Handler: onScanned feuert nach dem Fuellen
    processScannedValue(d.code, ziel.targetId, ziel.scanType, ziel.qrFaEnabled, ziel.faTargetId);
    d.handled = true; d.ziel = ziel.targetId;
});

// v1.67.2 Scannen ohne Feld (Freigabe-Nachtrag 5, Pilot Einbuchung): Scan ueber das unsichtbare Feld, Zuordnung nach Inhalt
// (HwScanCore.zuordnung): mit QR-Trennzeichen Artikel-QR, sonst Lagerplatz mit genau diesem Code, sonst Artikelnummer. Nie Formular-Submit.
function hwScanOhneFeld(d, vorhanden) {
    var feld = document.querySelector('[data-hw-scan-ohne-feld]');
    var nichtZuordenbar = function () { d.hinweis('Code ' + d.code + ' nicht zuordenbar'); };
    var artikel = vorhanden.filter(function (z) { return z.targetId === feld.getAttribute('data-artikel-ziel'); })[0];
    var lager = document.getElementById(feld.getAttribute('data-lagerplatz-ziel'));
    var option = function (c) {
        return lager ? Array.prototype.filter.call(lager.options, function (o) { return o.value && o.text.trim() === c; })[0] : null;
    };
    var z = HwScanCore.zuordnung(d.code, function (c) { return !!option(c); }, qrTrennzeichen());
    d.handled = true;
    if (z.feld === 'lagerplatz') {
        var o = option(d.code.trim());
        lager.value = o.value;
        lager.dispatchEvent(new Event('change'));
        showScanFeedback(lager, true, o.text.trim());
        d.ziel = lager.id + ' (' + z.art + ')';
    } else if (z.feld === 'artikel' && artikel) {
        // Artikel-QR wie Kamera/Hardware-Scan, FA nur bei QrMitFaNummer (data-qr-fa-enabled am Knopf)
        processScannedValue(d.code, artikel.targetId, 'article', artikel.qrFaEnabled, artikel.faTargetId, nichtZuordenbar);
        d.ziel = artikel.targetId + ' (' + z.art + ')';
    } else {
        d.ziel = 'nicht zuordenbar';
        nichtZuordenbar();
    }
}

// Initialisierung wenn DOM bereit
document.addEventListener('DOMContentLoaded', function () {
    initScanner('btnScanArticle', 'ArticleId', 'article');
    initScanner('btnScanStorageLocation', 'StorageLocationId', 'storageLocation');
    initScanner('btnScanSourceLocation', 'SourceStorageLocationId', 'storageLocation');
    initScanner('btnScanArticleInfo', 'articleNumber', 'article');
});
