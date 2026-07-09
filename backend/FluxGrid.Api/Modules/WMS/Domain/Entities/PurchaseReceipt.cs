using FluxGrid.Api.Modules.WMS.Domain.Enums;

namespace FluxGrid.Api.Modules.WMS.Domain.Entities;

public class PurchaseReceipt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ReceiptNo { get; set; } = string.Empty;
    public string PoReference { get; set; } = string.Empty;
    public ReceiptStatus Status { get; set; }
    public string ReceivedBy { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<PurchaseReceiptLine> Lines { get; set; } = [];
}
