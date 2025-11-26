

using Services.OrderService.Application.Orders.DTOs;

namespace Services.OrderService.Application.Interfaces
{
    public interface ICatalogServiceClient
    {
        Task<ProductDto?> GetProductAsync(string productId, CancellationToken ct = default);
    }
}
