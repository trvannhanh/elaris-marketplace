

namespace Services.OrderService.Application.Orders.DTOs
{
    public class CreateOrderRequest
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }
        public string CardToken { get; set; } = string.Empty;
    }
}
