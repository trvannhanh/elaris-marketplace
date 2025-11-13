
using MediatR;
using Services.InventoryService.Application.Interfaces;

namespace Services.InventoryService.Application.Inventory.Commands.UpdateStock
{
    public class UpdateStockCommandHandler : IRequestHandler<UpdateStockCommand>
    {
        private readonly IUnitOfWork _uow;

        public UpdateStockCommandHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task Handle(UpdateStockCommand request, CancellationToken ct)
        {
            var item = await _uow.Inventory.GetByProductIdAsync(request.ProductId, ct)
                   ?? throw new Exception("Product not found in inventory");

            item.AvailableStock -= request.Quantity;
            item.LastUpdated = DateTime.UtcNow;

            await _uow.SaveChangesAsync(ct);
        }
    }
}
   