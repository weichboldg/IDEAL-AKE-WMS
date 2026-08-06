using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public class WarehouseRequisitionEditViewModel
{
    public int Id { get; set; }
    public string WorkplaceName { get; set; } = string.Empty;
    public WarehouseRequisitionStatus Status { get; set; }
    public WarehouseRequisitionType Type { get; set; } = WarehouseRequisitionType.Lager;
    public DateTime CreatedAt { get; set; }
    public string? Comment { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public List<WarehouseRequisitionEditItemViewModel> Items { get; set; } = new();
}
