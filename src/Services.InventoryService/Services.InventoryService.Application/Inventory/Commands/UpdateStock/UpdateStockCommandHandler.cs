
using MediatR;
using Services.InventoryService.Application.Interfaces;

namespace Services.InventoryService.Application.Inventory.Commands.UpdateStock
{
    public class UpdateStockCommandHandler : IRequestHandler<UpdateStockCommand>
    {
        private readonly IInventoryRepository _repo;

        public UpdateStockCommandHandler(IInventoryRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(UpdateStockCommand request, CancellationToken ct)
        {
            var item = await _repo.GetByProductIdAsync(request.ProductId, ct)
                       ?? throw new Exception("Product not found in inventory");

            item.AvailableStock -= request.Quantity;
            item.LastUpdated = DateTime.UtcNow;

            await _repo.SaveChangesAsync(ct);
        }
    }
}
