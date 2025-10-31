

namespace Services.InventoryService.Application.DTOs
{
    public class InventoryResponse
    {
        public string ProductId { get; set; } = default!;
        public int Quantity { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
