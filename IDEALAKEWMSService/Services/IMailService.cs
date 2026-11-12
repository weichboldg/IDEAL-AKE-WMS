namespace IDEALAKEWMSService.Services;

public interface IMailService
{
    Task SendAsync(string subject, string htmlBody, IEnumerable<string> recipients, string? textBody = null, CancellationToken ct = default);

    /// <summary>v1.99.10 (Lieferverzug-Mail): Versand mit Antwortadresse, eigenem Absender und eingebetteten Bildern (CID). Liefert
    /// <c>false</c>, wenn still uebersprungen wurde (keine Empfaenger, kein SMTP-Host) – dann darf der Aufrufer nichts als gesendet
    /// protokollieren. SMTP-Fehler werfen wie bei <see cref="SendAsync"/>.</summary>
    Task<bool> SendMitOptionenAsync(MailNachricht nachricht, CancellationToken ct = default);
}

/// <param name="Absender">Absenderadresse; null/leer = <c>MailSettings:FromAddress</c>.</param>
/// <param name="AntwortAn">Reply-To; null/leer = keins.</param>
/// <param name="Bilder">Eingebettete Bilder, im HTML ueber <c>cid:&lt;ContentId&gt;</c> referenziert.</param>
public sealed record MailNachricht(string Betreff, string Html, string? Text, IReadOnlyList<string> Empfaenger,
    string? AntwortAn = null, string? Absender = null, IReadOnlyList<MailBild>? Bilder = null);

public sealed record MailBild(string ContentId, byte[] Daten, string MimeTyp);
