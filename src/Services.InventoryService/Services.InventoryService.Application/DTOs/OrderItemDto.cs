

namespace Services.InventoryService.Application.DTOs
{
    public class OrderItemDto
    {
        public string ProductId { get; set; } = null!;
        public int Quantity { get; set; }
    }
}
