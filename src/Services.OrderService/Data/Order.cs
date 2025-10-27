namespace Services.OrderService.Data
{
    public class Order
    {
        public Guid Id { get; set; }
        public string ProductId { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
