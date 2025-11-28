

namespace Services.InventoryService.Domain.Entities
{
    public class InventoryHistory
    {
        public Guid Id { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string ChangeType { get; set; } = string.Empty; // Increase, Decrease, OrderDeduction, etc.
        public int QuantityBefore { get; set; }
        public int QuantityAfter { get; set; }
        public int QuantityChanged { get; set; }
        public string? ChangedBy { get; set; }
        public Guid? OrderId { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
