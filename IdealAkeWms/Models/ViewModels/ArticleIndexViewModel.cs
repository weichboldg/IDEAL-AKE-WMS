namespace IdealAkeWms.Models.ViewModels;

public class ArticleIndexViewModel
{
    public List<Article> Items { get; set; } = new();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public string? Search { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
