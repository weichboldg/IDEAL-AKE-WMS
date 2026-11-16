namespace IdealAkeWms.Services;

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string hashedPassword, string password);

    /// <summary>Zufaelliges Erstkennwort (12 Zeichen, ohne verwechselbare Zeichen 0 O 1 l I) fuer /Users (ADR 0027).
    /// Wird genau einmal angezeigt, nie gespeichert oder geloggt.</summary>
    string GenerateInitialPassword();
}
