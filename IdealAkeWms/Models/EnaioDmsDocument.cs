using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class EnaioDmsDocument : AuditableEntity
{
    /// <summary>enaio object1.id</summary>
    public long EnaioDmsObjectId { get; set; }

    /// <summary>enaio-Typ aus vw_IDEAL-AKE_Fertigungsauftraege.Typ: "Werkstattauftrag",
    /// "Werkstattauftrag+Zeichnung" (Zeichnung direkt am WA, eigenes Icon, zuerst angezeigt)
    /// oder "Zeichnung".</summary>
    [Required]
    [MaxLength(100)]
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>FA-Nummer: feld44 (FA) oder left(feld43,7) (Zeichnung)</summary>
    [MaxLength(100)]
    public string? OrderNumber { get; set; }

    /// <summary>Datum aus enaio (object1.angelegt)</summary>
    public DateTime CreatedInEnaio { get; set; }

    /// <summary>Zeitpunkt des letzten Sync-Laufs</summary>
    public DateTime LastSyncedAt { get; set; }
}
