

namespace Services.OrderService.Application.Orders.DTOs
{
    public class OrderResponse
    {
        public string Id { get; set; }
        public string ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public DateTime? CancelledAt { get; set; }
        public string? CancellReason { get; set; }
    }
}
