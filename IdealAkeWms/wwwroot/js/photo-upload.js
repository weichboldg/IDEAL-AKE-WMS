// Photo upload with client-side compression for Kommissionierung
function initPhotoUpload(productionOrderId, token) {
    'use strict';

    var photoInput = document.getElementById('photoInput');
    var photoGrid = document.getElementById('photoGrid');
    if (!photoInput || !photoGrid) return;

    // Load existing photos
    loadPhotos();

    photoInput.addEventListener('change', function () {
        if (this.files && this.files[0]) {
            compressAndUpload(this.files[0]);
            this.value = '';
        }
    });

    function compressImage(file, maxWidth, quality) {
        return new Promise(function (resolve, reject) {
            var reader = new FileReader();
            reader.onload = function (e) {
                var img = new Image();
                img.onload = function () {
                    var canvas = document.createElement('canvas');
                    var width = img.width;
                    var height = img.height;

                    if (width > maxWidth) {
                        height = Math.round(height * maxWidth / width);
                        width = maxWidth;
                    }

                    canvas.width = width;
                    canvas.height = height;

                    var ctx = canvas.getContext('2d');
                    ctx.drawImage(img, 0, 0, width, height);

                    canvas.toBlob(function (blob) {
                        resolve(blob);
                    }, 'image/jpeg', quality);
                };
                img.onerror = reject;
                img.src = e.target.result;
            };
            reader.onerror = reject;
            reader.readAsDataURL(file);
        });
    }

    function compressAndUpload(file) {
        compressImage(file, 1920, 0.75).then(function (blob) {
            var formData = new FormData();
            formData.append('photo', blob, 'photo.jpg');
            formData.append('productionOrderId', productionOrderId);

            $.ajax({
                url: '/ProductionOrders/UploadPhoto',
                type: 'POST',
                data: formData,
                processData: false,
                contentType: false,
                headers: { 'RequestVerificationToken': token },
                success: function (data) {
                    if (data.success) {
                        addPhotoThumbnail(data.fileName, data.url);
                    }
                },
                error: function () {
                    alert('Fehler beim Hochladen des Fotos.');
                }
            });
        }).catch(function () {
            alert('Fehler bei der Bildkomprimierung.');
        });
    }

    function loadPhotos() {
        $.get('/ProductionOrders/GetPhotos?productionOrderId=' + productionOrderId, function (photos) {
            photoGrid.innerHTML = '';
            photos.forEach(function (photo) {
                addPhotoThumbnail(photo.fileName, photo.url);
            });
        });
    }

    function addPhotoThumbnail(fileName, url) {
        var div = document.createElement('div');
        div.className = 'photo-thumbnail';

        var img = document.createElement('img');
        img.src = url;
        img.alt = fileName;
        img.title = fileName;
        img.addEventListener('click', function () {
            window.open(url, '_blank');
        });

        var btn = document.createElement('button');
        btn.className = 'photo-delete';
        btn.innerHTML = '&times;';
        btn.title = 'Foto löschen';
        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            if (confirm('Foto wirklich löschen?')) {
                deletePhoto(fileName, div);
            }
        });

        div.appendChild(img);
        div.appendChild(btn);
        photoGrid.appendChild(div);
    }

    function deletePhoto(fileName, element) {
        $.ajax({
            url: '/ProductionOrders/DeletePhoto',
            type: 'POST',
            data: { fileName: fileName },
            headers: { 'RequestVerificationToken': token },
            success: function () {
                element.remove();
            },
            error: function () {
                alert('Fehler beim Löschen des Fotos.');
            }
        });
    }
}
