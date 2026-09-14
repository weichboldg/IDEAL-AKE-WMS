namespace IdealAkeWms.Services;

/// <summary>Fehler der PDF-Erzeugung mit fertiger Nutzermeldung (Freigabe-Antwort 4: zwei
/// unterscheidbare Faelle, weil sie zu verschiedenen Handlungen fuehren).</summary>
public sealed class PdfRenderException : Exception
{
    public const string NotConfiguredMessage = "PDF-Erzeugung ist auf diesem Server nicht eingerichtet.";
    public const string FailedMessage = "Das PDF konnte nicht erzeugt werden. Bitte den Bildschirmdruck verwenden.";

    public string UserMessage { get; }

    private PdfRenderException(string userMessage, string detail, Exception? inner)
        : base(detail, inner) => UserMessage = userMessage;

    public static PdfRenderException NotConfigured()
        => new(NotConfiguredMessage, "msedge.exe nicht gefunden (PdfRenderStatus.IsAvailable == false)", null);

    public static PdfRenderException Failed(string detail, Exception? inner = null)
        => new(FailedMessage, detail, inner);
}
