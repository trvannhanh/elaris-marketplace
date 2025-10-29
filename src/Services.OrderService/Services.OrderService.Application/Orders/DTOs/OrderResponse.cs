

namespace Services.OrderService.Application.Orders.DTOs
{
    public record OrderResponse(
        Guid Id,
        string ProductId,
        int Quantity,
        decimal TotalPrice,
        DateTime CreatedAt
    );
}
