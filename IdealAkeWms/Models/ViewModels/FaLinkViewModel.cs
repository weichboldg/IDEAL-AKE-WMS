namespace IdealAkeWms.Models.ViewModels;

/// <summary>v1.87.0: Modell fuer <c>Views/Shared/_FaLink.cshtml</c> – Text der Buchung, verknuepfter FA (oder null), FA-Leserecht, Master
/// „hierarchisch“.</summary>
public sealed record FaLinkViewModel(string? Text, ProductionOrder? Fa, bool LinkErlaubt, bool Hierarchisch);
