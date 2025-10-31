

using MapsterMapper;
using MediatR;
using Services.InventoryService.Application.DTOs;
using Services.InventoryService.Application.Interfaces;

namespace Services.InventoryService.Application.Inventory.Queries
{
    public class GetInventoryByProductIdQueryHandler
        : IRequestHandler<GetInventoryByProductIdQuery, InventoryResponse?>
    {
        private readonly IInventoryRepository _repo;
        private readonly IMapper _mapper;

        public GetInventoryByProductIdQueryHandler(IInventoryRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<InventoryResponse?> Handle(GetInventoryByProductIdQuery request, CancellationToken ct)
        {
            var inventory = await _repo.GetByProductIdAsync(request.ProductId, ct);
            return inventory is null ? null : _mapper.Map<InventoryResponse>(inventory);
        }
    }
}
