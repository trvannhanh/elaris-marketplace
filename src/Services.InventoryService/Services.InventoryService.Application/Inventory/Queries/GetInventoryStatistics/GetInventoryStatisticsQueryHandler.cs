using MediatR;
using Services.InventoryService.Application.Interfaces;

namespace Services.InventoryService.Application.Inventory.Queries.GetInventoryStatistics
{
    public class GetInventoryStatisticsQueryHandler
        : IRequestHandler<GetInventoryStatisticsQuery, InventoryStatisticsDto>
    {
        private readonly IUnitOfWork _uow;

        public GetInventoryStatisticsQueryHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<InventoryStatisticsDto> Handle(
            GetInventoryStatisticsQuery request,
            CancellationToken cancellationToken)
        {
            var items = await _uow.Inventory.GetAllAsync(cancellationToken);

            return new InventoryStatisticsDto
            {
                TotalProducts = items.Count,
                InStockProducts = items.Count(x => x.Status == Domain.Entities.InventoryStatus.InStock),
                LowStockProducts = items.Count(x => x.Status == Domain.Entities.InventoryStatus.LowStock),
                OutOfStockProducts = items.Count(x => x.Status == Domain.Entities.InventoryStatus.OutOfStock),
                TotalQuantity = items.Sum(x => x.Quantity),
                TotalReservedQuantity = items.Sum(x => x.ReservedQuantity)
            };
        }
    }
}
