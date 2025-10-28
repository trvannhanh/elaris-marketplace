namespace Services.OrderService.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public string ProductId { get; set; } = default!;
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
