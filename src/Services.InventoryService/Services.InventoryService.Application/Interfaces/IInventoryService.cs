

using Services.InventoryService.Domain.Entities;

namespace Services.InventoryService.Application.Interfaces
{
    public interface IInventoryService
    {
        Task<InventoryItem> ConfirmReservationAsync(Guid orderId, string productId, int quantity, CancellationToken ct);
        Task<InventoryItem> ReserveStockAsync(Guid orderId, string productId, int quantity, CancellationToken ct);
        Task<InventoryItem> ReleaseStockAsync(Guid orderId, string productId, int quantity, CancellationToken ct);
    }
}
