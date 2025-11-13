using MediatR;
using Services.InventoryService.Application.Interfaces;
using Services.InventoryService.Domain.Entities;

namespace Services.InventoryService.Application.Inventory.Commands.CreateOrUpdateInventoryItem
{
    public class CreateOrUpdateInventoryItemCommandHandler : IRequestHandler<CreateOrUpdateInventoryItemCommand>
    {
        private readonly IUnitOfWork _uow;

        public CreateOrUpdateInventoryItemCommandHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task Handle(CreateOrUpdateInventoryItemCommand request, CancellationToken ct)
        {
            var existing = await _uow.Inventory.GetByProductIdAsync(request.ProductId, ct);
            if (existing != null)
            {
                existing.AvailableStock = request.Stock;
                existing.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                await _uow.Inventory.AddAsync(new InventoryItem
                {
                    ProductId = request.ProductId,
                    AvailableStock = request.Stock,
                    LastUpdated = DateTime.UtcNow
                }, ct);
            }

            await _uow.SaveChangesAsync(ct);
        }
    }
}
