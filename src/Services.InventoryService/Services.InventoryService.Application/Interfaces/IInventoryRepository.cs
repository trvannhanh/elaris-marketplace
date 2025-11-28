
using Services.InventoryService.Domain.Entities;

namespace Services.InventoryService.Application.Interfaces
{
    public interface IInventoryRepository
    {
        Task<InventoryItem?> GetByProductIdAsync(string productId, CancellationToken ct);
        Task<List<InventoryItem>> GetAllAsync(CancellationToken ct);
        IQueryable<InventoryItem> GetQueryable();
        IQueryable<InventoryHistory> GetHistoryQueryable();
        Task AddAsync(InventoryItem item, CancellationToken ct);
        Task UpdateAsync(InventoryItem item, CancellationToken ct);
        Task AddHistoryAsync(InventoryHistory history, CancellationToken ct);
        Task AddReservationAsync(StockReservation reservation, CancellationToken ct);
        Task UpdateReservationStatusAsync(Guid orderId, ReservationStatus status, CancellationToken ct);
        Task<StockReservation?> GetReservationByOrderIdAsync(Guid orderId, CancellationToken ct);
        Task<List<StockReservation>> GetActiveReservationsAsync(string productId, CancellationToken ct);
        Task<List<StockReservation>> GetExpiredReservationsAsync(DateTime expirationTime, CancellationToken ct);
    }
}
