

namespace Services.InventoryService.Domain.Entities
{
    public class InventoryItem
    {
        public Guid Id { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string SellerId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public int LowStockThreshold { get; set; } = 10;
        public InventoryStatus Status { get; set; }
        public DateTime? LastRestockDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public enum InventoryStatus
    {
        InStock,
        LowStock,
        OutOfStock,
        Discontinued
    }

    
}
