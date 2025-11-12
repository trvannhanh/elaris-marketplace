using MediatR;
using Services.InventoryService.Application.Interfaces;
using Services.InventoryService.Domain.Entities;

namespace Services.InventoryService.Application.Inventory.Commands.CreateOrUpdateInventoryItem
{
    public class CreateOrUpdateInventoryItemCommandHandler : IRequestHandler<CreateOrUpdateInventoryItemCommand>
    {
        private readonly IInventoryRepository _repo;

        public CreateOrUpdateInventoryItemCommandHandler(IInventoryRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(CreateOrUpdateInventoryItemCommand request, CancellationToken ct)
        {
            var existing = await _repo.GetByProductIdAsync(request.ProductId, ct);
            if (existing != null)
            {
                existing.AvailableStock = request.Stock;
                existing.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                await _repo.AddAsync(new InventoryItem
                {
                    ProductId = request.ProductId,
                    AvailableStock = request.Stock,
                    LastUpdated = DateTime.UtcNow
                }, ct);
            }

            await _repo.SaveChangesAsync(ct);
        }
    }
}
