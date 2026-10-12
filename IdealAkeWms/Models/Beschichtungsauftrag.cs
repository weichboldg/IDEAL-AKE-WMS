using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

/// <summary>
/// Gespeicherter Beschichtungsauftrag (IDEAL, Spec 2026-10-06-beschichtungsauftrag-ausbau-teil-3): genau EIN HauptFA,
/// Nummer <c>BA-JJJJ-NNNNN</c> je Datenbank, Status-Automat, Kopf aus Sage vorbelegt (Freitext, im Entwurf aenderbar).
/// Reklamation = Folgeauftrag mit <see cref="UrsprungsauftragId"/> (Selbstverweis). Positionen tragen Herkunftsschluessel
/// + Snapshot statt FK auf den Sage-Cache (der Sync ersetzt die Cache-Zeilen bei jedem Lauf).
/// Aenderungen ausschliesslich ueber <c>BeschichtungsauftragWorkflow</c>.
/// </summary>
public class Beschichtungsauftrag : AuditableEntity
{
    [Required, StringLength(20)]
    public string Nummer { get; set; } = string.Empty;

    public int Jahr { get; set; }
    public int LaufendeNummer { get; set; }

    public int HauptFA { get; set; }

    public BeschichtungsauftragStatus Status { get; set; } = BeschichtungsauftragStatus.Entwurf;

    [StringLength(200)]
    public string? Dienstleister { get; set; }

    [StringLength(200)]
    public string? Ral { get; set; }

    public DateTime? StartBeschichtung { get; set; }
    public DateTime? BeschichtenRetour { get; set; }

    [StringLength(1000)]
    public string? Bemerkung { get; set; }

    /// <summary>Reklamations-Folgeauftrag: verweist auf den Ursprung (OnDelete Restrict). Null = Erstauftrag.</summary>
    public int? UrsprungsauftragId { get; set; }
    public Beschichtungsauftrag? Ursprungsauftrag { get; set; }

    /// <summary>Folgeauftraege (Reklamationen) dieses Auftrags — der „Vermerk am Ursprung" (Antwort 2).</summary>
    public ICollection<Beschichtungsauftrag> Folgeauftraege { get; set; } = new List<Beschichtungsauftrag>();

    public DateTime? UebergebenAt { get; set; }
    public int? UebergebenByUserId { get; set; }
    public DateTime? ErledigtAt { get; set; }
    public int? ErledigtByUserId { get; set; }
    public DateTime? StorniertAt { get; set; }
    public int? StorniertByUserId { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<BeschichtungsauftragPosition> Positionen { get; set; } = new List<BeschichtungsauftragPosition>();
}
