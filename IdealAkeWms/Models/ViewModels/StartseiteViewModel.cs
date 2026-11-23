namespace IdealAkeWms.Models.ViewModels;

/// <summary>v1.99.51: Startseite. <paramref name="Favoriten"/> null = keine Benutzerkennung (keine Reiter, kein Stern);
/// <paramref name="Reiter"/> = erzwungener Reiter („favoriten“/„alle“) oder null (Standard nach Rueckfrage 1).</summary>
public sealed record StartseiteViewModel(IReadOnlyList<string>? Favoriten, string? Reiter);
