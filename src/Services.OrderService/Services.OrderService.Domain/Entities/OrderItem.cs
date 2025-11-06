

namespace Services.OrderService.Domain.Entities
{
    public class OrderItem
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string ProductId { get; set; } = default!;
        public string Name { get; set; } = default!;
        public decimal Price { get; set; }
        public int Quantity { get; set; }

        public Order Order { get; set; } = default!;
    }
}
