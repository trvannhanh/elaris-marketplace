

namespace BuildingBlocks.Contracts.DTOs
{
    public class OrderDto
    {
        public string Id { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
