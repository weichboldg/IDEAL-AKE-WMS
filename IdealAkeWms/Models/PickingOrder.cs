namespace IdealAkeWms.Models;

/// <summary>v1.88.0: Typ des Kommissionierauftrags. 2 = Lagerbestellung, 3 = Glasbestellung reserviert (Folge-Epic) – bewusst KEINE Mitglieder.</summary>
public enum PickingOrderType : byte
{
    FA = 1,
}

/// <summary>v1.88.0: Status des Kommissionierauftrags (Werte mit Luecken, Muster Beschichtungsauftrag).</summary>
public enum PickingOrderStatus : byte
{
    Offen = 10,
    InArbeit = 20,
    TeilweiseAbgeschlossen = 30,
    Abgeschlossen = 40,
    Storniert = 90,
}

/// <summary>v1.88.0: Status einer Auftragszeile.</summary>
public enum PickingOrderLineStatus : byte
{
    Offen = 0,
    Gepickt = 10,
    Umgebucht = 20,
    /// <summary>Auftrag „vollstaendig“ abgeschlossen, Zeile nicht gebraucht (z. B. Baugruppe statt Einzelteile).</summary>
    Entfallen = 30,
    /// <summary>Rest in den Folgeauftrag uebergeben (Abschluss „teilweise“).</summary>
    Uebertragen = 40,
}

/// <summary>
/// v1.88.0 (Spec 2026-11-08-kommissionierung-auftraege-kopf-zeilen): Kommissionierauftrag – Beleg neben der Stueckliste.
/// Nummer <c>KO-JJJJ-NNNNN</c> (ein Kreis fuer alle Typen), je FA und Bereich ein Auftrag, Folgeauftrag ueber
/// <see cref="UrsprungsauftragId"/>. Einziger Schreibweg: <c>PickingOrderWorkflow</c>.
/// </summary>
public class PickingOrder : AuditableEntity
{
    public string Nummer { get; set; } = string.Empty;
    public int Jahr { get; set; }
    public int LaufendeNummer { get; set; }
    public PickingOrderType Typ { get; set; } = PickingOrderType.FA;

    /// <summary>Herkunfts-FA (in diesem Epic immer gesetzt; nullable fuer spaetere Sammelkommissionierung).</summary>
    public int? ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    public int PickingAreaId { get; set; }
    public PickingArea PickingArea { get; set; } = null!;

    public PickingOrderStatus Status { get; set; } = PickingOrderStatus.Offen;
    public int? PickingPriority { get; set; }
    public int? AssignedPickerId { get; set; }
    public User? AssignedPicker { get; set; }
    public string? AssignedPickerName { get; set; }
    public string? Bemerkung { get; set; }

    public int? UrsprungsauftragId { get; set; }
    public PickingOrder? Ursprungsauftrag { get; set; }
    public List<PickingOrder> Folgeauftraege { get; set; } = new();

    public DateTime? AbgeschlossenAm { get; set; }
    public string? AbgeschlossenVon { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public List<PickingOrderLine> Zeilen { get; set; } = new();
}

/// <summary>v1.88.0: Zeile eines Kommissionierauftrags; Herkunftsschluessel wie <see cref="PickingItem"/> (FA, Ressourcenummer, Position).</summary>
public class PickingOrderLine : AuditableEntity
{
    public int PickingOrderId { get; set; }
    public PickingOrder PickingOrder { get; set; } = null!;

    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    /// <summary>Ressourcenummer der Stuecklistenposition (fallstricke „Artikelnummer vs Ressourcenummer“).</summary>
    public string ArticleNumber { get; set; } = string.Empty;
    public string? BomPosition { get; set; }
    public bool IsBaugruppe { get; set; }

    public decimal SollMenge { get; set; }
    public decimal? IstMenge { get; set; }

    public int? SourceStorageLocationId { get; set; }
    public StorageLocation? SourceStorageLocation { get; set; }

    public PickingOrderLineStatus Status { get; set; } = PickingOrderLineStatus.Offen;
    public DateTime? PickedAt { get; set; }
    public string? PickedBy { get; set; }
    public string? PickedByWindows { get; set; }
    public DateTime? TransferredAt { get; set; }

    public int? StockMovementId { get; set; }
    public StockMovement? StockMovement { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
