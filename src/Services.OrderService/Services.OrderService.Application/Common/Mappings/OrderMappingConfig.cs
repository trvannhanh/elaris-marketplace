
using Mapster;
using Services.OrderService.Application.Orders.DTOs;
using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Application.Common.Mappings
{
    public class OrderMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Order, OrderDto>()
                  .Map(dest => dest.Id, src => src.Id.ToString())
                  .Map(dest => dest.TotalPrice, src => src.TotalPrice) // ← THÊM DÒNG NÀY
                  .Map(dest => dest.Status, src => src.Status.ToString())
                  .Map(dest => dest.Items, src => src.Items.Adapt<List<OrderItemDto>>());

            config.NewConfig<OrderItem, OrderItemDto>();
        }
    }
}
