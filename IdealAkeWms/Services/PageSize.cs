namespace IdealAkeWms.Services;

/// <summary>
/// Konstanten und Helper rund um die einheitliche Listen-Pagination.
///
/// Allowed values: 25 (default), 50, 100, 0 (= "Alle", gecappt auf <see cref="AllCap"/>).
/// User-Default in <see cref="Models.User.DefaultPageSize"/>; NULL = System-Default.
/// </summary>
public static class PageSize
{
    public const int Default = 25;
    public const int AllCap = 5000;
    public const int AllSentinel = 0;

    public static readonly int[] AllowedOptions = new[] { 25, 50, 100, AllSentinel };

    /// <summary>
    /// Resolves the effective page size for one request:
    /// <list type="bullet">
    ///   <item>explicit query param &gt; user default &gt; system default</item>
    ///   <item>"Alle" (0) wird auf <see cref="AllCap"/> begrenzt</item>
    ///   <item>Ungueltige Werte fallen auf <see cref="Default"/> zurueck</item>
    /// </list>
    /// </summary>
    public static int Resolve(int? requested, int? userDefault)
    {
        var raw = requested ?? userDefault ?? Default;
        if (!AllowedOptions.Contains(raw)) raw = Default;
        return raw == AllSentinel ? AllCap : raw;
    }

    /// <summary>
    /// Returns the original (unmapped) page-size value for use in URL/Form state &mdash;
    /// "Alle" stays 0 in the URL even though the SQL query uses <see cref="AllCap"/>.
    /// </summary>
    public static int ResolveRaw(int? requested, int? userDefault)
    {
        var raw = requested ?? userDefault ?? Default;
        if (!AllowedOptions.Contains(raw)) raw = Default;
        return raw;
    }
}
