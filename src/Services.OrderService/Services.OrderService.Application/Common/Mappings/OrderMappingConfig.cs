using Mapster;
using Services.OrderService.Application.Models;
using Services.OrderService.Application.Orders.DTOs;
using Services.OrderService.Domain.Entities;

namespace Services.OrderService.Application.Common.Mappings
{
    public class OrderMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Order, OrderDto>()
            .Map(dest => dest.Items, src => src.Items);

            config.NewConfig<OrderItem, OrderItemDto>();
        }
    }
}
