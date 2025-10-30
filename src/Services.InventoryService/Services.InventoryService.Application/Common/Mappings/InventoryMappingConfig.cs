using Mapster;
using Services.InventoryService.Application.DTOs;
using Services.InventoryService.Domain.Entities;

namespace Services.InventoryService.Application.Common.Mappings
{
    public class InventoryMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<InventoryItem, InventoryResponse>();
        }
    }
}
