

namespace Services.InventoryService.Domain.Entities
{
    public class InventoryItem
    {
        public Guid Id { get; set; }
        public string ProductId { get; set; } = default!;
        public int AvailableStock { get; set; }
        // Tổng số lượng đang được giữ chỗ (đặt hàng nhưng chưa xác nhận)
        public int ReservedQuantity { get; set; }
        public DateTime LastUpdated { get; set; }

        // EffectiveStock computed property — không lưu DB, chỉ giúp tính nhanh
        public int EffectiveStock => AvailableStock - ReservedQuantity;
    }
}
