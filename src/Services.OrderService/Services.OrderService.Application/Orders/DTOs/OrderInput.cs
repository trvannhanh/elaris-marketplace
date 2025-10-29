namespace Services.OrderService.Application.Orders.DTOs
{
    public record OrderInput(string ProductId, int Quantity, decimal TotalPrice, DateTime CreatedAt);
}
