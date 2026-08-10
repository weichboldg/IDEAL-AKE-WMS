using BarcodeStandard;
using SkiaSharp;

namespace IdealAkeWms.Services;

/// <summary>
/// Erzeugt Code-128-Barcodes als PNG-Base64-String (fuer <c>&lt;img src="data:image/png;base64,..."&gt;</c>
/// im Bildschirmdruck). Gemeinsamer Baustein-Bestandteil: Teil 3 (Barcode = HauptFA) und die
/// spaeteren IDEAL-Drucke (Teil 4/5) verwenden dieselbe Kodierung, statt die Logik aus
/// <c>StorageLocationsController</c> zu duplizieren.
/// </summary>
public interface IBarcodeService
{
    /// <summary>Kodiert <paramref name="value"/> als Code-128-Barcode und liefert die PNG-Bytes Base64.</summary>
    string EncodeCode128Base64(string value);
}

/// <inheritdoc />
public class BarcodeService : IBarcodeService
{
    public string EncodeCode128Base64(string value)
    {
        value ??= string.Empty;
        var barcode = new Barcode();
        // Breite dynamisch: laengere Werte brauchen mehr Pixel fuer dickere Balken (analog Etiketten).
        var width = System.Math.Max(600, value.Length * 45);
        var image = barcode.Encode(BarcodeStandard.Type.Code128, value, SKColors.Black, SKColors.White, width, 160);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return System.Convert.ToBase64String(data.ToArray());
    }
}
