namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// View-Model fuer das shared Partial <c>_VaultLink</c> (v1.67.0). Schalter und Vorlage liest das Partial selbst
/// (<see cref="IdealAkeWms.Helpers.LinkVorlage"/>). ShowLabel = Knopf „Im VAULT öffnen“ der Artikelinfo, sonst reines Symbol.
/// </summary>
public record VaultLinkViewModel(string? ArticleNumber, bool ShowLabel = false);
