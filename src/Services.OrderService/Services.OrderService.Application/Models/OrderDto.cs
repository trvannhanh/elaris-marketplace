

namespace Services.OrderService.Application.Models
{
    public class OrderDto
    {
        public Guid Id { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
