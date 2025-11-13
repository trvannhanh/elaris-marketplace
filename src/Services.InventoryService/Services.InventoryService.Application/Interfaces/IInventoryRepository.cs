
using BuildingBlocks.Contracts.DTOs;
using Services.InventoryService.Domain.Entities;

namespace Services.InventoryService.Application.Interfaces
{
    public interface IInventoryRepository
    {
        Task<InventoryItem?> GetByProductIdAsync(string productId, CancellationToken ct = default);
        Task<bool> HasStockAsync(string productId, int quantity, CancellationToken ct = default);
        Task<OrderDto?> FetchOrderDetails(Guid orderId, CancellationToken ct = default);

        Task DecreaseStockAsync(string productId, int quantity, CancellationToken ct = default);
        Task AddAsync(InventoryItem inventory, CancellationToken ct = default);

        Task<bool> TryReserveStockAsync(string productId, int quantity, CancellationToken ct);
        Task ReleaseReservationAsync(string productId, int quantity, CancellationToken ct);
        Task ConfirmReservationAsync(string productId, int quantity, CancellationToken ct);
    }
}
