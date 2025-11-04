

namespace Services.InventoryService.Application.DTOs
{
    public class InventoryResponse
    {
        public string ProductId { get; set; } = default!;
        public int AvailableStock { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
