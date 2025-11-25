

namespace Services.OrderService.Application.Orders.DTOs
{
    public record BuyNowRequest(
    string ProductId,
    int Quantity,
    string CardToken
);
}
