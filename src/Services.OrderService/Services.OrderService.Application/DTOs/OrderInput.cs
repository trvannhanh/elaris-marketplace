namespace Services.OrderService.Services.OrderService.Application.DTOs
{
    public record OrderInput(string ProductId, int Quantity, decimal TotalPrice, DateTime CreatedAt);
}
