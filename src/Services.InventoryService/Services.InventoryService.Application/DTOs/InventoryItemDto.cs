

using Services.InventoryService.Domain.Entities;

namespace Services.InventoryService.Application.DTOs
{
    public class InventoryItemDto
    {
        public string ProductId { get; set; } = default!;
        public string SellerId { get; set; } = default!;
        public int Quantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public int LowStockThreshold { get; set; } = 10;
        public string Status { get; set; }
        public DateTime? LastRestockDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
