using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Data.Repositories;

public interface IBomRepository
{
    /// <summary>Stueckliste des Auftrags. Flach (AKE): nur <see cref="BomKey.ArticleNumber"/> zaehlt,
    /// <paramref name="scope"/> wird ignoriert. Hierarchisch (IDEAL): Sub-FA + Scope (Spec Design A/D).</summary>
    Task<BomQueryResult> GetBomItemsAsync(BomKey key, BomScope scope = BomScope.DirectChildren);
}
