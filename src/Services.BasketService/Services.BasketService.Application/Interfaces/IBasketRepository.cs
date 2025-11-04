using Services.BasketService.Application.Models;


namespace Services.BasketService.Application.Interfaces
{
    public interface IBasketRepository
    {
        Task<List<BasketItem>> GetBasketAsync(string userId, CancellationToken ct = default);
        Task AddOrUpdateItemAsync(string userId, BasketItem item, CancellationToken ct = default);
        Task<bool> RemoveItemAsync(string userId, string productId, CancellationToken ct = default);
        Task<bool> ClearBasketAsync(string userId, CancellationToken ct = default);
    }
}

