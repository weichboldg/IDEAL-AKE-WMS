using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class OseonOperationConfig
{
    public int Id { get; set; }

    /// <summary>
    /// Kurzname des Arbeitsgangs aus OSEON (z.B. "B", "ST", "EG", "BG", "ZB").
    /// Wird mit OseonWorkOperation.Name gematched.
    /// </summary>
    [Required, MaxLength(100)]
    public string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// Optionaler Anzeigename (z.B. "Belegen", "Stanzen").
    /// </summary>
    [MaxLength(200)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Offset in Arbeitstagen relativ zum Stanztermin (OSEON Endtermin).
    /// Negativ = vor Stanztermin, Positiv = nach Stanztermin.
    /// </summary>
    public int DueDateOffsetDays { get; set; }

    /// <summary>
    /// Wenn false, wird dieser AG bei der Statusberechnung ignoriert.
    /// Ein Auftrag gilt als "fertig" wenn alle OSEON-relevanten AGs fertig sind.
    /// </summary>
    public bool IsOseonRelevant { get; set; } = true;
}
