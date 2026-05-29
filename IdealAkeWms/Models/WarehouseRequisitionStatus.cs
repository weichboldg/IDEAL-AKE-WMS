namespace IdealAkeWms.Models;

public enum WarehouseRequisitionStatus : byte
{
    Draft              = 1,
    Submitted          = 2,
    Closed             = 3,
    Cancelled          = 4,
    PartiallyDelivered = 5
}
