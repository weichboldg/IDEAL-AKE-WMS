namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// State block fuer das gemeinsame _Pagination-Partial.
/// PageSizeRaw ist der unmodifizierte URL-Wert (0 = "Alle"), PageSize der
/// effektive Wert fuer Skip/Take (mit Cap).
/// </summary>
public class PaginationState
{
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = Services.PageSize.Default;
    public int PageSizeRaw { get; set; } = Services.PageSize.Default;
    public int TotalCount { get; set; }

    public int TotalPages =>
        PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;

    public int FirstItemNumber =>
        TotalCount == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;

    public int LastItemNumber =>
        Math.Min(CurrentPage * PageSize, TotalCount);

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// True wenn der User "Alle" gewaehlt hat (<see cref="PageSizeRaw"/>==0) und die
    /// Gesamtanzahl ueber dem Hardcap (<see cref="Services.PageSize.AllCap"/>) liegt
    /// — d.h. Zeilen wurden abgeschnitten. Wird im Pagination-Partial als Banner gezeigt.
    /// </summary>
    public bool IsCappedAtAll =>
        PageSizeRaw == Services.PageSize.AllSentinel && TotalCount > Services.PageSize.AllCap;
}
