
using Microsoft.EntityFrameworkCore;
using Services.InventoryService.Application.Interfaces;
using Services.InventoryService.Domain.Entities;
using Services.InventoryService.Infrastructure.Persistence;

namespace Services.InventoryService.Infrastructure.Repositories
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly InventoryDbContext _context;

        public InventoryRepository(
            InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<InventoryItem?> GetByProductIdAsync(string productId, CancellationToken ct)
        {
            return await _context.InventoryItems
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.ProductId == productId, ct);
        }

        public async Task<List<InventoryItem>> GetAllAsync(CancellationToken ct)
        {
            return await _context.InventoryItems
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public IQueryable<InventoryItem> GetQueryable()
        {
            return _context.InventoryItems.AsNoTracking();
        }

        public IQueryable<InventoryHistory> GetHistoryQueryable()
        {
            return _context.InventoryHistories.AsNoTracking();
        }

        public async Task AddAsync(InventoryItem item, CancellationToken ct)
        {
            await _context.InventoryItems.AddAsync(item, ct);
        }

        public Task UpdateAsync(InventoryItem item, CancellationToken ct)
        {

            _context.InventoryItems.Update(item);

            return Task.CompletedTask;
        }

        public async Task AddHistoryAsync(InventoryHistory history, CancellationToken ct)
        {
            await _context.InventoryHistories.AddAsync(history, ct);
        }

        public async Task AddReservationAsync(StockReservation reservation, CancellationToken ct)
        {
            await _context.StockReservations.AddAsync(reservation, ct);
        }

        public async Task UpdateReservationStatusAsync(
            Guid orderId,
            ReservationStatus status,
            CancellationToken ct)
        {
            var reservation = await _context.StockReservations
                .FirstOrDefaultAsync(r => r.OrderId == orderId, ct);

            if (reservation != null)
            {
                reservation.Status = status;

                if (status == ReservationStatus.Released || status == ReservationStatus.Confirmed)
                {
                    reservation.ReleasedAt = DateTime.UtcNow;
                }

            }
        }

        public async Task<StockReservation?> GetReservationByOrderIdAsync(
            Guid orderId,
            CancellationToken ct)
        {
            return await _context.StockReservations
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.OrderId == orderId, ct);
        }

        public async Task<List<StockReservation>> GetActiveReservationsAsync(
            string productId,
            CancellationToken ct)
        {
            return await _context.StockReservations
                .AsNoTracking()
                .Where(r => r.ProductId == productId && r.Status == ReservationStatus.Active)
                .ToListAsync(ct);
        }

        public async Task<List<StockReservation>> GetExpiredReservationsAsync(
            DateTime expirationTime,
            CancellationToken ct)
        {
            return await _context.StockReservations
                .Where(r => r.Status == ReservationStatus.Active && r.ReservedAt < expirationTime)
                .ToListAsync(ct);
        }
    }
}
