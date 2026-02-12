// AKE BDE Light - Barcode/QR Scanner
// Verwendet html5-qrcode Library

function initScanner(buttonId, targetSelectId, scanType) {
    const button = document.getElementById(buttonId);
    if (!button) return;

    button.addEventListener('click', function () {
        openScannerModal(targetSelectId, scanType);
    });
}

function openScannerModal(targetSelectId, scanType) {
    // Modal erstellen
    let modal = document.getElementById('scannerModal');
    if (modal) modal.remove();

    modal = document.createElement('div');
    modal.id = 'scannerModal';
    modal.className = 'scanner-modal';
    modal.innerHTML = `
        <div class="scanner-modal-content">
            <div class="scanner-modal-header">
                <h5>${scanType === 'article' ? 'Artikel QR-Code scannen' : 'Lagerplatz Barcode scannen'}</h5>
                <button type="button" class="btn-close btn-close-white" onclick="closeScannerModal()"></button>
            </div>
            <div id="scannerReader" style="width: 100%;"></div>
            <div id="scannerResult" class="scanner-result" style="display:none;">
                <span id="scannerResultText"></span>
            </div>
            <div class="scanner-modal-footer">
                <button type="button" class="btn btn-outline-light" onclick="closeScannerModal()">Abbrechen</button>
            </div>
        </div>
    `;
    document.body.appendChild(modal);

    // Scanner starten
    const html5QrCode = new Html5Qrcode("scannerReader");
    const config = {
        fps: 10,
        qrbox: { width: 250, height: 250 },
        aspectRatio: 1.0
    };

    // Für Lagerplatz nur Barcodes, für Artikel QR-Code bevorzugt
    html5QrCode.start(
        { facingMode: "environment" },
        config,
        (decodedText) => {
            // Erfolg
            html5QrCode.stop().then(() => {
                processScannedValue(decodedText, targetSelectId, scanType);
                closeScannerModal();
            });
        },
        (errorMessage) => {
            // Ignoriere kontinuierliche Scan-Fehler
        }
    ).catch(err => {
        document.getElementById('scannerResult').style.display = 'block';
        document.getElementById('scannerResultText').textContent =
            'Kamera konnte nicht gestartet werden: ' + err;
    });

    // Modal-Referenz für Cleanup speichern
    modal._scanner = html5QrCode;
}

function closeScannerModal() {
    const modal = document.getElementById('scannerModal');
    if (modal) {
        if (modal._scanner) {
            modal._scanner.stop().catch(() => { });
        }
        modal.remove();
    }
}

function processScannedValue(value, targetSelectId, scanType) {
    let searchValue = value;

    if (scanType === 'article') {
        // QR-Code: Erster Teil vor ; ist die Artikelnummer
        // Format: 87040362;1472230-04;45
        const parts = value.split(';');
        searchValue = parts[0].trim();
    }

    // Wert im Select suchen
    const select = document.getElementById(targetSelectId);
    if (!select) return;

    let found = false;
    for (let i = 0; i < select.options.length; i++) {
        const optionText = select.options[i].text.trim();
        if (optionText === searchValue || optionText.startsWith(searchValue)) {
            select.selectedIndex = i;
            select.dispatchEvent(new Event('change'));
            found = true;

            // Visuelles Feedback
            showScanFeedback(select, true, searchValue);
            break;
        }
    }

    if (!found) {
        showScanFeedback(select, false, searchValue);
    }
}

function showScanFeedback(element, success, value) {
    // Bestehendes Feedback entfernen
    const existing = element.parentNode.querySelector('.scan-feedback');
    if (existing) existing.remove();

    const feedback = document.createElement('div');
    feedback.className = 'scan-feedback mt-1';

    if (success) {
        feedback.innerHTML = `<small class="text-success"><strong>&#10003;</strong> Gescannt: ${escapeHtml(value)}</small>`;
        element.classList.add('is-valid');
        element.classList.remove('is-invalid');
    } else {
        feedback.innerHTML = `<small class="text-danger"><strong>&#10007;</strong> Nicht gefunden: ${escapeHtml(value)}</small>`;
        element.classList.add('is-invalid');
        element.classList.remove('is-valid');
    }

    element.parentNode.appendChild(feedback);

    // Nach 5 Sekunden Feedback entfernen
    setTimeout(() => {
        feedback.remove();
        element.classList.remove('is-valid', 'is-invalid');
    }, 5000);
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Initialisierung wenn DOM bereit
document.addEventListener('DOMContentLoaded', function () {
    // Artikel-Scanner
    initScanner('btnScanArticle', 'ArticleId', 'article');
    // Lagerplatz-Scanner
    initScanner('btnScanStorageLocation', 'StorageLocationId', 'storageLocation');
});
