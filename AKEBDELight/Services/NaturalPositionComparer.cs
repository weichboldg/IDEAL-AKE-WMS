namespace AKEBDELight.Services;

/// <summary>
/// Sortiert Positionsnummern natürlich-numerisch:
/// "1", "2", "10", "10.1", "10.2", "11", "15", "15.1", "15.1.1"
/// statt alphabetisch: "1", "10", "11", "15", "2"
/// </summary>
public class NaturalPositionComparer : IComparer<string?>
{
    public int Compare(string? x, string? y)
    {
        if (x == y) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        var partsX = x.Split('.');
        var partsY = y.Split('.');

        for (int i = 0; i < Math.Min(partsX.Length, partsY.Length); i++)
        {
            if (int.TryParse(partsX[i], out var numX) && int.TryParse(partsY[i], out var numY))
            {
                var cmp = numX.CompareTo(numY);
                if (cmp != 0) return cmp;
            }
            else
            {
                var cmp = string.Compare(partsX[i], partsY[i], StringComparison.OrdinalIgnoreCase);
                if (cmp != 0) return cmp;
            }
        }

        return partsX.Length.CompareTo(partsY.Length);
    }
}
