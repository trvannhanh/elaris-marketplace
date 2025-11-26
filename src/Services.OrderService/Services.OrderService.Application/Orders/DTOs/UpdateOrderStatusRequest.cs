using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Application.Orders.DTOs
{
    public class UpdateOrderStatusRequest
    {
        public OrderStatus Status { get; set; }
        public string? Note { get; set; }
    }
}
