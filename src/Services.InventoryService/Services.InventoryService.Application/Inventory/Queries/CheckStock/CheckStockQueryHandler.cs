using MediatR;
using Services.InventoryService.Application.Interfaces;

namespace Services.InventoryService.Application.Inventory.Queries.CheckStock
{
    public class CheckStockQueryHandler
    : IRequestHandler<CheckStockQuery, CheckStockResponse>
    {
        private readonly IInventoryRepository _repo;

        public CheckStockQueryHandler(IInventoryRepository repo) => _repo = repo;

        public async Task<CheckStockResponse> Handle(CheckStockQuery request, CancellationToken ct)
        {
            var item = await _repo.GetByProductIdAsync(request.ProductId, ct);
            if (item == null)
                return new(false, 0, "Product not found");

            var inStock = item.AvailableStock >= request.Quantity;
            return new(inStock, item.AvailableStock, inStock ? "OK" : "Not enough stock");
        }
    }
}
