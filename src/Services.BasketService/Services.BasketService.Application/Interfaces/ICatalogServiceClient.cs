using Services.BasketService.Application.Models;

namespace Services.BasketService.Application.Interfaces
{
    public interface ICatalogServiceClient
    {
        Task<ProductDto?> GetProductAsync(string productId, CancellationToken ct = default);
    }
}
