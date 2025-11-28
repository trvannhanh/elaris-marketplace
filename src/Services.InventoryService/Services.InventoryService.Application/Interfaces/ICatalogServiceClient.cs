

using Services.InventoryService.Application.DTOs;

namespace Services.InventoryService.Application.Interfaces
{
    public interface ICatalogServiceClient
    {
        Task<ProductDto?> GetProductAsync(string productId, CancellationToken ct = default);
    }
}
