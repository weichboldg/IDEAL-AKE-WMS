using IdealAkeWms.Data.Repositories;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// View-Model fuer das shared Partial <c>_EnaioDmsBadges</c>.
/// Rendert die enaio-DMS-Links einer FA als Icon-Badges (mit eigenem Icon je DocumentType).
/// </summary>
public class EnaioDmsBadgesViewModel
{
    public EnaioDmsBadgesViewModel(IEnumerable<EnaioDmsDocumentLink>? links, bool showLabel = false)
    {
        Links = links?.ToList() ?? new List<EnaioDmsDocumentLink>();
        ShowLabel = showLabel;
    }

    /// <summary>Die enaio-Links der FA (bereits nach Typ-Prioritaet sortiert vom Repository).</summary>
    public List<EnaioDmsDocumentLink> Links { get; }

    /// <summary>
    /// true = Badge-Darstellung mit Text-Label (FA-Vervollstaendigung),
    /// false = reine Icon-Links fuer Listen-Views.
    /// </summary>
    public bool ShowLabel { get; }
}
