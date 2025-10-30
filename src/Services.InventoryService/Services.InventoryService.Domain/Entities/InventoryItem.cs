

namespace Services.InventoryService.Domain.Entities
{
    public class InventoryItem
    {
        public Guid Id { get; set; }
        public string ProductId { get; set; } = default!;
        public int AvailableStock { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
